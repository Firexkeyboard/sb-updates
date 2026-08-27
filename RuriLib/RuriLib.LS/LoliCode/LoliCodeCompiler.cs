using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using RuriLib.Functions.Conditions;
using RuriLib.Models;

namespace RuriLib.LS.LoliCode;

/// <summary>
/// Compiles a list of LoliCode segments into a single C# Roslyn script string.
/// </summary>
public static class LoliCodeCompiler
{
    // ThreadStatic: each thread gets its own counter/locals so concurrent compilations
    // (multiple bots running simultaneously) don't corrupt each other's state.
    [System.ThreadStatic]
    private static int _blockCounter;

    [System.ThreadStatic]
    private static HashSet<string> _knownLocals;

    // Output vars from RECURSIVE Parse blocks — populated in Compile() pre-pass so that
    // FixMethodGroupAmbiguity can transform `varName.Select(...)` → `data.GetListVar("varName").Select(...)`.
    [System.ThreadStatic]
    private static HashSet<string> _recursiveParseVars;

    // Regex patterns for SET CAP / SET VAR / SET STATUS appearing inline inside C# blocks
    private static readonly System.Text.RegularExpressions.Regex _rxInlineSetCap = new System.Text.RegularExpressions.Regex(
        @"^(?<indent>[ \t]*)SET[ \t]+CAP[ \t]+(?:@?(?<var>[A-Za-z_][A-Za-z0-9_]*)|""(?<var>[^""]*)"")[ \t]+(?<val>""(?:[^""\\]|\\.)*""|@[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z0-9_]+)?)\s*$",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);
    private static readonly System.Text.RegularExpressions.Regex _rxInlineSetVar = new System.Text.RegularExpressions.Regex(
        @"^(?<indent>[ \t]*)SET[ \t]+VAR[ \t]+(?:@?(?<var>[A-Za-z_][A-Za-z0-9_]*)|""(?<var>[^""]*)"")[ \t]+(?<val>""(?:[^""\\]|\\.)*""|@[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z0-9_]+)?)\s*$",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);
    private static readonly System.Text.RegularExpressions.Regex _rxInlineSetStatus = new System.Text.RegularExpressions.Regex(
        @"^(?<indent>[ \t]*)SET[ \t]+STATUS[ \t]+(?<status>[A-Za-z_][A-Za-z0-9_]*)\s*$",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);

    // Script preamble injected before user code
    internal const string Preamble = @"
// Pull OB2 API helpers (ConstantString, CheckCondition, MatchRegexGroups, AsString, AsBool …)
// into top-level scope so OB2 configs compile without namespace qualification.
using static RuriLib.LS.LoliCode.Ob2Compat;
// Common namespaces used by OB2 inline C# blocks without full qualification
using System.Text;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Pkcs;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
// OB2 logging compatibility: allows both LogColors.X and RuriLib.Logging.LogColors.X
using RuriLib.Logging;
// ─── LoliCode Runtime Helpers ───────────────────────────────────────────────
// __rv(s): replaces <varName> templates using BotData variables (like OB2 ReplaceValues)
// Also normalizes OB2-style <input.USER> → <USER> and <data.SOURCE> → <SOURCE>
Func<string, string> __rv = s => {
    s = System.Text.RegularExpressions.Regex.Replace(
        s, @""<(input|data)\.([A-Za-z0-9_]+)>"",
        m => ""<"" + m.Groups[2].Value.ToUpperInvariant() + "">"");
    return RuriLib.BlockBase.ReplaceValues(s, data._inner);
};
// log / print / LOG: write to the bot log
// LOG is uppercase so it can be used as a keyword-like statement in inline C#.
// Action<object> so it accepts value types (long, double) and reference types (string) without issues.
Action<string> log   = msg => data.Log(msg);
Action<string> print = msg => data.Log(msg);
Action<object> LOG   = obj => data.Log(obj?.ToString() ?? """");
// OB2 shorthand aliases
var input = data;   // OB2 uses input.USER / input.PASS directly
// ─────────────────────────────────────────────────────────────────────────────
";

    // Returns true for `using Namespace;` / `using static Type;` directives.
    // Returns false for `using var x = ...;` and `using (...)` statements.
    private static bool IsUsingDirective(string trimmed) =>
        trimmed.StartsWith("using ", StringComparison.Ordinal) &&
        trimmed.EndsWith(";") &&
        !trimmed.StartsWith("using var ", StringComparison.Ordinal) &&
        !trimmed.StartsWith("using (", StringComparison.Ordinal);

    public static string Compile(List<LoliCodeSegment> segments)
    {
        _blockCounter = 0;
        _knownLocals  = new HashSet<string>(StringComparer.Ordinal);

        // Pre-pass: find RECURSIVE Parse block output vars so FixMethodGroupAmbiguity can
        // transform `varName.Select(...)` in user code to `data.GetListVar("varName").Select(...)`.
        _recursiveParseVars = new HashSet<string>(StringComparer.Ordinal);
        foreach (var seg in segments)
        {
            if (seg.Type == LoliCodeSegmentType.Block &&
                string.Equals(seg.BlockType, "Parse", StringComparison.OrdinalIgnoreCase) &&
                seg.OutputVar != null &&
                seg.Properties.TryGetValue("recursive", out string _rv) &&
                _rv.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
                _recursiveParseVars.Add(seg.OutputVar);
        }

        // Pre-pass: collect `using` directives from Code segments so they can be hoisted
        // before the preamble. CSharpScript.Create().Compile() (strict mode) requires all
        // using directives to appear before any non-using statements.
        var hoistedUsings = new List<string>();
        foreach (var seg in segments)
        {
            if (seg.Type != LoliCodeSegmentType.Code) continue;
            foreach (string ln in seg.Code.Split('\n'))
            {
                string t = ln.Trim();
                if (IsUsingDirective(t) && !hoistedUsings.Contains(t, StringComparer.Ordinal))
                    hoistedUsings.Add(t);
            }
        }

        var sb = new StringBuilder();
        // Emit hoisted user using directives before the preamble so they precede
        // non-using statements (Preamble lambdas etc.) and satisfy the Roslyn strict parser.
        foreach (string u in hoistedUsings)
            sb.AppendLine(u);
        sb.AppendLine(Preamble);

        // SilverBullet-only block config encoded by the serializer as a "// _SB:key=val|..."
        // comment immediately before a BLOCK:HttpRequest. OB2 ignores it as a C# comment.
        Dictionary<string, string> pendingSbConfig = null;

        // Track net brace depth across segment boundaries so data.Flush() is never injected
        // inside an open try/catch/if/while block (Bug #1: flush between } and catch → CS1513).
        int crossSegBraceDepth = 0;

        foreach (var seg in segments)
        {
            if (seg.Type == LoliCodeSegmentType.Code)
            {
                string trimmed = seg.Code.Trim();
                if (trimmed.StartsWith("// _SB:", StringComparison.Ordinal))
                {
                    pendingSbConfig = ParseSbComment(trimmed.Substring("// _SB:".Length));
                    // _SB: markers are never inside a try/catch scope, so always flush here.
                    sb.AppendLine("data.Flush();");
                    continue;
                }
                // OB2 END SCRIPT -> VARS "..." markers are metadata, not valid C#. Skip them.
                if (trimmed.StartsWith("END SCRIPT", StringComparison.OrdinalIgnoreCase))
                    continue;
                // OB2 BEGIN SCRIPT CSharp ... headers are not valid C#. Skip the header line.
                if (trimmed.StartsWith("BEGIN SCRIPT", StringComparison.OrdinalIgnoreCase))
                    continue;
                pendingSbConfig = null;

                string segCode = seg.Code;
                if (hoistedUsings.Count > 0)
                {
                    var filteredLines = seg.Code.Split('\n')
                        .Where(ln => !IsUsingDirective(ln.Trim()));
                    segCode = string.Join("\n", filteredLines);
                }

                // Resolve the code to emit through the appropriate converter or transforms.
                string codeToEmit;
                string lsSet       = ConvertLsSetToCSharp(segCode);
                string lsTurnstile = lsSet == null ? ConvertTurnstileToCSharp(segCode) : null;
                string lsAltcha    = (lsSet == null && lsTurnstile == null) ? ConvertAltchaToCSharp(segCode) : null;
                string lsFrc       = (lsSet == null && lsTurnstile == null && lsAltcha == null) ? ConvertFriendlyCaptchaToCSharp(segCode) : null;
                string lsRc3       = (lsSet == null && lsTurnstile == null && lsAltcha == null && lsFrc == null) ? ConvertRecaptchaV3ToCSharp(segCode) : null;
                string lsAkm       = (lsSet == null && lsTurnstile == null && lsAltcha == null && lsFrc == null && lsRc3 == null) ? ConvertAkmCookiesToCSharp(segCode) : null;
                string lsDd        = (lsSet == null && lsTurnstile == null && lsAltcha == null && lsFrc == null && lsRc3 == null && lsAkm == null) ? ConvertDataDomeToCSharp(segCode) : null;
                string lsRc2i      = (lsSet == null && lsTurnstile == null && lsAltcha == null && lsFrc == null && lsRc3 == null && lsAkm == null && lsDd == null) ? ConvertRecaptchaV2InvisibleToCSharp(segCode) : null;
                string lsCfc       = (lsSet == null && lsTurnstile == null && lsAltcha == null && lsFrc == null && lsRc3 == null && lsAkm == null && lsDd == null && lsRc2i == null) ? ConvertCfClearanceToCSharp(segCode) : null;

                if      (lsSet       != null) codeToEmit = lsSet;
                else if (lsTurnstile != null) codeToEmit = lsTurnstile;
                else if (lsAltcha    != null) codeToEmit = lsAltcha;
                else if (lsFrc       != null) codeToEmit = lsFrc;
                else if (lsRc3       != null) codeToEmit = lsRc3;
                else if (lsAkm       != null) codeToEmit = lsAkm;
                else if (lsDd        != null) codeToEmit = lsDd;
                else if (lsRc2i      != null) codeToEmit = lsRc2i;
                else if (lsCfc       != null) codeToEmit = lsCfc;
                else
                {
                    // Expand SET CAP/VAR/STATUS lines that appear inside C# blocks (if/else/while)
                    // before @-ref resolution so they compile as valid C#.
                    segCode = ApplyInlineSetStatements(segCode);
                    // OB2 configs sometimes produce "try {\n{" (extra brace after try) which
                    // causes CS1513. Remove the redundant brace so the catch attaches correctly.
                    segCode = FixBraceAfterTry(segCode);
                    // Collect locals BEFORE resolving @-refs so that variables declared
                    // in THIS segment (e.g. "long DaysLeft = ...") are already in _knownLocals
                    // when ResolveAtVarRefs processes "@DaysLeft" in the same segment.
                    CollectDeclaredLocals(segCode);
                    codeToEmit = FixMethodGroupAmbiguity(ResolveAtVarRefs(segCode));
                }

                sb.AppendLine(codeToEmit);
                // Update cross-segment brace tracking; only flush at top-level scope.
                crossSegBraceDepth += CountNetBracesInScript(codeToEmit);
                if (crossSegBraceDepth == 0)
                    sb.AppendLine("data.Flush();");
            }
            else
            {
                if (pendingSbConfig != null && string.Equals(seg.BlockType, "HttpRequest", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var kv in pendingSbConfig)
                        seg.Properties[kv.Key] = kv.Value;
                }
                pendingSbConfig = null;

                string blockCode = CompileBlock(seg);
                sb.AppendLine(blockCode);

                // Mirror LoliScript TakeStep early-exit: stop running blocks when status
                // is no longer NONE or SUCCESS (e.g. KEYCHECK sets FAIL/BAN/RETRY/ERROR).
                // Guard with crossSegBraceDepth so status check is never injected inside a
                // try/catch block that spans a segment boundary.
                if (!seg.Disabled && crossSegBraceDepth == 0)
                {
                    sb.AppendLine("if (data._inner.Status != BotStatus.NONE && data._inner.Status != BotStatus.SUCCESS && data._inner.Status != BotStatus.CUSTOM) return;");
                    sb.AppendLine("data.Flush();");
                    // Yield 1 ms so the UI thread can process the BeginInvoke dispatches from
                    // FlushPartialLog and show this block's log entries before the next block runs.
                    // This makes LoliCode output appear progressively like LoliScript.
                    sb.AppendLine("Thread.Sleep(1);");
                }
            }
        }

        return sb.ToString();
    }

    // Counts net { minus } across a multi-line code string, respecting string literals.
    // Used by crossSegBraceDepth tracking in Compile().
    private static int CountNetBracesInScript(string code)
    {
        int depth = 0;
        bool inStr = false;
        bool verbatim = false;
        char strQ = '"';
        for (int k = 0; k < code.Length; k++)
        {
            char c = code[k];
            if (inStr)
            {
                if (verbatim)
                {
                    if (c == '"' && k + 1 < code.Length && code[k + 1] == '"') k++;
                    else if (c == '"') inStr = false;
                }
                else
                {
                    if (c == '\\') k++;
                    else if (c == strQ) inStr = false;
                }
            }
            else if (c == '@' && k + 1 < code.Length && code[k + 1] == '"')
            {
                inStr = true; verbatim = true; strQ = '"'; k++;
            }
            else if (c == '"' || c == '\'') { inStr = true; verbatim = false; strQ = c; }
            else if (c == '{') depth++;
            else if (c == '}') depth--;
        }
        return depth;
    }

    // Parses "key=val|key=val" from a "// _SB:" comment. Pipe (|) is safe because
    // it is invalid in Windows paths and does not appear in OB2 string literals.
    private static Dictionary<string, string> ParseSbComment(string config)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string pair in config.Split('|'))
        {
            int eq = pair.IndexOf('=');
            if (eq > 0)
                dict[pair.Substring(0, eq).Trim()] = pair.Substring(eq + 1).Trim();
        }
        return dict;
    }

    // Converts a LoliScript SET command embedded in LoliCode to valid C#.
    // Returns null if the line is not a SET command (caller should treat it as regular C# code).
    private static string ConvertLsSetToCSharp(string code)
    {
        var trimmed = code.Trim();
        if (!trimmed.StartsWith("SET ", StringComparison.OrdinalIgnoreCase)) return null;
        var parts = trimmed.Split(new[] {' '}, 4, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return "// SET (no field)";
        switch (parts[1].ToUpperInvariant())
        {
            case "USEPROXY":
                if (parts.Length >= 3)
                    return parts[2].Equals("TRUE", StringComparison.OrdinalIgnoreCase)
                        ? "data.UseProxy = true;"
                        : "data.UseProxy = false;";
                break;
            case "STATUS":
                if (parts.Length >= 3)
                    return $"data._inner.Status = BotStatus.{parts[2].ToUpperInvariant()};";
                break;
            case "CAP":
                if (parts.Length >= 4)
                {
                    string _cn = parts[2].TrimStart('@').Trim('"');
                    string _cv = parts[3];
                    if (_cv.StartsWith("@") && !_cv.StartsWith("\""))
                        return $"data.Captures[\"{_cn}\"] = data.GetVar(\"{_cv.TrimStart('@')}\");";
                    return $"data.Captures[\"{_cn}\"] = {_cv};";
                }
                return "// SET CAP (invalid syntax)";
            case "VAR":
                if (parts.Length >= 4)
                {
                    string _vn = parts[2].TrimStart('@').Trim('"');
                    string _vv = parts[3];
                    if (_vv.StartsWith("@"))
                        return $"data.Variables.Set(new RuriLib.Models.CVar(\"{_vn}\", data.GetVar(\"{_vv.TrimStart('@')}\")));";
                    return $"data.Variables.Set(new RuriLib.Models.CVar(\"{_vn}\", {_vv}));";
                }
                return "// SET VAR (invalid syntax)";
            case "SOURCE":
                return $"// SET SOURCE skipped in LoliCode mode";
        }
        return $"// SET {(parts.Length > 1 ? parts[1] : "")} (unsupported in LoliCode)";
    }

    // Replaces SET CAP / SET VAR / SET STATUS statements that appear inline inside C# blocks
    // (e.g. inside an `if (...) { SET CAP @var "value" }` construct).
    private static string ApplyInlineSetStatements(string code)
    {
        if (string.IsNullOrEmpty(code)) return code;

        code = _rxInlineSetCap.Replace(code, m =>
        {
            string indent  = m.Groups["indent"].Value;
            string varName = m.Groups["var"].Value;
            string value   = m.Groups["val"].Value;
            if (value.StartsWith("@"))
                return $"{indent}data.Captures[\"{varName}\"] = data.GetVar(\"{value.TrimStart('@')}\");";
            return $"{indent}data.Captures[\"{varName}\"] = {value};";
        });

        code = _rxInlineSetVar.Replace(code, m =>
        {
            string indent  = m.Groups["indent"].Value;
            string varName = m.Groups["var"].Value;
            string value   = m.Groups["val"].Value;
            if (value.StartsWith("@"))
                return $"{indent}data.Variables.Set(new RuriLib.Models.CVar(\"{varName}\", data.GetVar(\"{value.TrimStart('@')}\")));";
            return $"{indent}data.Variables.Set(new RuriLib.Models.CVar(\"{varName}\", {value}));";
        });

        code = _rxInlineSetStatus.Replace(code, m =>
            $"{m.Groups["indent"].Value}data._inner.Status = BotStatus.{m.Groups["status"].Value.ToUpperInvariant()};");

        return code;
    }

    // Resolves @-prefixed variable references in a LoliCode code segment (if/while conditions, etc.)
    // into valid C# expressions so the Roslyn compiler can run them even when the original variable
    // was declared inside a block {} scope (fixing CS0103 scoping errors).
    //
    //  @data.SOURCE       → __rv("<SOURCE>")      (BotData built-in, resolved via ReplaceValues)
    //  @data.RESPONSECODE → __rv("<RESPONSECODE>") etc.
    //  @input.USERNAME    → __rv("<USER>")         (wordlist input)
    //  @input.PASSWORD    → __rv("<PASS>")
    //  @input.ANYTHING    → __rv("<ANYTHING>")
    //  @varName           → data.GetVar("varName") (user variable from CVar store)
    //                       or bare varName if it is a known C# local from a preceding code block
    //  @@varName          → left as-is (variable whose name starts with @, handled separately)
    private static string ResolveAtVarRefs(string code)
    {
        if (string.IsNullOrEmpty(code)) return code;

        // Extract all string literals first so @-ref patterns inside them are not altered.
        // Handles both verbatim strings @"..." (where "" is the escape) and regular strings "...".
        var literals  = new List<string>();
        var sb2       = new System.Text.StringBuilder(code.Length);
        int pos       = 0;
        while (pos < code.Length)
        {
            char ch = code[pos];
            if (ch == '@' && pos + 1 < code.Length && code[pos + 1] == '"')
            {
                // Verbatim string @"..."
                int start = pos; pos += 2;
                while (pos < code.Length)
                {
                    if (code[pos] == '"' && pos + 1 < code.Length && code[pos + 1] == '"') { pos += 2; continue; }
                    if (code[pos] == '"') { pos++; break; }
                    pos++;
                }
                string lit = code.Substring(start, pos - start);
                sb2.Append($"\x02LIT{literals.Count}\x03");
                literals.Add(lit);
            }
            else if (ch == '"' || ch == '\'')
            {
                // Regular string "..." or char '...'
                char q = ch; int start = pos++;
                while (pos < code.Length)
                {
                    if (code[pos] == '\\') { pos += 2; continue; }
                    if (code[pos] == q)    { pos++; break; }
                    pos++;
                }
                string lit = code.Substring(start, pos - start);
                sb2.Append($"\x02LIT{literals.Count}\x03");
                literals.Add(lit);
            }
            else { sb2.Append(ch); pos++; }
        }
        code = sb2.ToString();

        // Pass 1 — @data.PROP (OB2 BotData property refs)
        code = Regex.Replace(code, @"@data\.([A-Za-z0-9_]+)", m =>
            $"__rv(\"<{m.Groups[1].Value.ToUpperInvariant()}>\")");

        // Pass 2 — @input.PROP (OB2 wordlist / credential refs)
        code = Regex.Replace(code, @"@input\.([A-Za-z0-9_]+)", m =>
        {
            string sbName = m.Groups[1].Value.ToUpperInvariant() switch {
                "USERNAME" => "USER",
                "PASSWORD" => "PASS",
                string n   => n,
            };
            return $"__rv(\"<{sbName}>\")";
        });

        // Pass 3 — @varName (user-defined variable; @@varName skipped via @ in lookbehind)
        code = Regex.Replace(code, @"(?<![a-zA-Z0-9_\\@])@([A-Za-z_][A-Za-z0-9_]*)", m =>
        {
            string varName = m.Groups[1].Value;
            return (_knownLocals != null && _knownLocals.Contains(varName))
                ? varName
                : $"data.GetVar(\"{varName}\")";
        });

        // Restore extracted string literals
        for (int li = 0; li < literals.Count; li++)
            code = code.Replace($"\x02LIT{li}\x03", literals[li]);

        return code;
    }

    // In .NET 8, numeric Parse method groups (e.g. int.Parse) are ambiguous in Select()
    // OB2 configs sometimes emit "try {" on one line followed by "{" on the next,
    // which is a C# syntax error (CS1513). The extra "{" comes from OB2's own block
    // wrapper being included when users copy-paste from OB2's compiled script output.
    // Remove the dangling "{" so the "}" before "catch" correctly closes the try block.
    private static string FixBraceAfterTry(string code)
    {
        if (string.IsNullOrEmpty(code) || !code.Contains("try")) return code;
        // Match: try { (optional trailing spaces) newline, then a line that is ONLY "{"
        return System.Text.RegularExpressions.Regex.Replace(
            code,
            @"(try\s*\{[^\S\r\n]*)\r?\n([^\S\r\n]*)\{([^\S\r\n]*)\r?\n",
            "$1\n$2\n"
        );
    }

    // because string→ReadOnlySpan<char> implicit conversion makes the Parse(ReadOnlySpan<char>)
    // overload also appear applicable, causing CS0411 type-inference failure.
    // Also replaces RECURSIVE Parse var references with data.GetListVar(...) so the
    // receiver is always List<string> (not string/IEnumerable<char>), fixing CS1503.
    private static string FixMethodGroupAmbiguity(string code)
    {
        if (string.IsNullOrEmpty(code)) return code;

        // 1. For RECURSIVE Parse output vars: replace `varName.` with `data.GetListVar("varName").`
        //    so that the receiver of .Select() (and other LINQ calls) is List<string>, not string.
        if (_recursiveParseVars != null)
        {
            foreach (string varName in _recursiveParseVars)
            {
                // Match varName as a standalone identifier (word boundary) followed by a dot.
                // Negative lookbehind prevents matching inside "varName" strings or as part of longer names.
                string pattern = $@"(?<![a-zA-Z0-9_@""])\b{Regex.Escape(varName)}\b(?!\s*[a-zA-Z0-9_])(?=\s*\.)";
                code = Regex.Replace(code, pattern, $"data.GetListVar(\"{varName}\")");
            }
        }

        // 2. Convert numeric Parse method groups to unambiguous lambdas.
        const string numeric = "int|long|double|float|decimal|short|byte|uint|ulong|ushort|sbyte|" +
                               "Int32|Int64|Double|Single|Decimal|Int16|Byte|UInt32|UInt64|UInt16|SByte";
        return Regex.Replace(code,
            @"\.Select\(\s*((?:" + numeric + @")\.Parse)\s*\)",
            m => $".Select(__sv => {m.Groups[1].Value}(__sv))");
    }

    // Scans a C# code segment for variable declarations and records the names in _knownLocals.
    // This enables <varName> in subsequent block templates to compile as {varName}
    // (C# interpolation) rather than __rv CVar lookup, bridging C# locals to block templates.
    // Only declarations at scope depth 0 are collected: variables declared inside nested
    // if/while/try/etc. blocks are out of scope at the outer level where block output-var
    // sync code is emitted, so treating them as known locals causes CS0103.
    private static void CollectDeclaredLocals(string code)
    {
        if (string.IsNullOrEmpty(code)) return;
        if (_knownLocals == null) _knownLocals = new HashSet<string>(StringComparer.Ordinal);
        int depth = 0;
        foreach (string rawLine in code.Replace("\r\n", "\n").Split('\n'))
        {
            if (depth == 0)
            {
                // Primitive types: string x = ..., int x, etc.
                foreach (Match m in Regex.Matches(rawLine,
                    @"\b(?:string|int|bool|long|double|float|decimal|char|byte|short|uint|ulong|object)\s+([A-Za-z_][A-Za-z0-9_]*)\s*[=;,(]"))
                    _knownLocals.Add(m.Groups[1].Value);
                // var declarations
                foreach (Match m in Regex.Matches(rawLine, @"\bvar\s+([A-Za-z_][A-Za-z0-9_]*)\s*="))
                    _knownLocals.Add(m.Groups[1].Value);
                // Generic type declarations: List<T>, Dictionary<K,V>, etc.
                foreach (Match m in Regex.Matches(rawLine,
                    @"\b[A-Za-z][A-Za-z0-9_]*\s*<[^>]+>\s+([A-Za-z_][A-Za-z0-9_]*)\s*[=;,(]"))
                    _knownLocals.Add(m.Groups[1].Value);
            }
            // Update brace depth after processing declarations on this line.
            // Strip string literals first so braces inside strings don't skew depth.
            string stripped = Regex.Replace(rawLine, @"@?""(?:[^""\\]|\\.)*""|'[^'\\]'", "");
            foreach (char c in stripped)
            {
                if      (c == '{') depth++;
                else if (c == '}') { depth--; if (depth < 0) depth = 0; }
            }
        }
    }

    // ─── Block dispatch ──────────────────────────────────────────────────────

    private static string CompileBlock(LoliCodeSegment b)
    {
        string label = !string.IsNullOrEmpty(b.Label) ? $" [{b.Label}]" : "";
        var sb = new StringBuilder();
        sb.AppendLine($"// BLOCK:{b.BlockType}{label}");

        if (b.Disabled)
        {
            sb.AppendLine($"// [DISABLED — skipped]");
            return sb.ToString();
        }

        string body = b.BlockType switch
        {
            "ConstantString"  => CompileConstantString(b),
            "ConstantList"    => CompileConstantList(b),
            "HttpRequest"     => CompileHttpRequest(b),
            "Parse"           => CompileParse(b),
            "Function"        => CompileFunction(b),
            // OB2 dedicated function block names
            "UrlEncode"       => CompileOb2FunctionBlock(b, "URLEncode"),
            "UrlDecode"       => CompileOb2FunctionBlock(b, "URLDecode"),
            "Base64Encode"    => CompileOb2FunctionBlock(b, "Base64Encode"),
            "Base64Decode"    => CompileOb2FunctionBlock(b, "Base64Decode"),
            // OB2 serializer emits these names; map them to the same function types
            "UTF8ToBase64"    => CompileOb2ConversionUtility(b, "Utf8ToBase64"),
            "Base64ToUTF8"    => CompileOb2ConversionUtility(b, "Base64ToUtf8"),
            "GenerateGuid"    => CompileOb2FunctionBlock(b, "GenerateGUID"),
            "ToUppercase"     => CompileOb2FunctionBlock(b, "ToUppercase"),
            "ToLowercase"     => CompileOb2FunctionBlock(b, "ToLowercase"),
            "Translate"       => CompileTranslate(b),
            "GetRemainingDay" => CompileOb2FunctionBlock(b, "GetRemainingDay"),
            "UnixTimeToDate"  => CompileUnixTimeToDate(b),
            "MergeByteArrays"  => CompileOb2FunctionBlock(b, "MergeByteArrays"),
            "Unescape"         => CompileOb2FunctionBlock(b, "Unescape"),
            "AESEncrypt"       => CompileOb2FunctionBlock(b, "AESEncrypt"),
            "AESDecrypt"       => CompileOb2FunctionBlock(b, "AESDecrypt"),
            "AESEncryptString" => CompileOb2FunctionBlock(b, "AESEncrypt"),
            "AESDecryptString" => CompileOb2FunctionBlock(b, "AESDecrypt"),
            "RSAEncrypt"       => CompileOb2FunctionBlock(b, "RSAEncrypt"),
            "RSADecrypt"       => CompileOb2FunctionBlock(b, "RSADecrypt"),
            "Hash"             => CompileOb2FunctionBlock(b, "Hash"),
            "HashString"       => CompileOb2FunctionBlock(b, "Hash"),
            "Hmac"             => CompileOb2FunctionBlock(b, "HMAC"),
            "HmacString"       => CompileOb2FunctionBlock(b, "HMAC"),
            "NTLMHash"         => CompileOb2FunctionBlock(b, "Ntlm"),
            "ScryptString"     => CompileOb2FunctionBlock(b, "SCrypt"),
            "BCryptHash"       => CompileOb2FunctionBlock(b, "BCrypt"),
            "BCryptHashGenSalt"=> CompileOb2FunctionBlock(b, "BCrypt"),
            "Split"            => CompileOb2FunctionBlock(b, "Split"),
            "CharAt"           => CompileOb2FunctionBlock(b, "CharAt"),
            "Ceil"             => CompileOb2FunctionBlock(b, "Ceil"),
            "Floor"            => CompileOb2FunctionBlock(b, "Floor"),
            "Round"            => CompileOb2FunctionBlock(b, "Round"),
            "RandomInteger"    => CompileOb2FunctionBlock(b, "RandomNum"),
            "RandomString"     => CompileOb2FunctionBlock(b, "RandomString"),
            "ClearCookies"          => CompileOb2MiscUtility(b, "ClearCookies"),
            "GetHWID"               => CompileOb2MiscUtility(b, "GetHWID"),
            "BigIntegerToByteArray"   => CompileOb2ConversionUtility(b, "BigIntegerToByteArray"),
            "ByteArrayToBigInteger"   => CompileOb2ConversionUtility(b, "ByteArrayToBigInteger"),
            "ReadableSize"            => CompileOb2ConversionUtility(b, "ReadableSize"),
            "Base64StringToByteArray" => CompileOb2ConversionUtility(b, "Base64ToBytes"),
            "BinaryStringToByteArray" => CompileOb2ConversionUtility(b, "BinaryStringToBytes"),
            "ByteArrayToBase64String" => CompileOb2ConversionUtility(b, "BytesToBase64"),
            "ByteArrayToBinaryString" => CompileOb2ConversionUtility(b, "BytesToBinaryString"),
            "ByteArrayToHexString"    => CompileOb2ConversionUtility(b, "BytesToHex"),
            "BytesToString"           => CompileOb2ConversionUtility(b, "BytesToString"),
            "HexStringToByteArray"    => CompileOb2ConversionUtility(b, "HexToBytes"),
            "StringToBytes"           => CompileOb2ConversionUtility(b, "StringToBytes"),
            "SvgToPng"                => CompileOb2ImagesUtility(b, "SvgToPng"),
            "RegexReplace"       => CompileOb2FunctionBlock(b, "RegexReplace"),
            "Replace"            => CompileOb2FunctionBlock(b, "Replace"),
            "Substring"          => CompileOb2FunctionBlock(b, "Substring"),
            "Trim"               => CompileOb2FunctionBlock(b, "Trim"),
            "Reverse"            => CompileOb2FunctionBlock(b, "ReverseString"),
            "CountOccurrences"   => CompileOb2FunctionBlock(b, "CountOccurrences"),
            "EncodeHTMLEntities" => CompileOb2FunctionBlock(b, "HTMLEntityEncode"),
            "DecodeHTMLEntities" => CompileOb2FunctionBlock(b, "HTMLEntityDecode"),
            "JwtEncode"          => CompileOb2FunctionBlock(b, "JWTEncode"),
            "JWTEncode"          => CompileOb2FunctionBlock(b, "JWTEncode"),
            "MaxFloat"           => CompileOb2FunctionBlock(b, "MaxFloat"),
            "MinFloat"           => CompileOb2FunctionBlock(b, "MinFloat"),
            "RandomFloat"        => CompileOb2FunctionBlock(b, "RandomFloat"),
            "MaxInt"             => CompileOb2FunctionBlock(b, "MaxInt"),
            "MinInt"             => CompileOb2FunctionBlock(b, "MinInt"),
            "AddKeyValuePair"    => CompileOb2FunctionBlock(b, "AddKeyValuePair"),
            "GetKey"             => CompileOb2FunctionBlock(b, "GetKey"),
            "RemoveByKey"        => CompileOb2FunctionBlock(b, "RemoveByKey"),
            "CreateListOfNumbers"=> CompileOb2FunctionBlock(b, "CreateListOfNumbers"),
            "IndexOf"            => CompileOb2FunctionBlock(b, "IndexOf"),
            "ToDictionary"       => CompileOb2FunctionBlock(b, "ListToDict"),
            "XOR"              => CompileOb2FunctionBlock(b, "XOR"),
            "XORStrings"       => CompileOb2FunctionBlock(b, "XORStrings"),
            "BCryptVerify"     => CompileOb2FunctionBlock(b, "BCryptVerify"),
            "ScryptDeriveKey"  => CompileOb2FunctionBlock(b, "ScryptDeriveKey"),
            "AWS4Signature"    => CompileOb2FunctionBlock(b, "AWS4Signature"),
            // OB2-native blocks compiled inline (no matching SilverBullet block class)
            "RandomUserAgent"  => CompileRandomUserAgent(b),
            "Length"           => CompileLength(b),
            "CurrentUnixTime"  => CompileCurrentUnixTime(b),
            "DateToUnixTime"   => CompileDateToUnixTime(b),
            "Compute"          => CompileCompute(b),
            "RoundToInteger"   => CompileRoundToInteger(b),
            "Delay"           => CompileDelay(b),
            "KeyCheck"        => CompileKeyCheck(b),
            "Keycheck"        => CompileKeyCheck(b),
            "Utility"         => CompileUtility(b),
            "TCP"             => CompileTcp(b),
            "WebSocket"       => CompileWebSocket(b),
            "DnsLookup"       => CompileDns(b),
            "CfClearance"     => CompileCfClearanceBlock(b),
            "Turnstile"       => CompileTurnstileBlock(b),
            "Altcha"             => CompileAltchaBlock(b),
            "RecaptchaV3Bypass"  => CompileRecaptchaV3BypassBlock(b),
            "FriendlyCaptcha" => CompileFriendlyCaptchaBlock(b),
            "RecaptchaV3"          => CompileRecaptchaV3Block(b),
            "RecaptchaV2Invisible" => CompileRecaptchaV2InvisibleBlock(b),
            "AkmCookies"      => CompileAkmCookiesBlock(b),
            "DataDome"        => CompileDataDomeBlock(b),
            _                 => CompileReflection(b)
        };

        sb.Append(body);

        // OB2 compat: expose block output variable as a C# local in the outer scope
        // so subsequent inline code can reference it as a bare identifier (without @).
        // Block-scope methods (wrapped in {}) declare the var inside {} where it goes
        // out of scope at }. We re-read from the CVar store here so code like
        //   expiryDate = expiryDate != "" ? expiryDate : nextRenewalDate;
        // compiles without CS0103.
        // Flat methods (Length, Compute, ConstantString…) already emit `string v = …`
        // in the outer scope themselves — detect by last char of body.
        if (b.OutputVar != null)
        {
            string v = SafeId(b.OutputVar);
            string trimmedBody = body.TrimEnd();
            if (trimmedBody.Length > 0 && trimmedBody[trimmedBody.Length - 1] == '}')
            {
                bool alreadyDeclared = _knownLocals.Contains(b.OutputVar);
                if (alreadyDeclared)
                    sb.AppendLine($"{v} = data.GetVar({JsonConvert.SerializeObject(b.OutputVar)});");
                else
                    sb.AppendLine($"string {v} = data.GetVar({JsonConvert.SerializeObject(b.OutputVar)});");
            }
            _knownLocals.Add(b.OutputVar);
        }

        return sb.ToString();
    }

    // ─── ConstantString ──────────────────────────────────────────────────────

    private static string CompileConstantString(LoliCodeSegment b)
    {
        string rawVal = b.Properties.GetValueOrDefault("value", "\"\"");
        string csVal  = ResolveConstantValue(rawVal);

        if (b.OutputVar != null)
        {
            string v = SafeId(b.OutputVar);
            string cap = b.IsCapture ? "true" : "false";
            bool alreadyDeclared = _knownLocals != null && _knownLocals.Contains(b.OutputVar);
            string decl = alreadyDeclared ? $"{v} = {csVal};" : $"string {v} = {csVal};";
            if (_knownLocals != null) _knownLocals.Add(b.OutputVar);
            return $"{decl}\n" +
                   $"data.Variables.Set(new RuriLib.Models.CVar(\"{b.OutputVar}\", {v}, {cap}));\n";
        }
        return $"/* ConstantString with no output var: {csVal} */\n";
    }

    // Resolves a ConstantString/ConstantList value that may be an OB2 @ref.
    // @input.VARNAME / @data.VARNAME → __rv("<VARNAME>") at runtime.
    // @VARNAME (bare CVar) → data.GetVar("VARNAME") at runtime.
    // Anything else → normal ToCs() handling.
    private static string ResolveConstantValue(string rawVal)
    {
        string inner = rawVal?.Trim() ?? "";
        // Unquote if wrapped in double-quotes
        if (inner.StartsWith("\"") && inner.EndsWith("\"") && inner.Length >= 2)
            inner = inner.Substring(1, inner.Length - 2);

        // @input.VARNAME or @data.VARNAME → __rv("<VARNAME>")
        var atInputMatch = Regex.Match(inner, @"^@(?:input|data)\.([A-Za-z0-9_]+)$", RegexOptions.IgnoreCase);
        if (atInputMatch.Success)
            return $"__rv(\"<{atInputMatch.Groups[1].Value.ToUpperInvariant()}>\")";

        // @VARNAME (bare CVar reference) → data.GetVar("VARNAME")
        var atVarMatch = Regex.Match(inner, @"^@([A-Za-z_][A-Za-z0-9_]*)$");
        if (atVarMatch.Success)
        {
            string vn = atVarMatch.Groups[1].Value;
            return (_knownLocals != null && _knownLocals.Contains(vn))
                ? vn
                : $"data.GetVar(\"{vn}\")";
        }

        return ToCs(rawVal);
    }

    // ─── ConstantList ────────────────────────────────────────────────────────

    private static string CompileConstantList(LoliCodeSegment b)
    {
        string rawVal = b.Properties.GetValueOrDefault("value", "$[]");
        string csVal  = ToCs(rawVal);   // should produce a List<string> expression

        if (b.OutputVar != null)
        {
            string v = SafeId(b.OutputVar);
            string capL = b.IsCapture ? "true" : "false";
            bool alreadyDeclared = _knownLocals != null && _knownLocals.Contains(b.OutputVar);
            string decl = alreadyDeclared ? $"{v} = {csVal};" : $"var {v} = {csVal};";
            if (_knownLocals != null) _knownLocals.Add(b.OutputVar);
            return $"{decl}\n" +
                   $"data.Variables.Set(new RuriLib.Models.CVar(\"{b.OutputVar}\", {v} is System.Collections.Generic.List<string> __l_{v} ? __l_{v} : new System.Collections.Generic.List<string>(), {capL}));\n";
        }
        return $"/* ConstantList with no output var */\n";
    }

    // ─── HttpRequest ─────────────────────────────────────────────────────────

    private static string CompileHttpRequest(LoliCodeSegment b)
    {
        string bn   = NextBlock();
        var    sb   = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"    var {bn} = new RuriLib.BlockRequest();");

        // Map OB2 property names → BlockRequest property names
        foreach (var kv in b.Properties)
        {
            string key = kv.Key.ToLower();
            string val = kv.Value;
            switch (key)
            {
                case "url":
                    sb.AppendLine($"    {bn}.Url = {ToCs(val)};");
                    break;
                case "method":
                    string meth = val.Trim().ToUpperInvariant();
                    sb.AppendLine($"    {bn}.Method = RuriLib.Functions.Requests.HttpMethod.{meth};");
                    break;
                case "requestbody":
                case "postbody":
                case "postdata":
                case "body":
                case "content":
                    sb.AppendLine($"    {bn}.PostData = {ToCs(val)};");
                    break;
                case "contenttype":
                    sb.AppendLine($"    {bn}.ContentType = {ToCs(val)};");
                    break;
                case "autoredirect":
                    sb.AppendLine($"    {bn}.AutoRedirect = {ToCsBool(val)};");
                    break;
                case "readresponsesource":
                case "readresponsecontent":
                    sb.AppendLine($"    {bn}.ReadResponseSource = {ToCsBool(val)};");
                    break;
                case "encodecontent":
                case "urlencodecontent":
                    sb.AppendLine($"    {bn}.EncodeContent = {ToCsBool(val)};");
                    break;
                case "acceptencoding":
                    sb.AppendLine($"    {bn}.AcceptEncoding = {ToCsBool(val)};");
                    break;
                case "customheaders":
                    sb.AppendLine($"    {bn}.CustomHeaders = {ToCsDict(val)};");
                    break;
                case "customcookies":
                    sb.AppendLine($"    {bn}.CustomCookies = {ToCsDict(val)};");
                    break;
                case "rawdata":
                    if (val.StartsWith("@"))
                        sb.AppendLine($"    {bn}.RawBytes = {val.Substring(1)};");
                    else
                        sb.AppendLine($"    {bn}.RawData = {ToCs(val)};");
                    break;
                case "authuser": case "username":
                    sb.AppendLine($"    {bn}.AuthUser = {ToCs(val)};");
                    break;
                case "authpass": case "password":
                    sb.AppendLine($"    {bn}.AuthPass = {ToCs(val)};");
                    break;
                case "requesttype":
                    sb.AppendLine($"    {bn}.RequestType = (RuriLib.RequestType)System.Enum.Parse(typeof(RuriLib.RequestType), {JsonConvert.SerializeObject(val.Trim())}, true);");
                    break;
                case "securityprotocol":
                    sb.AppendLine($"    {bn}.SecurityProtocol = (RuriLib.Functions.Requests.SecurityProtocol)System.Enum.Parse(typeof(RuriLib.Functions.Requests.SecurityProtocol), {JsonConvert.SerializeObject(val.Trim())}, true);");
                    break;
                case "httplibrary":
                    sb.AppendLine($"    {bn}.HttpLibrary = (RuriLib.Functions.Requests.HttpLibrary)System.Enum.Parse(typeof(RuriLib.Functions.Requests.HttpLibrary), {JsonConvert.SerializeObject(val.Trim())}, true);");
                    break;
                case "ignorecertvalidation":
                case "ignorecertificatevalidation":
                    sb.AppendLine($"    {bn}.IgnoreCertificateValidation = {ToCsBool(val)};");
                    break;
                case "alwayssendcontent":
                    sb.AppendLine($"    {bn}.AlwaysSendContent = {ToCsBool(val)};");
                    break;
                case "codepagesencoding":
                    sb.AppendLine($"    {bn}.CodePagesEncoding = {ToCs(val)};");
                    break;
                case "requesttimeout":
                case "timeoutmilliseconds":
                    if (int.TryParse(val.Trim(), out int _tout))
                        sb.AppendLine($"    {bn}.RequestTimeoutMs = {_tout};");
                    break;
                case "saveresponsecookies":
                    sb.AppendLine($"    {bn}.SaveResponseCookies = {ToCsBool(val)};");
                    break;
                case "loadrequestcookies":
                    sb.AppendLine($"    {bn}.LoadRequestCookies = {ToCsBool(val)};");
                    break;
                case "retrycount":
                    if (int.TryParse(val.Trim(), out int _rc))
                        sb.AppendLine($"    {bn}.RetryCount = {_rc};");
                    break;
                case "retrydelay":
                    if (int.TryParse(val.Trim(), out int _rd))
                        sb.AppendLine($"    {bn}.RetryDelayMs = {_rd};");
                    break;
                case "curlprofile":
                case "curlimpersonatebrowserprofile":
                    sb.AppendLine($"    {bn}.CurlImpersonateProfile = RuriLib.Functions.Requests.CurlImpersonateBrowserProfile.{val.Trim()};");
                    break;
                case "multipartpart":
                    foreach (string _ps in val.Split('\x1E'))
                    {
                        if (string.IsNullOrWhiteSpace(_ps)) continue;
                        try
                        {
                            var _arr = JsonConvert.DeserializeObject<string[]>(_ps);
                            if (_arr == null || _arr.Length < 3) continue;
                            string _ptype  = _arr[0];
                            string _pname  = _arr[1];
                            string _pvalue = _arr[2];
                            string _pct    = _arr.Length > 3 ? _arr[3] : "";
                            sb.AppendLine($"    {bn}.MultipartContents.Add(new RuriLib.Functions.Requests.MultipartContent {{");
                            sb.AppendLine($"        Type  = RuriLib.MultipartContentType.{_ptype},");
                            sb.AppendLine($"        Name  = {JsonConvert.SerializeObject(_pname)},");
                            sb.AppendLine($"        Value = {JsonConvert.SerializeObject(_pvalue)},");
                            if (!string.IsNullOrEmpty(_pct))
                                sb.AppendLine($"        ContentType = {JsonConvert.SerializeObject(_pct)},");
                            sb.AppendLine($"    }});");
                        }
                        catch { }
                    }
                    break;
                case "multipartboundary":
                    sb.AppendLine($"    {bn}.MultipartBoundary = {ToCs(val)};");
                    break;
                case "protocolversion":
                case "httpversion":
                {
                    string _stripped = val.Trim().Trim('"');
                    var _vp = _stripped.Split('.');
                    if (_vp.Length == 2 && int.TryParse(_vp[0], out int _vmaj) && int.TryParse(_vp[1], out int _vmin))
                        sb.AppendLine($"    {bn}.ProtocolVersion = new System.Version({_vmaj}, {_vmin});");
                    break;
                }
                case "decodehtml":
                    sb.AppendLine($"    {bn}.DecodeHtml = {ToCsBool(val)};");
                    break;
                case "allowemptyheadervalues":
                    sb.AppendLine($"    {bn}.AllowEmptyHeaderValues = {ToCsBool(val)};");
                    break;
                case "responsetype":
                    sb.AppendLine($"    {bn}.ResponseType = (RuriLib.ResponseType)System.Enum.Parse(typeof(RuriLib.ResponseType), {JsonConvert.SerializeObject(val.Trim())}, true);");
                    break;
                case "downloadpath":
                    sb.AppendLine($"    {bn}.DownloadPath = {ToCs(val)};");
                    break;
                case "saveasscreenshot":
                    sb.AppendLine($"    {bn}.SaveAsScreenshot = {ToCsBool(val)};");
                    break;
                case "outputvariable":
                    sb.AppendLine($"    {bn}.OutputVariable = {ToCs(val)};");
                    break;
                case "label":
                case "enabled":
                    break;
                default:
                    sb.AppendLine($"    // Unknown property: {kv.Key} = {val}");
                    break;
            }
        }

        if (!string.IsNullOrEmpty(b.Label))
            sb.AppendLine($"    {bn}.Label = {JsonConvert.SerializeObject(b.Label)};");

        sb.AppendLine($"    {bn}.Process(data._inner);");

        if (b.OutputVar != null)
        {
            string v   = SafeId(b.OutputVar);
            string cap = b.IsCapture ? "true" : "false";
            sb.AppendLine($"    string {v} = data._inner.ResponseSource ?? \"\";");
            sb.AppendLine($"    data.Variables.Set(new RuriLib.Models.CVar(\"{b.OutputVar}\", {v}, {cap}));");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    // ─── Parse ───────────────────────────────────────────────────────────────

    private static string CompileParse(LoliCodeSegment b)
    {
        string bn = NextBlock();
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"    var {bn} = new RuriLib.BlockParse();");

        foreach (var kv in b.Properties)
        {
            string key = kv.Key.ToLower();
            string val = kv.Value;
            switch (key)
            {
                case "variablename":
                case "outputvar":
                    sb.AppendLine($"    {bn}.VariableName = {ToCs(val)};");
                    break;
                case "iscapture":
                    sb.AppendLine($"    {bn}.IsCapture = {ToCsBool(val)};");
                    break;
                case "regexstring":
                case "regexpattern":
                case "pattern":
                    sb.AppendLine($"    {bn}.RegexString = {ToCs(val)};");
                    break;
                case "regexoutput":
                case "outputformat":
                    sb.AppendLine($"    {bn}.RegexOutput = {ToCs(val)};");
                    break;
                case "multiline": // OB2 property — no BlockParse equivalent; accepted and ignored
                    break;
                case "target":
                case "source":
                case "parsesource":
                case "parsetarget":
                    // ParseTarget is a string like "<SOURCE>", "<HEADERS>", etc.
                    sb.AppendLine($"    {bn}.ParseTarget = {ToCs(val)};");
                    break;
                case "input":
                {
                    // OB2 "input = @data.SOURCE" or "input = @data.COOKIES[\"name\"]"
                    // Convert to SilverBullet ParseTarget format: <SOURCE>, <COOKIES(name)>, etc.
                    string sbTgt = Ob2RefToSbRef(val.Trim());
                    sb.AppendLine($"    {bn}.ParseTarget = {JsonConvert.SerializeObject(sbTgt)};");
                    break;
                }
                case "type":
                case "parsetype":
                    sb.AppendLine($"    {bn}.Type = (RuriLib.ParseType)System.Enum.Parse(typeof(RuriLib.ParseType), {JsonConvert.SerializeObject(val.Trim().ToUpperInvariant())}, true);");
                    break;
                case "recursive":
                    sb.AppendLine($"    {bn}.Recursive = {ToCsBool(val)};");
                    break;
                case "dotmatches":
                    sb.AppendLine($"    {bn}.DotMatches = {ToCsBool(val)};");
                    break;
                case "casesensitive":
                    sb.AppendLine($"    {bn}.CaseSensitive = {ToCsBool(val)};");
                    break;
                case "lstring":
                case "leftstring":
                case "leftdelim":
                case "ld":
                    sb.AppendLine($"    {bn}.LeftString = {ToCs(val)};");
                    break;
                case "rstring":
                case "rightstring":
                case "rightdelim":
                case "rd":
                    sb.AppendLine($"    {bn}.RightString = {ToCs(val)};");
                    break;
                case "cssselector":
                    sb.AppendLine($"    {bn}.CssSelector = {ToCs(val)};");
                    break;
                case "attributename":
                    sb.AppendLine($"    {bn}.AttributeName = {ToCs(val)};");
                    break;
                case "jsonfield":
                case "jtoken":
                    sb.AppendLine($"    {bn}.JsonField = {ToCs(val)};");
                    break;
                case "prefix":
                    sb.AppendLine($"    {bn}.Prefix = {ToCs(val)};");
                    break;
                case "suffix":
                    sb.AppendLine($"    {bn}.Suffix = {ToCs(val)};");
                    break;
                case "touppercase":
                    sb.AppendLine($"    {bn}.ToUppercase = {ToCsBool(val)};");
                    break;
                case "tolowercase":
                    sb.AppendLine($"    {bn}.ToLowercase = {ToCsBool(val)};");
                    break;
            }
        }

        // Set output variable name on the block so it stores to the right CVar
        if (b.OutputVar != null)
            sb.AppendLine($"    {bn}.VariableName = {JsonConvert.SerializeObject(b.OutputVar)};");

        if (!string.IsNullOrEmpty(b.Label))
            sb.AppendLine($"    {bn}.Label = {JsonConvert.SerializeObject(b.Label)};");

        sb.AppendLine($"    {bn}.IsCapture = {(b.IsCapture ? "true" : "false")};");
        sb.AppendLine($"    {bn}.Process(data._inner);");

        if (b.OutputVar != null)
        {
            string v = SafeId(b.OutputVar);
            // Detect RECURSIVE by checking what the foreach loop already wrote.
            // If the block had RECURSIVE, the foreach wrote "{bn}.Recursive = true;" into sb.
            bool isRecursive = sb.ToString().Contains($"{bn}.Recursive = true;", StringComparison.Ordinal);
            if (isRecursive)
                sb.AppendLine($"    System.Collections.Generic.List<string> {v} = data.GetListVar({JsonConvert.SerializeObject(b.OutputVar)});");
            else
                sb.AppendLine($"    string {v} = data.GetVar({JsonConvert.SerializeObject(b.OutputVar)});");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    // ─── Function ────────────────────────────────────────────────────────────

    private static string CompileFunction(LoliCodeSegment b)
    {
        string bn = NextBlock();
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"    var {bn} = new RuriLib.BlockFunction();");

        foreach (var kv in b.Properties)
        {
            string key = kv.Key.ToLower();
            string val = kv.Value;
            switch (key)
            {
                case "function":
                case "functiontype":
                    sb.AppendLine($"    {bn}.FunctionType = (RuriLib.BlockFunction.Function)System.Enum.Parse(typeof(RuriLib.BlockFunction.Function), {JsonConvert.SerializeObject(val.Trim())}, true);");
                    break;
                case "inputstring":
                case "input":
                    sb.AppendLine($"    {bn}.InputString = {ToCs(val)};");
                    break;
                case "outputvariable":
                case "outputvar":
                case "variablename":
                    sb.AppendLine($"    {bn}.VariableName = {ToCs(val)};");
                    break;
                case "iscapture":
                    sb.AppendLine($"    {bn}.IsCapture = {ToCsBool(val)};");
                    break;
            }
        }

        if (b.OutputVar != null)
            sb.AppendLine($"    {bn}.VariableName = {JsonConvert.SerializeObject(b.OutputVar)};");

        if (!string.IsNullOrEmpty(b.Label))
            sb.AppendLine($"    {bn}.Label = {JsonConvert.SerializeObject(b.Label)};");

        sb.AppendLine($"    {bn}.IsCapture = {(b.IsCapture ? "true" : "false")};");
        sb.AppendLine($"    {bn}.Process(data._inner);");

        if (b.OutputVar != null)
        {
            string v = SafeId(b.OutputVar);
            sb.AppendLine($"    string {v} = data.GetVar({JsonConvert.SerializeObject(b.OutputVar)});");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    // ─── UnixTimeToDate ──────────────────────────────────────────────────────────

    private static string CompileUnixTimeToDate(LoliCodeSegment b)
    {
        string bn = NextBlock();
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"    var {bn} = new RuriLib.BlockFunction();");
        sb.AppendLine($"    {bn}.FunctionType = RuriLib.BlockFunction.Function.UnixTimeToDate;");

        // Support OB2 property name "unixTime" and generic "input" fallback
        string rawInput = b.Properties.TryGetValue("unixTime", out string u) ? u
                        : b.Properties.GetValueOrDefault("input", null);
        if (rawInput != null)
        {
            string trimmed = rawInput.Trim();
            string sbRef   = trimmed == "@"                                        ? ""   // bare @ = current time (empty input)
                           : trimmed.StartsWith("@", StringComparison.Ordinal)     ? Ob2AtRefToSbRef(trimmed)
                           : trimmed;
            sb.AppendLine($"    {bn}.InputString = {ToCs(sbRef)};");
        }

        if (b.Properties.TryGetValue("format", out string df) || b.Properties.TryGetValue("dateFormat", out df))
            sb.AppendLine($"    {bn}.DateFormat = {ToCs(df)};");

        if (b.OutputVar != null)
            sb.AppendLine($"    {bn}.VariableName = {JsonConvert.SerializeObject(b.OutputVar)};");
        if (!string.IsNullOrEmpty(b.Label))
            sb.AppendLine($"    {bn}.Label = {JsonConvert.SerializeObject(b.Label)};");
        sb.AppendLine($"    {bn}.IsCapture = {(b.IsCapture ? "true" : "false")};");
        sb.AppendLine($"    {bn}.Process(data._inner);");

        if (b.OutputVar != null)
        {
            string v = SafeId(b.OutputVar);
            sb.AppendLine($"    string {v} = data.GetVar({JsonConvert.SerializeObject(b.OutputVar)});");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    // ─── OB2 dedicated function blocks (UrlEncode, UrlDecode, Base64Encode, …) ──
    // Compiles BLOCK:UrlEncode / BLOCK:UrlDecode / BLOCK:Base64Encode / BLOCK:Base64Decode
    // into a BlockFunction call, converting OB2's @ref input to SilverBullet's <ref> format.

    private static string CompileOb2FunctionBlock(LoliCodeSegment b, string funcTypeName)
    {
        string bn = NextBlock();
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"    var {bn} = new RuriLib.BlockFunction();");
        sb.AppendLine($"    {bn}.FunctionType = (RuriLib.BlockFunction.Function)System.Enum.Parse(typeof(RuriLib.BlockFunction.Function), {JsonConvert.SerializeObject(funcTypeName)}, true);");

        // Local helper: read a property and resolve @ref → <ref>
        string ResolveInp(string propKey)
        {
            if (!b.Properties.TryGetValue(propKey, out string v)) return null;
            string t = v.Trim();
            return t.StartsWith("@", StringComparison.Ordinal) ? Ob2AtRefToSbRef(t) : t;
        }

        // GenerateGuid: read version, format and uppercase flag instead of input
        if (funcTypeName == "GenerateGUID")
        {
            if (b.Properties.TryGetValue("version", out string ver) && !string.IsNullOrWhiteSpace(ver))
                sb.AppendLine($"    {bn}.GuidVer = (RuriLib.GuidVersion)System.Enum.Parse(typeof(RuriLib.GuidVersion), {JsonConvert.SerializeObject(ver.Trim())}, true);");
            if (b.Properties.TryGetValue("format", out string fmt) && !string.IsNullOrWhiteSpace(fmt))
                sb.AppendLine($"    {bn}.GuidFmt = (RuriLib.GuidFormat)System.Enum.Parse(typeof(RuriLib.GuidFormat), {JsonConvert.SerializeObject(fmt.Trim())}, true);");
            if (b.Properties.TryGetValue("guidUppercase", out string uc) && ToCsBool(uc) == "true")
                sb.AppendLine($"    {bn}.GuidUppercase = true;");
        }
        else if (funcTypeName == "RegexReplace")
        {
            string rrInp = ResolveInp("original") ?? ResolveInp("input");
            if (rrInp != null) sb.AppendLine($"    {bn}.InputString = {ToCs(rrInp)};");
            if (b.Properties.TryGetValue("pattern",     out string pat))  sb.AppendLine($"    {bn}.RegexMatch  = {ToCs(pat.Trim())};");
            if (b.Properties.TryGetValue("replacement", out string repl)) sb.AppendLine($"    {bn}.ReplaceWith = {ToCs(repl.Trim())};");
        }
        else if (funcTypeName == "Substring")
        {
            string subInp = ResolveInp("input") ?? ResolveInp("original");
            if (subInp != null) sb.AppendLine($"    {bn}.InputString = {ToCs(subInp)};");
            if (b.Properties.TryGetValue("startIndex", out string si)) sb.AppendLine($"    {bn}.SubstringIndex  = {ToCs(si.Trim())};");
            if (b.Properties.TryGetValue("length",     out string sl)) sb.AppendLine($"    {bn}.SubstringLength = {ToCs(sl.Trim())};");
        }
        else if (funcTypeName == "CountOccurrences")
        {
            string coInp = ResolveInp("input") ?? ResolveInp("original");
            if (coInp != null) sb.AppendLine($"    {bn}.InputString = {ToCs(coInp)};");
            if (b.Properties.TryGetValue("stringToFind", out string stf)) sb.AppendLine($"    {bn}.StringToFind = {ToCs(stf.Trim())};");
        }
        else if (funcTypeName == "DateToUnixTime")
        {
            string dtInp = ResolveInp("datetime") ?? ResolveInp("input");
            if (dtInp != null) sb.AppendLine($"    {bn}.InputString = {ToCs(dtInp)};");
            if (b.Properties.TryGetValue("format", out string df) || b.Properties.TryGetValue("dateFormat", out df))
                sb.AppendLine($"    {bn}.DateFormat = {ToCs(df.Trim())};");
            if (b.Properties.TryGetValue("type", out string utt) && !string.IsNullOrWhiteSpace(utt))
                sb.AppendLine($"    {bn}.UnixTimeType = (RuriLib.BlockFunction.DateToUnixTimeType)System.Enum.Parse(typeof(RuriLib.BlockFunction.DateToUnixTimeType), {JsonConvert.SerializeObject(utt.Trim())}, true);");
        }
        else if (funcTypeName == "Replace")
        {
            string orig = ResolveInp("original");
            if (orig != null) sb.AppendLine($"    {bn}.InputString = {ToCs(orig)};");
            if (b.Properties.TryGetValue("toReplace",   out string trProp))   sb.AppendLine($"    {bn}.ReplaceWhat = {ToCs(trProp.Trim())};");
            if (b.Properties.TryGetValue("replacement", out string replProp)) sb.AppendLine($"    {bn}.ReplaceWith = {ToCs(replProp.Trim())};");
        }
        else if (funcTypeName == "XOR" || funcTypeName == "XORStrings")
        {
            string inp = ResolveInp("input");
            if (inp != null) sb.AppendLine($"    {bn}.InputString = {ToCs(inp)};");
            if (b.Properties.TryGetValue("key", out string k)) sb.AppendLine($"    {bn}.SecondInput = {ToCs(k.Trim())};");
        }
        else if (funcTypeName == "RSADecrypt")
        {
            string inp = ResolveInp("input");
            if (inp != null) sb.AppendLine($"    {bn}.InputString = {ToCs(inp)};");
            if (b.Properties.TryGetValue("n",    out string rn)) sb.AppendLine($"    {bn}.RsaN  = {ToCs(rn.Trim())};");
            if (b.Properties.TryGetValue("d",    out string rd)) sb.AppendLine($"    {bn}.RsaD  = {ToCs(rd.Trim())};");
            if (b.Properties.TryGetValue("oaep", out string ro) && ToCsBool(ro) == "true") sb.AppendLine($"    {bn}.RsaOAEP = true;");
        }
        else if (funcTypeName == "JWTEncode")
        {
            string inp = ResolveInp("input");
            if (inp != null) sb.AppendLine($"    {bn}.InputString = {ToCs(inp)};");
            if (b.Properties.TryGetValue("secret",    out string sec))  sb.AppendLine($"    {bn}.SecondInput  = {ToCs(sec.Trim())};");
            if (b.Properties.TryGetValue("algorithm", out string algo)) sb.AppendLine($"    {bn}.JwtAlgorithm = {ToCs(algo.Trim())};");
        }
        else if (funcTypeName == "MaxFloat"  || funcTypeName == "MinFloat"  ||
                 funcTypeName == "MaxInt"    || funcTypeName == "MinInt"    ||
                 funcTypeName == "IndexOf")
        {
            string inp = ResolveInp("input");
            if (inp != null) sb.AppendLine($"    {bn}.InputString = {ToCs(inp)};");
            if (b.Properties.TryGetValue("second", out string s2)) sb.AppendLine($"    {bn}.SecondInput = {ToCs(s2.Trim())};");
        }
        else if (funcTypeName == "GetKey" || funcTypeName == "RemoveByKey")
        {
            string dict = ResolveInp("dictionary");
            if (dict != null) sb.AppendLine($"    {bn}.InputString = {ToCs(dict)};");
            if (b.Properties.TryGetValue("key", out string gkey)) sb.AppendLine($"    {bn}.SecondInput = {ToCs(gkey.Trim())};");
        }
        else if (funcTypeName == "RandomFloat")
        {
            if (b.Properties.TryGetValue("minimum", out string mn)) sb.AppendLine($"    {bn}.RandomMin = {ToCs(mn.Trim())};");
            if (b.Properties.TryGetValue("maximum", out string mx)) sb.AppendLine($"    {bn}.RandomMax = {ToCs(mx.Trim())};");
        }
        else if (funcTypeName == "AddKeyValuePair")
        {
            string dict = ResolveInp("dictionary");
            if (dict != null) sb.AppendLine($"    {bn}.InputString = {ToCs(dict)};");
            if (b.Properties.TryGetValue("key",   out string k2)) sb.AppendLine($"    {bn}.SecondInput = {ToCs(k2.Trim())};");
            if (b.Properties.TryGetValue("value", out string v2)) sb.AppendLine($"    {bn}.ThirdInput  = {ToCs(v2.Trim())};");
        }
        else if (funcTypeName == "CreateListOfNumbers" || funcTypeName == "ListToDict")
        {
            string inp = ResolveInp("input");
            if (inp != null) sb.AppendLine($"    {bn}.InputString = {ToCs(inp)};");
            if (b.Properties.TryGetValue("count", out string cnt)) sb.AppendLine($"    {bn}.SecondInput = {ToCs(cnt.Trim())};");
            if (b.Properties.TryGetValue("step",  out string stp)) sb.AppendLine($"    {bn}.ThirdInput  = {ToCs(stp.Trim())};");
        }
        else if (funcTypeName == "BCryptVerify")
        {
            string pass = ResolveInp("password");
            if (pass != null) sb.AppendLine($"    {bn}.InputString = {ToCs(pass)};");
            if (b.Properties.TryGetValue("hash", out string bvH)) sb.AppendLine($"    {bn}.SecondInput = {ToCs(bvH.Trim())};");
        }
        else if (funcTypeName == "ScryptDeriveKey")
        {
            string pass = ResolveInp("password");
            if (pass != null) sb.AppendLine($"    {bn}.InputString = {ToCs(pass)};");
            if (b.Properties.TryGetValue("salt",   out string sdS))  sb.AppendLine($"    {bn}.SecondInput      = {ToCs(sdS.Trim())};");
            if (b.Properties.TryGetValue("n",      out string sdN))  sb.AppendLine($"    {bn}.ScryptCost       = {sdN.Trim().Trim('"')};");
            if (b.Properties.TryGetValue("r",      out string sdR))  sb.AppendLine($"    {bn}.ScryptBlockSize  = {sdR.Trim().Trim('"')};");
            if (b.Properties.TryGetValue("keyLen", out string sdKL)) sb.AppendLine($"    {bn}.ScryptOutputLength = {sdKL.Trim().Trim('"')};");
        }
        else if (funcTypeName == "AWS4Signature")
        {
            string sts = ResolveInp("stringToSign");
            if (sts != null) sb.AppendLine($"    {bn}.InputString = {ToCs(sts)};");
            if (b.Properties.TryGetValue("secretKey", out string awsSec)) sb.AppendLine($"    {bn}.SecondInput = {ToCs(awsSec.Trim())};");
            if (b.Properties.TryGetValue("date",      out string awsDt))  sb.AppendLine($"    {bn}.ThirdInput  = {ToCs(awsDt.Trim())};");
            if (b.Properties.TryGetValue("region",    out string awsRg))  sb.AppendLine($"    {bn}.AwsRegion   = {ToCs(awsRg.Trim())};");
            if (b.Properties.TryGetValue("service",   out string awsSv))  sb.AppendLine($"    {bn}.AwsService  = {ToCs(awsSv.Trim())};");
        }
        else if (funcTypeName == "RandomNum")
        {
            // OB2 uses minimum/maximum; BlockFunction uses RandomMin/RandomMax
            string minVal = b.Properties.TryGetValue("minimum", out string mn) ? mn
                          : b.Properties.GetValueOrDefault("min", "0");
            string maxVal = b.Properties.TryGetValue("maximum", out string mx) ? mx
                          : b.Properties.GetValueOrDefault("max", "0");
            string minClean = minVal.Trim().Trim('"');
            string maxClean = maxVal.Trim().Trim('"');
            sb.AppendLine($"    {bn}.RandomMin = {JsonConvert.SerializeObject(minClean)};");
            sb.AppendLine($"    {bn}.RandomMax = {JsonConvert.SerializeObject(maxClean)};");
            if (b.Properties.TryGetValue("randomZeroPad", out string zp))
                sb.AppendLine($"    {bn}.RandomZeroPad = {ToCsBool(zp)};");
        }
        else if (funcTypeName == "Translate")
        {
            if (b.Properties.TryGetValue("input", out string transInput))
            {
                string sbRef = transInput.Trim().StartsWith("@")
                    ? Ob2AtRefToSbRef(transInput.Trim())
                    : transInput.Trim();
                sb.AppendLine($"    {bn}.InputString = {ToCs(sbRef)};");
            }
            if (b.Properties.TryGetValue("translations", out string transDict))
                sb.AppendLine($"    {bn}.TranslationDictionary = {ToCsDict(transDict)};");
        }
        else if (b.Properties.TryGetValue("input", out string inputVal))
        {
            string sbRef = inputVal.Trim().StartsWith("@")
                ? Ob2AtRefToSbRef(inputVal.Trim())
                : inputVal.Trim();
            sb.AppendLine($"    {bn}.InputString = {ToCs(sbRef)};");

            if (b.Properties.TryGetValue("secondInput", out string secondInputVal))
                sb.AppendLine($"    {bn}.SecondInput = {ToCs(secondInputVal)};");
        }

        // Hash / HMAC: OB2 uses "hashFunction" property; default in BlockFunction is SHA512 so we must always set it.
        if (funcTypeName == "Hash" || funcTypeName == "HMAC")
        {
            if (b.Properties.TryGetValue("hashFunction", out string hf) && !string.IsNullOrWhiteSpace(hf))
                sb.AppendLine($"    {bn}.HashType = (RuriLib.Functions.Crypto.Hash)System.Enum.Parse(typeof(RuriLib.Functions.Crypto.Hash), {JsonConvert.SerializeObject(hf.Trim())}, true);");
        }
        // HMAC: OB2 uses "key" property + optional flags
        if (funcTypeName == "HMAC")
        {
            if (b.Properties.TryGetValue("key", out string hmacKeyVal) && !string.IsNullOrWhiteSpace(hmacKeyVal))
            {
                string kRef = hmacKeyVal.Trim().StartsWith("@")
                    ? Ob2AtRefToSbRef(hmacKeyVal.Trim())
                    : hmacKeyVal.Trim();
                sb.AppendLine($"    {bn}.HmacKey = {ToCs(kRef)};");
            }
            if (b.Properties.TryGetValue("keyBase64", out string kb64))
                sb.AppendLine($"    {bn}.KeyBase64 = {ToCsBool(kb64)};");
            if (b.Properties.TryGetValue("outputBase64", out string ob64) ||
                b.Properties.TryGetValue("hmacBase64",   out ob64))
                sb.AppendLine($"    {bn}.HmacBase64 = {ToCsBool(ob64)};");
        }
        // AESEncrypt / AESDecrypt: key, iv, mode, padding, hexKeys
        if (funcTypeName == "AESEncrypt" || funcTypeName == "AESDecrypt")
        {
            if (b.Properties.TryGetValue("key", out string aesKey))
                sb.AppendLine($"    {bn}.AesKey = {ToCs(aesKey)};");
            if (b.Properties.TryGetValue("iv", out string aesIV))
                sb.AppendLine($"    {bn}.AesIV = {ToCs(aesIV)};");
            if (b.Properties.TryGetValue("mode", out string aesMode) && !string.IsNullOrWhiteSpace(aesMode))
                sb.AppendLine($"    {bn}.AesMode = (System.Security.Cryptography.CipherMode)System.Enum.Parse(typeof(System.Security.Cryptography.CipherMode), {JsonConvert.SerializeObject(aesMode.Trim())}, true);");
            if (b.Properties.TryGetValue("padding", out string aesPad) && !string.IsNullOrWhiteSpace(aesPad))
                sb.AppendLine($"    {bn}.AesPadding = (System.Security.Cryptography.PaddingMode)System.Enum.Parse(typeof(System.Security.Cryptography.PaddingMode), {JsonConvert.SerializeObject(aesPad.Trim())}, true);");
            if (b.Properties.TryGetValue("hexKeys", out string hkeys))
                sb.AppendLine($"    {bn}.HexKeys = {ToCsBool(hkeys)};");
        }
        // RSAEncrypt: n (modulus), e (exponent), oaep
        if (funcTypeName == "RSAEncrypt")
        {
            if (b.Properties.TryGetValue("n", out string rsaN))
                sb.AppendLine($"    {bn}.RsaN = {ToCs(rsaN)};");
            if (b.Properties.TryGetValue("e", out string rsaE))
                sb.AppendLine($"    {bn}.RsaE = {ToCs(rsaE)};");
            if (b.Properties.TryGetValue("oaep", out string oaep))
                sb.AppendLine($"    {bn}.RsaOAEP = {ToCsBool(oaep)};");
        }
        // Split: separator, index, stringSplitOption
        if (funcTypeName == "Split")
        {
            if (b.Properties.TryGetValue("separator", out string sep))
                sb.AppendLine($"    {bn}.Separator = {ToCs(sep)};");
            if (b.Properties.TryGetValue("index", out string sidx) &&
                int.TryParse(sidx.Trim().Trim('"'), out int sidxInt))
                sb.AppendLine($"    {bn}.SplitIndex = {sidxInt};");
            if (b.Properties.TryGetValue("stringSplitOption", out string sso) && !string.IsNullOrWhiteSpace(sso))
                sb.AppendLine($"    {bn}.StringSplitOption = (System.StringSplitOptions)System.Enum.Parse(typeof(System.StringSplitOptions), {JsonConvert.SerializeObject(sso.Trim())}, true);");
        }
        // CharAt: index
        if (funcTypeName == "CharAt")
        {
            if (b.Properties.TryGetValue("index", out string cidx))
                sb.AppendLine($"    {bn}.CharIndex = {ToCs(cidx)};");
        }
        // SCrypt: method, salt, cost, blockSize, outputLength, base64Output, hashedPassword
        if (funcTypeName == "SCrypt")
        {
            if (b.Properties.TryGetValue("method", out string scMeth) && !string.IsNullOrWhiteSpace(scMeth))
                sb.AppendLine($"    {bn}.ScryptMeth = (RuriLib.BlockFunction.ScryptMethods)System.Enum.Parse(typeof(RuriLib.BlockFunction.ScryptMethods), {JsonConvert.SerializeObject(scMeth.Trim())}, true);");
            if (b.Properties.TryGetValue("salt", out string scSalt))
                sb.AppendLine($"    {bn}.ScryptSalt = {ToCs(scSalt)};");
            if (b.Properties.TryGetValue("cost", out string scCost) && int.TryParse(scCost.Trim(), out int scCostInt))
                sb.AppendLine($"    {bn}.ScryptCost = {scCostInt};");
            if (b.Properties.TryGetValue("blockSize", out string scBs) && int.TryParse(scBs.Trim(), out int scBsInt))
                sb.AppendLine($"    {bn}.ScryptBlockSize = {scBsInt};");
            if (b.Properties.TryGetValue("outputLength", out string scOl) && int.TryParse(scOl.Trim(), out int scOlInt))
                sb.AppendLine($"    {bn}.ScryptOutputLength = {scOlInt};");
            if (b.Properties.TryGetValue("base64Output", out string scB64))
                sb.AppendLine($"    {bn}.Base64Output = {ToCsBool(scB64)};");
            if (b.Properties.TryGetValue("hashedPassword", out string scHpw))
                sb.AppendLine($"    {bn}.ScryptHashedPassword = {ToCs(scHpw)};");
        }
        // BCrypt: method (BCryptHashGenSalt defaults to GenerateSalt), salt, workFactor, useWorkFactor, hashedPassword
        if (funcTypeName == "BCrypt")
        {
            string defaultMeth = b.BlockType.Equals("BCryptHashGenSalt", StringComparison.OrdinalIgnoreCase)
                ? "GenerateSalt" : "Encode";
            string methToUse = b.Properties.TryGetValue("method", out string bcM) && !string.IsNullOrWhiteSpace(bcM)
                ? bcM.Trim() : defaultMeth;
            sb.AppendLine($"    {bn}.BCryptMeth = (RuriLib.BlockFunction.BCryptMethods)System.Enum.Parse(typeof(RuriLib.BlockFunction.BCryptMethods), {JsonConvert.SerializeObject(methToUse)}, true);");
            if (b.Properties.TryGetValue("salt", out string bcSalt))
                sb.AppendLine($"    {bn}.BCryptSalt = {ToCs(bcSalt)};");
            if (b.Properties.TryGetValue("useWorkFactor", out string uwf))
                sb.AppendLine($"    {bn}.UseWorkFactor = {ToCsBool(uwf)};");
            if (b.Properties.TryGetValue("workFactor", out string wf) && int.TryParse(wf.Trim(), out int wfInt))
                sb.AppendLine($"    {bn}.BCryptWorkFactor = {wfInt};");
            if (b.Properties.TryGetValue("hashedPassword", out string bcHpw))
                sb.AppendLine($"    {bn}.BCryptHashedPassword = {ToCs(bcHpw)};");
        }

        if (b.OutputVar != null)
            sb.AppendLine($"    {bn}.VariableName = {JsonConvert.SerializeObject(b.OutputVar)};");

        if (!string.IsNullOrEmpty(b.Label))
            sb.AppendLine($"    {bn}.Label = {JsonConvert.SerializeObject(b.Label)};");

        sb.AppendLine($"    {bn}.IsCapture = {(b.IsCapture ? "true" : "false")};");
        sb.AppendLine($"    {bn}.Process(data._inner);");

        if (b.OutputVar != null)
        {
            string v = SafeId(b.OutputVar);
            sb.AppendLine($"    string {v} = data.GetVar({JsonConvert.SerializeObject(b.OutputVar)});");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    // Converts OB2 @ref → SilverBullet <ref> so BlockFunction.InputString resolves correctly.
    // @input.USERNAME → <USER>
    // @input.PASSWORD → <PASS>
    // @VARNAME        → <VARNAME>
    private static string Ob2AtRefToSbRef(string atRef)
    {
        if (string.IsNullOrEmpty(atRef) || !atRef.StartsWith("@")) return atRef;
        if (atRef.Equals("@input.USERNAME", StringComparison.OrdinalIgnoreCase)) return "<USER>";
        if (atRef.Equals("@input.PASSWORD", StringComparison.OrdinalIgnoreCase)) return "<PASS>";
        if (atRef.StartsWith("@input.", StringComparison.OrdinalIgnoreCase)) return "<" + atRef.Substring(7) + ">";
        if (atRef.StartsWith("@data.",  StringComparison.OrdinalIgnoreCase)) return "<" + atRef.Substring(6) + ">";
        return "<" + atRef.Substring(1) + ">";
    }

    // ─── Delay ───────────────────────────────────────────────────────────────

    private static string CompileDelay(LoliCodeSegment b)
    {
        string msRaw = b.Properties.GetValueOrDefault("milliseconds",
                        b.Properties.GetValueOrDefault("ms",
                        b.Properties.GetValueOrDefault("input", "0")));
        if (!int.TryParse(msRaw.Trim().Trim('"'), out int ms)) ms = 0;
        return $"System.Threading.Thread.Sleep({Math.Max(0, ms)});";
    }

    // ─── KeyCheck ────────────────────────────────────────────────────────────
    // Supports both formats:
    //   A) OB2 KEYCHAIN/STRINGKEY/REGEXKEY structure (parsed into b.KeyChains)
    //   B) Legacy simplified format: property keys success/fail/ban/retry = "term1, term2"

    private static string CompileKeyCheck(LoliCodeSegment b)
    {
        return b.KeyChains.Count > 0
            ? CompileKeyCheckFromChains(b)
            : CompileKeyCheckLegacy(b);
    }

    // ── Format A: full KEYCHAIN/STRINGKEY structure ──────────────────────────

    private static string CompileKeyCheckFromChains(LoliCodeSegment b)
    {
        string bn = NextBlock();
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"    var {bn} = new RuriLib.BlockKeycheck();");

        // Support both "banIfNoMatch" (serializer name) and "banOnToCheck" (legacy name).
        // Always set explicitly so the runtime value matches what was serialized.
        if (!b.Properties.TryGetValue("banIfNoMatch", out string botc) &&
            !b.Properties.TryGetValue("banOnToCheck", out botc))
            botc = null;
        if (botc != null)
            sb.AppendLine($"    {bn}.BanOnToCheck = {ToCsBool(botc)};");

        if (!b.Properties.TryGetValue("banOn4XX", out string bo4) &&
            !b.Properties.TryGetValue("banon4xx",  out bo4))
            bo4 = null;
        if (bo4 != null)
            sb.AppendLine($"    {bn}.BanOn4XX = {ToCsBool(bo4)};");

        if (!string.IsNullOrEmpty(b.Label))
            sb.AppendLine($"    {bn}.Label = {JsonConvert.SerializeObject(b.Label)};");

        int chainIdx = 0;
        foreach (var chain in b.KeyChains)
        {
            string cv = $"{bn}_c{chainIdx++}";

            string typeName = chain.ChainType switch {
                LoliCodeKeyChainType.SUCCESS   => "Success",
                LoliCodeKeyChainType.FAIL      => "Failure",
                LoliCodeKeyChainType.BAN       => "Ban",
                LoliCodeKeyChainType.RETRY     => "Retry",
                LoliCodeKeyChainType.CUSTOM    => "Custom",
                LoliCodeKeyChainType.TOCHECK   => "Custom",
                LoliCodeKeyChainType.EXPIRED   => "Custom",
                LoliCodeKeyChainType.TWOFACTOR => "Custom",
                _                              => "Failure",
            };
            // Auto-assign CustomType for pseudo-custom types so results display correctly
            if (chain.ChainType == LoliCodeKeyChainType.TOCHECK && string.IsNullOrEmpty(chain.CustomType))
                chain.CustomType = "TOCHECK";
            if (chain.ChainType == LoliCodeKeyChainType.EXPIRED && string.IsNullOrEmpty(chain.CustomType))
                chain.CustomType = "EXPIRED";
            if (chain.ChainType == LoliCodeKeyChainType.TWOFACTOR && string.IsNullOrEmpty(chain.CustomType))
                chain.CustomType = "2FACTOR";
            string modeName = chain.Mode == LoliCodeKeyMode.AND ? "AND" : "OR";

            sb.AppendLine($"    var {cv} = new RuriLib.Models.KeyChain {{");
            sb.AppendLine($"        Type = RuriLib.Models.KeyChain.KeychainType.{typeName},");
            sb.AppendLine($"        Mode = RuriLib.Models.KeyChain.KeychainMode.{modeName},");
            if (typeName == "Custom")
                sb.AppendLine($"        CustomType = {JsonConvert.SerializeObject(chain.CustomType)},");
            sb.AppendLine($"    }};");

            foreach (var key in chain.Keys)
            {
                string leftTerm  = Ob2RefToSbRef(key.LeftTerm);
                string comparer  = MapToSbComparer(key.Comparer);
                string rightTerm = JsonConvert.SerializeObject(key.RightTerm);
                sb.AppendLine($"    {cv}.Keys.Add(new RuriLib.Models.Key {{");
                sb.AppendLine($"        LeftTerm  = {JsonConvert.SerializeObject(leftTerm)},");
                sb.AppendLine($"        Comparer  = RuriLib.Functions.Conditions.Comparer.{comparer},");
                sb.AppendLine($"        RightTerm = {rightTerm},");
                sb.AppendLine($"    }});");
            }

            sb.AppendLine($"    {bn}.KeyChains.Add({cv});");
        }

        sb.AppendLine($"    {bn}.Process(data._inner);");
        sb.AppendLine("}");
        return sb.ToString();
    }

    // ── Format B: legacy simplified  ─────────────────────────────────────────
    // BLOCK:KeyCheck
    //   success = access_token, logged in
    //   ban     = banned, blocked
    //   retry   = rate limit
    // ENDBLOCK

    private static string CompileKeyCheckLegacy(LoliCodeSegment b)
    {
        string bn = NextBlock();
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"    var {bn} = new RuriLib.BlockKeycheck();");

        // Support both serializer name ("banIfNoMatch") and legacy name ("banOnToCheck")
        if (!b.Properties.TryGetValue("banIfNoMatch", out string botcL) &&
            !b.Properties.TryGetValue("banOnToCheck", out botcL))
            botcL = null;
        if (botcL != null)
            sb.AppendLine($"    {bn}.BanOnToCheck = {ToCsBool(botcL)};");
        if (!b.Properties.TryGetValue("banOn4XX", out string bo4L) &&
            !b.Properties.TryGetValue("banon4xx",  out bo4L))
            bo4L = null;
        if (bo4L != null)
            sb.AppendLine($"    {bn}.BanOn4XX = {ToCsBool(bo4L)};");

        var order = new[] {
            ("success", "Success"),
            ("ban",     "Ban"),
            ("retry",   "Retry"),
            ("fail",    "Failure"),
            ("custom",  "Custom"),
        };

        foreach (var (propKey, typeName) in order)
        {
            string[] aliases = propKey switch {
                "ban"  => new[] { "ban", "banned" },
                "fail" => new[] { "fail", "failure", "failed" },
                _      => new[] { propKey },
            };
            foreach (string alias in aliases)
            {
                if (!b.Properties.TryGetValue(alias, out string val) || string.IsNullOrWhiteSpace(val))
                    continue;

                var terms = val.Split(',')
                               .Select(t => t.Trim().Trim('"'))
                               .Where(t => !string.IsNullOrEmpty(t))
                               .ToList();
                if (terms.Count == 0) continue;

                string cv = $"{bn}_c{propKey}";
                sb.AppendLine($"    var {cv} = new RuriLib.Models.KeyChain {{");
                sb.AppendLine($"        Type = RuriLib.Models.KeyChain.KeychainType.{typeName},");
                sb.AppendLine($"        Mode = RuriLib.Models.KeyChain.KeychainMode.OR,");
                sb.AppendLine($"    }};");
                foreach (string term in terms)
                    sb.AppendLine($"    {cv}.Keys.Add(new RuriLib.Models.Key {{ LeftTerm = \"<SOURCE>\", Comparer = RuriLib.Functions.Conditions.Comparer.Contains, RightTerm = {JsonConvert.SerializeObject(term)} }});");
                sb.AppendLine($"    {bn}.KeyChains.Add({cv});");
                break;
            }
        }

        sb.AppendLine($"    {bn}.Process(data._inner);");
        sb.AppendLine("}");
        return sb.ToString();
    }

    // ─── Helper: OB2 @data.SOURCE → SilverBullet <SOURCE> ────────────────────
    // Also handles dict access: @data.COOKIES["name"] → <COOKIES(name)>

    private static string Ob2RefToSbRef(string ob2)
    {
        if (string.IsNullOrEmpty(ob2)) return ob2;
        var cookieM = System.Text.RegularExpressions.Regex.Match(ob2,
            @"^@data\.COOKIES\[""([^""]+)""\]$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (cookieM.Success) return $"<COOKIES({cookieM.Groups[1].Value})>";
        var headerM = System.Text.RegularExpressions.Regex.Match(ob2,
            @"^@data\.HEADERS\[""([^""]+)""\]$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (headerM.Success) return $"<HEADERS({headerM.Groups[1].Value})>";
        if (ob2.StartsWith("@data."))  return "<" + ob2.Substring(6) + ">";
        if (ob2.Equals("@input.USERNAME", StringComparison.OrdinalIgnoreCase)) return "<USER>";
        if (ob2.Equals("@input.PASSWORD", StringComparison.OrdinalIgnoreCase)) return "<PASS>";
        if (ob2.StartsWith("@input.")) return "<" + ob2.Substring(7) + ">";
        if (ob2.StartsWith("<"))       return ob2; // already SB form
        // User variable: @@VARNAME → <@VARNAME>  (var name starts with @)
        if (ob2.StartsWith("@@"))      return "<@" + ob2.Substring(2) + ">";
        // User variable: @VARNAME → <VARNAME>
        if (ob2.StartsWith("@"))       return "<" + ob2.Substring(1) + ">";
        return ob2;
    }

    // ─── Helper: OB2 comparer name → RuriLib.Functions.Conditions.Comparer ──

    private static string MapToSbComparer(string name) =>
        (name ?? "").ToLowerInvariant() switch {
            "contains"           => "Contains",
            "doesnotcontain"     => "DoesNotContain",
            "equalto" or "equal" => "EqualTo",
            "notequalto" or "notequal" => "NotEqualTo",
            "lessthan"           => "LessThan",
            "lessthanorequal"    => "LessThanOrEqual",
            "greaterthan"        => "GreaterThan",
            "greaterorequal"     => "GreaterOrEqual",
            "matchesregex"       => "MatchesRegex",
            "doesnotmatchregex"  => "DoesNotMatchRegex",
            "exists"             => "Exists",
            "doesnotexist"       => "DoesNotExist",
            "startswith"         => "StartsWith",
            "endswith"           => "EndsWith",
            _                    => "Contains",
        };

    // ─── Misc Utility (OB2 → BlockUtility.Misc) ─────────────────────────────

    private static string CompileOb2MiscUtility(LoliCodeSegment b, string actionName)
    {
        string bn = NextBlock();
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"    var {bn} = new RuriLib.BlockUtility();");
        sb.AppendLine($"    {bn}.Group = RuriLib.UtilityGroup.Misc;");
        sb.AppendLine($"    {bn}.MiscAction = RuriLib.MiscAction.{actionName};");

        if (b.OutputVar != null)
        {
            sb.AppendLine($"    {bn}.VariableName = {JsonConvert.SerializeObject(b.OutputVar)};");
            sb.AppendLine($"    {bn}.IsCapture = {(b.IsCapture ? "true" : "false")};");
        }
        if (!string.IsNullOrEmpty(b.Label))
            sb.AppendLine($"    {bn}.Label = {JsonConvert.SerializeObject(b.Label)};");

        sb.AppendLine($"    {bn}.Process(data._inner);");

        if (b.OutputVar != null)
        {
            string v = SafeId(b.OutputVar);
            sb.AppendLine($"    string {v} = data.GetVar({JsonConvert.SerializeObject(b.OutputVar)});");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    // ─── Conversion Utility (OB2 → BlockUtility.Conversion extended) ─────────

    private static string CompileOb2ConversionUtility(LoliCodeSegment b, string actionName)
    {
        string bn = NextBlock();
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"    var {bn} = new RuriLib.BlockUtility();");
        sb.AppendLine($"    {bn}.Group = RuriLib.UtilityGroup.Conversion;");
        sb.AppendLine($"    {bn}.ConversionAct = RuriLib.ConversionAction.{actionName};");

        // Resolve OB2 input property → InputString
        string GetConvInput(params string[] keys)
        {
            foreach (var k in keys)
                if (b.Properties.TryGetValue(k, out string v))
                {
                    string t = v.Trim();
                    return t.StartsWith("@") ? Ob2AtRefToSbRef(t) : t;
                }
            return null;
        }

        switch (actionName)
        {
        case "BigIntegerToByteArray":
        {
            string inp = GetConvInput("bigInteger", "input");
            if (inp != null) sb.AppendLine($"    {bn}.InputString = {ToCs(inp)};");
            break;
        }
        case "ByteArrayToBigInteger":
        case "BytesToBase64":
        case "BytesToBinaryString":
        case "BytesToHex":
        {
            string inp = GetConvInput("bytes", "input");
            if (inp != null) sb.AppendLine($"    {bn}.InputString = {ToCs(inp)};");
            break;
        }
        case "BytesToString":
        {
            string inp = GetConvInput("input", "bytes");
            if (inp != null) sb.AppendLine($"    {bn}.InputString = {ToCs(inp)};");
            if (b.Properties.TryGetValue("encoding", out string bsEnc))
                sb.AppendLine($"    {bn}.ByteStringEncoding = {ToCs(bsEnc.Trim().Trim('"'))};");
            break;
        }
        case "StringToBytes":
        {
            string inp = GetConvInput("input", "str");
            if (inp != null) sb.AppendLine($"    {bn}.InputString = {ToCs(inp)};");
            if (b.Properties.TryGetValue("encoding", out string sbEnc))
                sb.AppendLine($"    {bn}.ByteStringEncoding = {ToCs(sbEnc.Trim().Trim('"'))};");
            break;
        }
        case "Base64ToBytes":
        {
            string inp = GetConvInput("base64String", "input");
            if (inp != null) sb.AppendLine($"    {bn}.InputString = {ToCs(inp)};");
            break;
        }
        case "HexToBytes":
        {
            string inp = GetConvInput("hexString", "input");
            if (inp != null) sb.AppendLine($"    {bn}.InputString = {ToCs(inp)};");
            break;
        }
        case "Base64ToUtf8":
        case "BinaryStringToBytes":
        case "Utf8ToBase64":
        {
            string inp = GetConvInput("input", "binaryString");
            if (inp != null) sb.AppendLine($"    {bn}.InputString = {ToCs(inp)};");
            break;
        }
        case "ReadableSize":
        {
            string inp = GetConvInput("input");
            if (inp != null) sb.AppendLine($"    {bn}.InputString = {ToCs(inp)};");
            if (b.Properties.TryGetValue("outputBits", out string obits))
                sb.AppendLine($"    {bn}.ReadableSizeOutputBits = {ToCsBool(obits)};");
            if (b.Properties.TryGetValue("binaryUnit", out string bunit))
                sb.AppendLine($"    {bn}.ReadableSizeBinaryUnit = {ToCsBool(bunit)};");
            if (b.Properties.TryGetValue("decimalPlaces", out string dplaces))
                sb.AppendLine($"    {bn}.ReadableSizeDecimalPlaces = {ToCs(dplaces.Trim().Trim('"'))};");
            break;
        }
        }

        if (b.OutputVar != null)
        {
            sb.AppendLine($"    {bn}.VariableName = {JsonConvert.SerializeObject(b.OutputVar)};");
            sb.AppendLine($"    {bn}.IsCapture = {(b.IsCapture ? "true" : "false")};");
        }
        if (!string.IsNullOrEmpty(b.Label))
            sb.AppendLine($"    {bn}.Label = {JsonConvert.SerializeObject(b.Label)};");

        sb.AppendLine($"    {bn}.Process(data._inner);");

        if (b.OutputVar != null)
        {
            string v = SafeId(b.OutputVar);
            sb.AppendLine($"    string {v} = data.GetVar({JsonConvert.SerializeObject(b.OutputVar)});");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    // ─── Images Utility (OB2 → BlockUtility.Images) ──────────────────────────

    private static string CompileOb2ImagesUtility(LoliCodeSegment b, string actionName)
    {
        string bn = NextBlock();
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"    var {bn} = new RuriLib.BlockUtility();");
        sb.AppendLine($"    {bn}.Group = RuriLib.UtilityGroup.Images;");
        sb.AppendLine($"    {bn}.ImageAct = RuriLib.ImageAction.{actionName};");

        if (actionName == "SvgToPng")
        {
            if (b.Properties.TryGetValue("xml", out string xmlVal))
            {
                string xmlRef = xmlVal.Trim().StartsWith("@") ? Ob2AtRefToSbRef(xmlVal.Trim()) : xmlVal.Trim();
                sb.AppendLine($"    {bn}.InputString = {ToCs(xmlRef)};");
            }
            if (b.Properties.TryGetValue("width", out string svgW))
                sb.AppendLine($"    {bn}.ImageSvgWidth = {JsonConvert.SerializeObject(svgW.Trim().Trim('"'))};");
            if (b.Properties.TryGetValue("height", out string svgH))
                sb.AppendLine($"    {bn}.ImageSvgHeight = {JsonConvert.SerializeObject(svgH.Trim().Trim('"'))};");
        }

        if (b.OutputVar != null)
        {
            sb.AppendLine($"    {bn}.VariableName = {JsonConvert.SerializeObject(b.OutputVar)};");
            sb.AppendLine($"    {bn}.IsCapture = {(b.IsCapture ? "true" : "false")};");
        }
        if (!string.IsNullOrEmpty(b.Label))
            sb.AppendLine($"    {bn}.Label = {JsonConvert.SerializeObject(b.Label)};");

        sb.AppendLine($"    {bn}.Process(data._inner);");

        if (b.OutputVar != null)
        {
            string v = SafeId(b.OutputVar);
            sb.AppendLine($"    string {v} = data.GetVar({JsonConvert.SerializeObject(b.OutputVar)});");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    // ─── Utility ─────────────────────────────────────────────────────────────

    private static string CompileUtility(LoliCodeSegment b)
    {
        string bn = NextBlock();
        var sb = new StringBuilder();
        sb.AppendLine("{");

        // The serializer stores the full LoliScript line as property "ls" so BlockParser
        // can reconstruct the block with all its properties intact.
        if (b.Properties.TryGetValue("ls", out string lsCode))
        {
            string csStr = ToCs(lsCode);
            sb.AppendLine($"    var {bn} = (RuriLib.BlockUtility)RuriLib.LS.BlockParser.Parse({csStr});");
            if (!string.IsNullOrEmpty(b.Label))
                sb.AppendLine($"    if ({bn} != null) {bn}.Label = {JsonConvert.SerializeObject(b.Label)};");
            sb.AppendLine($"    if ({bn} != null && !{bn}.Disabled) {bn}.Process(data._inner);");
        }
        else
        {
            // Fallback for manually written BLOCK:Utility without "ls" property
            sb.AppendLine($"    var {bn} = new RuriLib.BlockUtility();");
            if (!string.IsNullOrEmpty(b.Label))
                sb.AppendLine($"    {bn}.Label = {JsonConvert.SerializeObject(b.Label)};");
            sb.AppendLine($"    {bn}.Process(data._inner);");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    // ─── TCP ─────────────────────────────────────────────────────────────────

    private static string CompileTcp(LoliCodeSegment b)
    {
        string bn = NextBlock();
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"    var {bn} = new RuriLib.BlockTCP();");

        foreach (var kv in b.Properties)
        {
            string key = kv.Key.ToLower();
            string val = kv.Value;
            switch (key)
            {
                case "host":      sb.AppendLine($"    {bn}.Host = {ToCs(val)};"); break;
                case "port":      sb.AppendLine($"    {bn}.Port = {ToCs(val)};"); break;
                case "message":   sb.AppendLine($"    {bn}.Message = {ToCs(val)};"); break;
                case "ssl":       sb.AppendLine($"    {bn}.SSL = {ToCsBool(val)};"); break;
            }
        }

        if (b.OutputVar != null)
        {
            sb.AppendLine($"    {bn}.VariableName = {JsonConvert.SerializeObject(b.OutputVar)};");
            sb.AppendLine($"    {bn}.IsCapture = {(b.IsCapture ? "true" : "false")};");
        }

        if (!string.IsNullOrEmpty(b.Label))
            sb.AppendLine($"    {bn}.Label = {JsonConvert.SerializeObject(b.Label)};");

        sb.AppendLine($"    {bn}.Process(data._inner);");

        if (b.OutputVar != null)
        {
            string v = SafeId(b.OutputVar);
            sb.AppendLine($"    string {v} = data.GetVar({JsonConvert.SerializeObject(b.OutputVar)});");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    // ─── WebSocket ───────────────────────────────────────────────────────────

    private static string CompileWebSocket(LoliCodeSegment b)
    {
        string bn = NextBlock();
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"    var {bn} = new RuriLib.BlockWebSocket();");

        foreach (var kv in b.Properties)
        {
            string key = kv.Key.ToLower();
            string val = kv.Value;
            switch (key)
            {
                case "url":     sb.AppendLine($"    {bn}.Url = {ToCs(val)};"); break;
                case "message": sb.AppendLine($"    {bn}.Message = {ToCs(val)};"); break;
            }
        }

        if (b.OutputVar != null)
        {
            sb.AppendLine($"    {bn}.VariableName = {JsonConvert.SerializeObject(b.OutputVar)};");
            sb.AppendLine($"    {bn}.IsCapture = {(b.IsCapture ? "true" : "false")};");
        }

        if (!string.IsNullOrEmpty(b.Label))
            sb.AppendLine($"    {bn}.Label = {JsonConvert.SerializeObject(b.Label)};");

        sb.AppendLine($"    {bn}.Process(data._inner);");

        if (b.OutputVar != null)
        {
            string v = SafeId(b.OutputVar);
            sb.AppendLine($"    string {v} = data.GetVar({JsonConvert.SerializeObject(b.OutputVar)});");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    // ─── DnsLookup ───────────────────────────────────────────────────────────────

    private static string CompileDns(LoliCodeSegment b)
    {
        string bn = NextBlock();
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"    var {bn} = new RuriLib.BlockDns();");

        foreach (var kv in b.Properties)
        {
            string key = kv.Key.ToLower();
            string val = kv.Value.Trim();
            switch (key)
            {
                case "query":
                    sb.AppendLine($"    {bn}.Query = {ToCs(kv.Value)};");
                    break;
                case "recordtype":
                    if (Enum.TryParse<RuriLib.DnsRecordType>(val, true, out var drt))
                        sb.AppendLine($"    {bn}.RecordType = RuriLib.DnsRecordType.{drt};");
                    break;
                case "transport":
                    if (Enum.TryParse<RuriLib.DnsTransport>(val, true, out var dtr))
                        sb.AppendLine($"    {bn}.Transport = RuriLib.DnsTransport.{dtr};");
                    break;
                case "server":
                    sb.AppendLine($"    {bn}.Server = {ToCs(kv.Value)};");
                    break;
                case "timeoutmilliseconds":
                    if (int.TryParse(val, out int tms))
                        sb.AppendLine($"    {bn}.TimeoutMs = {tms};");
                    break;
            }
        }

        if (b.OutputVar != null)
        {
            sb.AppendLine($"    {bn}.VariableName = {JsonConvert.SerializeObject(b.OutputVar)};");
            sb.AppendLine($"    {bn}.IsCapture = {(b.IsCapture ? "true" : "false")};");
        }

        if (!string.IsNullOrEmpty(b.Label))
            sb.AppendLine($"    {bn}.Label = {JsonConvert.SerializeObject(b.Label)};");

        sb.AppendLine($"    {bn}.Process(data._inner);");
        sb.AppendLine("}");
        return sb.ToString();
    }

    // ─── Translate ───────────────────────────────────────────────────────────
    // Translate is a BlockFunction type in SilverBullet; compile it via function block
    // but also support the inline dictionary path for configs that write translations
    // directly as OB2 ${()} dict.

    private static string CompileTranslate(LoliCodeSegment b)
    {
        // If no translations dict is present, fall back to the function block path
        if (!b.Properties.ContainsKey("translations"))
            return CompileOb2FunctionBlock(b, "Translate");

        string bn = NextBlock();
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"    var {bn} = new RuriLib.BlockFunction();");
        sb.AppendLine($"    {bn}.FunctionType = RuriLib.BlockFunction.Function.Translate;");

        if (b.Properties.TryGetValue("input", out string transInput))
        {
            string sbRef = transInput.Trim().StartsWith("@") ? Ob2AtRefToSbRef(transInput.Trim()) : transInput.Trim();
            sb.AppendLine($"    {bn}.InputString = {ToCs(sbRef)};");
        }
        sb.AppendLine($"    {bn}.TranslationDictionary = {ToCsDict(b.Properties["translations"])};");

        if (b.OutputVar != null) sb.AppendLine($"    {bn}.VariableName = {JsonConvert.SerializeObject(b.OutputVar)};");
        if (!string.IsNullOrEmpty(b.Label)) sb.AppendLine($"    {bn}.Label = {JsonConvert.SerializeObject(b.Label)};");
        sb.AppendLine($"    {bn}.IsCapture = {(b.IsCapture ? "true" : "false")};");
        sb.AppendLine($"    {bn}.Process(data._inner);");

        if (b.OutputVar != null)
        {
            string v = SafeId(b.OutputVar);
            sb.AppendLine($"    string {v} = data.GetVar({JsonConvert.SerializeObject(b.OutputVar)});");
        }
        sb.AppendLine("}");
        return sb.ToString();
    }

    // ─── RandomUserAgent ─────────────────────────────────────────────────────
    // OB2: BLOCK:RandomUserAgent  platform = Desktop  => VAR @ua  ENDBLOCK
    // Maps to BlockFunction.Function.GetRandomUA in SilverBullet.

    private static string CompileRandomUserAgent(LoliCodeSegment b)
    {
        string bn = NextBlock();
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"    var {bn} = new RuriLib.BlockFunction();");
        sb.AppendLine($"    {bn}.FunctionType = RuriLib.BlockFunction.Function.GetRandomUA;");

        if (b.OutputVar != null) sb.AppendLine($"    {bn}.VariableName = {JsonConvert.SerializeObject(b.OutputVar)};");
        if (!string.IsNullOrEmpty(b.Label)) sb.AppendLine($"    {bn}.Label = {JsonConvert.SerializeObject(b.Label)};");
        sb.AppendLine($"    {bn}.IsCapture = {(b.IsCapture ? "true" : "false")};");
        sb.AppendLine($"    {bn}.Process(data._inner);");

        if (b.OutputVar != null)
        {
            string v = SafeId(b.OutputVar);
            sb.AppendLine($"    string {v} = data.GetVar({JsonConvert.SerializeObject(b.OutputVar)});");
        }
        sb.AppendLine("}");
        return sb.ToString();
    }

    // ─── Length ──────────────────────────────────────────────────────────────
    // OB2: BLOCK:Length  input = $"<text>"  => VAR @length  ENDBLOCK
    // Returns the character count of the input string.

    private static string CompileLength(LoliCodeSegment b)
    {
        string rawInput = b.Properties.GetValueOrDefault("input", "\"\"");
        string sbRef    = rawInput.Trim().StartsWith("@") ? Ob2AtRefToSbRef(rawInput.Trim()) : rawInput.Trim();
        string csInput  = ToCs(sbRef);
        string cap      = b.IsCapture ? "true" : "false";

        if (b.OutputVar != null)
        {
            string v = SafeId(b.OutputVar);
            return $"string {v} = ({csInput}).Length.ToString();\n" +
                   $"data.Variables.Set(new RuriLib.Models.CVar(\"{b.OutputVar}\", {v}, {cap}));\n";
        }
        return $"/* Length with no output var: {csInput} */\n";
    }

    // ─── CurrentUnixTime ─────────────────────────────────────────────────────
    // OB2: BLOCK:CurrentUnixTime  useUtc = True  => VAR @unixNow  ENDBLOCK

    private static string CompileCurrentUnixTime(LoliCodeSegment b)
    {
        string cap = b.IsCapture ? "true" : "false";
        if (b.OutputVar != null)
        {
            string v = SafeId(b.OutputVar);
            return $"string {v} = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();\n" +
                   $"data.Variables.Set(new RuriLib.Models.CVar(\"{b.OutputVar}\", {v}, {cap}));\n";
        }
        return "/* CurrentUnixTime with no output var */\n";
    }

    // ─── DateToUnixTime ──────────────────────────────────────────────────────
    // OB2: BLOCK:DateToUnixTime  datetime = @Expiry  format = "yyyy-MM-dd"  => VAR @unixExp  ENDBLOCK

    private static string CompileDateToUnixTime(LoliCodeSegment b)
    {
        string rawInput = b.Properties.TryGetValue("datetime", out string dt) ? dt
                        : b.Properties.GetValueOrDefault("input", "\"\"");
        string format   = b.Properties.GetValueOrDefault("format", "\"yyyy-MM-dd HH:mm:ss\"");

        string sbRef   = rawInput.Trim().StartsWith("@") ? Ob2AtRefToSbRef(rawInput.Trim()) : rawInput.Trim();
        string csInput = ToCs(sbRef);
        string csFormat = ToCs(format);
        string cap = b.IsCapture ? "true" : "false";

        if (b.OutputVar != null)
        {
            string v  = SafeId(b.OutputVar);
            string bn = NextBlock();
            return $"string {v} = \"0\";\n" +
                   $"{{ if (System.DateTime.TryParseExact({csInput}, {csFormat}, " +
                   $"System.Globalization.CultureInfo.InvariantCulture, " +
                   $"System.Globalization.DateTimeStyles.None, out System.DateTime __dt_{bn})) " +
                   $"{{ {v} = new System.DateTimeOffset(__dt_{bn}, System.TimeSpan.Zero).ToUnixTimeSeconds().ToString(); }} }}\n" +
                   $"data.Variables.Set(new RuriLib.Models.CVar(\"{b.OutputVar}\", {v}, {cap}));\n";
        }
        return "/* DateToUnixTime with no output var */\n";
    }

    // ─── Compute ─────────────────────────────────────────────────────────────
    // OB2: BLOCK:Compute  input = $"(<a>-<b>)/86400"  => VAR @result  ENDBLOCK
    // Evaluates a mathematical expression (OB2 uses NCalc; we use DataTable.Compute).

    private static string CompileCompute(LoliCodeSegment b)
    {
        string rawInput = b.Properties.GetValueOrDefault("input", "\"0\"");
        string sbRef    = rawInput.Trim().StartsWith("@") ? Ob2AtRefToSbRef(rawInput.Trim()) : rawInput.Trim();
        string csInput  = ToCs(sbRef);
        string cap      = b.IsCapture ? "true" : "false";
        string bn       = NextBlock();

        if (b.OutputVar != null)
        {
            string v = SafeId(b.OutputVar);
            return $"string {v} = \"0\";\n" +
                   $"{{ try {{ var _dt_{bn} = new System.Data.DataTable();\n" +
                   $"  {v} = _dt_{bn}.Compute({csInput}, \"\")?.ToString() ?? \"0\"; }}\n" +
                   $"  catch {{ {v} = \"0\"; }} }}\n" +
                   $"data.Variables.Set(new RuriLib.Models.CVar(\"{b.OutputVar}\", {v}, {cap}));\n";
        }
        return "/* Compute with no output var */\n";
    }

    // ─── RoundToInteger ──────────────────────────────────────────────────────
    // OB2: BLOCK:RoundToInteger  input = @computeOutput  => CAP @DaysLeft  ENDBLOCK

    private static string CompileRoundToInteger(LoliCodeSegment b)
    {
        string rawInput = b.Properties.GetValueOrDefault("input", "\"0\"");
        string sbRef    = rawInput.Trim().StartsWith("@") ? Ob2AtRefToSbRef(rawInput.Trim()) : rawInput.Trim();
        string csInput  = ToCs(sbRef);
        string cap      = b.IsCapture ? "true" : "false";

        if (b.OutputVar != null)
        {
            string v  = SafeId(b.OutputVar);
            string bn = NextBlock();
            return $"string {v} = \"0\";\n" +
                   $"{{ if (double.TryParse({csInput}, System.Globalization.NumberStyles.Float, " +
                   $"System.Globalization.CultureInfo.InvariantCulture, out double __rd_{bn})) " +
                   $"{{ {v} = ((long)System.Math.Round(__rd_{bn})).ToString(); }} }}\n" +
                   $"data.Variables.Set(new RuriLib.Models.CVar(\"{b.OutputVar}\", {v}, {cap}));\n";
        }
        return "/* RoundToInteger with no output var */\n";
    }

    // ─── Reflection fallback (unknown block types) ────────────────────────────

    private static string CompileReflection(LoliCodeSegment b)
    {
        // Try to find the block class by reflection at runtime
        string bn = NextBlock();
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"    // Unknown block type '{b.BlockType}' — attempting reflection");
        sb.AppendLine($"    var {bn}Type = System.AppDomain.CurrentDomain.GetAssemblies()");
        sb.AppendLine($"        .SelectMany(a => {{ try {{ return a.GetTypes(); }} catch {{ return System.Array.Empty<System.Type>(); }} }})");
        sb.AppendLine($"        .FirstOrDefault(t => t.Name == \"Block{b.BlockType}\" || t.Name == \"{b.BlockType}\");");
        sb.AppendLine($"    if ({bn}Type != null)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        var {bn} = (RuriLib.BlockBase)System.Activator.CreateInstance({bn}Type);");
        foreach (var kv in b.Properties)
        {
            string safeProp = SafeId(kv.Key);
            sb.AppendLine($"        try {{ var _pi = {bn}Type.GetProperty(\"{safeProp}\", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);");
            sb.AppendLine($"              if (_pi != null) _pi.SetValue({bn}, System.Convert.ChangeType({ToCs(kv.Value)}, _pi.PropertyType)); }} catch {{}}");
        }
        if (!string.IsNullOrEmpty(b.Label))
            sb.AppendLine($"        {bn}.Label = {JsonConvert.SerializeObject(b.Label)};");
        if (b.OutputVar != null)
        {
            // Set VariableName and IsCapture via reflection so Process() stores the
            // result under the correct CVar name (OB2 => VAR @name / => CAP @name).
            sb.AppendLine($"        try {{ var _pVN = {bn}Type.GetProperty(\"VariableName\", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);");
            sb.AppendLine($"              if (_pVN != null) _pVN.SetValue({bn}, {JsonConvert.SerializeObject(b.OutputVar)}); }} catch {{}}");
            sb.AppendLine($"        try {{ var _pIC = {bn}Type.GetProperty(\"IsCapture\", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);");
            sb.AppendLine($"              if (_pIC != null) _pIC.SetValue({bn}, (object){(b.IsCapture ? "true" : "false")}); }} catch {{}}");
        }
        sb.AppendLine($"        {bn}.Process(data._inner);");
        sb.AppendLine($"    }}");
        sb.AppendLine($"    else log(\"[LoliCode] Unknown block: {b.BlockType}\");");
        sb.AppendLine("}");
        return sb.ToString();
    }

    // ─── Value translation helpers ────────────────────────────────────────────

    /// <summary>
    /// Converts an OB2 property value to a C# expression.
    /// • $"text &lt;varName&gt;" → C# interpolated string, with __rv() wrapper for complex &lt;vars&gt;
    /// • ${("k","v"),...}       → Dictionary&lt;string,string&gt; initializer
    /// • $["a","b",...]         → List&lt;string&gt; initializer
    /// • true/false             → lowercase bool literal
    /// • integer/float          → numeric literal
    /// • bare word              → quoted string constant
    /// </summary>
    public static string ToCs(string value)
    {
        if (value == null) return "\"\"";
        value = value.Trim();

        // Template string: $"..."
        if (value.StartsWith("$\"") && value.EndsWith("\"") && value.Length >= 3)
        {
            string inner = value.Substring(2, value.Length - 3);
            return TranslateTemplateString(inner);
        }

        // Quoted plain string (no $): "..."
        if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
        {
            // Return as C# string literal (already quoted)
            return value;
        }

        // OB2 Dictionary literal: ${("k","v"),...}
        if (value.StartsWith("${") && value.EndsWith("}"))
            return ToCsDict(value);

        // OB2 List literal: $["a","b"]
        if (value.StartsWith("$[") && value.EndsWith("]"))
            return ToCsList(value);

        // Boolean
        if (value.Equals("true",  StringComparison.OrdinalIgnoreCase)) return "true";
        if (value.Equals("false", StringComparison.OrdinalIgnoreCase)) return "false";

        // Integer
        if (Regex.IsMatch(value, @"^-?\d+$")) return value;

        // Float
        if (Regex.IsMatch(value, @"^-?\d+\.\d+$")) return value + "d";

        // Bare word / enum value → quoted string
        return JsonConvert.SerializeObject(value);
    }

    /// <summary>
    /// Translates an OB2 template string interior (inside $"...") into a C# expression.
    ///
    /// OB2 uses \"  for literal " inside $"..." (same as C# string escaping).
    /// OB2 uses &lt;varName&gt; for BotData variable substitution.
    /// Users may also write {varName} for C# local-variable interpolation.
    ///
    /// Strategy:
    ///   1. Unescape OB2 sequences (\"→", \\→\) to get plain text.
    ///   2. Pure literal  → JsonConvert.SerializeObject (safe C# string literal).
    ///   3. Only OB2 refs → __rv(JsonConvert.SerializeObject(plain)).
    ///   4. Has {varName} interp → $"..." with literal { } escaped as {{ }}.
    /// </summary>
    private static string TranslateTemplateString(string inner)
    {
        // Step 1 — unescape OB2 escape sequences (\"→", \\→\)
        string plain = UnescapeOb2(inner);

        // Step 1b — promote C# local variable refs: <varName> → {varName} for any variable
        // declared in a preceding code segment. This allows raw C# code that computes
        // string frameId = "..." to be referenced as <frameId> in block URL/header templates
        // without needing an explicit data.SetVar("frameId", frameId) call.
        if (_knownLocals.Count > 0)
        {
            plain = Regex.Replace(plain, @"<([A-Za-z_][A-Za-z0-9_]*)>", m =>
                _knownLocals.Contains(m.Groups[1].Value) ? $"{{{m.Groups[1].Value}}}" : m.Value);
        }

        bool hasOb2Refs  = Regex.IsMatch(plain, @"<[^>]+>");
        bool hasCsInterp = Regex.IsMatch(plain, @"\{[A-Za-z_][A-Za-z0-9_]*\}");

        // ── Pure literal (no variable refs of any kind) ──────────────────────
        if (!hasOb2Refs && !hasCsInterp)
            return JsonConvert.SerializeObject(plain);   // produces "escaped literal"

        // ── Only OB2 refs, no C# interpolation ───────────────────────────────
        if (hasOb2Refs && !hasCsInterp)
            return $"__rv({JsonConvert.SerializeObject(plain)})";

        // ── Has C# {varName} interpolation ────────────────────────────────────
        // Convert simple OB2 <varName> → {varName} for C# interpolation
        string withVars = hasOb2Refs
            ? Regex.Replace(plain, @"<([A-Za-z_][A-Za-z0-9_]*)>", m => "{" + m.Groups[1].Value + "}")
            : plain;

        bool hasComplexOb2Remaining = Regex.IsMatch(withVars, @"<[^>]+>");

        // Collect distinct {identifier} and {obj.prop} patterns (longest first to avoid substring collisions)
        var interpMatches = Regex.Matches(withVars, @"\{([A-Za-z_][A-Za-z0-9_]*)\}")
                                 .Cast<Match>()
                                 .Select(m => m.Value)
                                 .Distinct()
                                 .OrderByDescending(s => s.Length)
                                 .ToList();

        // Replace each {identifier} with a sentinel so we can safely escape literal { }
        string work = withVars;
        var sentinelMap = new Dictionary<string, string>();
        int sentIdx = 0;
        foreach (string interp in interpMatches)
        {
            string sentinel = $"\x01{sentIdx++}\x01";
            sentinelMap[sentinel] = interp;
            work = work.Replace(interp, sentinel);
        }

        // Escape remaining literal braces (JSON structure, not C# interpolation)
        work = work.Replace("{", "{{").Replace("}", "}}");

        // Restore C# interpolation expressions
        foreach (var kv in sentinelMap)
            work = work.Replace(kv.Key, kv.Value);

        // Escape for C# string literal content: \ → \\, " → \"
        string escaped = work.Replace("\\", "\\\\").Replace("\"", "\\\"");

        return hasComplexOb2Remaining
            ? $"__rv($\"{escaped}\")"
            : $"$\"{escaped}\"";
    }

    /// <summary>
    /// Parses OB2 Dictionary literal ${("k1","v1"),("k2","v2")} into a C# expression.
    /// Also handles plain {{"k","v"}} style if user writes it that way.
    /// </summary>
    private static string ToCsDict(string value)
    {
        var pairs = new List<(string key, string val)>();

        // Strip outer ${...} or {...}
        string inner = value;
        if (inner.StartsWith("${")) inner = inner.Substring(2, inner.Length - 3);
        else if (inner.StartsWith("{")) inner = inner.Substring(1, inner.Length - 2);

        // Extract ("key","value") tuples — values may contain OB2-escaped quotes like \"
        foreach (Match m in Regex.Matches(inner, @"\(\s*""((?:[^""\\]|\\.)*)""\s*,\s*""((?:[^""\\]|\\.)*)""\s*\)"))
        {
            string k = UnescapeOb2(m.Groups[1].Value);
            string v = UnescapeOb2(m.Groups[2].Value);
            pairs.Add((k, v));
        }

        if (pairs.Count == 0)
            return "new System.Collections.Generic.Dictionary<string,string>();";

        var sb = new StringBuilder();
        sb.Append("new System.Collections.Generic.Dictionary<string,string>(System.StringComparer.OrdinalIgnoreCase) {");
        foreach (var (k, v) in pairs)
        {
            // Promote C# local variable refs: <varName> → {varName} for known locals.
            // This mirrors TranslateTemplateString so headers like X-Nonce-17: <proofSig>
            // compile as {proofSig} C# interpolation instead of __rv CVar lookup.
            string promoted = _knownLocals.Count > 0
                ? Regex.Replace(v, @"<([A-Za-z_][A-Za-z0-9_]*)>", m =>
                    _knownLocals.Contains(m.Groups[1].Value) ? $"{{{m.Groups[1].Value}}}" : m.Value)
                : v;

            bool hasOb2Refs  = Regex.IsMatch(promoted, @"<[^>]+>");
            bool hasCsInterp = Regex.IsMatch(promoted, @"\{[A-Za-z_][A-Za-z0-9_]*\}");

            string valExpr;
            if (!hasOb2Refs && !hasCsInterp)
            {
                // Plain literal — no variable substitution needed
                valExpr = JsonConvert.SerializeObject(promoted);
            }
            else if (hasCsInterp)
            {
                // Build a C# interpolated string $"..." with safe brace escaping
                var interps = Regex.Matches(promoted, @"\{[A-Za-z_][A-Za-z0-9_]*\}")
                    .Cast<Match>().Select(m => m.Value).Distinct()
                    .OrderByDescending(s => s.Length).ToList();
                string work = promoted;
                var sents = new Dictionary<string, string>();
                int si = 0;
                foreach (string interp in interps)
                {
                    string sentinel = $"\x01{si++}\x01";
                    sents[sentinel] = interp;
                    work = work.Replace(interp, sentinel);
                }
                work = work.Replace("{", "{{").Replace("}", "}}");
                foreach (var kv2 in sents) work = work.Replace(kv2.Key, kv2.Value);
                string csEscaped = work.Replace("\\", "\\\\").Replace("\"", "\\\"");
                valExpr = hasOb2Refs
                    ? $"__rv($\"{csEscaped}\")"
                    : $"$\"{csEscaped}\"";
            }
            else
            {
                // Only OB2 CVar refs remain — resolve at runtime via __rv
                valExpr = $"__rv({JsonConvert.SerializeObject(promoted)})";
            }

            sb.Append($" {{ {JsonConvert.SerializeObject(k)}, {valExpr} }},");
        }
        if (sb[sb.Length - 1] == ',') sb.Length--;
        sb.Append(" }");
        return sb.ToString();
    }

    /// <summary>Parses OB2 List literal $["a","b","c"] into a C# List&lt;string&gt; expression.</summary>
    private static string ToCsList(string value)
    {
        string inner = value;
        if (inner.StartsWith("$[")) inner = inner.Substring(2, inner.Length - 3);
        else if (inner.StartsWith("[")) inner = inner.Substring(1, inner.Length - 2);

        var items = new List<string>();
        foreach (Match m in Regex.Matches(inner, @"""((?:[^""\\]|\\.)*)"""))
            items.Add(m.Groups[1].Value);

        if (items.Count == 0) return "new System.Collections.Generic.List<string>()";

        var sb = new StringBuilder("new System.Collections.Generic.List<string> {");
        foreach (string item in items)
            sb.Append($" {JsonConvert.SerializeObject(item)},");
        if (sb[sb.Length - 1] == ',') sb.Length--;
        sb.Append(" }");
        return sb.ToString();
    }

    /// <summary>Converts OB2 list property (like BanKeys) that may be $[...] or csv to a C# List&lt;string&gt; expression.</summary>
    private static string ToCsStringList(string value)
    {
        if (value.StartsWith("$[") || value.StartsWith("["))
            return ToCsList(value);

        // Comma-separated plain strings
        var items = value.Split(',');
        var sb = new StringBuilder("new System.Collections.Generic.List<string> {");
        foreach (string item in items)
            sb.Append($" {JsonConvert.SerializeObject(item.Trim().Trim('"'))},");
        if (sb[sb.Length - 1] == ',') sb.Length--;
        sb.Append(" }");
        return sb.ToString();
    }

    private static string ToCsBool(string value)
    {
        return value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) ? "true" : "false";
    }

    // ─── Utilities ────────────────────────────────────────────────────────────

    private static string NextBlock() => $"__b{_blockCounter++}";

    /// <summary>Makes a string safe to use as a C# identifier.</summary>
    private static string SafeId(string name) =>
        Regex.Replace(name ?? "_", @"[^\w]", "_");

    /// <summary>Unescapes OB2 backslash-escaped characters (e.g. \" → ") inside extracted string values.</summary>
    private static string UnescapeOb2(string s) =>
        s.Replace("\\\"", "\"").Replace("\\\\", "\\");

    /// <summary>Escapes a string for use inside a C# string literal (not verbatim).</summary>
    private static string EscapeForCsString(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string CompileCfClearanceBlock(LoliCodeSegment b)
    {
        string url    = b.Properties.GetValueOrDefault("url",            "\"\"");
        string outVar = b.Properties.GetValueOrDefault("outputVariable", "\"CF_CLEARANCE\"");
        int    timeout     = b.Properties.TryGetValue("timeout",      out var tm) && int.TryParse(tm.Trim(), out int tv) ? tv : 30;
        int    port        = b.Properties.TryGetValue("port",         out var pt) && int.TryParse(pt.Trim(), out int pv) ? pv : 9516;
        bool   storeCookies = b.Properties.TryGetValue("storeCookies", out var sc) && sc.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);

        string bn        = NextBlock();
        string rawOutVar = UnescapeOb2(outVar.Trim().Trim('"'));
        string v         = SafeId(rawOutVar);
        bool   declared  = _knownLocals.Contains(rawOutVar);
        _knownLocals.Add(rawOutVar);
        string varDecl   = declared
            ? $"{v} = data.GetVar({JsonConvert.SerializeObject(rawOutVar)});"
            : $"string {v} = data.GetVar({JsonConvert.SerializeObject(rawOutVar)});";

        return $@"{{
    var {bn} = new RuriLib.BlockCfClearance();
    {bn}.Url = {ToCs(url)};
    {bn}.OutputVariable = {ToCs(outVar)};
    {bn}.Timeout = {timeout};
    {bn}.Port = {port};
    {bn}.StoreCookies = {(storeCookies ? "true" : "false")};
    {bn}.Process(data._inner);
}}" + "\n" + varDecl;
    }

    // Syntax: CF-CLEARANCE "url" ["outputVar"] [TIMEOUT n] [PORT n]
    private static string ConvertCfClearanceToCSharp(string code)
    {
        var trimmed = code.Trim();
        if (!trimmed.StartsWith("CF-CLEARANCE", StringComparison.OrdinalIgnoreCase)) return null;
        string rest = trimmed.Substring("CF-CLEARANCE".Length).Trim();

        var tokens  = new List<string>();
        int timeoutVal = 30;
        int portVal    = 9516;

        bool storeCookiesVal = false;

        int i = 0;
        while (i < rest.Length)
        {
            if (rest[i] == '"')
            {
                int end = rest.IndexOf('"', i + 1);
                if (end < 0) break;
                tokens.Add(rest.Substring(i + 1, end - i - 1));
                i = end + 1;
            }
            else
            {
                string remaining = rest.Substring(i).TrimStart();
                if (remaining.StartsWith("TIMEOUT ", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = remaining.Split(' ');
                    if (parts.Length >= 2 && int.TryParse(parts[1].Trim('"'), out int tt)) timeoutVal = tt;
                    i += remaining.Length; continue;
                }
                else if (remaining.StartsWith("PORT ", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = remaining.Split(' ');
                    if (parts.Length >= 2 && int.TryParse(parts[1].Trim('"'), out int pp)) portVal = pp;
                    i += remaining.Length; continue;
                }
                else if (remaining.StartsWith("STORECOOKIES", StringComparison.OrdinalIgnoreCase))
                {
                    storeCookiesVal = true;
                    i += remaining.Length; continue;
                }
                i++;
            }
        }

        if (tokens.Count < 1) return "// CF-CLEARANCE: missing url";

        string url    = EscapeForCsString(tokens[0]);
        string outVar = tokens.Count >= 2 ? EscapeForCsString(tokens[1]) : "CF_CLEARANCE";

        int id = _blockCounter++;
        return $@"{{
    var __cfc{id} = new RuriLib.BlockCfClearance();
    __cfc{id}.Url = __rv(""{url}"");
    __cfc{id}.OutputVariable = ""{outVar}"";
    __cfc{id}.Timeout = {timeoutVal};
    __cfc{id}.Port = {portVal};
    __cfc{id}.StoreCookies = {(storeCookiesVal ? "true" : "false")};
    __cfc{id}.Process(data._inner);
}}";
    }

    private static string CompileTurnstileBlock(LoliCodeSegment b)
    {
        string domain  = b.Properties.GetValueOrDefault("domain",         "\"\"");
        string siteKey = b.Properties.GetValueOrDefault("siteKey",        "\"\"");
        string outVar  = b.Properties.GetValueOrDefault("outputVariable", "\"TURNSTILE_TOKEN\"");
        string action  = b.Properties.GetValueOrDefault("action",         "\"\"");
        string proxy   = b.Properties.GetValueOrDefault("proxy",          "\"\"");
        int    port    = b.Properties.TryGetValue("port", out var pt) && int.TryParse(pt.Trim(), out int pv) ? pv : 8742;

        string bn        = NextBlock();
        string rawOutVar = UnescapeOb2(outVar.Trim().Trim('"'));
        string v         = SafeId(rawOutVar);
        bool   declared  = _knownLocals.Contains(rawOutVar);
        _knownLocals.Add(rawOutVar);
        string varDecl   = declared
            ? $"{v} = data.GetVar({JsonConvert.SerializeObject(rawOutVar)});"
            : $"string {v} = data.GetVar({JsonConvert.SerializeObject(rawOutVar)});";

        return $@"{{
    var {bn} = new RuriLib.BlockTurnstile();
    {bn}.Domain = {ToCs(domain)};
    {bn}.SiteKey = {ToCs(siteKey)};
    {bn}.OutputVariable = {ToCs(outVar)};
    {bn}.Action = {ToCs(action)};
    {bn}.Proxy = {ToCs(proxy)};
    {bn}.Port = {port};
    {bn}.Process(data._inner);
}}" + "\n" + varDecl;
    }

    private static string CompileDataDomeBlock(LoliCodeSegment b)
    {
        string url    = b.Properties.GetValueOrDefault("url",             "\"\"");
        string outCk  = b.Properties.GetValueOrDefault("outputCookie",    "\"DD_COOKIE\"");
        string outUa  = b.Properties.GetValueOrDefault("outputUserAgent", "\"DD_UA\"");
        string proxy  = b.Properties.GetValueOrDefault("proxy",           "\"\"");
        int    port   = b.Properties.TryGetValue("port", out var pt) && int.TryParse(pt.Trim(), out int pv) ? pv : 9505;

        string bn = NextBlock();

        string rawCk  = UnescapeOb2(outCk.Trim().Trim('"'));
        string rawUa  = UnescapeOb2(outUa.Trim().Trim('"'));
        string vCk    = SafeId(rawCk);
        string vUa    = SafeId(rawUa);
        string ckDecl = _knownLocals.Contains(rawCk)
            ? $"{vCk} = data.GetVar({JsonConvert.SerializeObject(rawCk)});"
            : $"string {vCk} = data.GetVar({JsonConvert.SerializeObject(rawCk)});";
        string uaDecl = _knownLocals.Contains(rawUa)
            ? $"{vUa} = data.GetVar({JsonConvert.SerializeObject(rawUa)});"
            : $"string {vUa} = data.GetVar({JsonConvert.SerializeObject(rawUa)});";
        _knownLocals.Add(rawCk);
        _knownLocals.Add(rawUa);

        return $@"{{
    var {bn} = new RuriLib.BlockDataDome();
    {bn}.Url = {ToCs(url)};
    {bn}.OutputCookie = {ToCs(outCk)};
    {bn}.OutputUserAgent = {ToCs(outUa)};
    {bn}.Proxy = {ToCs(proxy)};
    {bn}.Port = {port};
    {bn}.Process(data._inner);
}}" + "\n" + ckDecl + "\n" + uaDecl;
    }

    // Syntax: DATADOME "url" ["outCookie"] [UA "outUA"] [PROXY "proxy"] [PORT n]
    private static string ConvertDataDomeToCSharp(string code)
    {
        var trimmed = code.Trim();
        if (!trimmed.StartsWith("DATADOME", StringComparison.OrdinalIgnoreCase)) return null;
        string rest = trimmed.Substring("DATADOME".Length).Trim();

        var tokens  = new List<string>();
        string uaVar = "DD_UA";
        string proxy = "";
        int    portV = 9505;

        int i = 0;
        while (i < rest.Length)
        {
            if (rest[i] == '"')
            {
                int end = rest.IndexOf('"', i + 1);
                if (end < 0) break;
                tokens.Add(rest.Substring(i + 1, end - i - 1));
                i = end + 1;
            }
            else
            {
                string rem = rest.Substring(i).TrimStart();
                if (rem.StartsWith("UA ", StringComparison.OrdinalIgnoreCase))
                {
                    int q = rem.IndexOf('"'); if (q >= 0) { int q2 = rem.IndexOf('"', q + 1); if (q2 > q) { uaVar = rem.Substring(q + 1, q2 - q - 1); i += q2 + 1; continue; } }
                }
                else if (rem.StartsWith("PROXY ", StringComparison.OrdinalIgnoreCase))
                {
                    int q = rem.IndexOf('"'); if (q >= 0) { int q2 = rem.IndexOf('"', q + 1); if (q2 > q) { proxy = rem.Substring(q + 1, q2 - q - 1); i += q2 + 1; continue; } }
                }
                else if (rem.StartsWith("PORT ", StringComparison.OrdinalIgnoreCase))
                {
                    string numPart = rem.Substring(5).TrimStart();
                    int sp = numPart.IndexOf(' '); if (sp < 0) sp = numPart.Length;
                    if (int.TryParse(numPart.Substring(0, sp), out int pn)) portV = pn;
                    i += 5 + sp; continue;
                }
                i++;
            }
        }

        string url   = tokens.Count > 0 ? tokens[0] : "";
        string outCk = tokens.Count > 1 ? tokens[1] : "DD_COOKIE";
        string bn    = NextBlock();
        return $@"{{
    var {bn} = new RuriLib.BlockDataDome();
    {bn}.Url = ""{url}"";
    {bn}.OutputCookie = ""{outCk}"";
    {bn}.OutputUserAgent = ""{uaVar}"";
    {bn}.Proxy = ""{proxy}"";
    {bn}.Port = {portV};
    {bn}.Process(data._inner);
}}";
    }

    private static string CompileAkmCookiesBlock(LoliCodeSegment b)
    {
        string url     = b.Properties.GetValueOrDefault("url",             "\"\"");
        string outCk   = b.Properties.GetValueOrDefault("outputCookies",   "\"AKM_COOKIES\"");
        string outUa   = b.Properties.GetValueOrDefault("outputUserAgent", "\"AKM_UA\"");
        string proxy   = b.Properties.GetValueOrDefault("proxy",           "\"\"");
        int    port    = b.Properties.TryGetValue("port", out var pt) && int.TryParse(pt.Trim(), out int pv) ? pv : 8085;

        string bn     = NextBlock();
        string rawCk  = UnescapeOb2(outCk.Trim().Trim('"'));
        string rawUa  = UnescapeOb2(outUa.Trim().Trim('"'));
        string vCk    = SafeId(rawCk);
        string vUa    = SafeId(rawUa);
        string ckDecl = _knownLocals.Contains(rawCk)
            ? $"{vCk} = data.GetVar({JsonConvert.SerializeObject(rawCk)});"
            : $"string {vCk} = data.GetVar({JsonConvert.SerializeObject(rawCk)});";
        string uaDecl = _knownLocals.Contains(rawUa)
            ? $"{vUa} = data.GetVar({JsonConvert.SerializeObject(rawUa)});"
            : $"string {vUa} = data.GetVar({JsonConvert.SerializeObject(rawUa)});";
        _knownLocals.Add(rawCk);
        _knownLocals.Add(rawUa);

        return $@"{{
    var {bn} = new RuriLib.BlockAkmCookies();
    {bn}.Url = {ToCs(url)};
    {bn}.OutputCookies = {ToCs(outCk)};
    {bn}.OutputUserAgent = {ToCs(outUa)};
    {bn}.Proxy = {ToCs(proxy)};
    {bn}.Port = {port};
    {bn}.Process(data._inner);
}}" + "\n" + ckDecl + "\n" + uaDecl;
    }

    // Converts a LoliScript-style AKM-COOKIES line to valid C#.
    // Syntax: AKM-COOKIES "url" ["outCookies"] [UA "outUA"] [PROXY "proxy"] [PORT n]
    private static string ConvertAkmCookiesToCSharp(string code)
    {
        var trimmed = code.Trim();
        if (!trimmed.StartsWith("AKM-COOKIES", StringComparison.OrdinalIgnoreCase)) return null;
        string rest = trimmed.Substring("AKM-COOKIES".Length).Trim();

        var tokens    = new List<string>();
        string uaVar  = "AKM_UA";
        string proxy  = "";
        int    portV  = 8085;

        int i = 0;
        while (i < rest.Length)
        {
            if (rest[i] == '"')
            {
                int end = rest.IndexOf('"', i + 1);
                if (end < 0) break;
                tokens.Add(rest.Substring(i + 1, end - i - 1));
                i = end + 1;
            }
            else
            {
                string remaining = rest.Substring(i).TrimStart();
                if (remaining.StartsWith("UA ", StringComparison.OrdinalIgnoreCase))
                {
                    int q = remaining.IndexOf('"');
                    if (q >= 0) { int q2 = remaining.IndexOf('"', q + 1); if (q2 > q) { uaVar = remaining.Substring(q + 1, q2 - q - 1); i += q2 + 1; continue; } }
                }
                else if (remaining.StartsWith("PROXY ", StringComparison.OrdinalIgnoreCase))
                {
                    int q = remaining.IndexOf('"');
                    if (q >= 0) { int q2 = remaining.IndexOf('"', q + 1); if (q2 > q) { proxy = remaining.Substring(q + 1, q2 - q - 1); i += q2 + 1; continue; } }
                }
                else if (remaining.StartsWith("PORT ", StringComparison.OrdinalIgnoreCase))
                {
                    string numPart = remaining.Substring(5).TrimStart();
                    int sp = numPart.IndexOf(' '); if (sp < 0) sp = numPart.Length;
                    if (int.TryParse(numPart.Substring(0, sp), out int pn)) portV = pn;
                    i += 5 + sp; continue;
                }
                i++;
            }
        }

        string url    = tokens.Count > 0 ? tokens[0] : "";
        string outCk  = tokens.Count > 1 ? tokens[1] : "AKM_COOKIES";
        string bn     = NextBlock();
        return $@"{{
    var {bn} = new RuriLib.BlockAkmCookies();
    {bn}.Url = ""{url}"";
    {bn}.OutputCookies = ""{outCk}"";
    {bn}.OutputUserAgent = ""{uaVar}"";
    {bn}.Proxy = ""{proxy}"";
    {bn}.Port = {portV};
    {bn}.Process(data._inner);
}}";
    }

    private static string CompileRecaptchaV3Block(LoliCodeSegment b)
    {
        string url     = b.Properties.GetValueOrDefault("url",            "\"\"");
        string siteKey = b.Properties.GetValueOrDefault("siteKey",        "\"\"");
        string outVar  = b.Properties.GetValueOrDefault("outputVariable", "\"RC3_TOKEN\"");
        string action  = b.Properties.GetValueOrDefault("action", "\"submit\"");
        int    port    = b.Properties.TryGetValue("port", out var pt) && int.TryParse(pt.Trim(), out int pv) ? pv : 9512;

        string bn        = NextBlock();
        string rawOutVar = UnescapeOb2(outVar.Trim().Trim('"'));
        string v         = SafeId(rawOutVar);
        bool   declared  = _knownLocals.Contains(rawOutVar);
        _knownLocals.Add(rawOutVar);
        string varDecl   = declared
            ? $"{v} = data.GetVar({JsonConvert.SerializeObject(rawOutVar)});"
            : $"string {v} = data.GetVar({JsonConvert.SerializeObject(rawOutVar)});";

        return $@"{{
    var {bn} = new RuriLib.BlockRecaptchaV3();
    {bn}.Url = {ToCs(url)};
    {bn}.SiteKey = {ToCs(siteKey)};
    {bn}.OutputVariable = {ToCs(outVar)};
    {bn}.Action = {ToCs(action)};
    {bn}.Port = {port};
    {bn}.Process(data._inner);
}}" + "\n" + varDecl;
    }

    // Converts a LoliScript-style RECAPTCHA-V3 line to valid C#.
    // Syntax: RECAPTCHA-V3 "url" "siteKey" ["outputVar"] [ACTION "action"] [WORKERS n] [PORT n]
    private static string ConvertRecaptchaV3ToCSharp(string code)
    {
        var trimmed = code.Trim();
        if (!trimmed.StartsWith("RECAPTCHA-V3", StringComparison.OrdinalIgnoreCase)) return null;
        string rest = trimmed.Substring("RECAPTCHA-V3".Length).Trim();

        var tokens = new List<string>();
        string actionVal  = "submit";
        int    workersVal = 2;
        int    portVal    = 9512;

        int i = 0;
        while (i < rest.Length)
        {
            if (rest[i] == '"')
            {
                int end = rest.IndexOf('"', i + 1);
                if (end < 0) break;
                tokens.Add(rest.Substring(i + 1, end - i - 1));
                i = end + 1;
            }
            else
            {
                // keyword params
                string remaining = rest.Substring(i).TrimStart();
                if (remaining.StartsWith("ACTION ", StringComparison.OrdinalIgnoreCase))
                {
                    int q = remaining.IndexOf('"');
                    if (q >= 0) { int e = remaining.IndexOf('"', q + 1); if (e > q) { actionVal = remaining.Substring(q + 1, e - q - 1); i += q + e + 2; continue; } }
                }
                else if (remaining.StartsWith("WORKERS ", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = remaining.Split(' ');
                    if (parts.Length >= 2 && int.TryParse(parts[1].Trim('"'), out int ww)) workersVal = ww;
                    i += remaining.Length; continue;
                }
                else if (remaining.StartsWith("PORT ", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = remaining.Split(' ');
                    if (parts.Length >= 2 && int.TryParse(parts[1].Trim('"'), out int pp)) portVal = pp;
                    i += remaining.Length; continue;
                }
                i++;
            }
        }

        if (tokens.Count < 2) return "// RECAPTCHA-V3: missing url and siteKey";

        string url     = EscapeForCsString(tokens[0]);
        string siteKey = EscapeForCsString(tokens[1]);
        string outVar  = tokens.Count >= 3 ? EscapeForCsString(tokens[2]) : "RC3_TOKEN";

        int id = _blockCounter++;
        return $@"{{
    var __rc3{id} = new RuriLib.BlockRecaptchaV3();
    __rc3{id}.Url = __rv(""{url}"");
    __rc3{id}.SiteKey = __rv(""{siteKey}"");
    __rc3{id}.OutputVariable = __rv(""{outVar}"");
    __rc3{id}.Action = __rv(""{EscapeForCsString(actionVal)}"");
    __rc3{id}.Port = {portVal};
    __rc3{id}.Process(data._inner);
}}";
    }

    private static string CompileRecaptchaV2InvisibleBlock(LoliCodeSegment b)
    {
        string url    = b.Properties.GetValueOrDefault("url",            "\"\"");
        string outVar = b.Properties.GetValueOrDefault("outputVariable", "\"RC2I_TOKEN\"");
        string action = b.Properties.GetValueOrDefault("action",         "\"submit\"");
        string proxy  = b.Properties.GetValueOrDefault("proxy",          "\"\"");
        int    port   = b.Properties.TryGetValue("port", out var pt) && int.TryParse(pt.Trim(), out int pv) ? pv : 9513;

        string bn        = NextBlock();
        string rawOutVar = UnescapeOb2(outVar.Trim().Trim('"'));
        string v         = SafeId(rawOutVar);
        bool   declared  = _knownLocals.Contains(rawOutVar);
        _knownLocals.Add(rawOutVar);
        string varDecl   = declared
            ? $"{v} = data.GetVar({JsonConvert.SerializeObject(rawOutVar)});"
            : $"string {v} = data.GetVar({JsonConvert.SerializeObject(rawOutVar)});";

        return $@"{{
    var {bn} = new RuriLib.BlockRecaptchaV2Invisible();
    {bn}.Url = {ToCs(url)};
    {bn}.OutputVariable = {ToCs(outVar)};
    {bn}.Action = {ToCs(action)};
    {bn}.Proxy = {ToCs(proxy)};
    {bn}.Port = {port};
    {bn}.Process(data._inner);
}}" + "\n" + varDecl;
    }

    // Converts a LoliScript-style RECAPTCHA-V2-INVISIBLE line to valid C#.
    // Syntax: RECAPTCHA-V2-INVISIBLE "url" ["outputVar"] [ACTION "action"] [PROXY "proxy"] [PORT n]
    private static string ConvertRecaptchaV2InvisibleToCSharp(string code)
    {
        var trimmed = code.Trim();
        if (!trimmed.StartsWith("RECAPTCHA-V2-INVISIBLE", StringComparison.OrdinalIgnoreCase)) return null;
        string rest = trimmed.Substring("RECAPTCHA-V2-INVISIBLE".Length).Trim();

        var tokens    = new List<string>();
        string actionVal = "submit";
        string proxyVal  = "";
        int    portVal   = 9513;

        int i = 0;
        while (i < rest.Length)
        {
            if (rest[i] == '"')
            {
                int end = rest.IndexOf('"', i + 1);
                if (end < 0) break;
                tokens.Add(rest.Substring(i + 1, end - i - 1));
                i = end + 1;
            }
            else
            {
                string remaining = rest.Substring(i).TrimStart();
                if (remaining.StartsWith("ACTION ", StringComparison.OrdinalIgnoreCase))
                {
                    int q = remaining.IndexOf('"');
                    if (q >= 0) { int e = remaining.IndexOf('"', q + 1); if (e > q) { actionVal = remaining.Substring(q + 1, e - q - 1); i += q + e + 2; continue; } }
                }
                else if (remaining.StartsWith("PROXY ", StringComparison.OrdinalIgnoreCase))
                {
                    int q = remaining.IndexOf('"');
                    if (q >= 0) { int e = remaining.IndexOf('"', q + 1); if (e > q) { proxyVal = remaining.Substring(q + 1, e - q - 1); i += q + e + 2; continue; } }
                }
                else if (remaining.StartsWith("PORT ", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = remaining.Split(' ');
                    if (parts.Length >= 2 && int.TryParse(parts[1].Trim('"'), out int pp)) portVal = pp;
                    i += remaining.Length; continue;
                }
                i++;
            }
        }

        if (tokens.Count < 1) return "// RECAPTCHA-V2-INVISIBLE: missing url";

        string url    = EscapeForCsString(tokens[0]);
        string outVar = tokens.Count >= 2 ? EscapeForCsString(tokens[1]) : "RC2I_TOKEN";

        int id = _blockCounter++;
        return $@"{{
    var __rc2i{id} = new RuriLib.BlockRecaptchaV2Invisible();
    __rc2i{id}.Url = __rv(""{url}"");
    __rc2i{id}.OutputVariable = __rv(""{outVar}"");
    __rc2i{id}.Action = __rv(""{EscapeForCsString(actionVal)}"");
    __rc2i{id}.Proxy = __rv(""{EscapeForCsString(proxyVal)}"");
    __rc2i{id}.Port = {portVal};
    __rc2i{id}.Process(data._inner);
}}";
    }

    private static string CompileAltchaBlock(LoliCodeSegment b)
    {
        string url    = b.Properties.GetValueOrDefault("challengeUrl",   "\"\"");
        string outVar = b.Properties.GetValueOrDefault("outputVariable", "\"ALTCHA_TOKEN\"");

        string bn = NextBlock();
        return $@"{{
    var {bn} = new RuriLib.BlockAltcha();
    {bn}.ChallengeUrl = {ToCs(url)};
    {bn}.OutputVariable = {ToCs(outVar)};
    {bn}.Process(data._inner);
}}";
    }

    private static string CompileRecaptchaV3BypassBlock(LoliCodeSegment b)
    {
        string varName  = b.Properties.GetValueOrDefault("variableName", "\"RECAPTCHA_TOKEN\"");
        string getUrl   = b.Properties.GetValueOrDefault("getUrl",       "\"\"");
        string bg       = b.Properties.GetValueOrDefault("bg",           "\"\"");
        string postUrl  = b.Properties.GetValueOrDefault("postUrl",      "\"\"");
        string referer  = b.Properties.GetValueOrDefault("referer",      "\"\"");
        string ua       = b.Properties.GetValueOrDefault("userAgent",    "\"\"");

        string bn = NextBlock();
        return $@"{{
    var {bn} = new RuriLib.BlockRecaptchaV3Bypass();
    {bn}.VariableName = {ToCs(varName)};
    {bn}.GetUrl       = {ToCs(getUrl)};
    {bn}.Bg           = {ToCs(bg)};
    {bn}.PostUrl      = {ToCs(postUrl)};
    {bn}.Referer      = {ToCs(referer)};
    {bn}.UserAgent    = {ToCs(ua)};
    {bn}.Process(data._inner);
}}";
    }

    private static string CompileFriendlyCaptchaBlock(LoliCodeSegment b)
    {
        string siteKey  = b.Properties.GetValueOrDefault("siteKey",        "\"\"");
        string euRaw    = b.Properties.GetValueOrDefault("useEuEndpoint",   "False");
        string outVar   = b.Properties.GetValueOrDefault("outputVariable",  "\"FRCAPTCHA_TOKEN\"");
        bool   useEu    = euRaw.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);

        string bn = NextBlock();
        return $@"{{
    var {bn} = new RuriLib.BlockFriendlyCaptcha();
    {bn}.SiteKey = {ToCs(siteKey)};
    {bn}.UseEuEndpoint = {(useEu ? "true" : "false")};
    {bn}.OutputVariable = {ToCs(outVar)};
    {bn}.Process(data._inner);
}}";
    }

    // Converts a LoliScript-style FRIENDLYCAPTCHA line to valid C#.
    // Syntax: FRIENDLYCAPTCHA "siteKey" [True|False] ["outputVar"]
    // Returns null if the line does not start with FRIENDLYCAPTCHA.
    private static string ConvertFriendlyCaptchaToCSharp(string code)
    {
        var trimmed = code.Trim();
        if (!trimmed.StartsWith("FRIENDLYCAPTCHA", StringComparison.OrdinalIgnoreCase)) return null;
        string rest = trimmed.Substring("FRIENDLYCAPTCHA".Length).Trim();

        var tokens = new List<string>();
        int i = 0;
        while (i < rest.Length)
        {
            if (rest[i] == '"')
            {
                int end = rest.IndexOf('"', i + 1);
                if (end < 0) break;
                tokens.Add(rest.Substring(i + 1, end - i - 1));
                i = end + 1;
            }
            else { i++; }
        }

        if (tokens.Count < 1) return "// FRIENDLYCAPTCHA: missing siteKey";

        string siteKey = EscapeForCsString(tokens[0]);
        string outVar  = tokens.Count >= 2 ? EscapeForCsString(tokens[1]) : "FRCAPTCHA_TOKEN";

        int id = _blockCounter++;
        return $@"{{
    var __frc{id} = new RuriLib.BlockFriendlyCaptcha();
    __frc{id}.SiteKey = __rv(""{siteKey}"");
    __frc{id}.OutputVariable = ""{outVar}"";
    __frc{id}.Process(data._inner);
}}";
    }

    // Converts a LoliScript-style ALTCHA line to valid C#.
    // Syntax: ALTCHA "challengeUrl" ["outputVar"]
    // Returns null if the line does not start with ALTCHA.
    private static string ConvertAltchaToCSharp(string code)
    {
        var trimmed = code.Trim();
        if (!trimmed.StartsWith("ALTCHA", StringComparison.OrdinalIgnoreCase)) return null;
        string rest = trimmed.Substring("ALTCHA".Length).Trim();

        var tokens = new List<string>();
        int i = 0;
        while (i < rest.Length)
        {
            if (rest[i] == '"')
            {
                int end = rest.IndexOf('"', i + 1);
                if (end < 0) break;
                tokens.Add(rest.Substring(i + 1, end - i - 1));
                i = end + 1;
            }
            else { i++; }
        }

        if (tokens.Count < 1) return "// ALTCHA: falta challengeUrl";

        string url    = EscapeForCsString(tokens[0]);
        string outVar = tokens.Count >= 2 ? EscapeForCsString(tokens[1]) : "ALTCHA_TOKEN";

        int id = _blockCounter++;
        return $@"{{
    var __al{id} = new RuriLib.BlockAltcha();
    __al{id}.ChallengeUrl = __rv(""{url}"");
    __al{id}.OutputVariable = ""{outVar}"";
    __al{id}.Process(data._inner);
}}";
    }

    // Converts a LoliScript-style TURNSTILE line to valid C#.
    // Syntax: TURNSTILE "domain" "siteKey" ["outputVar"] [ACTION "action"] [PROXY "proxy"] [PORT "port"]
    // Returns null if the line does not start with TURNSTILE.
    private static string ConvertTurnstileToCSharp(string code)
    {
        var trimmed = code.Trim();
        if (!trimmed.StartsWith("TURNSTILE", StringComparison.OrdinalIgnoreCase)) return null;
        string rest = trimmed.Substring("TURNSTILE".Length).Trim();

        // Extract all quoted tokens (positional)
        var tokens = new List<string>();
        int i = 0;
        while (i < rest.Length)
        {
            if (rest[i] == '"')
            {
                int end = rest.IndexOf('"', i + 1);
                if (end < 0) break;
                tokens.Add(rest.Substring(i + 1, end - i - 1));
                i = end + 1;
            }
            else { i++; }
        }

        if (tokens.Count < 2) return "// TURNSTILE: faltan domain y siteKey";

        string domain  = EscapeForCsString(tokens[0]);
        string siteKey = EscapeForCsString(tokens[1]);
        string outVar  = tokens.Count >= 3 ? EscapeForCsString(tokens[2]) : "TURNSTILE_TOKEN";
        int port = 8742;
        string action = "";
        string proxy  = "";

        var portMatch   = Regex.Match(rest, @"\bPORT\s+""(\d+)""",    RegexOptions.IgnoreCase);
        var actionMatch = Regex.Match(rest, @"\bACTION\s+""([^""]*?)""", RegexOptions.IgnoreCase);
        var proxyMatch  = Regex.Match(rest, @"\bPROXY\s+""([^""]*?)""",  RegexOptions.IgnoreCase);
        if (portMatch.Success)   port   = int.Parse(portMatch.Groups[1].Value);
        if (actionMatch.Success) action = EscapeForCsString(actionMatch.Groups[1].Value);
        if (proxyMatch.Success)  proxy  = EscapeForCsString(proxyMatch.Groups[1].Value);

        int id = _blockCounter++;
        return $@"{{
    var __ts{id} = new RuriLib.BlockTurnstile();
    __ts{id}.Domain = __rv(""{domain}"");
    __ts{id}.SiteKey = __rv(""{siteKey}"");
    __ts{id}.OutputVariable = ""{outVar}"";
    __ts{id}.Action = __rv(""{action}"");
    __ts{id}.Proxy = __rv(""{proxy}"");
    __ts{id}.Port = {port};
    __ts{id}.Process(data._inner);
}}";
    }
}
