using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using RuriLib;
using RuriLib.Functions.Conditions;
using RuriLib.Functions.Requests;
using RuriLib.Models;

namespace RuriLib.LS.LoliCode;

/// <summary>
/// Handles bidirectional conversion between LoliCode text and Stacker block lists.
///
/// LoliCode → Blocks (for visual Stacker):
///   BLOCK:HttpRequest → BlockRequest
///   BLOCK:Parse       → BlockParse
///   BLOCK:Function    → BlockFunction
///   Inline C# code   → BlockLSCode
///   Other blocks      → BlockLSCode (raw LoliCode preserved)
///
/// Blocks → LoliCode (saving from Stacker in LoliCode mode):
///   BlockRequest  → BLOCK:HttpRequest ... ENDBLOCK
///   BlockParse    → BLOCK:Parse       ... ENDBLOCK
///   BlockFunction → BLOCK:Function    ... ENDBLOCK
///   BlockLSCode   → raw inline C# code
///   Other blocks  → LoliScript inline (fallback)
/// </summary>
public static class LoliCodeSerializer
{
    // ─── Blocks → LoliCode text ──────────────────────────────────────────────

    // Built-in BotData refs that OB2 cannot resolve in customHeaders values
    // (OB2 compiles <NAME> → bare C# identifier; these are not C# locals → CS0103).
    // The serializer auto-injects a Parse capture block before any HttpRequest that uses them.
    private static readonly (string SbRef, string DataRef, string SafeVar)[] s_BuiltinHeaderRefs =
    {
        ("<ADDRESS>",      "ADDRESS",      "sb_adr"),
        ("<SOURCE>",       "SOURCE",       "sb_src"),
        ("<RAWSOURCE>",    "RAWSOURCE",    "sb_rawsrc"),
        ("<RESPONSECODE>", "RESPONSECODE", "sb_rc"),
        ("<ERROR>",        "ERROR",        "sb_err"),
    };

    public static string BlocksToLoliCode(IEnumerable<BlockBase> blocks)
    {
        var sb = new StringBuilder();
        foreach (var block in blocks)
        {
            // Before each HttpRequest, auto-inject Parse capture blocks for any built-in
            // BotData refs used in customHeaders values so OB2 can resolve them as C# locals.
            if (block is BlockRequest req && req.CustomHeaders != null && req.CustomHeaders.Count > 0)
            {
                string allHeaderVals = string.Join("\n", req.CustomHeaders.Values);
                var replacements = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var (sbRef, dataRef, safeVar) in s_BuiltinHeaderRefs)
                {
                    if (!allHeaderVals.Contains(sbRef, StringComparison.Ordinal)) continue;
                    replacements[sbRef] = $"<{safeVar}>";
                    sb.AppendLine($"BLOCK:Parse");
                    sb.AppendLine($"LABEL:{safeVar}");
                    sb.AppendLine($"  input = @data.{dataRef}");
                    sb.AppendLine($"  leftDelim = \"\"");
                    sb.AppendLine($"  rightDelim = \"\"");
                    sb.AppendLine($"  MODE:LR");
                    sb.AppendLine($"  => VAR @{safeVar}");
                    sb.AppendLine("ENDBLOCK");
                    sb.AppendLine();
                }
                sb.AppendLine(replacements.Count > 0
                    ? RequestToLoliCode(req, replacements)
                    : BlockToLoliCode(block));
            }
            else
            {
                sb.AppendLine(BlockToLoliCode(block));
            }
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    // Namespaces SilverBullet pre-imports (via LoliScript.cs) but OB2 does NOT.
    // Scanning generated LoliCode for these type names lets us auto-inject the missing usings.
    private static readonly (string[] TypeNames, string UsingDirective)[] s_OB2ExtraUsings =
    {
        (new[] { "X509Certificate2", "X509Certificate", "X509Store", "X509Chain", "X509Certificate2Collection" },
         "using System.Security.Cryptography.X509Certificates;"),
        (new[] { "EnvelopedCms", "ContentInfo", "CmsRecipient", "CmsSigner", "SignedCms", "AlgorithmIdentifier" },
         "using System.Security.Cryptography.Pkcs;"),
        (new[] { "GZipStream", "DeflateStream", "ZipArchive", "ZipFile", "BrotliStream" },
         "using System.IO.Compression;"),
    };

    /// <summary>
    /// Prepends any missing <c>using</c> directives to the generated LoliCode so it
    /// compiles in OB2 without CS0246 errors for namespaces SilverBullet pre-imports
    /// but OB2 does not. Only injects usings for types actually referenced in the code.
    /// </summary>
    public static string InjectMissingUsings(string loliCode)
    {
        if (string.IsNullOrEmpty(loliCode))
            return loliCode;

        var toInject = new List<string>();
        foreach (var (typeNames, directive) in s_OB2ExtraUsings)
        {
            if (typeNames.Any(t => loliCode.Contains(t, StringComparison.Ordinal)))
                toInject.Add(directive);
        }
        if (toInject.Count == 0)
            return loliCode;

        // Avoid duplicating usings already present at the top of the code
        var existingUsings = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in loliCode.Split('\n'))
        {
            string t = line.Trim();
            if (t.StartsWith("using ", StringComparison.Ordinal) && t.EndsWith(";", StringComparison.Ordinal))
                existingUsings.Add(t);
            else if (!string.IsNullOrWhiteSpace(t))
                break;
        }

        var missing = toInject.Where(u => !existingUsings.Contains(u)).ToList();
        if (missing.Count == 0)
            return loliCode;

        return string.Join(Environment.NewLine, missing) + Environment.NewLine + loliCode;
    }

    public static string BlockToLoliCode(BlockBase block)
    {
        return block switch
        {
            BlockRequest  br                                           => RequestToLoliCode(br),
            BlockParse    bp                                           => ParseToLoliCode(bp),
            BlockDns      bd                                           => DnsToLoliCode(bd),
            BlockFunction bf when bf.FunctionType == BlockFunction.Function.Constant
                                                                       => ConstantStringToLoliCode(bf),
            BlockFunction bf                                           => FunctionToLoliCode(bf),
            BlockKeycheck bk                                           => KeyCheckToLoliCode(bk),
            BlockUtility  bu                                           => UtilityToLoliCode(bu),
            BlockBypassCF bcf                                          => BypassCFToLoliCode(bcf),
            BlockCfClearance bcfc                                      => CfClearanceToLoliCode(bcfc),
            BlockTurnstile bt                                          => TurnstileToLoliCode(bt),
            BlockAltcha               ba                                => AltchaToLoliCode(ba),
            BlockRecaptchaV3Bypass    brcb                              => RecaptchaV3BypassToLoliCode(brcb),
            BlockFriendlyCaptcha  bfc                                  => FriendlyCaptchaToLoliCode(bfc),
            BlockRecaptchaV3      brc3                                  => RecaptchaV3ToLoliCode(brc3),
            BlockRecaptchaV2Invisible brc2i                            => RecaptchaV2InvisibleToLoliCode(brc2i),
            BlockAkmCookies       bakm                                  => AkmCookiesToLoliCode(bakm),
            BlockDataDome         bdd                                   => DataDomeToLoliCode(bdd),
            BlockLSCode   bl                                           => ConvertLSCodeToLoliCode(bl.Script ?? ""),
            _                                                          => block.ToLS()
        };
    }

    // Converts any BEGIN SCRIPT Lang ... END SCRIPT -> VARS "outputs" blocks
    // embedded in a BlockLSCode.Script into OB2-compatible BLOCK:Script format.
    // Converts a LoliScript condition string to inline C# if(...) { form.
    // Handles both LoliScript source form:  IF "<left>" Comparer "right"
    // and the compiled C# form:  if (RuriLib.Functions.Conditions.Condition.ReplaceAndVerify("left", ...))
    // Output is pure C# that compiles in both SilverBullet (Roslyn) and OB2 (Roslyn).
    private static string LsConditionToOb2If(string condLine, string openBrace = "{")
    {
        string t = condLine.TrimStart();

        // ── Form A: LoliScript source: IF "<left>" Comparer "right" ──
        // Also handles: WHILE "<left>" Comparer "right"
        var mLS = Regex.Match(t,
            @"^(?:IF|WHILE)\s+""((?:[^""\\]|\\.)*)""\s+(\w+)(?:\s+""((?:[^""\\]|\\.)*)"")?\s*$",
            RegexOptions.IgnoreCase);
        if (mLS.Success)
        {
            bool   isWhile  = t.StartsWith("WHILE", StringComparison.OrdinalIgnoreCase);
            string keyword  = isWhile ? "while" : "if";
            string rawLeft  = mLS.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\");
            string comparer = mLS.Groups[2].Value;
            string rawRight = mLS.Groups[3].Success ? mLS.Groups[3].Value.Replace("\\\"", "\"").Replace("\\\\", "\\") : "";
            string ob2Left  = LsCondLeftToOb2(rawLeft);
            return $"{keyword} ({CondToInlineCSharp(ob2Left, comparer, rawRight)}) {openBrace}";
        }

        // ── Form B: compiled C# from LoliCodeParser: if/while (RuriLib…ReplaceAndVerify(…)) { ──
        var mCS = Regex.Match(t,
            @"^(if|while)\s*\(\s*RuriLib\.Functions\.Conditions\.Condition\.ReplaceAndVerify\(""((?:[^""\\]|\\.)*)""\s*,\s*RuriLib\.Functions\.Conditions\.Comparer\.(\w+)\s*,\s*""((?:[^""\\]|\\.)*)""\s*,\s*data\._inner\)\s*\)\s*\{",
            RegexOptions.IgnoreCase);
        if (mCS.Success)
        {
            bool   isWhile   = mCS.Groups[1].Value.Equals("while", StringComparison.OrdinalIgnoreCase);
            string keyword   = isWhile ? "while" : "if";
            string rawLeft   = mCS.Groups[2].Value.Replace("\\\"", "\"").Replace("\\\\", "\\");
            string comparer  = mCS.Groups[3].Value;
            string rawRight  = mCS.Groups[4].Value.Replace("\\\"", "\"").Replace("\\\\", "\\");
            string ob2Left   = LsCondLeftToOb2(rawLeft);
            return $"{keyword} ({CondToInlineCSharp(ob2Left, comparer, rawRight)}) {openBrace}";
        }

        return condLine; // unrecognized — pass through
    }

    // Converts @data.SOURCE / Comparer / "val" → inline C# bool expression.
    // Works in both SilverBullet (data is LoliCodeData) and OB2 (data is BotData).
    private static string CondToInlineCSharp(string ob2Left, string comparer, string rawRight)
    {
        string csLeft = ob2Left.StartsWith("@") ? ob2Left.Substring(1)
                        : $"\"{ob2Left.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
        // RESPONSECODE is int in SilverBullet's LoliCodeData — normalize to string for comparisons
        string strExpr = csLeft.Equals("data.RESPONSECODE", StringComparison.Ordinal)
                            ? $"{csLeft}.ToString()" : csLeft;
        string esc = rawRight.Replace("\\", "\\\\").Replace("\"", "\\\"");

        return comparer.ToLowerInvariant() switch {
            "contains"             => $"{strExpr}.Contains(\"{esc}\")",
            "doesnotcontain"       => $"!{strExpr}.Contains(\"{esc}\")",
            "equalto"              => $"{strExpr} == \"{esc}\"",
            "notequalto"           => $"{strExpr} != \"{esc}\"",
            "matchesregex"         => $"Regex.IsMatch({strExpr}, \"{esc}\")",
            "doesnotmatchregex"    => $"!Regex.IsMatch({strExpr}, \"{esc}\")",
            "greaterthan"          => $"string.Compare({strExpr}, \"{esc}\", StringComparison.Ordinal) > 0",
            "lessthan"             => $"string.Compare({strExpr}, \"{esc}\", StringComparison.Ordinal) < 0",
            "greaterthanorequalto" => $"string.Compare({strExpr}, \"{esc}\", StringComparison.Ordinal) >= 0",
            "lessthanorequalto"    => $"string.Compare({strExpr}, \"{esc}\", StringComparison.Ordinal) <= 0",
            "exists"               => $"!string.IsNullOrEmpty({strExpr})",
            "doesnotexist"         => $"string.IsNullOrEmpty({strExpr})",
            _                      => $"{strExpr}.Contains(\"{esc}\")",
        };
    }

    // Converts a LoliScript left-term <VARNAME> to OB2 @ref.
    private static string LsCondLeftToOb2(string left)
    {
        var m = Regex.Match(left, @"^<(@?[A-Za-z0-9_ ]+)>$");
        if (!m.Success) return $"\"{left}\""; // literal string
        string name = m.Groups[1].Value.TrimStart('@');
        return name.ToUpperInvariant() switch {
            "SOURCE"       => "@data.SOURCE",
            "RESPONSECODE" => "@data.RESPONSECODE",
            "ADDRESS"      => "@data.ADDRESS",
            "ERROR"        => "@data.ERROR",
            "USER" or "USERNAME" => "@input.USERNAME",
            "PASS" or "PASSWORD" => "@input.PASSWORD",
            _ => "@" + m.Groups[1].Value, // keep original (including leading @ if any)
        };
    }

    private static string ConvertLSCodeToLoliCode(string script)
    {
        // Convert IF/WHILE/ELSE/ENDIF/FOREACH/TRY/CATCH/PYTHON blocks even when no BEGIN SCRIPT present
        bool hasControlFlow = Regex.IsMatch(script,
            @"(?m)^\s*(?:IF |WHILE |(?-i:ELSE)\s*$|ENDIF|END IF|ENDWHILE|END WHILE|FOREACH |ENDFOREACH|TRY\s*$|CATCH\s*$|ENDTRY\s*$|(?:IRON)?PYTHON\s*$|if\s*\(RuriLib\.)",
            RegexOptions.IgnoreCase);

        if (!script.Contains("BEGIN SCRIPT", StringComparison.OrdinalIgnoreCase) && !hasControlFlow)
            return script;

        var lines = script.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var result = new StringBuilder();
        int i = 0;
        while (i < lines.Length)
        {
            string line    = lines[i];
            string trimmed = line.TrimStart();

            // ── Control-flow lines: IF / WHILE / ELSE / ENDIF ───────────────
            // LoliScript keywords are ALWAYS uppercase; use Ordinal to avoid catching C# if(...).
            if (trimmed.StartsWith("IF ", StringComparison.Ordinal) ||
                trimmed.StartsWith("WHILE ", StringComparison.Ordinal) ||
                trimmed.StartsWith("if (RuriLib.", StringComparison.Ordinal) ||
                trimmed.StartsWith("while (RuriLib.", StringComparison.Ordinal))
            {
                result.AppendLine(LsConditionToOb2If(trimmed));
                i++;
                continue;
            }
            if (trimmed.Equals("ELSE", StringComparison.Ordinal) ||
                trimmed.Equals("} else {", StringComparison.Ordinal))
            {
                result.AppendLine("} else {");
                i++;
                continue;
            }
            if (trimmed.Equals("ENDIF", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("END IF", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("}", StringComparison.Ordinal))
            {
                result.AppendLine("}");
                i++;
                continue;
            }
            if (trimmed.Equals("END WHILE", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("ENDWHILE", StringComparison.OrdinalIgnoreCase))
            {
                result.AppendLine("}");
                i++;
                continue;
            }

            // ── FOREACH / ENDFOREACH ─────────────────────────────────────────
            // LoliScript: FOREACH "outVar" IN "listVarName"
            // Serialized as C# foreach over the variable store list (SilverBullet)
            // or over a Roslyn local List<string> (OB2, if declared in preceding C#).
            {
                var mFe = Regex.Match(trimmed,
                    @"^FOREACH\s+""([^""]+)""\s+IN\s+[""<]?([A-Za-z0-9_]+)[>""]?\s*$",
                    RegexOptions.IgnoreCase);
                if (mFe.Success)
                {
                    string outVar  = mFe.Groups[1].Value;
                    string listVar = mFe.Groups[2].Value;
                    result.AppendLine(
                        $"foreach (string {outVar} in data.Variables.GetList(\"{listVar}\") ?? new System.Collections.Generic.List<string>())");
                    result.AppendLine("{");
                    i++;
                    continue;
                }
            }
            if (trimmed.Equals("ENDFOREACH", StringComparison.OrdinalIgnoreCase))
            {
                result.AppendLine("}");
                i++;
                continue;
            }

            // ── TRY / CATCH / ENDTRY ─────────────────────────────────────────
            if (trimmed.Equals("TRY", StringComparison.OrdinalIgnoreCase))
            {
                result.AppendLine("try {");
                i++;
                continue;
            }
            if (trimmed.Equals("CATCH", StringComparison.OrdinalIgnoreCase))
            {
                // Expose caught exception as string ERROR variable (matches LoliScript behaviour)
                result.AppendLine("} catch (Exception __tryErr__) { string ERROR = __tryErr__.Message;");
                i++;
                continue;
            }
            if (trimmed.Equals("ENDTRY", StringComparison.OrdinalIgnoreCase))
            {
                result.AppendLine("}");
                i++;
                continue;
            }

            // ── Inline PYTHON / IRONPYTHON blocks (without BEGIN SCRIPT prefix) ─
            // LoliScript: PYTHON\n...code...\nEND PYTHON -> VARS "var1" "var2"
            // Serialized identically to BEGIN SCRIPT Python/IronPython ... END SCRIPT -> VARS
            if (trimmed.Equals("PYTHON", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("IRONPYTHON", StringComparison.OrdinalIgnoreCase))
            {
                bool isIronPy = trimmed.Equals("IRONPYTHON", StringComparison.OrdinalIgnoreCase);
                string endKw  = isIronPy ? "END IRONPYTHON" : "END PYTHON";
                i++;
                var pyLines  = new List<string>();
                string pyOut = "";
                while (i < lines.Length)
                {
                    string pl = lines[i].TrimStart();
                    if (pl.StartsWith(endKw, StringComparison.OrdinalIgnoreCase))
                    {
                        // Parse "END PYTHON -> VARS "var1" "var2""
                        string rest = pl.Substring(endKw.Length).Trim();
                        pyOut = string.Join(" ",
                            Regex.Matches(rest, @"""([^""]*)""")
                                 .Cast<Match>()
                                 .Select(m => m.Groups[1].Value)
                                 .Where(v => !string.IsNullOrEmpty(v)));
                        i++;
                        break;
                    }
                    pyLines.Add(lines[i]);
                    i++;
                }
                // Reuse the same BEGIN SCRIPT path by prepending "BEGIN SCRIPT Lang"
                // and appending "END SCRIPT -> VARS ..." then recursing once.
                string langKw = isIronPy ? "IronPython" : "Python";
                string reconstructed = $"BEGIN SCRIPT {langKw}\n" +
                                       string.Join("\n", pyLines) +
                                       $"\nEND SCRIPT -> VARS \"{pyOut}\"";
                result.AppendLine(ConvertLSCodeToLoliCode(reconstructed));
                continue;
            }

            if (trimmed.StartsWith("BEGIN SCRIPT", StringComparison.OrdinalIgnoreCase))
            {
                // Extract the language name from "BEGIN SCRIPT IronPython"
                string scriptLang = "IronPython";
                string afterKw = trimmed.Substring("BEGIN SCRIPT".Length).Trim();
                if (!string.IsNullOrEmpty(afterKw)) scriptLang = afterKw;

                // Map SilverBullet ScriptingLanguage name to OB2 INTERPRETER name.
                // Valid OB2 interpreters: Jint, NodeJS, IronPython, Python.
                // CSharp is handled separately (emitted as raw inline C#, not BLOCK:Script).
                string interpName = scriptLang.ToLowerInvariant() switch {
                    "javascript" or "js"      => "Jint",
                    "nodejs" or "node.js"     => "NodeJS",
                    "python"                  => "Python",
                    "ironpython"              => "IronPython",
                    "lua"                     => "Jint",      // no Lua in OB2; Jint is closest runtime
                    "typescript"              => "Jint",      // TypeScript superset of JS; Jint handles most TS
                    "csharp" or "c#"          => "CSharp",    // caught below; emitted as raw inline C#
                    _                         => scriptLang   // pass through (Jint, NodeJS, etc. already valid)
                };

                // Two-pass: collect body first, then scan for <VARNAME> input refs
                i++;
                var scriptLines = new List<string>();
                string scriptOutputs = "";
                while (i < lines.Length)
                {
                    string pyTrimmed = lines[i].TrimStart();
                    if (pyTrimmed.StartsWith("END SCRIPT", StringComparison.OrdinalIgnoreCase))
                    {
                        var om = Regex.Match(pyTrimmed,
                            @"END\s+SCRIPT\s*->\s*VARS\s+(.+)$",
                            RegexOptions.IgnoreCase);
                        if (om.Success)
                            scriptOutputs = string.Join(" ",
                                Regex.Matches(om.Groups[1].Value, @"""([^""]*)""")
                                     .Cast<Match>()
                                     .Select(m => m.Groups[1].Value)
                                     .Where(v => !string.IsNullOrEmpty(v)));
                        i++;
                        break;
                    }
                    scriptLines.Add(lines[i]);
                    i++;
                }

                // ── CSharp: OB2 has no INTERPRETER:CSharp in BLOCK:Script.
                // Emit the body as raw inline C# — OB2 compiles all LoliCode as one Roslyn script,
                // so "var NONCE = ..." becomes a Roslyn local accessible to subsequent blocks.
                // In SilverBullet, CollectDeclaredLocals() picks up these locals and
                // ResolveAtVarRefs() resolves @NONCE → bare local (not data.GetVar), so no
                // data.Variables.Set() calls are needed — and they would fail in OB2 anyway.
                if (interpName.Equals("CSharp", StringComparison.OrdinalIgnoreCase) ||
                    interpName.Equals("C#",     StringComparison.OrdinalIgnoreCase))
                {
                    foreach (string sl in scriptLines)
                        result.AppendLine(sl);
                    continue; // i is already past END SCRIPT; rejoin outer loop
                }

                // SB special var → (OB2 variable name, ConstantString source reference)
                // OB2 compiles LoliCode to C# via Roslyn; INPUT vars must exist as C# local variables.
                // Pre-populated runtime vars (USERNAME, PASSWORD, etc.) are in data.Variables but NOT
                // as C# locals, so INPUT USERNAME fails with CS0103. Fix: emit a ConstantString block
                // before the script to create the C# local variable from the correct OB2 source.
                var sbSpecialVarDefs = new[]
                {
                    (SbNames: new[]{"USER","USERNAME"},     Ob2Name:"USERNAME",     Ob2Source:"@input.USERNAME"),
                    (SbNames: new[]{"PASS","PASSWORD"},     Ob2Name:"PASSWORD",     Ob2Source:"@input.PASSWORD"),
                    (SbNames: new[]{"SOURCE"},              Ob2Name:"SOURCE",       Ob2Source:"@data.SOURCE"),
                    (SbNames: new[]{"RESPONSECODE"},        Ob2Name:"RESPONSECODE", Ob2Source:"@data.RESPONSECODE"),
                    (SbNames: new[]{"ADDRESS"},             Ob2Name:"ADDRESS",      Ob2Source:"@data.ADDRESS"),
                };

                // Build rename map (SB name → OB2 name) for body transformation
                var sbToOb2InputName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var sv in sbSpecialVarDefs)
                    foreach (var sbName in sv.SbNames)
                        sbToOb2InputName[sbName] = sv.Ob2Name;

                // If the first line inside BEGIN SCRIPT is the special round-trip marker
                // "// _INPUTS:ke,jti" (written by SegmentsToBlocks), recover the user's
                // explicit INPUT vars from it and skip the PascalCase auto-detection below.
                string explicitInputs = null;
                if (scriptLines.Count > 0 &&
                    scriptLines[0].TrimStart().StartsWith("// _INPUTS:", StringComparison.Ordinal))
                {
                    explicitInputs = scriptLines[0].TrimStart().Substring("// _INPUTS:".Length).Trim();
                    scriptLines.RemoveAt(0);
                }

                var inputVars = new List<string>();
                var seenInputVars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                string originalBody = string.Join("\n", scriptLines);

                var specialVarHelpers = new List<(string ob2Name, string ob2Source)>();

                if (explicitInputs != null)
                {
                    // Use the stored INPUT vars verbatim; no body scanning needed.
                    foreach (string iv in explicitInputs.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0))
                        if (seenInputVars.Add(iv)) inputVars.Add(iv);
                }
                else
                {
                    // 1. <VARNAME> patterns (user-defined vars, excluding built-ins)
                    foreach (Match vm in Regex.Matches(originalBody, @"<([A-Za-z0-9_]+)>"))
                    {
                        string vn = vm.Groups[1].Value;
                        if (!sbToOb2InputName.ContainsKey(vn) && seenInputVars.Add(vn))
                            inputVars.Add(vn);
                    }
                    // 2. Detect which SB built-in special vars are used; collect helper info
                    var seenHelpers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var sv in sbSpecialVarDefs)
                    {
                        bool used = sv.SbNames.Any(n => Regex.IsMatch(originalBody, $@"\b{n}\b"));
                        if (used && seenHelpers.Add(sv.Ob2Name))
                        {
                            specialVarHelpers.Add((sv.Ob2Name, sv.Ob2Source));
                            if (seenInputVars.Add(sv.Ob2Name))
                                inputVars.Add(sv.Ob2Name);
                        }
                    }

                    // 3. Detect bare PascalCase identifiers used but not defined in this script.
                    //    These come from previous blocks (e.g. FirexkeyboardTK) and must appear in INPUT.
                    {
                        var definedLocally = new HashSet<string>(StringComparer.Ordinal);
                        foreach (Match vm in Regex.Matches(originalBody, @"^[ \t]*import\s+(.+)$", RegexOptions.Multiline))
                            foreach (string mod in vm.Groups[1].Value.Split(','))
                            {
                                string modName = mod.Trim().Split(new[]{' '}, 2)[0];
                                definedLocally.Add(modName);
                                // Add individual components of dotted names (e.g. "import System.Xml" → "System", "Xml")
                                foreach (string part in modName.Split('.'))
                                    if (!string.IsNullOrEmpty(part)) definedLocally.Add(part);
                            }
                        foreach (Match vm in Regex.Matches(originalBody, @"^[ \t]*from\s+(\S+)\s+import\s+(.+)$", RegexOptions.Multiline))
                        {
                            // Add module path components (e.g. "from System.Text import Encoding" → adds "System", "Text")
                            foreach (string part in vm.Groups[1].Value.Split('.'))
                                if (!string.IsNullOrEmpty(part)) definedLocally.Add(part);
                            // Add imported names
                            foreach (string imp in vm.Groups[2].Value.Split(','))
                            {
                                string[] parts = imp.Trim().Split(new[]{' '}, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length > 0) definedLocally.Add(parts[parts.Length - 1]);
                            }
                        }
                        foreach (Match vm in Regex.Matches(originalBody, @"^([A-Za-z_][A-Za-z0-9_]*)\s*=", RegexOptions.Multiline))
                            definedLocally.Add(vm.Groups[1].Value);
                        foreach (Match vm in Regex.Matches(originalBody, @"\bdef\s+([A-Za-z_][A-Za-z0-9_]*)\s*\("))
                            definedLocally.Add(vm.Groups[1].Value);
                        foreach (Match vm in Regex.Matches(originalBody, @"\bfor\s+([A-Za-z_][A-Za-z0-9_]*)\s+in\b"))
                            definedLocally.Add(vm.Groups[1].Value);
                        foreach (string s in new[]{
                            "True","False","None","str","int","float","bool","list","dict","set","tuple",
                            "bytes","unicode","bytearray","range","len","print","sorted","reversed",
                            "enumerate","zip","map","filter","sum","min","max","abs","round",
                            "isinstance","issubclass","hasattr","getattr","setattr","type","ord","chr",
                            "hex","bin","iter","next","object","open","format","repr","hash","id",
                            "any","all","vars","dir","help","Exception","ValueError","TypeError",
                            "KeyError","IndexError","AttributeError","NameError","ImportError",
                            "StopIteration","RuntimeError","NotImplementedError" })
                            definedLocally.Add(s);
                        // Strip string literals so literal content doesn't produce false positives
                        string scanBody = Regex.Replace(originalBody, @"""(?:[^""\\]|\\.)*""", "\"\"");
                        scanBody = Regex.Replace(scanBody, @"'(?:[^'\\]|\\.)*'", "''");
                        // PascalCase names (≥4 chars, mixed case, not preceded by `.`)
                        foreach (Match vm in Regex.Matches(scanBody, @"(?<![.\w])([A-Z][A-Za-z0-9]{3,})\b"))
                        {
                            string vn = vm.Groups[1].Value;
                            if (vn.Any(c => char.IsLower(c))
                                && !definedLocally.Contains(vn)
                                && !sbToOb2InputName.ContainsKey(vn)
                                && seenInputVars.Add(vn))
                                inputVars.Add(vn);
                        }
                    }
                }

                // OB2 accepts "INPUT " with no names when there are no external variables.
                // (An undefined dummy like "INPUT dummy" causes CS0103 in OB2's Roslyn compiler.)

                // Transform the script body for OB2 compatibility:
                // 1. Replace "<VARNAME>" / '<VARNAME>' string literals → bare VARNAME
                string transformedBody = Regex.Replace(originalBody,
                    @"[""']<([A-Za-z0-9_]+)>[""']", "$1");
                // 2. Replace bare SB special var names with their OB2 INPUT names
                foreach (var kv in sbToOb2InputName)
                    transformedBody = Regex.Replace(transformedBody, $@"\b{kv.Key}\b", kv.Value);

                // 3. IronPython: fix deep namespace imports.
                // `import System.A.B` creates a namespace object but does NOT expose CLR types as
                // attributes, and `from System.A.B import Type` fails unless the assembly is loaded.
                // Fix: add `clr.AddReference("System.A.B")` + `from System.A.B import Type1, ...`
                // and rewrite all `System.A.B.TypeName` → bare `TypeName` in the script body.
                if (interpName.Equals("IronPython", StringComparison.OrdinalIgnoreCase))
                {
                    string ipBody = transformedBody;
                    // Collect all multi-level namespace import lines (System.X.Y or deeper)
                    var multiNsImports = Regex.Matches(ipBody,
                            @"^import (System(?:\.\w+){1,})\s*$", RegexOptions.Multiline)
                        .Cast<Match>()
                        .Select(m => m.Groups[1].Value)
                        .Distinct()
                        .ToList();
                    bool needClr = false;
                    foreach (string ns in multiNsImports)
                    {
                        // Find all PascalCase identifiers used as types: ns.TypeName.Member
                        var typeNames = Regex.Matches(ipBody,
                                $@"\b{Regex.Escape(ns)}\.([A-Z][A-Za-z0-9]*)(?=\.)")
                            .Cast<Match>()
                            .Select(m => m.Groups[1].Value)
                            .Distinct()
                            .OrderBy(x => x)
                            .ToList();
                        if (typeNames.Count > 0)
                        {
                            // Rewrite namespace-qualified usages → bare type names
                            foreach (string typeName in typeNames)
                                ipBody = Regex.Replace(ipBody,
                                    $@"\b{Regex.Escape(ns)}\.{Regex.Escape(typeName)}\b", typeName);
                            // Replace import line with clr.AddReference + from-import
                            string replacement = $"clr.AddReference(\"{ns}\")\nfrom {ns} import {string.Join(", ", typeNames)}";
                            ipBody = Regex.Replace(ipBody,
                                $@"^import {Regex.Escape(ns)}\s*$", replacement, RegexOptions.Multiline);
                            needClr = true;
                        }
                    }
                    // Prepend `import clr` if not already present
                    if (needClr && !Regex.IsMatch(ipBody, @"^import clr\s*$", RegexOptions.Multiline))
                        ipBody = "import clr\n" + ipBody;

                    // 4. System.Uri.EscapeDataString / UnescapeDataString work natively in IronPython
                    // because RunScript() pre-loads the System.Uri assembly. No urllib shim needed.
                    // Remove bare `import System.Uri` lines — the pre-loaded assembly covers them.
                    ipBody = Regex.Replace(ipBody, @"^import System\.Uri\s*$\n?", "", RegexOptions.Multiline);

                    // 5. Replace Python stdlib `time` and `random` with .NET equivalents.
                    // OB2's IronPython cannot load Python's `time` or `random` modules.
                    bool usesTime   = Regex.IsMatch(ipBody, @"\btime\.time\s*\(\)");
                    bool usesRandom = Regex.IsMatch(ipBody, @"\brandom\.(choice|randint|random|shuffle|uniform)\b");
                    if (usesTime || usesRandom)
                    {
                        // Remove 'time' and/or 'random' from `import a, b, c` lines
                        ipBody = Regex.Replace(ipBody, @"^import\s+(.+)$", m => {
                            var mods = m.Groups[1].Value.Split(',')
                                .Select(p => p.Trim())
                                .Where(p => !string.IsNullOrEmpty(p)
                                         && !(usesTime   && string.Equals(p, "time",   StringComparison.Ordinal))
                                         && !(usesRandom && string.Equals(p, "random", StringComparison.Ordinal)))
                                .ToList();
                            return mods.Count > 0 ? "import " + string.Join(", ", mods) : "";
                        }, RegexOptions.Multiline);
                        // Collapse any triple blank lines introduced by removing entire import lines
                        ipBody = Regex.Replace(ipBody, @"\n[ \t]*\n[ \t]*\n", "\n\n");

                        // Build the new System imports to inject
                        var sysExtras = new List<string>();
                        if (usesTime)   sysExtras.Add("DateTime");
                        if (usesRandom) sysExtras.Add("Random as SysRandom");
                        string extraSysStr = string.Join(", ", sysExtras);

                        // Append to the existing `from System import ...` line (not sub-namespace lines)
                        bool mergedIntoSys = false;
                        ipBody = Regex.Replace(ipBody, @"^(from System import )(.+)$", m => {
                            if (mergedIntoSys) return m.Value;
                            mergedIntoSys = true;
                            return m.Groups[1].Value + m.Groups[2].Value.TrimEnd() + ", " + extraSysStr;
                        }, RegexOptions.Multiline);
                        // No existing `from System import` line — add one after the import block
                        if (!mergedIntoSys)
                            ipBody = IpInsertAfterImportBlock(ipBody, $"from System import {extraSysStr}");

                        // Inject `_rng = SysRandom()` once after the import block
                        if (usesRandom && !ipBody.Contains("SysRandom()"))
                            ipBody = IpInsertAfterImportBlock(ipBody, "_rng  = SysRandom()");

                        // Replace time.time() → .NET DateTime expression
                        if (usesTime)
                            ipBody = Regex.Replace(ipBody, @"\btime\.time\s*\(\)",
                                "(DateTime.UtcNow - DateTime(1970, 1, 1)).TotalSeconds");

                        if (usesRandom)
                        {
                            // Replace random.choice(POOL) → POOL[int(_rng.Next(len(POOL)))]
                            ipBody = Regex.Replace(ipBody, @"\brandom\.choice\(([A-Za-z0-9_]+)\)",
                                mc => $"{mc.Groups[1].Value}[int(_rng.Next(len({mc.Groups[1].Value})))]");
                            // Make empty-string joins return unicode so .NET string interop works correctly
                            ipBody = ipBody.Replace("\"\".join(", "u\"\".join(");
                        }
                    }

                    // 6. Transform SilverBullet-specific .NET patterns that fail in OB2's IronPython.
                    // SilverBullet pre-loads all .NET assemblies; OB2 has a restricted environment.

                    // 6a. Security.Cryptography.HMACSHA1 → clr + from-import + bare HMACSHA1
                    if (Regex.IsMatch(ipBody, @"\bSecurity\.Cryptography\.HMACSHA1\b"))
                    {
                        if (!Regex.IsMatch(ipBody, @"clr\.AddReference\(""System\.Security\.Cryptography"))
                            ipBody = IpInsertAfterImportBlock(ipBody,
                                "clr.AddReference(\"System.Security.Cryptography.Algorithms\")");
                        bool hasSCImport = Regex.IsMatch(ipBody,
                            @"^from System\.Security\.Cryptography import", RegexOptions.Multiline);
                        if (!hasSCImport)
                            ipBody = IpInsertAfterImportBlock(ipBody,
                                "from System.Security.Cryptography import HMACSHA1");
                        else
                            ipBody = Regex.Replace(ipBody,
                                @"^(from System\.Security\.Cryptography import\s+)(.+)$", m => {
                                    if (m.Groups[2].Value.Contains("HMACSHA1")) return m.Value;
                                    return m.Groups[1].Value + m.Groups[2].Value.TrimEnd() + ", HMACSHA1";
                                }, RegexOptions.Multiline);
                        ipBody = ipBody.Replace("Security.Cryptography.HMACSHA1", "HMACSHA1");
                    }

                    // 6b. Array.CreateInstance(Byte,N) + RandomNumberGenerator.Create().GetBytes(BUF)
                    //     → remove both lines; replace "".join(pool[b%N] for b in BUF) with SysRandom
                    {
                        var bufVarSizes = new Dictionary<string, string>(StringComparer.Ordinal);
                        ipBody = Regex.Replace(ipBody,
                            @"[ \t]*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*Array\.CreateInstance\(Byte,\s*([^)\n]+)\)\r?\n" +
                            @"[ \t]*(?:Security\.Cryptography\.)?RandomNumberGenerator\.Create\(\)\.GetBytes\(\1\)",
                            m => { bufVarSizes[m.Groups[1].Value] = m.Groups[2].Value.Trim(); return ""; },
                            RegexOptions.Multiline);
                        if (bufVarSizes.Count > 0)
                        {
                            if (!ipBody.Contains("SysRandom"))
                            {
                                bool addedSR = false;
                                ipBody = Regex.Replace(ipBody, @"^(from System import )(.+)$", m => {
                                    if (addedSR) return m.Value;
                                    addedSR = true;
                                    return m.Groups[1].Value + m.Groups[2].Value.TrimEnd() + ", Random as SysRandom";
                                }, RegexOptions.Multiline);
                                if (!addedSR)
                                    ipBody = IpInsertAfterImportBlock(ipBody, "from System import Random as SysRandom");
                                ipBody = IpInsertAfterImportBlock(ipBody, "_rng  = SysRandom()");
                            }
                            foreach (var kv in bufVarSizes)
                                ipBody = Regex.Replace(ipBody,
                                    $@"""\"".join\(([A-Za-z_][A-Za-z0-9_]*)\[b\s*%\s*\d+\]\s+for\s+b\s+in\s+{Regex.Escape(kv.Key)}\)",
                                    m2 => $"u\"\".join({m2.Groups[1].Value}[int(_rng.Next(len({m2.Groups[1].Value})))] for _ in range({kv.Value}))");
                            // Remove leftover RandomNumberGenerator calls
                            ipBody = Regex.Replace(ipBody,
                                @"[ \t]*(?:Security\.Cryptography\.)?RandomNumberGenerator\.Create\(\)\.GetBytes\([^)\n]+\)\r?\n?",
                                "");
                        }
                    }

                    // 6c. Text.Encoding → Encoding (add from System.Text import Encoding)
                    if (Regex.IsMatch(ipBody, @"\bText\.Encoding\b"))
                    {
                        bool addedEnc = false;
                        ipBody = Regex.Replace(ipBody, @"^(from System\.Text import )(.+)$", m => {
                            if (addedEnc) return m.Value; addedEnc = true;
                            return m.Groups[2].Value.TrimEnd().Contains("Encoding") ? m.Value
                                : m.Groups[1].Value + m.Groups[2].Value.TrimEnd() + ", Encoding";
                        }, RegexOptions.Multiline);
                        if (!addedEnc)
                            ipBody = IpInsertAfterImportBlock(ipBody, "from System.Text import Encoding");
                        ipBody = ipBody.Replace("Text.Encoding", "Encoding");
                    }

                    // 6d. System.Uri.EscapeDataString → custom pure-Python uri_esc function
                    if (Regex.IsMatch(ipBody, @"\bSystem\.Uri\.EscapeDataString\b"))
                    {
                        if (!ipBody.Contains("def uri_esc("))
                        {
                            string uriEscDef =
                                "def uri_esc(s):\n" +
                                "    out, _sf = [], set(u\"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~\")\n" +
                                "    for c in str(s):\n" +
                                "        if c in _sf: out.append(c)\n" +
                                "        else:\n" +
                                "            for b in c.encode(\"utf-8\"): out.append(\"%%%02X\" % (b if isinstance(b, int) else ord(b)))\n" +
                                "    return \"\".join(out)";
                            ipBody = IpInsertAfterImportBlock(ipBody, uriEscDef);
                        }
                        ipBody = Regex.Replace(ipBody, @"\bSystem\.Uri\.EscapeDataString\(", "uri_esc(");
                        // Remove bare `import System` if System is no longer used with dot notation
                        if (!Regex.IsMatch(ipBody, @"\bSystem\.[A-Za-z]"))
                            ipBody = Regex.Replace(ipBody, @"^import System\s*$\r?\n?", "",
                                RegexOptions.Multiline);
                    }

                    // 6e. Remove now-unused names from `from System import ...`
                    {
                        // Strip all import/from/clr lines before checking — a name like Array might still
                        // appear inside "from System.Xxx import Yyy" even though the code no longer uses it.
                        string nonImportBody = Regex.Replace(ipBody,
                            @"^[ \t]*(?:import\b|from\b|clr\.).*$", "", RegexOptions.Multiline);
                        var toTrim = new HashSet<string>(
                            new[]{ "Array", "Byte", "Security", "Text" }
                            .Where(n => !Regex.IsMatch(nonImportBody, $@"\b{Regex.Escape(n)}\b")));
                        if (toTrim.Count > 0)
                        {
                            ipBody = Regex.Replace(ipBody, @"^from System import (.+)$", m => {
                                var parts = m.Groups[1].Value.Split(',')
                                    .Select(p => p.Trim())
                                    .Where(p => !string.IsNullOrEmpty(p) && !toTrim.Contains(p))
                                    .ToList();
                                return parts.Count > 0 ? "from System import " + string.Join(", ", parts) : "";
                            }, RegexOptions.Multiline);
                            ipBody = Regex.Replace(ipBody, @"\n[ \t]*\n[ \t]*\n", "\n\n");
                        }
                    }

                    // Ensure `import clr` is present whenever a clr.AddReference() was added
                    if (Regex.IsMatch(ipBody, @"^clr\.AddReference\(", RegexOptions.Multiline)
                        && !Regex.IsMatch(ipBody, @"^import\s+clr\b", RegexOptions.Multiline))
                        ipBody = "import clr\n" + ipBody;

                    transformedBody = ipBody;
                }

                // IronPython treats scripts as Python 2 source files and requires an explicit
                // encoding declaration when the script contains non-ASCII characters (e.g. Unicode
                // in comments like ──, →). Add it as the very first line if needed.
                if (interpName.Equals("IronPython", StringComparison.OrdinalIgnoreCase)
                    && transformedBody.Any(c => c > 127))
                {
                    const string codingDecl = "# -*- coding: utf-8 -*-";
                    if (!transformedBody.TrimStart().StartsWith(codingDecl, StringComparison.Ordinal))
                        transformedBody = codingDecl + "\n" + transformedBody;
                }

                scriptLines = new List<string>(transformedBody.Split('\n'));

                // Emit ConstantString helper blocks to create C# local variables for special vars.
                // OB2's Roslyn compiler generates `scope.SetVariable("USERNAME", USERNAME)` for INPUT,
                // referencing USERNAME as a C# local var — so it must be created by a prior block.
                foreach (var (ob2Name, ob2Source) in specialVarHelpers)
                {
                    result.AppendLine("BLOCK:ConstantString");
                    result.AppendLine($"  value = {ob2Source}");
                    result.AppendLine($"  => VAR @{ob2Name}");
                    result.AppendLine("ENDBLOCK");
                    result.AppendLine();
                }

                result.AppendLine("BLOCK:Script");
                result.AppendLine($"INTERPRETER:{interpName}");
                // OB2 requires INPUT line before BEGIN SCRIPT.
                // Empty INPUT (no variables) is valid in OB2 — emits "INPUT " with trailing space.
                // SilverBullet's own parser ignores "INPUT" lines with no recognized variables.
                result.AppendLine($"INPUT {string.Join(",", inputVars)}");
                result.AppendLine("BEGIN SCRIPT");
                foreach (string sl in scriptLines)
                    result.AppendLine(sl);
                result.AppendLine("END SCRIPT");
                if (!string.IsNullOrEmpty(scriptOutputs))
                {
                    foreach (string outVar in scriptOutputs.Split(new[]{' ', ','}, StringSplitOptions.RemoveEmptyEntries))
                        result.AppendLine($"OUTPUT String @{outVar}");
                }
                result.AppendLine("ENDBLOCK");
            }
            else
            {
                result.AppendLine(line);
                i++;
            }
        }
        return result.ToString().TrimEnd();
    }

    // Inserts newLine immediately after the last import/from/clr.* line in an IronPython script body.
    private static string IpInsertAfterImportBlock(string code, string newLine)
    {
        var lines = code.Split('\n').ToList();
        int lastIdx = -1;
        for (int i = 0; i < lines.Count; i++)
        {
            string t = lines[i].TrimStart();
            if (t.StartsWith("import ", StringComparison.Ordinal) ||
                t.StartsWith("from ",   StringComparison.Ordinal) ||
                t.StartsWith("clr.",    StringComparison.Ordinal))
                lastIdx = i;
        }
        lines.Insert(lastIdx >= 0 ? lastIdx + 1 : 0, newLine);
        return string.Join("\n", lines);
    }

    private static string BlockHeader(string typeName, bool disabled)
        => disabled ? $"BLOCK:{typeName}{Environment.NewLine}DISABLED" : $"BLOCK:{typeName}";

    private static string RequestToLoliCode(BlockRequest b, Dictionary<string, string> headerValueReplacements = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine(BlockHeader("HttpRequest", b.Disabled));
        if (!string.IsNullOrEmpty(b.Label)) sb.AppendLine($"  LABEL:{b.Label}");

        sb.AppendLine($"  url = {ToOb2String(b.Url)}");
        sb.AppendLine($"  method = {b.Method}");

        // OB2-compatible body: positional TYPE:STANDARD + $"body" + "content-type"
        if (!string.IsNullOrEmpty(b.PostData))
        {
            sb.AppendLine("  TYPE:STANDARD");
            sb.AppendLine($"  {ToOb2String(b.PostData)}");
            sb.AppendLine($"  \"{EscapeOb2(b.ContentType ?? "application/x-www-form-urlencoded")}\"");
        }
        else if (b.RequestType == RequestType.BasicAuth)
        {
            // OB2 positional format: TYPE:BASICAUTH / "user" / "pass"
            sb.AppendLine("  TYPE:BASICAUTH");
            sb.AppendLine($"  {ToOb2String(b.AuthUser ?? "")}");
            sb.AppendLine($"  {ToOb2String(b.AuthPass ?? "")}");
        }
        else if (b.RequestType == RequestType.Raw)
        {
            // OB2 positional format: TYPE:RAW / body / "content-type"
            // @variable references must NOT be quoted (OB2 format); literal data uses "..."
            sb.AppendLine("  TYPE:RAW");
            string rawBodyLine = !string.IsNullOrEmpty(b.RawData) && b.RawData.StartsWith("@")
                ? b.RawData
                : ToOb2String(b.RawData ?? "");
            sb.AppendLine($"  {rawBodyLine}");
            sb.AppendLine($"  \"{EscapeOb2(b.ContentType ?? "application/octet-stream")}\"");
        }
        else if (b.RequestType == RequestType.Multipart)
        {
            // OB2 positional format: TYPE:MULTIPART / "boundary" / CONTENT:STRING|FILE ...
            sb.AppendLine("  TYPE:MULTIPART");
            sb.AppendLine($"  {ToOb2String(b.MultipartBoundary ?? "")}");
            foreach (var part in b.MultipartContents)
            {
                if (part.Type == MultipartContentType.File)
                    sb.AppendLine($"  CONTENT:FILE {ToOb2String(part.Name ?? "")} {ToOb2String(part.Value ?? "")} {ToOb2String(part.ContentType ?? "application/octet-stream")}");
                else
                    sb.AppendLine($"  CONTENT:STRING {ToOb2String(part.Name ?? "")} {ToOb2String(part.Value ?? "")} {ToOb2String(part.ContentType ?? "text/plain")}");
            }
        }
        else if ((int)b.RequestType != 0)
        {
            sb.AppendLine($"  requestType = {b.RequestType}");
        }

        if (b.ProtocolVersion != null && b.ProtocolVersion.ToString() != "1.1")
            sb.AppendLine($"  httpVersion = \"{b.ProtocolVersion.Major}.{b.ProtocolVersion.Minor}\"");

        if (!b.AutoRedirect)
            sb.AppendLine("  autoRedirect = false");

        // AcceptEncoding and AllowEmptyHeaderValues are SilverBullet-only settings;
        // OB2's BLOCK:HttpRequest parser does not recognise them and throws a parse error.
        // They are intentionally NOT emitted here. The LoliCode→Block parser still reads
        // them for backward compatibility with old SilverBullet LoliCode configs.

        if (!b.ReadResponseSource)
            sb.AppendLine("  readResponseContent = false");

        if (b.EncodeContent)
            sb.AppendLine("  urlEncodeContent = true");

        if (b.CustomCookies != null && b.CustomCookies.Count > 0)
        {
            var cookiePairs = b.CustomCookies.Select(kv =>
                $"({JsonConvert.SerializeObject(kv.Key)}, {JsonConvert.SerializeObject(kv.Value)})");
            sb.AppendLine($"  customCookies = ${{{string.Join(", ", cookiePairs)}}}");
        }

        if (b.CustomHeaders != null && b.CustomHeaders.Count > 0)
        {
            var pairs = b.CustomHeaders.Select(kv => {
                string val = kv.Value;
                if (headerValueReplacements != null)
                    foreach (var (from, to) in headerValueReplacements)
                        val = val.Replace(from, to, StringComparison.Ordinal);
                return $"({JsonConvert.SerializeObject(kv.Key)}, {JsonConvert.SerializeObject(val)})";
            });
            sb.AppendLine($"  customHeaders = ${{{string.Join(", ", pairs)}}}");
        }

        if (b.SecurityProtocol != SecurityProtocol.SystemDefault)
            sb.AppendLine($"  securityProtocol = {b.SecurityProtocol}");

        // New options (only emit when non-default to keep scripts concise)
        if (b.HttpLibrary != HttpLibrary.SystemNet)
        {
            sb.AppendLine($"  httpLibrary = {b.HttpLibrary}");
            if (b.HttpLibrary == HttpLibrary.CurlImpersonate &&
                b.CurlImpersonateProfile != CurlImpersonateBrowserProfile.Chrome142)
                sb.AppendLine($"  curlImpersonateBrowserProfile = {b.CurlImpersonateProfile}");
        }
        if (!b.IgnoreCertificateValidation)
            sb.AppendLine("  ignoreCertificateValidation = false");
        if (b.AlwaysSendContent)
            sb.AppendLine("  alwaysSendContent = true");
        if (!string.IsNullOrEmpty(b.CodePagesEncoding))
            sb.AppendLine($"  codePagesEncoding = {ToOb2String(b.CodePagesEncoding)}");
        if (b.RequestTimeoutMs > 0)
            sb.AppendLine($"  timeoutMilliseconds = {b.RequestTimeoutMs}");
        if (!b.SaveResponseCookies)
            sb.AppendLine("  saveResponseCookies = false");
        if (!b.LoadRequestCookies)
            sb.AppendLine("  loadRequestCookies = false");
        if (b.RetryCount > 0)
        {
            sb.AppendLine($"  retryCount = {b.RetryCount}");
            if (b.RetryDelayMs != 1000)
                sb.AppendLine($"  retryDelay = {b.RetryDelayMs}");
        }

        sb.Append("ENDBLOCK");

        // SilverBullet-only properties (responseType, downloadPath, etc.) cannot go inside
        // BLOCK:HttpRequest because OB2 throws LoliCodeParsingException on unknown settings.
        // They are emitted as a C# comment immediately before the block; OB2 treats it as a
        // harmless comment while SilverBullet's compiler and deserializer read it back.
        if (b.ResponseType != ResponseType.String)
        {
            var sbParts = new List<string> { $"responseType={b.ResponseType}" };
            if (b.ResponseType == ResponseType.File)
            {
                if (!string.IsNullOrEmpty(b.DownloadPath))
                    sbParts.Add($"downloadPath={ToOb2String(b.DownloadPath)}");
                if (b.SaveAsScreenshot)
                    sbParts.Add("saveAsScreenshot=true");
            }
            else if (b.ResponseType == ResponseType.Base64String)
            {
                if (!string.IsNullOrEmpty(b.OutputVariable))
                    sbParts.Add($"outputVariable={ToOb2String(b.OutputVariable)}");
            }
            return $"// _SB:{string.Join("|", sbParts)}{Environment.NewLine}{sb}";
        }
        return sb.ToString();
    }

    private static string ParseToLoliCode(BlockParse b)
    {
        var sb = new StringBuilder();
        sb.AppendLine(BlockHeader("Parse", b.Disabled));
        if (!string.IsNullOrEmpty(b.Label)) sb.AppendLine($"  LABEL:{b.Label}");

        // OB2-compatible: "input = @data.SOURCE" / "@data.COOKIES[\"name\"]"
        if (!string.IsNullOrEmpty(b.ParseTarget))
            sb.AppendLine($"  input = {SbTargetToOb2Input(b.ParseTarget)}");

        switch (b.Type)
        {
            case ParseType.LR:
                sb.AppendLine($"  leftDelim = {ToOb2String(b.LeftString)}");
                sb.AppendLine($"  rightDelim = {ToOb2String(b.RightString)}");
                break;
            case ParseType.REGEX:
                // OB2 uses "pattern" and "outputFormat" (not regexString/regexOutput)
                sb.AppendLine($"  pattern = {ToOb2String(b.RegexString)}");
                if (!string.IsNullOrEmpty(b.RegexOutput))
                    sb.AppendLine($"  outputFormat = {ToOb2String(b.RegexOutput)}");
                break;
            case ParseType.CSS:
                sb.AppendLine($"  cssSelector = {ToOb2String(b.CssSelector)}");
                if (!string.IsNullOrEmpty(b.AttributeName))
                    sb.AppendLine($"  attributeName = {ToOb2String(b.AttributeName)}");
                break;
            case ParseType.JSON:
                sb.AppendLine($"  jToken = {ToOb2String(b.JsonField)}");
                break;
        }

        if (b.Recursive) sb.AppendLine("  RECURSIVE");

        // OB2-compatible MODE directive. OB2 uses mixed-case names (Json, Regex, XPath)
        // while SilverBullet's ParseType enum is all-caps (JSON, REGEX, XPATH, LR, CSS).
        string ob2Mode = b.Type switch {
            ParseType.JSON  => "Json",
            ParseType.REGEX => "Regex",
            ParseType.CSS   => "CSS",
            ParseType.LR    => "LR",
            _               => b.Type.ToString(),
        };
        sb.AppendLine($"  MODE:{ob2Mode}");

        if (b.DotMatches)     sb.AppendLine("  dotMatches = true");
        if (!b.CaseSensitive) sb.AppendLine("  caseSensitive = false");
        if (!string.IsNullOrEmpty(b.Prefix)) sb.AppendLine($"  prefix = {ToOb2String(b.Prefix)}");
        if (!string.IsNullOrEmpty(b.Suffix)) sb.AppendLine($"  suffix = {ToOb2String(b.Suffix)}");

        if (!string.IsNullOrEmpty(b.VariableName))
            sb.AppendLine(b.IsCapture ? $"  => CAP @{b.VariableName}" : $"  => VAR @{b.VariableName}");

        sb.Append("ENDBLOCK");

        return sb.ToString();
    }

    // OB2 built-in BotData properties — these use @data.VARNAME.
    // All other variables are user-defined and use @VARNAME (no data. prefix).
    private static readonly HashSet<string> Ob2BuiltInDataVars = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "SOURCE", "RESPONSECODE", "ADDRESS", "ERROR"
    };

    // Convert SilverBullet ParseTarget notation to OB2 input reference format.
    // <SOURCE>         → @data.SOURCE    (built-in BotData property)
    // <COOKIES(name)>  → @data.COOKIES["name"]
    // <HEADERS(name)>  → @data.HEADERS["name"]
    // <MYVAR>          → @MYVAR          (user variable — no data. prefix)
    private static string SbTargetToOb2Input(string sbTarget)
    {
        if (string.IsNullOrEmpty(sbTarget)) return "@data.SOURCE";
        var cookieM = Regex.Match(sbTarget, @"^<COOKIES\(([^)]+)\)>$", RegexOptions.IgnoreCase);
        if (cookieM.Success) return $"@data.COOKIES[\"{cookieM.Groups[1].Value}\"]";
        var headerM = Regex.Match(sbTarget, @"^<HEADERS\(([^)]+)\)>$", RegexOptions.IgnoreCase);
        if (headerM.Success) return $"@data.HEADERS[\"{headerM.Groups[1].Value}\"]";
        if (sbTarget.StartsWith("<") && sbTarget.EndsWith(">"))
        {
            string varName = sbTarget.Substring(1, sbTarget.Length - 2);
            return Ob2BuiltInDataVars.Contains(varName)
                ? $"@data.{varName}"
                : $"@{varName}";
        }
        return sbTarget;
    }

    private static string ConstantStringToLoliCode(BlockFunction b)
    {
        var sb = new StringBuilder();
        sb.AppendLine(BlockHeader("ConstantString", b.Disabled));
        if (!string.IsNullOrEmpty(b.Label)) sb.AppendLine($"  LABEL:{b.Label}");
        if (!string.IsNullOrEmpty(b.InputString))
            sb.AppendLine($"  value = {ToOb2String(b.InputString)}");
        if (!string.IsNullOrEmpty(b.VariableName))
            sb.AppendLine(b.IsCapture ? $"  => CAP @{b.VariableName}" : $"  => VAR @{b.VariableName}");
        sb.Append("ENDBLOCK");
        return sb.ToString();
    }

    // Maps SilverBullet function types to their OB2 block names.
    // Types with a different OB2 name than their C# enum name are listed here.
    // All other function types fall back to using the enum name directly as the block name.
    // Maps SilverBullet function enum values to their OB2 block names (confirmed from OB2 binary).
    // Functions listed here are emitted as BLOCK:<Ob2Name> (OB2 dedicated block format).
    // Functions NOT listed fall back to BLOCK:Function with function = <EnumName> (SilverBullet-only format).
    private static readonly Dictionary<BlockFunction.Function, string> Ob2FunctionBlockNames =
        new Dictionary<BlockFunction.Function, string>
        {
            // Encoding / decoding
            { BlockFunction.Function.URLEncode,        "UrlEncode"       },
            { BlockFunction.Function.URLDecode,        "UrlDecode"       },
            { BlockFunction.Function.Base64Encode,     "UTF8ToBase64"    },
            { BlockFunction.Function.Base64Decode,     "Base64ToUTF8"    },
            { BlockFunction.Function.Unescape,         "Unescape"        },
            // Crypto
            { BlockFunction.Function.AESEncrypt,       "AESEncrypt"      },
            { BlockFunction.Function.AESDecrypt,       "AESDecrypt"      },
            { BlockFunction.Function.RSAEncrypt,       "RSAEncrypt"      },
            { BlockFunction.Function.Hash,             "Hash"            },
            { BlockFunction.Function.HMAC,             "Hmac"            },
            { BlockFunction.Function.Ntlm,             "NTLMHash"        },
            { BlockFunction.Function.SCrypt,           "ScryptString"    },
            { BlockFunction.Function.BCrypt,           "BCryptHash"      },
            // String operations
            { BlockFunction.Function.ToUppercase,      "ToUppercase"     },
            { BlockFunction.Function.ToLowercase,      "ToLowercase"     },
            { BlockFunction.Function.Translate,        "Translate"       },
            { BlockFunction.Function.Split,            "Split"           },
            { BlockFunction.Function.CharAt,           "CharAt"          },
            // Math
            { BlockFunction.Function.Ceil,             "Ceil"            },
            { BlockFunction.Function.Floor,            "Floor"           },
            { BlockFunction.Function.Round,            "Round"           },
            { BlockFunction.Function.RandomNum,        "RandomInteger"   },
            { BlockFunction.Function.RandomString,     "RandomString"    },
            // Byte arrays
            { BlockFunction.Function.MergeByteArrays,        "MergeByteArrays"        },
            // New SilverBullet functions
            { BlockFunction.Function.RegexReplace,       "RegexReplace"       },
            { BlockFunction.Function.XOR,                "XOR"                },
            { BlockFunction.Function.XORStrings,         "XORStrings"         },
            { BlockFunction.Function.RSADecrypt,         "RSADecrypt"         },
            { BlockFunction.Function.JWTEncode,          "JwtEncode"          },
            { BlockFunction.Function.MaxFloat,           "MaxFloat"           },
            { BlockFunction.Function.MinFloat,           "MinFloat"           },
            { BlockFunction.Function.RandomFloat,        "RandomFloat"        },
            { BlockFunction.Function.MaxInt,             "MaxInt"             },
            { BlockFunction.Function.MinInt,             "MinInt"             },
            { BlockFunction.Function.AddKeyValuePair,    "AddKeyValuePair"    },
            { BlockFunction.Function.GetKey,             "GetKey"             },
            { BlockFunction.Function.RemoveByKey,        "RemoveByKey"        },
            { BlockFunction.Function.CreateListOfNumbers,"CreateListOfNumbers"},
            { BlockFunction.Function.IndexOf,            "IndexOf"            },
            { BlockFunction.Function.ListToDict,         "ToDictionary"       },
            { BlockFunction.Function.BCryptVerify,       "BCryptVerify"       },
            { BlockFunction.Function.ScryptDeriveKey,    "ScryptDeriveKey"    },
            { BlockFunction.Function.AWS4Signature,      "AWS4Signature"      },
            // XOR: not in SilverBullet's Function enum — OB2 only
            // ID generation
            { BlockFunction.Function.GenerateGUID,     "GenerateGuid"    },
            // SilverBullet-native date functions (not in OB2)
            { BlockFunction.Function.GetRemainingDay,  "GetRemainingDay"   },
            // Date conversion
            { BlockFunction.Function.UnixTimeToDate,   "UnixTimeToDate"    },
            // Delay / Sleep
            { BlockFunction.Function.Delay,            "Delay"             },
            // Random User Agent
            { BlockFunction.Function.GetRandomUA,      "RandomUserAgent"   },
            // String length
            { BlockFunction.Function.Length,           "Length"            },
            // String replace
            { BlockFunction.Function.Replace,          "Replace"           },
            // String operations
            { BlockFunction.Function.Substring,        "Substring"         },
            { BlockFunction.Function.Trim,             "Trim"              },
            { BlockFunction.Function.ReverseString,    "Reverse"           },
            { BlockFunction.Function.CountOccurrences, "CountOccurrences"  },
            { BlockFunction.Function.HTMLEntityEncode, "EncodeHTMLEntities"},
            { BlockFunction.Function.HTMLEntityDecode, "DecodeHTMLEntities"},
            { BlockFunction.Function.Compute,          "Compute"           },
            // Date/time
            { BlockFunction.Function.CurrentUnixTime,  "CurrentUnixTime"   },
            { BlockFunction.Function.DateToUnixTime,   "DateToUnixTime"    },
        };

    private static string FunctionToLoliCode(BlockFunction b)
    {
        var sb = new StringBuilder();

        // OB2-dedicated types keep their OB2 block name so the compiler's dedicated cases handle them.
        // All other function types are emitted as BLOCK:Function with a function= property
        // so CompileBlock("Function") → CompileFunction handles them (instead of falling to
        // CompileReflection which silently fails because BlockHash, BlockAES, etc. don't exist).
        bool isOb2Dedicated = Ob2FunctionBlockNames.TryGetValue(b.FunctionType, out string ob2Block);

        // OB2 has no generic BLOCK:Function — unsupported function types are emitted as
        // BLOCK:ConstantString placeholders so OB2 doesn't crash with "Invalid block id: Function".
        if (!isOb2Dedicated)
        {
            sb.AppendLine(BlockHeader("ConstantString", b.Disabled));
            if (!string.IsNullOrEmpty(b.Label)) sb.AppendLine($"  LABEL:{b.Label}");
            sb.AppendLine($"  value = \"[UNSUPPORTED:{b.FunctionType}]\"");
            if (!string.IsNullOrEmpty(b.VariableName))
                sb.AppendLine(b.IsCapture ? $"  => CAP @{b.VariableName}" : $"  => VAR @{b.VariableName}");
            sb.Append("ENDBLOCK");
            return sb.ToString();
        }

        sb.AppendLine(BlockHeader(ob2Block, b.Disabled));
        if (!string.IsNullOrEmpty(b.Label)) sb.AppendLine($"  LABEL:{b.Label}");

        if (b.FunctionType == BlockFunction.Function.GenerateGUID)
        {
            sb.AppendLine($"  version = {b.GuidVer}");
            sb.AppendLine($"  format = {b.GuidFmt}");
            if (b.GuidUppercase) sb.AppendLine("  guidUppercase = true");
        }
        else if (b.FunctionType == BlockFunction.Function.MergeByteArrays)
        {
            if (!string.IsNullOrEmpty(b.InputString))
                sb.AppendLine($"  input = {SbInputToOb2Ref(b.InputString)}");
            if (!string.IsNullOrEmpty(b.SecondInput))
                sb.AppendLine($"  secondInput = {ToOb2String(b.SecondInput)}");
        }
        else if (b.FunctionType == BlockFunction.Function.Translate)
        {
            if (!string.IsNullOrEmpty(b.InputString))
                sb.AppendLine($"  input = {SbInputToOb2Ref(b.InputString)}");
            if (b.TranslationDictionary != null && b.TranslationDictionary.Count > 0)
            {
                var pairs = b.TranslationDictionary.Select(kv =>
                    $"({JsonConvert.SerializeObject(kv.Key)}, {JsonConvert.SerializeObject(kv.Value)})");
                sb.AppendLine($"  translations = {{{string.Join(", ", pairs)}}}");
            }
        }
        else if (b.FunctionType == BlockFunction.Function.UnixTimeToDate)
        {
            // b.InputString is a genuine variable ref only when it starts with '<'.
            // Old LoliScript stored the format string there (not an actual variable),
            // so treat anything that isn't a '<…>' ref as "no input" → bare @.
            if (!string.IsNullOrEmpty(b.InputString) && b.InputString.TrimStart().StartsWith("<"))
                sb.AppendLine($"  unixTime = {SbInputToOb2Ref(b.InputString)}");
            else
                sb.AppendLine("  unixTime = @");
            if (!string.IsNullOrEmpty(b.DateFormat) && b.DateFormat != "yyyy-MM-dd:HH-mm-ss")
                sb.AppendLine($"  format = \"{b.DateFormat.Replace("\"", "\\\"")}\"");
        }
        else if (b.FunctionType == BlockFunction.Function.Delay)
        {
            int ms = 0;
            if (!string.IsNullOrEmpty(b.InputString))
                int.TryParse(b.InputString.Trim(), out ms);
            sb.AppendLine($"  milliseconds = {Math.Max(0, ms)}");
            sb.Append("ENDBLOCK");
            return sb.ToString();
        }
        else if (b.FunctionType == BlockFunction.Function.RandomNum)
        {
            sb.AppendLine($"  minimum = {b.RandomMin ?? "0"}");
            sb.AppendLine($"  maximum = {b.RandomMax ?? "0"}");
            if (b.RandomZeroPad) sb.AppendLine("  randomZeroPad = true");
        }
        else if (b.FunctionType == BlockFunction.Function.RegexReplace)
        {
            if (!string.IsNullOrEmpty(b.InputString)) sb.AppendLine($"  original = {SbInputToOb2Ref(b.InputString)}");
            if (!string.IsNullOrEmpty(b.RegexMatch))  sb.AppendLine($"  pattern = {ToOb2String(b.RegexMatch)}");
            if (!string.IsNullOrEmpty(b.ReplaceWith)) sb.AppendLine($"  replacement = {ToOb2String(b.ReplaceWith)}");
        }
        else if (b.FunctionType == BlockFunction.Function.Substring)
        {
            if (!string.IsNullOrEmpty(b.InputString))     sb.AppendLine($"  input = {SbInputToOb2Ref(b.InputString)}");
            if (!string.IsNullOrEmpty(b.SubstringIndex))  sb.AppendLine($"  startIndex = {ToOb2String(b.SubstringIndex)}");
            if (!string.IsNullOrEmpty(b.SubstringLength)) sb.AppendLine($"  length = {ToOb2String(b.SubstringLength)}");
        }
        else if (b.FunctionType == BlockFunction.Function.CountOccurrences)
        {
            if (!string.IsNullOrEmpty(b.InputString))  sb.AppendLine($"  input = {SbInputToOb2Ref(b.InputString)}");
            if (!string.IsNullOrEmpty(b.StringToFind)) sb.AppendLine($"  stringToFind = {ToOb2String(b.StringToFind)}");
        }
        else if (b.FunctionType == BlockFunction.Function.DateToUnixTime)
        {
            if (!string.IsNullOrEmpty(b.InputString)) sb.AppendLine($"  datetime = {SbInputToOb2Ref(b.InputString)}");
            if (!string.IsNullOrEmpty(b.DateFormat))  sb.AppendLine($"  format = {ToOb2String(b.DateFormat)}");
            if (b.UnixTimeType != BlockFunction.DateToUnixTimeType.Seconds)
                sb.AppendLine($"  type = {b.UnixTimeType}");
        }
        else if (b.FunctionType == BlockFunction.Function.Replace)
        {
            if (!string.IsNullOrEmpty(b.InputString)) sb.AppendLine($"  original = {SbInputToOb2Ref(b.InputString)}");
            if (!string.IsNullOrEmpty(b.ReplaceWhat)) sb.AppendLine($"  toReplace = {ToOb2String(b.ReplaceWhat)}");
            if (!string.IsNullOrEmpty(b.ReplaceWith)) sb.AppendLine($"  replacement = {ToOb2String(b.ReplaceWith)}");
        }
        else if (b.FunctionType == BlockFunction.Function.XOR || b.FunctionType == BlockFunction.Function.XORStrings)
        {
            if (!string.IsNullOrEmpty(b.InputString)) sb.AppendLine($"  input = {SbInputToOb2Ref(b.InputString)}");
            if (!string.IsNullOrEmpty(b.SecondInput)) sb.AppendLine($"  key = {ToOb2String(b.SecondInput)}");
        }
        else if (b.FunctionType == BlockFunction.Function.RSADecrypt)
        {
            if (!string.IsNullOrEmpty(b.InputString)) sb.AppendLine($"  input = {SbInputToOb2Ref(b.InputString)}");
            if (!string.IsNullOrEmpty(b.RsaN))        sb.AppendLine($"  n = {ToOb2String(b.RsaN)}");
            if (!string.IsNullOrEmpty(b.RsaD))        sb.AppendLine($"  d = {ToOb2String(b.RsaD)}");
            if (b.RsaOAEP)                             sb.AppendLine("  oaep = True");
        }
        else if (b.FunctionType == BlockFunction.Function.JWTEncode)
        {
            if (!string.IsNullOrEmpty(b.InputString)) sb.AppendLine($"  input = {SbInputToOb2Ref(b.InputString)}");
            if (!string.IsNullOrEmpty(b.SecondInput)) sb.AppendLine($"  secret = {ToOb2String(b.SecondInput)}");
            sb.AppendLine($"  algorithm = {ToOb2String(b.JwtAlgorithm ?? "HS256")}");
        }
        else if (b.FunctionType == BlockFunction.Function.MaxFloat || b.FunctionType == BlockFunction.Function.MinFloat ||
                 b.FunctionType == BlockFunction.Function.MaxInt   || b.FunctionType == BlockFunction.Function.MinInt   ||
                 b.FunctionType == BlockFunction.Function.IndexOf)
        {
            if (!string.IsNullOrEmpty(b.InputString)) sb.AppendLine($"  input = {SbInputToOb2Ref(b.InputString)}");
            if (!string.IsNullOrEmpty(b.SecondInput)) sb.AppendLine($"  second = {ToOb2String(b.SecondInput)}");
        }
        else if (b.FunctionType == BlockFunction.Function.GetKey)
        {
            if (!string.IsNullOrEmpty(b.InputString)) sb.AppendLine($"  dictionary = {SbInputToOb2Ref(b.InputString)}");
            if (!string.IsNullOrEmpty(b.SecondInput)) sb.AppendLine($"  key = {ToOb2String(b.SecondInput)}");
        }
        else if (b.FunctionType == BlockFunction.Function.RemoveByKey)
        {
            if (!string.IsNullOrEmpty(b.InputString)) sb.AppendLine($"  dictionary = {SbInputToOb2Ref(b.InputString)}");
            if (!string.IsNullOrEmpty(b.SecondInput)) sb.AppendLine($"  key = {ToOb2String(b.SecondInput)}");
        }
        else if (b.FunctionType == BlockFunction.Function.RandomFloat)
        {
            sb.AppendLine($"  minimum = {b.RandomMin ?? "0"}");
            sb.AppendLine($"  maximum = {b.RandomMax ?? "0"}");
        }
        else if (b.FunctionType == BlockFunction.Function.AddKeyValuePair)
        {
            if (!string.IsNullOrEmpty(b.InputString)) sb.AppendLine($"  dictionary = {SbInputToOb2Ref(b.InputString)}");
            if (!string.IsNullOrEmpty(b.SecondInput)) sb.AppendLine($"  key = {ToOb2String(b.SecondInput)}");
            if (!string.IsNullOrEmpty(b.ThirdInput))  sb.AppendLine($"  value = {ToOb2String(b.ThirdInput)}");
        }
        else if (b.FunctionType == BlockFunction.Function.CreateListOfNumbers)
        {
            if (!string.IsNullOrEmpty(b.InputString)) sb.AppendLine($"  input = {SbInputToOb2Ref(b.InputString)}");
            sb.AppendLine($"  count = {ToOb2String(b.SecondInput ?? "0")}");
            if (!string.IsNullOrEmpty(b.ThirdInput))  sb.AppendLine($"  step = {ToOb2String(b.ThirdInput)}");
        }
        else if (b.FunctionType == BlockFunction.Function.ListToDict)
        {
            if (!string.IsNullOrEmpty(b.InputString)) sb.AppendLine($"  input = {SbInputToOb2Ref(b.InputString)}");
        }
        else if (b.FunctionType == BlockFunction.Function.BCryptVerify)
        {
            if (!string.IsNullOrEmpty(b.InputString)) sb.AppendLine($"  password = {SbInputToOb2Ref(b.InputString)}");
            if (!string.IsNullOrEmpty(b.SecondInput)) sb.AppendLine($"  hash = {ToOb2String(b.SecondInput)}");
        }
        else if (b.FunctionType == BlockFunction.Function.ScryptDeriveKey)
        {
            if (!string.IsNullOrEmpty(b.InputString)) sb.AppendLine($"  password = {SbInputToOb2Ref(b.InputString)}");
            if (!string.IsNullOrEmpty(b.SecondInput)) sb.AppendLine($"  salt = {ToOb2String(b.SecondInput)}");
            sb.AppendLine($"  n = {b.ScryptCost}");
            sb.AppendLine($"  r = {b.ScryptBlockSize}");
            sb.AppendLine("  p = 1");
            sb.AppendLine($"  keyLen = {b.ScryptOutputLength}");
        }
        else if (b.FunctionType == BlockFunction.Function.AWS4Signature)
        {
            if (!string.IsNullOrEmpty(b.InputString)) sb.AppendLine($"  stringToSign = {SbInputToOb2Ref(b.InputString)}");
            if (!string.IsNullOrEmpty(b.SecondInput)) sb.AppendLine($"  secretKey = {ToOb2String(b.SecondInput)}");
            if (!string.IsNullOrEmpty(b.ThirdInput))  sb.AppendLine($"  date = {ToOb2String(b.ThirdInput)}");
            if (!string.IsNullOrEmpty(b.AwsRegion))   sb.AppendLine($"  region = {ToOb2String(b.AwsRegion)}");
            if (!string.IsNullOrEmpty(b.AwsService))  sb.AppendLine($"  service = {ToOb2String(b.AwsService)}");
        }
        else if (!string.IsNullOrEmpty(b.InputString))
        {
            sb.AppendLine($"  input = {SbInputToOb2Ref(b.InputString)}");
        }

        // Hash / HMAC: always emit hashFunction so round-trips preserve the algorithm.
        if (b.FunctionType == BlockFunction.Function.Hash || b.FunctionType == BlockFunction.Function.HMAC)
            sb.AppendLine($"  hashFunction = {b.HashType}");
        if (b.FunctionType == BlockFunction.Function.HMAC)
        {
            if (!string.IsNullOrEmpty(b.HmacKey))
                sb.AppendLine($"  key = {SbInputToOb2Ref(b.HmacKey)}");
            if (b.KeyBase64)  sb.AppendLine("  keyBase64 = true");
            if (b.HmacBase64) sb.AppendLine("  outputBase64 = true");
        }
        // AESEncrypt / AESDecrypt
        if (b.FunctionType == BlockFunction.Function.AESEncrypt || b.FunctionType == BlockFunction.Function.AESDecrypt)
        {
            sb.AppendLine($"  key = {ToOb2String(b.AesKey ?? "")}");
            sb.AppendLine($"  iv = {ToOb2String(b.AesIV ?? "")}");
            sb.AppendLine($"  mode = {b.AesMode}");
            sb.AppendLine($"  padding = {b.AesPadding}");
            if (b.HexKeys) sb.AppendLine("  hexKeys = True");
        }
        // RSAEncrypt
        if (b.FunctionType == BlockFunction.Function.RSAEncrypt)
        {
            sb.AppendLine($"  n = {ToOb2String(b.RsaN ?? "")}");
            sb.AppendLine($"  e = {ToOb2String(b.RsaE ?? "")}");
            if (b.RsaOAEP) sb.AppendLine("  oaep = True");
        }
        // Split
        if (b.FunctionType == BlockFunction.Function.Split)
        {
            sb.AppendLine($"  separator = {ToOb2String(b.Separator ?? "")}");
            sb.AppendLine($"  index = {b.SplitIndex}");
            if (b.StringSplitOption != System.StringSplitOptions.None)
                sb.AppendLine($"  stringSplitOption = {b.StringSplitOption}");
        }
        // CharAt
        if (b.FunctionType == BlockFunction.Function.CharAt)
        {
            sb.AppendLine($"  index = {ToOb2String(b.CharIndex ?? "0")}");
        }
        // SCrypt
        if (b.FunctionType == BlockFunction.Function.SCrypt)
        {
            sb.AppendLine($"  method = {b.ScryptMeth}");
            if (b.ScryptMeth == BlockFunction.ScryptMethods.Encode)
            {
                sb.AppendLine($"  salt = {ToOb2String(b.ScryptSalt ?? "")}");
                sb.AppendLine($"  cost = {b.ScryptCost}");
                sb.AppendLine($"  blockSize = {b.ScryptBlockSize}");
                sb.AppendLine($"  outputLength = {b.ScryptOutputLength}");
                if (b.Base64Output) sb.AppendLine("  base64Output = True");
            }
            else if (b.ScryptMeth == BlockFunction.ScryptMethods.Compare)
                sb.AppendLine($"  hashedPassword = {ToOb2String(b.ScryptHashedPassword ?? "")}");
        }
        // BCrypt
        if (b.FunctionType == BlockFunction.Function.BCrypt)
        {
            sb.AppendLine($"  method = {b.BCryptMeth}");
            if (b.BCryptMeth == BlockFunction.BCryptMethods.Encode || b.BCryptMeth == BlockFunction.BCryptMethods.GenerateSalt)
            {
                if (!string.IsNullOrEmpty(b.BCryptSalt)) sb.AppendLine($"  salt = {ToOb2String(b.BCryptSalt)}");
                if (b.UseWorkFactor) sb.AppendLine($"  useWorkFactor = True");
                if (b.UseWorkFactor) sb.AppendLine($"  workFactor = {b.BCryptWorkFactor}");
            }
            else if (b.BCryptMeth == BlockFunction.BCryptMethods.Verify)
                sb.AppendLine($"  hashedPassword = {ToOb2String(b.BCryptHashedPassword ?? "")}");
        }

        if (!string.IsNullOrEmpty(b.VariableName))
            sb.AppendLine(b.IsCapture ? $"  => CAP @{b.VariableName}" : $"  => VAR @{b.VariableName}");

        sb.Append("ENDBLOCK");
        return sb.ToString();
    }

    // Converts a SilverBullet InputString to an OB2 @ref or $"..." value.
    // $"<USER>"     → @input.USERNAME
    // $"<PASS>"     → @input.PASSWORD
    // $"<VARNAME>"  → @VARNAME
    // $"literal..."  → $"literal..."  (complex template, keep as-is)
    private static string SbInputToOb2Ref(string sbInput)
    {
        if (string.IsNullOrEmpty(sbInput)) return "$\"\"";

        // Single variable reference: $"<VARNAME>"
        var m = Regex.Match(sbInput, @"^\$""<([A-Za-z0-9_]+)>""$");
        if (m.Success)
        {
            string varName = m.Groups[1].Value;
            return varName.ToUpperInvariant() switch {
                "USER" or "USERNAME" => "@input.USERNAME",
                "PASS" or "PASSWORD" => "@input.PASSWORD",
                _                    => $"@{varName}",
            };
        }

        // Plain <VARNAME> without $"..." wrapping
        var m2 = Regex.Match(sbInput, @"^<([A-Za-z0-9_]+)>$");
        if (m2.Success)
        {
            string varName = m2.Groups[1].Value;
            return varName.ToUpperInvariant() switch {
                "USER" or "USERNAME" => "@input.USERNAME",
                "PASS" or "PASSWORD" => "@input.PASSWORD",
                _                    => $"@{varName}",
            };
        }

        // Plain <@VARNAME> — SilverBullet variable name starting with @
        // OB2 sees it as a var ref: @{name} where name includes the @, giving @@varname
        var m3 = Regex.Match(sbInput, @"^<(@[A-Za-z0-9_]+)>$");
        if (m3.Success)
            return $"@{m3.Groups[1].Value}";

        // Complex template or literal — keep $"..." format
        return ToOb2String(sbInput);
    }

    // Converts an OB2 @ref value back to a SilverBullet InputString.
    // @input.USERNAME → <USER>
    // @input.PASSWORD → <PASS>
    // @VARNAME        → <VARNAME>
    private static string Ob2AtRefToSbInput(string atRef)
    {
        if (string.IsNullOrEmpty(atRef) || !atRef.StartsWith("@")) return atRef;
        if (atRef.Equals("@input.USERNAME", StringComparison.OrdinalIgnoreCase)) return "<USER>";
        if (atRef.Equals("@input.PASSWORD", StringComparison.OrdinalIgnoreCase)) return "<PASS>";
        if (atRef.StartsWith("@input.", StringComparison.OrdinalIgnoreCase)) return "<" + atRef.Substring(7) + ">";
        if (atRef.StartsWith("@data.",  StringComparison.OrdinalIgnoreCase)) return "<" + atRef.Substring(6) + ">";
        return "<" + atRef.Substring(1) + ">";
    }

    private static string KeyCheckToLoliCode(BlockKeycheck b)
    {
        var sb = new StringBuilder();
        sb.AppendLine(BlockHeader("Keycheck", b.Disabled));
        if (!string.IsNullOrEmpty(b.Label)) sb.AppendLine($"  LABEL:{b.Label}");
        // Only emit banIfNoMatch when it's false (true is the default); omitting keeps
        // the round-tripped LoliCode clean while still preserving the user's opt-out.
        if (!b.BanOnToCheck) sb.AppendLine("  banIfNoMatch = false");
        if (b.BanOn4XX) sb.AppendLine("  banOn4XX = true");

        foreach (var chain in b.KeyChains)
        {
            // Detect EXPIRED/2FACTOR/TOCHECK stored as Custom with a known CustomType
            string chainType = (chain.Type == KeyChain.KeychainType.Custom && !string.IsNullOrEmpty(chain.CustomType))
                ? chain.CustomType.ToUpperInvariant() switch {
                    "EXPIRED"                    => "EXPIRED",
                    "2FACTOR" or "TWOFACTOR"     => "2FACTOR",
                    "TOCHECK"                    => "TOCHECK",
                    _                            => "CUSTOM",
                  }
                : chain.Type switch {
                    KeyChain.KeychainType.Success => "SUCCESS",
                    KeyChain.KeychainType.Failure => "FAIL",
                    KeyChain.KeychainType.Ban     => "BAN",
                    KeyChain.KeychainType.Retry   => "RETRY",
                    KeyChain.KeychainType.Custom  => "CUSTOM",
                    _                             => "FAIL",
                  };
            string chainMode = chain.Mode == KeyChain.KeychainMode.AND ? "AND" : "OR";

            // OB2 KEYCHAIN CUSTOM has no inline name — format is: KEYCHAIN CUSTOM <mode>
            sb.AppendLine($"  KEYCHAIN {chainType} {chainMode}");

            foreach (var key in chain.Keys)
            {
                string leftTerm = SbRefToOb2DataRef(key.LeftTerm);
                string comparer = key.Comparer.ToString();

                // OB2 only supports STRINGKEY for all key comparers — REGEXKEY does not exist.
                // MatchesRegex / DoesNotMatchRegex are valid comparers on STRINGKEY in OB2.
                // Map numeric comparers to the closest string equivalent:
                //   GreaterThan / LessThan   → NotEqualTo  (value differs from threshold)
                //   LessThanOrEqual / GreaterOrEqual → EqualTo  (value matches threshold)
                const string keyWord = "STRINGKEY";
                string ob2Comparer = key.Comparer switch {
                    Comparer.GreaterThan or Comparer.LessThan         => "NotEqualTo",
                    Comparer.LessThanOrEqual or Comparer.GreaterOrEqual => "EqualTo",
                    _                                                  => comparer,
                };

                // OB2 cannot compile @COOKIES (bare all-cookies dict) as a C# identifier.
                // When the left term maps to @COOKIES, convert to a per-cookie
                // Exists/DoesNotExist check using the right-term value as the cookie name.
                if (leftTerm.Equals("@COOKIES", StringComparison.OrdinalIgnoreCase))
                {
                    string cookieName = key.RightTerm ?? "";
                    string existsOp = key.Comparer switch {
                        Comparer.DoesNotContain or Comparer.NotEqualTo
                            or Comparer.DoesNotExist or Comparer.DoesNotMatchRegex => "DoesNotExist",
                        _ => "Exists",
                    };
                    sb.AppendLine($"    {keyWord} @data.COOKIES[\"{EscapeOb2(cookieName)}\"] {existsOp} \"\"");
                    continue;
                }

                // OB2 requires a right-term token on every key line, even for Exists/DoesNotExist.
                // Use ToOb2String so patterns with \ or " get the $"..." form OB2 needs.
                string rightToken = (key.Comparer == Comparer.Exists || key.Comparer == Comparer.DoesNotExist)
                    ? "\"\""
                    : ToOb2String(key.RightTerm);
                sb.AppendLine($"    {keyWord} {leftTerm} {ob2Comparer} {rightToken}");
            }
        }

        sb.Append("ENDBLOCK");
        return sb.ToString();
    }

    // Built-in BotData properties that map to @data.PROP in OB2.
    // Everything else is a user variable and maps to @VARNAME.
    private static readonly HashSet<string> BotDataProps = new(StringComparer.OrdinalIgnoreCase)
    {
        "SOURCE", "RAWSOURCE", "RESPONSECODE", "ADDRESS", "ERROR",
    };

    // <SOURCE> → @data.SOURCE,  <BALANCE1> → @BALANCE1,  <COOKIES(n)> → @data.COOKIES["n"]
    private static string SbRefToOb2DataRef(string sbRef)
    {
        if (string.IsNullOrEmpty(sbRef)) return sbRef;
        if (sbRef.StartsWith("<") && sbRef.EndsWith(">"))
        {
            string name = sbRef.Substring(1, sbRef.Length - 2);
            var cm = Regex.Match(name, @"^COOKIES\(([^)]+)\)$", RegexOptions.IgnoreCase);
            if (cm.Success) return $"@data.COOKIES[\"{cm.Groups[1].Value}\"]";
            var hm = Regex.Match(name, @"^HEADERS\(([^)]+)\)$", RegexOptions.IgnoreCase);
            if (hm.Success) return $"@data.HEADERS[\"{hm.Groups[1].Value}\"]";
            if (name.Equals("USER", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("USERNAME", StringComparison.OrdinalIgnoreCase))
                return "@input.USERNAME";
            if (name.Equals("PASS", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("PASSWORD", StringComparison.OrdinalIgnoreCase))
                return "@input.PASSWORD";
            return BotDataProps.Contains(name) ? $"@data.{name}" : $"@{name}";
        }
        return sbRef;
    }

    private static string UtilityToLoliCode(BlockUtility b)
    {
        // For Conversion group, emit the proper OB2 native block format.
        if (b.Group == UtilityGroup.Conversion && b.ConversionAct != ConversionAction.Encoding)
            return ConversionUtilityToLoliCode(b);

        // All other groups: wrap as BLOCK:Utility with embedded LoliScript.
        string lsText = string.Join(" ",
            b.ToLS(indent: true)
             .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
             .Select(l => l.Trim())
             .Where(l => !string.IsNullOrEmpty(l)));

        var sb = new StringBuilder();
        sb.AppendLine(BlockHeader("Utility", b.Disabled));
        if (!string.IsNullOrEmpty(b.Label)) sb.AppendLine($"  LABEL:{b.Label}");
        sb.AppendLine($"  ls = {ToOb2String(lsText)}");
        sb.Append("ENDBLOCK");
        return sb.ToString();
    }

    private static string ConversionUtilityToLoliCode(BlockUtility b)
    {
        // Map ConversionAction → (OB2 block name, input property name)
        (string blockName, string inputProp) = b.ConversionAct switch
        {
            ConversionAction.BigIntegerToByteArray => ("BigIntegerToByteArray", "bigInteger"),
            ConversionAction.ByteArrayToBigInteger => ("ByteArrayToBigInteger", "bytes"),
            ConversionAction.ReadableSize          => ("ReadableSize",           "input"),
            ConversionAction.Base64ToBytes         => ("Base64StringToByteArray","base64String"),
            ConversionAction.Base64ToUtf8          => ("Base64ToUTF8",           "input"),
            ConversionAction.BinaryStringToBytes   => ("BinaryStringToByteArray","binaryString"),
            ConversionAction.BytesToBase64         => ("ByteArrayToBase64String","bytes"),
            ConversionAction.BytesToBinaryString   => ("ByteArrayToBinaryString","bytes"),
            ConversionAction.BytesToHex            => ("ByteArrayToHexString",   "bytes"),
            ConversionAction.BytesToString         => ("BytesToString",           "input"),
            ConversionAction.HexToBytes            => ("HexStringToByteArray",   "hexString"),
            ConversionAction.StringToBytes         => ("StringToBytes",           "input"),
            ConversionAction.Utf8ToBase64          => ("UTF8ToBase64",            "input"),
            _                                      => ("Utility",                 "input"),
        };

        var sb = new StringBuilder();
        sb.AppendLine(BlockHeader(blockName, b.Disabled));
        if (!string.IsNullOrEmpty(b.Label)) sb.AppendLine($"  LABEL:{b.Label}");

        // Input property
        string inputVal = string.IsNullOrEmpty(b.InputString) ? "@" : SbInputToOb2Ref(b.InputString);
        sb.AppendLine($"  {inputProp} = {inputVal}");

        // Extra properties per action
        if (b.ConversionAct == ConversionAction.ReadableSize)
        {
            if (b.ReadableSizeOutputBits) sb.AppendLine("  outputBits = true");
            if (b.ReadableSizeBinaryUnit) sb.AppendLine("  binaryUnit = true");
            if (!string.IsNullOrEmpty(b.ReadableSizeDecimalPlaces) && b.ReadableSizeDecimalPlaces != "2")
                sb.AppendLine($"  decimalPlaces = \"{b.ReadableSizeDecimalPlaces}\"");
        }
        else if (b.ConversionAct == ConversionAction.BytesToString ||
                 b.ConversionAct == ConversionAction.StringToBytes)
        {
            if (!string.IsNullOrEmpty(b.ByteStringEncoding) && b.ByteStringEncoding != "UTF8")
                sb.AppendLine($"  encoding = \"{b.ByteStringEncoding}\"");
        }

        // Output variable
        if (!string.IsNullOrEmpty(b.VariableName))
            sb.AppendLine($"  => {(b.IsCapture ? "CAP" : "VAR")} @{b.VariableName}");

        sb.Append("ENDBLOCK");
        return sb.ToString();
    }

    // ─── DnsLookup LoliCode (OB2-compatible) ─────────────────────────────────

    private static string DnsToLoliCode(BlockDns b)
    {
        var sb = new StringBuilder();
        sb.AppendLine(BlockHeader("DnsLookup", b.Disabled));
        if (!string.IsNullOrEmpty(b.Label)) sb.AppendLine($"  LABEL:{b.Label}");
        sb.AppendLine($"  query = {ToOb2String(b.Query)}");
        sb.AppendLine($"  recordType = {b.RecordType}");
        sb.AppendLine($"  transport = {b.Transport}");
        sb.AppendLine($"  server = {ToOb2String(b.Server)}");
        sb.AppendLine($"  timeoutMilliseconds = {b.TimeoutMs}");
        if (!string.IsNullOrEmpty(b.VariableName))
            sb.AppendLine($"  => {(b.IsCapture ? "CAP" : "LIST")} @{b.VariableName}");
        sb.Append("ENDBLOCK");
        return sb.ToString();
    }

    private static BlockDns SegToDns(LoliCodeSegment seg)
    {
        var b = new BlockDns();
        if (!string.IsNullOrEmpty(seg.Label)) b.Label = seg.Label;
        b.Disabled = seg.Disabled;
        if (seg.OutputVar != null) { b.VariableName = seg.OutputVar; b.IsCapture = seg.IsCapture; }

        if (seg.Properties.TryGetValue("query", out string q))
            b.Query = FromOb2String(q.Trim());
        if (seg.Properties.TryGetValue("recordType", out string rt)
            && System.Enum.TryParse<DnsRecordType>(rt.Trim(), true, out var rte))
            b.RecordType = rte;
        if (seg.Properties.TryGetValue("transport", out string tr)
            && System.Enum.TryParse<DnsTransport>(tr.Trim(), true, out var tre))
            b.Transport = tre;
        if (seg.Properties.TryGetValue("server", out string srv))
            b.Server = FromOb2String(srv.Trim());
        if (seg.Properties.TryGetValue("timeoutMilliseconds", out string tms)
            && int.TryParse(tms.Trim(), out int tmsInt))
            b.TimeoutMs = tmsInt;
        return b;
    }

    // ─── LoliCode text → Block list ──────────────────────────────────────────

    public static List<BlockBase> SegmentsToBlocks(List<LoliCodeSegment> segments)
    {
        var blocks = new List<BlockBase>();
        Dictionary<string, string> pendingSbConfig = null;
        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            if (seg.Type == LoliCodeSegmentType.Code)
            {
                string _trimmedCode = (seg.Code ?? "").Trim();
                if (_trimmedCode.StartsWith("// _SB:", StringComparison.Ordinal))
                {
                    pendingSbConfig = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (string _pair in _trimmedCode.Substring("// _SB:".Length).Split('|'))
                    {
                        int _eq = _pair.IndexOf('=');
                        if (_eq > 0) pendingSbConfig[_pair.Substring(0, _eq).Trim()] = _pair.Substring(_eq + 1).Trim();
                    }
                    continue;
                }
                pendingSbConfig = null;

                string lsScript;
                if (seg.PythonLines != null)
                {
                    // Reconstruct BEGIN SCRIPT ... END SCRIPT for round-trip back to LoliScript.
                    // Prefer ScriptInterpreter (set by BLOCK:Script parser) over the legacy bool.
                    // Map OB2 interpreter names to ScriptingLanguage enum names used by LoliScript.
                    string rawLang = !string.IsNullOrEmpty(seg.ScriptInterpreter)
                        ? seg.ScriptInterpreter
                        : (seg.IsIronPython ? "IronPython" : "Python");
                    string lang = rawLang.ToLowerInvariant() switch {
                        "jint" or "javascript" => "JavaScript",
                        "nodejs" or "node.js"  => "NodeJS",
                        "python"               => "Python",
                        "ironpython"           => "IronPython",
                        _                      => rawLang
                    };
                    var pySb = new StringBuilder();
                    pySb.AppendLine($"BEGIN SCRIPT {lang}");
                    // Embed the INPUT vars as a special comment so ConvertLSCodeToLoliCode
                    // can recover them without re-scanning the body for PascalCase names.
                    if (!string.IsNullOrEmpty(seg.PythonInputs))
                        pySb.AppendLine($"// _INPUTS:{seg.PythonInputs}");
                    foreach (string pl in seg.PythonLines)
                        pySb.AppendLine(pl);
                    string outs = seg.PythonOutputs ?? "";
                    pySb.Append(string.IsNullOrEmpty(outs)
                        ? "END SCRIPT"
                        : $"END SCRIPT -> VARS \"{outs}\"");
                    lsScript = pySb.ToString();
                }
                else
                {
                    // Generic C# code block — keep as-is.
                    lsScript = (seg.Code ?? "").TrimStart('\r', '\n').TrimEnd('\r', '\n', ' ', '\t');
                }
                if (!string.IsNullOrEmpty(lsScript))
                    blocks.Add(new BlockLSCode { Script = lsScript, Label = "CODE" });
            }
            else
            {
                if (pendingSbConfig != null && string.Equals(seg.BlockType, "HttpRequest", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var _kv in pendingSbConfig)
                        seg.Properties[_kv.Key] = _kv.Value;
                }
                pendingSbConfig = null;

                var block = SegmentToBlock(seg);

                blocks.Add(block);
            }
        }
        return blocks;
    }

    public static BlockBase SegmentToBlock(LoliCodeSegment seg)
    {
        try
        {
            return seg.BlockType switch
            {
                "ConstantString"  => SegToConstantString(seg),
                "HttpRequest"     => SegToRequest(seg),
                "Parse"           => SegToParse(seg),
                "DnsLookup"       => SegToDns(seg),
                "Function"        => SegToFunction(seg),
                // OB2 dedicated function block names → map to BlockFunction
                // OB2 dedicated function blocks → BlockFunction
                "UrlEncode"       => SegToOb2FunctionBlock(seg, BlockFunction.Function.URLEncode),
                "UrlDecode"       => SegToOb2FunctionBlock(seg, BlockFunction.Function.URLDecode),
                "UTF8ToBase64"    => SegToConversionUtility(seg, ConversionAction.Utf8ToBase64),
                "Base64ToUTF8"    => SegToConversionUtility(seg, ConversionAction.Base64ToUtf8),
                "Base64Encode"    => SegToOb2FunctionBlock(seg, BlockFunction.Function.Base64Encode),
                "Base64Decode"    => SegToOb2FunctionBlock(seg, BlockFunction.Function.Base64Decode),
                "GenerateGuid"    => SegToOb2FunctionBlock(seg, BlockFunction.Function.GenerateGUID),
                "ToUppercase"     => SegToOb2FunctionBlock(seg, BlockFunction.Function.ToUppercase),
                "ToLowercase"     => SegToOb2FunctionBlock(seg, BlockFunction.Function.ToLowercase),
                "Translate"       => SegToOb2FunctionBlock(seg, BlockFunction.Function.Translate),
                "GetRemainingDay" => SegToOb2FunctionBlock(seg, BlockFunction.Function.GetRemainingDay),
                "MergeByteArrays"        => SegToOb2FunctionBlock(seg, BlockFunction.Function.MergeByteArrays),
                "RegexReplace"           => SegToOb2FunctionBlock(seg, BlockFunction.Function.RegexReplace),
                "XOR"                    => SegToOb2FunctionBlock(seg, BlockFunction.Function.XOR),
                "XORStrings"             => SegToOb2FunctionBlock(seg, BlockFunction.Function.XORStrings),
                "RSADecrypt"             => SegToOb2FunctionBlock(seg, BlockFunction.Function.RSADecrypt),
                "JwtEncode"              => SegToOb2FunctionBlock(seg, BlockFunction.Function.JWTEncode),
                "JWTEncode"              => SegToOb2FunctionBlock(seg, BlockFunction.Function.JWTEncode),
                "MaxFloat"               => SegToOb2FunctionBlock(seg, BlockFunction.Function.MaxFloat),
                "MinFloat"               => SegToOb2FunctionBlock(seg, BlockFunction.Function.MinFloat),
                "RandomFloat"            => SegToOb2FunctionBlock(seg, BlockFunction.Function.RandomFloat),
                "MaxInt"                 => SegToOb2FunctionBlock(seg, BlockFunction.Function.MaxInt),
                "MinInt"                 => SegToOb2FunctionBlock(seg, BlockFunction.Function.MinInt),
                "AddKeyValuePair"        => SegToOb2FunctionBlock(seg, BlockFunction.Function.AddKeyValuePair),
                "GetKey"                 => SegToOb2FunctionBlock(seg, BlockFunction.Function.GetKey),
                "RemoveByKey"            => SegToOb2FunctionBlock(seg, BlockFunction.Function.RemoveByKey),
                "CreateListOfNumbers"    => SegToOb2FunctionBlock(seg, BlockFunction.Function.CreateListOfNumbers),
                "IndexOf"                => SegToOb2FunctionBlock(seg, BlockFunction.Function.IndexOf),
                "ToDictionary"           => SegToOb2FunctionBlock(seg, BlockFunction.Function.ListToDict),
                "BCryptVerify"           => SegToOb2FunctionBlock(seg, BlockFunction.Function.BCryptVerify),
                "ScryptDeriveKey"        => SegToOb2FunctionBlock(seg, BlockFunction.Function.ScryptDeriveKey),
                "AWS4Signature"          => SegToOb2FunctionBlock(seg, BlockFunction.Function.AWS4Signature),
                // OB2 block names that differ from the enum value
                "Reverse"                => SegToOb2FunctionBlock(seg, BlockFunction.Function.ReverseString),
                "EncodeHTMLEntities"     => SegToOb2FunctionBlock(seg, BlockFunction.Function.HTMLEntityEncode),
                "DecodeHTMLEntities"     => SegToOb2FunctionBlock(seg, BlockFunction.Function.HTMLEntityDecode),
                "ByteArrayToBase64String" => SegToConversionUtility(seg, ConversionAction.BytesToBase64),
                "Base64StringToByteArray" => SegToConversionUtility(seg, ConversionAction.Base64ToBytes),
                "Unescape"        => SegToOb2FunctionBlock(seg, BlockFunction.Function.Unescape),
                "AESEncrypt"      => SegToOb2FunctionBlock(seg, BlockFunction.Function.AESEncrypt),
                "AESDecrypt"      => SegToOb2FunctionBlock(seg, BlockFunction.Function.AESDecrypt),
                "AESEncryptString"=> SegToOb2FunctionBlock(seg, BlockFunction.Function.AESEncrypt),
                "AESDecryptString"=> SegToOb2FunctionBlock(seg, BlockFunction.Function.AESDecrypt),
                "RSAEncrypt"      => SegToOb2FunctionBlock(seg, BlockFunction.Function.RSAEncrypt),
                // RSADecrypt has no equivalent in BlockFunction.Function — fall through to SegToLSCode
                // so it surfaces as an unknown block rather than silently encrypting
                "Hash"            => SegToOb2FunctionBlock(seg, BlockFunction.Function.Hash),
                "HashString"      => SegToOb2FunctionBlock(seg, BlockFunction.Function.Hash),
                "Hmac"            => SegToOb2FunctionBlock(seg, BlockFunction.Function.HMAC),
                "HmacString"      => SegToOb2FunctionBlock(seg, BlockFunction.Function.HMAC),
                "NTLMHash"        => SegToOb2FunctionBlock(seg, BlockFunction.Function.Ntlm),
                "ScryptString"    => SegToOb2FunctionBlock(seg, BlockFunction.Function.SCrypt),
                "BCryptHash"      => SegToOb2FunctionBlock(seg, BlockFunction.Function.BCrypt),
                "BCryptHashGenSalt"=> SegToOb2FunctionBlock(seg, BlockFunction.Function.BCrypt),
                "Split"           => SegToOb2FunctionBlock(seg, BlockFunction.Function.Split),
                "CharAt"          => SegToOb2FunctionBlock(seg, BlockFunction.Function.CharAt),
                "Ceil"            => SegToOb2FunctionBlock(seg, BlockFunction.Function.Ceil),
                "Floor"           => SegToOb2FunctionBlock(seg, BlockFunction.Function.Floor),
                "Round"           => SegToOb2FunctionBlock(seg, BlockFunction.Function.Round),
                "RandomInteger"    => SegToOb2FunctionBlock(seg, BlockFunction.Function.RandomNum),
                "RandomUserAgent"  => SegToOb2FunctionBlock(seg, BlockFunction.Function.GetRandomUA),
                "Length"           => SegToOb2FunctionBlock(seg, BlockFunction.Function.Length),
                "ClearCookies"          => SegToMiscUtility(seg, MiscAction.ClearCookies),
                "GetHWID"               => SegToMiscUtility(seg, MiscAction.GetHWID),
                "BigIntegerToByteArray"      => SegToConversionUtility(seg, ConversionAction.BigIntegerToByteArray),
                "ByteArrayToBigInteger"      => SegToConversionUtility(seg, ConversionAction.ByteArrayToBigInteger),
                "ReadableSize"               => SegToConversionUtility(seg, ConversionAction.ReadableSize),
                "BinaryStringToByteArray"    => SegToConversionUtility(seg, ConversionAction.BinaryStringToBytes),
                "ByteArrayToBinaryString"    => SegToConversionUtility(seg, ConversionAction.BytesToBinaryString),
                "ByteArrayToHexString"       => SegToConversionUtility(seg, ConversionAction.BytesToHex),
                "BytesToString"              => SegToConversionUtility(seg, ConversionAction.BytesToString),
                "HexStringToByteArray"       => SegToConversionUtility(seg, ConversionAction.HexToBytes),
                "StringToBytes"              => SegToConversionUtility(seg, ConversionAction.StringToBytes),
                "SvgToPng"                   => SegToImagesUtility(seg, ImageAction.SvgToPng),
                "Keycheck"        => SegToKeyCheck(seg),
                "KeyCheck"        => SegToKeyCheck(seg),
                "BypassCF"        => SegToBypassCF(seg),
                "CfClearance"     => SegToCfClearance(seg),
                "Turnstile"       => SegToTurnstile(seg),
                "Altcha"               => SegToAltcha(seg),
                "RecaptchaV3Bypass"    => SegToRecaptchaV3Bypass(seg),
                "FriendlyCaptcha"    => SegToFriendlyCaptcha(seg),
                "RecaptchaV3"        => SegToRecaptchaV3(seg),
                "RecaptchaV2Invisible" => SegToRecaptchaV2Invisible(seg),
                "AkmCookies"         => SegToAkmCookies(seg),
                "DataDome"           => SegToDataDome(seg),
                "Utility"         => SegToUtility(seg),
                // Any other block name that matches a Function enum value → treat as function block
                _ when Enum.TryParse<BlockFunction.Function>(seg.BlockType, true, out var ft)
                                  => SegToOb2FunctionBlock(seg, ft),
                _                 => SegToLSCode(seg)
            };
        }
        catch
        {
            return SegToLSCode(seg);
        }
    }

    private static BlockFunction SegToConstantString(LoliCodeSegment seg)
    {
        var b = new BlockFunction { FunctionType = BlockFunction.Function.Constant };
        if (!string.IsNullOrEmpty(seg.Label)) b.Label = seg.Label;
        b.Disabled = seg.Disabled;
        if (seg.OutputVar != null) { b.VariableName = seg.OutputVar; b.IsCapture = seg.IsCapture; }

        if (seg.Properties.TryGetValue("value", out string rawVal))
            b.InputString = FromOb2String(rawVal);

        return b;
    }

    private static BlockRequest SegToRequest(LoliCodeSegment seg)
    {
        var b = new BlockRequest();
        if (!string.IsNullOrEmpty(seg.Label)) b.Label = seg.Label;
        b.Disabled = seg.Disabled;

        foreach (var kv in seg.Properties)
        {
            string key = kv.Key.ToLower();
            string val = kv.Value;
            switch (key)
            {
                case "url":
                    b.Url = FromOb2String(val);
                    break;
                case "method":
                    // Use FromOb2String so that both quoted ("POST") and bare (POST) forms work.
                    if (Enum.TryParse<HttpMethod>(FromOb2String(val), true, out var m)) b.Method = m;
                    break;
                case "requestbody": case "postbody": case "postdata": case "body": case "content":
                {
                    string tv = val.Trim();
                    b.PostData = tv.StartsWith("@", StringComparison.Ordinal)
                        ? Ob2AtRefToSbInput(tv)
                        : FromOb2String(val);
                    break;
                }
                case "contenttype":
                    b.ContentType = FromOb2String(val);
                    break;
                case "autoredirect":
                    b.AutoRedirect = val.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
                case "acceptencoding":
                    b.AcceptEncoding = val.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
                case "readresponsesource": case "readresponsecontent":
                    b.ReadResponseSource = val.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
                case "encodecontent": case "urlencodecontent":
                    b.EncodeContent = val.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
                case "customheaders":
                    b.CustomHeaders = ParseOb2Dict(val);
                    break;
                case "customcookies":
                    b.CustomCookies = ParseOb2Dict(val);
                    break;
                case "rawdata":
                    b.RawData = FromOb2String(val);
                    break;
                case "authuser": case "username":
                    b.AuthUser = FromOb2String(val);
                    break;
                case "authpass": case "password":
                    b.AuthPass = FromOb2String(val);
                    break;
                case "requesttype":
                    if (Enum.TryParse<RequestType>(val.Trim(), true, out var rt)) b.RequestType = rt;
                    break;
                case "httplibrary":
                    if (Enum.TryParse<HttpLibrary>(val.Trim(), true, out var hl)) b.HttpLibrary = hl;
                    break;
                case "curlprofile":
                case "curlimpersonatebrowserprofile":
                    if (Enum.TryParse<CurlImpersonateBrowserProfile>(val.Trim(), true, out var cp)) b.CurlImpersonateProfile = cp;
                    break;
                case "ignorecertvalidation": case "ignorecertificatevalidation":
                    b.IgnoreCertificateValidation = val.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
                case "alwayssendcontent":
                    b.AlwaysSendContent = val.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
                case "codepagesencoding":
                    b.CodePagesEncoding = FromOb2String(val);
                    break;
                case "requesttimeout": case "timeoutmilliseconds":
                    if (int.TryParse(val.Trim(), out int rto)) b.RequestTimeoutMs = rto;
                    break;
                case "saveresponsecookies":
                    b.SaveResponseCookies = val.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
                case "loadrequestcookies":
                    b.LoadRequestCookies = val.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
                case "retrycount":
                    if (int.TryParse(val.Trim(), out int rc)) b.RetryCount = rc;
                    break;
                case "retrydelay":
                    if (int.TryParse(val.Trim(), out int rd)) b.RetryDelayMs = rd;
                    break;
                case "securityprotocol":
                    if (Enum.TryParse<SecurityProtocol>(val.Trim(), true, out var sp)) b.SecurityProtocol = sp;
                    break;
                case "httpversion": case "protocolversion":
                {
                    // OB2 uses httpVersion = "2.0" (quoted); SilverBullet uses protocolVersion = 2.0 (bare).
                    // FromOb2String strips quotes so both forms parse correctly.
                    var vp = FromOb2String(val).Trim().Split('.');
                    if (vp.Length == 2 && int.TryParse(vp[0], out int vmaj) && int.TryParse(vp[1], out int vmin))
                        b.ProtocolVersion = new Version(vmaj, vmin);
                    break;
                }
                case "multipartboundary":
                    b.MultipartBoundary = FromOb2String(val);
                    break;
                case "multipartpart":
                {
                    b.RequestType = RequestType.Multipart;
                    foreach (string entry in val.Split('\x1E'))
                    {
                        string e = entry.Trim();
                        if (string.IsNullOrEmpty(e)) continue;
                        var mc = e.StartsWith("CONTENT:", StringComparison.OrdinalIgnoreCase)
                            ? ParseOb2ContentLine(e)
                            : ParseJsonMultipartPart(e);
                        if (mc.HasValue) b.MultipartContents.Add(mc.Value);
                    }
                    break;
                }
                case "responsetype":
                    if (Enum.TryParse<ResponseType>(val.Trim(), true, out var rtype)) b.ResponseType = rtype;
                    break;
                case "downloadpath":
                    b.DownloadPath = FromOb2String(val);
                    break;
                case "saveasscreenshot":
                    b.SaveAsScreenshot = val.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
                case "outputvariable":
                    b.OutputVariable = FromOb2String(val);
                    break;
            }
        }
        return b;
    }

    private static RuriLib.Functions.Requests.MultipartContent? ParseOb2ContentLine(string line)
    {
        // CONTENT:STRING "name" "value" "content-type"
        // CONTENT:RAW    "name" rawdata  "content-type"
        // CONTENT:FILE   "name" "path"  "content-type"
        int colon = line.IndexOf(':');
        if (colon < 0) return null;
        string kind = line.Substring(colon + 1).Trim();
        int sp2 = kind.IndexOf(' ');
        if (sp2 < 0) return null;
        string typeName = kind.Substring(0, sp2).ToUpperInvariant();
        string rest = kind.Substring(sp2).Trim();

        var tokens = new List<string>();
        int pos = 0;
        while (pos < rest.Length)
        {
            while (pos < rest.Length && rest[pos] == ' ') pos++;
            if (pos >= rest.Length) break;
            if (rest[pos] == '"')
            {
                int end = rest.IndexOf('"', pos + 1);
                if (end < 0) end = rest.Length - 1;
                tokens.Add(rest.Substring(pos + 1, end - pos - 1));
                pos = end + 1;
            }
            else
            {
                int end = rest.IndexOf(' ', pos);
                if (end < 0) end = rest.Length;
                tokens.Add(rest.Substring(pos, end - pos));
                pos = end;
            }
        }

        return new RuriLib.Functions.Requests.MultipartContent
        {
            Type        = typeName == "FILE" ? MultipartContentType.File : MultipartContentType.String,
            Name        = tokens.Count > 0 ? tokens[0] : "",
            Value       = tokens.Count > 1 ? tokens[1] : "",
            ContentType = tokens.Count > 2 ? tokens[2] : "",
        };
    }

    private static RuriLib.Functions.Requests.MultipartContent? ParseJsonMultipartPart(string json)
    {
        try
        {
            var arr = JsonConvert.DeserializeObject<string[]>(json);
            if (arr == null || arr.Length < 2) return null;
            return new RuriLib.Functions.Requests.MultipartContent
            {
                Type        = arr[0].Equals("File", StringComparison.OrdinalIgnoreCase) ? MultipartContentType.File : MultipartContentType.String,
                Name        = arr.Length > 1 ? arr[1] : "",
                Value       = arr.Length > 2 ? arr[2] : "",
                ContentType = arr.Length > 3 ? arr[3] : "",
            };
        }
        catch { return null; }
    }

    private static BlockParse SegToParse(LoliCodeSegment seg)
    {
        var b = new BlockParse();
        if (!string.IsNullOrEmpty(seg.Label)) b.Label = seg.Label;
        b.Disabled = seg.Disabled;
        if (seg.OutputVar != null) b.VariableName = seg.OutputVar;
        b.IsCapture = seg.IsCapture;

        foreach (var kv in seg.Properties)
        {
            string key = kv.Key.ToLower();
            string val = kv.Value;
            switch (key)
            {
                case "target": case "parsesource": case "parsetarget":
                    b.ParseTarget = FromOb2String(val);
                    break;
                case "input":
                    // OB2 uses "input = @data.SOURCE" → convert @data.X ref to SB <X> format
                    b.ParseTarget = Ob2DataRefToSbRef(FromOb2String(val));
                    break;
                case "type": case "parsetype":
                    if (Enum.TryParse<ParseType>(val.Trim(), true, out var pt)) b.Type = pt;
                    break;
                case "variablename": case "outputvar":
                    b.VariableName = FromOb2String(val);
                    break;
                case "iscapture":
                    b.IsCapture = val.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
                case "leftstring": case "lstring": case "leftdelim": case "ld":
                    b.LeftString = FromOb2String(val);
                    break;
                case "rightstring": case "rstring": case "rightdelim": case "rd":
                    b.RightString = FromOb2String(val);
                    break;
                case "regexstring": case "pattern":
                    b.RegexString = FromOb2String(val);
                    break;
                case "regexoutput": case "outputformat":
                    b.RegexOutput = FromOb2String(val);
                    break;
                case "multiline": // OB2 property — no direct equivalent in BlockParse; ignored
                    break;
                case "cssselector":
                    b.CssSelector = FromOb2String(val);
                    break;
                case "attributename":
                    b.AttributeName = FromOb2String(val);
                    break;
                case "jsonfield": case "jtoken":
                    b.JsonField = FromOb2String(val);
                    break;
                case "recursive":
                    b.Recursive = val.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
                case "dotmatches":
                    b.DotMatches = val.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
                case "casesensitive":
                    b.CaseSensitive = val.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
                case "prefix":
                    b.Prefix = FromOb2String(val);
                    break;
                case "suffix":
                    b.Suffix = FromOb2String(val);
                    break;
            }
        }
        if (seg.OutputVar != null) { b.VariableName = seg.OutputVar; b.IsCapture = seg.IsCapture; }
        return b;
    }

    private static BlockFunction SegToFunction(LoliCodeSegment seg)
    {
        var b = new BlockFunction();
        if (!string.IsNullOrEmpty(seg.Label)) b.Label = seg.Label;
        b.Disabled = seg.Disabled;
        if (seg.OutputVar != null) { b.VariableName = seg.OutputVar; b.IsCapture = seg.IsCapture; }

        foreach (var kv in seg.Properties)
        {
            string key = kv.Key.ToLower();
            string val = kv.Value;
            switch (key)
            {
                case "function": case "functiontype":
                    if (Enum.TryParse<BlockFunction.Function>(val.Trim(), true, out var ft)) b.FunctionType = ft;
                    break;
                case "inputstring": case "input":
                    b.InputString = FromOb2String(val);
                    break;
                case "outputvariable": case "variablename": case "outputvar":
                    b.VariableName = FromOb2String(val);
                    break;
                case "iscapture":
                    b.IsCapture = val.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
            }
        }
        return b;
    }

    // Handles OB2 dedicated function blocks like BLOCK:UrlEncode, BLOCK:GenerateGuid, etc.
    private static BlockFunction SegToOb2FunctionBlock(LoliCodeSegment seg, BlockFunction.Function funcType)
    {
        var b = new BlockFunction { FunctionType = funcType };
        if (!string.IsNullOrEmpty(seg.Label)) b.Label = seg.Label;
        b.Disabled = seg.Disabled;
        if (seg.OutputVar != null) { b.VariableName = seg.OutputVar; b.IsCapture = seg.IsCapture; }

        if (funcType == BlockFunction.Function.GenerateGUID)
        {
            if (seg.Properties.TryGetValue("version", out string ver)
                && System.Enum.TryParse<RuriLib.GuidVersion>(ver.Trim(), true, out var gv))
                b.GuidVer = gv;
            if (seg.Properties.TryGetValue("format", out string fmt)
                && System.Enum.TryParse<RuriLib.GuidFormat>(fmt.Trim(), true, out var gf))
                b.GuidFmt = gf;
            if (seg.Properties.TryGetValue("guidUppercase", out string guc))
                b.GuidUppercase = guc.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        else if (funcType == BlockFunction.Function.MergeByteArrays)
        {
            if (seg.Properties.TryGetValue("input", out string inputVal))
            {
                inputVal = inputVal.Trim();
                b.InputString = inputVal.StartsWith("@")
                    ? Ob2AtRefToSbInput(inputVal)
                    : FromOb2String(inputVal);
            }
            if (seg.Properties.TryGetValue("secondInput", out string secondVal))
                b.SecondInput = FromOb2String(secondVal.Trim());
        }
        else if (funcType == BlockFunction.Function.Translate)
        {
            if (seg.Properties.TryGetValue("input", out string tInput))
            {
                tInput = tInput.Trim();
                b.InputString = tInput.StartsWith("@")
                    ? Ob2AtRefToSbInput(tInput)
                    : FromOb2String(tInput);
            }
            if (seg.Properties.TryGetValue("translations", out string transStr))
            {
                foreach (Match m in Regex.Matches(transStr,
                    @"\(\s*""((?:[^""\\]|\\.)*)""\s*,\s*""((?:[^""\\]|\\.)*)""\s*\)"))
                    b.TranslationDictionary[UnescapeOb2(m.Groups[1].Value)] = UnescapeOb2(m.Groups[2].Value);
            }
        }
        else if (funcType == BlockFunction.Function.RegexReplace)
        {
            // support both "original" (new name) and "input" (old name) for backward compat
            if (seg.Properties.TryGetValue("original", out string rrOrig) || seg.Properties.TryGetValue("input", out rrOrig))
            { rrOrig = rrOrig.Trim(); b.InputString = rrOrig.StartsWith("@") ? Ob2AtRefToSbInput(rrOrig) : FromOb2String(rrOrig); }
            if (seg.Properties.TryGetValue("pattern", out string pat))
                b.RegexMatch = FromOb2String(pat);
            if (seg.Properties.TryGetValue("replacement", out string repl))
                b.ReplaceWith = FromOb2String(repl);
        }
        else if (funcType == BlockFunction.Function.Substring)
        {
            if (seg.Properties.TryGetValue("input", out string subInp) || seg.Properties.TryGetValue("original", out subInp))
            { subInp = subInp.Trim(); b.InputString = subInp.StartsWith("@") ? Ob2AtRefToSbInput(subInp) : FromOb2String(subInp); }
            if (seg.Properties.TryGetValue("startIndex", out string si)) b.SubstringIndex  = FromOb2String(si).Trim().Trim('"');
            if (seg.Properties.TryGetValue("length",     out string sl)) b.SubstringLength = FromOb2String(sl).Trim().Trim('"');
        }
        else if (funcType == BlockFunction.Function.CountOccurrences)
        {
            if (seg.Properties.TryGetValue("input", out string coInp) || seg.Properties.TryGetValue("original", out coInp))
            { coInp = coInp.Trim(); b.InputString = coInp.StartsWith("@") ? Ob2AtRefToSbInput(coInp) : FromOb2String(coInp); }
            if (seg.Properties.TryGetValue("stringToFind", out string stf)) b.StringToFind = FromOb2String(stf);
        }
        else if (funcType == BlockFunction.Function.DateToUnixTime)
        {
            if (seg.Properties.TryGetValue("datetime", out string dtInp) || seg.Properties.TryGetValue("input", out dtInp))
            { dtInp = dtInp.Trim(); b.InputString = dtInp.StartsWith("@") ? Ob2AtRefToSbInput(dtInp) : FromOb2String(dtInp); }
            if (seg.Properties.TryGetValue("format", out string df) || seg.Properties.TryGetValue("dateFormat", out df))
                b.DateFormat = FromOb2String(df);
            if (seg.Properties.TryGetValue("type", out string utt)
                && System.Enum.TryParse<BlockFunction.DateToUnixTimeType>(utt.Trim(), true, out var uttVal))
                b.UnixTimeType = uttVal;
        }
        else if (funcType == BlockFunction.Function.UnixTimeToDate)
        {
            if (seg.Properties.TryGetValue("unixTime", out string utVal))
            {
                utVal = utVal.Trim();
                if (utVal == "@")
                    b.InputString = ""; // bare @ = use current time, no input variable
                else if (utVal.StartsWith("@"))
                    b.InputString = Ob2AtRefToSbInput(utVal);
                else
                    b.InputString = FromOb2String(utVal);
            }
            b.DateFormat = "yyyy-MM-dd:HH-mm-ss";
            if (seg.Properties.TryGetValue("format", out string utFmt))
                b.DateFormat = FromOb2String(utFmt);
        }
        else if (funcType == BlockFunction.Function.Replace)
        {
            if (seg.Properties.TryGetValue("original", out string origVal))
            { origVal = origVal.Trim(); b.InputString = origVal.StartsWith("@") ? Ob2AtRefToSbInput(origVal) : FromOb2String(origVal); }
            if (seg.Properties.TryGetValue("toReplace",   out string trVal))   b.ReplaceWhat = FromOb2String(trVal);
            if (seg.Properties.TryGetValue("replacement", out string replVal)) b.ReplaceWith  = FromOb2String(replVal);
        }
        else if (funcType == BlockFunction.Function.XOR || funcType == BlockFunction.Function.XORStrings)
        {
            if (seg.Properties.TryGetValue("key", out string xorKey))
                b.SecondInput = FromOb2String(xorKey);
        }
        else if (funcType == BlockFunction.Function.RSADecrypt)
        {
            if (seg.Properties.TryGetValue("n", out string rsaN)) b.RsaN = FromOb2String(rsaN);
            if (seg.Properties.TryGetValue("d", out string rsaD)) b.RsaD = FromOb2String(rsaD);
            if (seg.Properties.TryGetValue("oaep", out string oaep)) b.RsaOAEP = oaep.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
        }
        else if (funcType == BlockFunction.Function.JWTEncode)
        {
            if (seg.Properties.TryGetValue("secret", out string sec))    b.SecondInput  = FromOb2String(sec);
            if (seg.Properties.TryGetValue("algorithm", out string algo)) b.JwtAlgorithm = FromOb2String(algo);
        }
        else if (funcType == BlockFunction.Function.MaxFloat  || funcType == BlockFunction.Function.MinFloat  ||
                 funcType == BlockFunction.Function.MaxInt    || funcType == BlockFunction.Function.MinInt    ||
                 funcType == BlockFunction.Function.IndexOf)
        {
            if (seg.Properties.TryGetValue("second", out string sec2)) b.SecondInput = FromOb2String(sec2);
        }
        else if (funcType == BlockFunction.Function.GetKey)
        {
            if (seg.Properties.TryGetValue("dictionary", out string gkDict))
            { gkDict = gkDict.Trim(); b.InputString = gkDict.StartsWith("@") ? Ob2AtRefToSbInput(gkDict) : FromOb2String(gkDict); }
            if (seg.Properties.TryGetValue("key",        out string gkKey))  b.SecondInput  = FromOb2String(gkKey);
        }
        else if (funcType == BlockFunction.Function.RemoveByKey)
        {
            if (seg.Properties.TryGetValue("dictionary", out string rbDict))
            { rbDict = rbDict.Trim(); b.InputString = rbDict.StartsWith("@") ? Ob2AtRefToSbInput(rbDict) : FromOb2String(rbDict); }
            if (seg.Properties.TryGetValue("key",        out string rbKey))  b.SecondInput  = FromOb2String(rbKey);
        }
        else if (funcType == BlockFunction.Function.RandomFloat)
        {
            if (seg.Properties.TryGetValue("minimum", out string mn)) b.RandomMin = mn.Trim();
            if (seg.Properties.TryGetValue("maximum", out string mx)) b.RandomMax = mx.Trim();
        }
        else if (funcType == BlockFunction.Function.AddKeyValuePair)
        {
            if (seg.Properties.TryGetValue("dictionary", out string akDict))
            { akDict = akDict.Trim(); b.InputString = akDict.StartsWith("@") ? Ob2AtRefToSbInput(akDict) : FromOb2String(akDict); }
            if (seg.Properties.TryGetValue("key",        out string k))  b.SecondInput = FromOb2String(k);
            if (seg.Properties.TryGetValue("value",      out string v))  b.ThirdInput  = FromOb2String(v);
        }
        else if (funcType == BlockFunction.Function.CreateListOfNumbers)
        {
            if (seg.Properties.TryGetValue("count", out string cnt))  b.SecondInput = FromOb2String(cnt);
            if (seg.Properties.TryGetValue("step",  out string stp))  b.ThirdInput  = FromOb2String(stp);
        }
        else if (funcType == BlockFunction.Function.BCryptVerify)
        {
            if (seg.Properties.TryGetValue("password", out string bvPass))
            { bvPass = bvPass.Trim(); b.InputString = bvPass.StartsWith("@") ? Ob2AtRefToSbInput(bvPass) : FromOb2String(bvPass); }
            if (seg.Properties.TryGetValue("hash", out string bvHash)) b.SecondInput = FromOb2String(bvHash);
        }
        else if (funcType == BlockFunction.Function.ScryptDeriveKey)
        {
            if (seg.Properties.TryGetValue("password", out string sdPass))
            { sdPass = sdPass.Trim(); b.InputString = sdPass.StartsWith("@") ? Ob2AtRefToSbInput(sdPass) : FromOb2String(sdPass); }
            if (seg.Properties.TryGetValue("salt",    out string sdSalt)) b.SecondInput      = FromOb2String(sdSalt);
            if (seg.Properties.TryGetValue("n",       out string sdN)   && int.TryParse(sdN.Trim(), out int sdNv))   b.ScryptCost         = sdNv;
            if (seg.Properties.TryGetValue("r",       out string sdR)   && int.TryParse(sdR.Trim(), out int sdRv))   b.ScryptBlockSize    = sdRv;
            if (seg.Properties.TryGetValue("keyLen",  out string sdKL)  && int.TryParse(sdKL.Trim(), out int sdKLv)) b.ScryptOutputLength = sdKLv;
        }
        else if (funcType == BlockFunction.Function.AWS4Signature)
        {
            if (seg.Properties.TryGetValue("stringToSign", out string awsStr))
            { awsStr = awsStr.Trim(); b.InputString = awsStr.StartsWith("@") ? Ob2AtRefToSbInput(awsStr) : FromOb2String(awsStr); }
            if (seg.Properties.TryGetValue("secretKey", out string awsSec))  b.SecondInput = FromOb2String(awsSec);
            if (seg.Properties.TryGetValue("date",      out string awsDt))   b.ThirdInput  = FromOb2String(awsDt);
            if (seg.Properties.TryGetValue("region",    out string awsReg))  b.AwsRegion   = FromOb2String(awsReg);
            if (seg.Properties.TryGetValue("service",   out string awsSvc))  b.AwsService  = FromOb2String(awsSvc);
        }
        else if (funcType == BlockFunction.Function.RandomNum)
        {
            // OB2 uses minimum/maximum; BlockFunction uses RandomMin/RandomMax
            if (seg.Properties.TryGetValue("minimum", out string minVal) || seg.Properties.TryGetValue("min", out minVal))
                b.RandomMin = minVal?.Trim().Trim('"') ?? "0";
            if (seg.Properties.TryGetValue("maximum", out string maxVal) || seg.Properties.TryGetValue("max", out maxVal))
                b.RandomMax = maxVal?.Trim().Trim('"') ?? "0";
            if (seg.Properties.TryGetValue("randomZeroPad", out string zpStr))
                b.RandomZeroPad = zpStr.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        else if (seg.Properties.TryGetValue("input", out string inputValGen))
        {
            inputValGen = inputValGen.Trim();
            b.InputString = inputValGen.StartsWith("@")
                ? Ob2AtRefToSbInput(inputValGen)
                : FromOb2String(inputValGen);
        }

        // Hash / HMAC: OB2 uses "hashFunction" property; default in BlockFunction is SHA512 so must always set it.
        if (funcType == BlockFunction.Function.Hash || funcType == BlockFunction.Function.HMAC)
        {
            if (seg.Properties.TryGetValue("hashFunction", out string hf) && !string.IsNullOrWhiteSpace(hf)
                && System.Enum.TryParse<RuriLib.Functions.Crypto.Hash>(hf.Trim(), true, out var parsedHash))
                b.HashType = parsedHash;
        }
        // HMAC: OB2 uses "key" property
        if (funcType == BlockFunction.Function.HMAC)
        {
            if (seg.Properties.TryGetValue("key", out string hmacKeyVal) && !string.IsNullOrWhiteSpace(hmacKeyVal))
            {
                hmacKeyVal = hmacKeyVal.Trim();
                b.HmacKey = hmacKeyVal.StartsWith("@")
                    ? Ob2AtRefToSbInput(hmacKeyVal)
                    : FromOb2String(hmacKeyVal);
            }
            if (seg.Properties.TryGetValue("keyBase64", out string kb64))
                b.KeyBase64 = kb64.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
            if (seg.Properties.TryGetValue("outputBase64", out string ob64) ||
                seg.Properties.TryGetValue("hmacBase64",   out ob64))
                b.HmacBase64 = ob64.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        // AESEncrypt / AESDecrypt: key, iv, mode, padding, hexKeys
        if (funcType == BlockFunction.Function.AESEncrypt || funcType == BlockFunction.Function.AESDecrypt)
        {
            if (seg.Properties.TryGetValue("key", out string aesKey))
                b.AesKey = FromOb2String(aesKey.Trim());
            if (seg.Properties.TryGetValue("iv", out string aesIV))
                b.AesIV = FromOb2String(aesIV.Trim());
            if (seg.Properties.TryGetValue("mode", out string aesMode) && !string.IsNullOrWhiteSpace(aesMode)
                && System.Enum.TryParse<System.Security.Cryptography.CipherMode>(aesMode.Trim(), true, out var cm))
                b.AesMode = cm;
            if (seg.Properties.TryGetValue("padding", out string aesPad) && !string.IsNullOrWhiteSpace(aesPad)
                && System.Enum.TryParse<System.Security.Cryptography.PaddingMode>(aesPad.Trim(), true, out var pm))
                b.AesPadding = pm;
            if (seg.Properties.TryGetValue("hexKeys", out string hkeys))
                b.HexKeys = hkeys.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        // RSAEncrypt: n (modulus), e (exponent), oaep
        if (funcType == BlockFunction.Function.RSAEncrypt)
        {
            if (seg.Properties.TryGetValue("n", out string rsaN))
                b.RsaN = FromOb2String(rsaN.Trim());
            if (seg.Properties.TryGetValue("e", out string rsaE))
                b.RsaE = FromOb2String(rsaE.Trim());
            if (seg.Properties.TryGetValue("oaep", out string oaep))
                b.RsaOAEP = oaep.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        // Split: separator, index, stringSplitOption
        if (funcType == BlockFunction.Function.Split)
        {
            if (seg.Properties.TryGetValue("separator", out string sep))
                b.Separator = FromOb2String(sep.Trim());
            if (seg.Properties.TryGetValue("index", out string sidx)
                && int.TryParse(sidx.Trim().Trim('"'), out int sidxInt))
                b.SplitIndex = sidxInt;
            if (seg.Properties.TryGetValue("stringSplitOption", out string sso) && !string.IsNullOrWhiteSpace(sso)
                && System.Enum.TryParse<System.StringSplitOptions>(sso.Trim(), true, out var ssoVal))
                b.StringSplitOption = ssoVal;
        }
        // CharAt: index
        if (funcType == BlockFunction.Function.CharAt)
        {
            if (seg.Properties.TryGetValue("index", out string cidx))
                b.CharIndex = FromOb2String(cidx.Trim());
        }
        // SCrypt: method, salt, cost, blockSize, outputLength, base64Output, hashedPassword
        if (funcType == BlockFunction.Function.SCrypt)
        {
            if (seg.Properties.TryGetValue("method", out string scMeth) && !string.IsNullOrWhiteSpace(scMeth)
                && System.Enum.TryParse<BlockFunction.ScryptMethods>(scMeth.Trim(), true, out var scM))
                b.ScryptMeth = scM;
            if (seg.Properties.TryGetValue("salt", out string scSalt))
                b.ScryptSalt = FromOb2String(scSalt.Trim());
            if (seg.Properties.TryGetValue("cost", out string scCost)
                && int.TryParse(scCost.Trim(), out int scCostInt))
                b.ScryptCost = scCostInt;
            if (seg.Properties.TryGetValue("blockSize", out string scBs)
                && int.TryParse(scBs.Trim(), out int scBsInt))
                b.ScryptBlockSize = scBsInt;
            if (seg.Properties.TryGetValue("outputLength", out string scOl)
                && int.TryParse(scOl.Trim(), out int scOlInt))
                b.ScryptOutputLength = scOlInt;
            if (seg.Properties.TryGetValue("base64Output", out string scB64))
                b.Base64Output = scB64.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
            if (seg.Properties.TryGetValue("hashedPassword", out string scHpw))
                b.ScryptHashedPassword = FromOb2String(scHpw.Trim());
        }
        // BCrypt: method (BCryptHashGenSalt defaults to GenerateSalt), salt, workFactor, useWorkFactor, hashedPassword
        if (funcType == BlockFunction.Function.BCrypt)
        {
            string defaultMeth = seg.BlockType.Equals("BCryptHashGenSalt", StringComparison.OrdinalIgnoreCase)
                ? "GenerateSalt" : "Encode";
            string methToUse = seg.Properties.TryGetValue("method", out string bcM) && !string.IsNullOrWhiteSpace(bcM)
                ? bcM.Trim() : defaultMeth;
            if (System.Enum.TryParse<BlockFunction.BCryptMethods>(methToUse, true, out var bcMeth))
                b.BCryptMeth = bcMeth;
            if (seg.Properties.TryGetValue("salt", out string bcSalt))
                b.BCryptSalt = FromOb2String(bcSalt.Trim());
            if (seg.Properties.TryGetValue("useWorkFactor", out string uwf))
                b.UseWorkFactor = uwf.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
            if (seg.Properties.TryGetValue("workFactor", out string wf)
                && int.TryParse(wf.Trim(), out int wfInt))
                b.BCryptWorkFactor = wfInt;
            if (seg.Properties.TryGetValue("hashedPassword", out string bcHpw))
                b.BCryptHashedPassword = FromOb2String(bcHpw.Trim());
        }

        return b;
    }

    private static BlockKeycheck SegToKeyCheck(LoliCodeSegment seg)
    {
        var b = new BlockKeycheck();
        if (!string.IsNullOrEmpty(seg.Label)) b.Label = seg.Label;
        b.Disabled = seg.Disabled;

        // Block-level flags from Properties (support both SB "banOnToCheck" and OB2 "banIfNoMatch")
        if (seg.Properties.TryGetValue("banOnToCheck", out string botc))
            b.BanOnToCheck = botc.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        if (seg.Properties.TryGetValue("banIfNoMatch", out string bNM))
            b.BanOnToCheck = bNM.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        if (seg.Properties.TryGetValue("banifnomatch", out string bNM2))
            b.BanOnToCheck = bNM2.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        if (seg.Properties.TryGetValue("banOn4XX", out string bo4))
            b.BanOn4XX = bo4.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);

        foreach (var lc in seg.KeyChains)
        {
            var chain = new KeyChain
            {
                Type = lc.ChainType switch {
                    LoliCodeKeyChainType.SUCCESS   => KeyChain.KeychainType.Success,
                    LoliCodeKeyChainType.FAIL      => KeyChain.KeychainType.Failure,
                    LoliCodeKeyChainType.BAN       => KeyChain.KeychainType.Ban,
                    LoliCodeKeyChainType.RETRY     => KeyChain.KeychainType.Retry,
                    LoliCodeKeyChainType.CUSTOM    => KeyChain.KeychainType.Custom,
                    LoliCodeKeyChainType.TOCHECK   => KeyChain.KeychainType.Custom,
                    LoliCodeKeyChainType.EXPIRED   => KeyChain.KeychainType.Custom,
                    LoliCodeKeyChainType.TWOFACTOR => KeyChain.KeychainType.Custom,
                    _                              => KeyChain.KeychainType.Failure,
                },
                Mode = lc.Mode == LoliCodeKeyMode.AND
                    ? KeyChain.KeychainMode.AND : KeyChain.KeychainMode.OR,
                CustomType = lc.ChainType switch {
                    LoliCodeKeyChainType.TOCHECK   => "TOCHECK",
                    LoliCodeKeyChainType.EXPIRED   => "EXPIRED",
                    LoliCodeKeyChainType.TWOFACTOR => "2FACTOR",
                    _                              => lc.CustomType,
                },
            };

            foreach (var lk in lc.Keys)
            {
                var key = new Key
                {
                    LeftTerm  = Ob2DataRefToSbRef(lk.LeftTerm),
                    RightTerm = lk.RightTerm,
                };
                if (Enum.TryParse<Comparer>(MapStringToComparer(lk.Comparer), true, out var cmp))
                    key.Comparer = cmp;
                chain.Keys.Add(key);
            }
            b.KeyChains.Add(chain);
        }
        return b;
    }

    // @data.SOURCE → <SOURCE>;  @data.COOKIES["name"] → <COOKIES(name)>;  @input.USER → <USER>
    private static string Ob2DataRefToSbRef(string ob2)
    {
        if (string.IsNullOrEmpty(ob2)) return ob2;
        var cookieM = Regex.Match(ob2, @"^@data\.COOKIES\[""([^""]+)""\]$", RegexOptions.IgnoreCase);
        if (cookieM.Success) return $"<COOKIES({cookieM.Groups[1].Value})>";
        var headerM = Regex.Match(ob2, @"^@data\.HEADERS\[""([^""]+)""\]$", RegexOptions.IgnoreCase);
        if (headerM.Success) return $"<HEADERS({headerM.Groups[1].Value})>";
        if (ob2.StartsWith("@data."))  return "<" + ob2.Substring(6) + ">";
        if (ob2.StartsWith("@input.")) return "<" + ob2.Substring(7) + ">";
        if (ob2.StartsWith("@"))       return "<" + ob2.Substring(1) + ">"; // user variable
        if (ob2.StartsWith("<"))       return ob2;
        return ob2;
    }

    private static string MapStringToComparer(string name) =>
        (name ?? "").ToLowerInvariant() switch {
            "contains"                  => "Contains",
            "doesnotcontain"            => "DoesNotContain",
            "equalto" or "equal"        => "EqualTo",
            "notequalto" or "notequal"  => "NotEqualTo",
            "lessthan"                  => "LessThan",
            "lessthanorequal"           => "LessThanOrEqual",
            "greaterthan"               => "GreaterThan",
            "greaterorequal"            => "GreaterOrEqual",
            "matchesregex"              => "MatchesRegex",
            "doesnotmatchregex"         => "DoesNotMatchRegex",
            "exists"                    => "Exists",
            "doesnotexist"              => "DoesNotExist",
            "startswith"                => "StartsWith",
            "endswith"                  => "EndsWith",
            _                           => "Contains",
        };

    private static BlockUtility SegToMiscUtility(LoliCodeSegment seg, MiscAction action)
    {
        var b = new BlockUtility { Group = UtilityGroup.Misc, MiscAction = action };
        if (!string.IsNullOrEmpty(seg.Label)) b.Label = seg.Label;
        b.Disabled = seg.Disabled;
        if (seg.OutputVar != null) { b.VariableName = seg.OutputVar; b.IsCapture = seg.IsCapture; }
        return b;
    }

    private static BlockUtility SegToConversionUtility(LoliCodeSegment seg, ConversionAction action)
    {
        var b = new BlockUtility { Group = UtilityGroup.Conversion, ConversionAct = action };
        if (!string.IsNullOrEmpty(seg.Label)) b.Label = seg.Label;
        b.Disabled = seg.Disabled;
        if (seg.OutputVar != null) { b.VariableName = seg.OutputVar; b.IsCapture = seg.IsCapture; }

        // OB2 property names vary per block — try all known names for the input value
        string Ob2Input(params string[] keys)
        {
            foreach (var k in keys)
                if (seg.Properties.TryGetValue(k, out string v))
                {
                    v = v.Trim();
                    return v.StartsWith("@") ? Ob2AtRefToSbInput(v) : FromOb2String(v);
                }
            return "";
        }

        switch (action)
        {
        case ConversionAction.BigIntegerToByteArray:
            b.InputString = Ob2Input("bigInteger", "input"); break;
        case ConversionAction.ByteArrayToBigInteger:
        case ConversionAction.BytesToBase64:
        case ConversionAction.BytesToBinaryString:
        case ConversionAction.BytesToHex:
            b.InputString = Ob2Input("bytes", "input"); break;
        case ConversionAction.BytesToString:
            b.InputString = Ob2Input("input", "bytes");
            if (seg.Properties.TryGetValue("encoding", out string bsEnc))
                b.ByteStringEncoding = bsEnc.Trim().Trim('"');
            break;
        case ConversionAction.StringToBytes:
            b.InputString = Ob2Input("input", "str");
            if (seg.Properties.TryGetValue("encoding", out string sbEnc))
                b.ByteStringEncoding = sbEnc.Trim().Trim('"');
            break;
        case ConversionAction.Base64ToBytes:
            b.InputString = Ob2Input("base64String", "input"); break;
        case ConversionAction.Base64ToUtf8:
        case ConversionAction.BinaryStringToBytes:
        case ConversionAction.Utf8ToBase64:
            b.InputString = Ob2Input("input", "binaryString"); break;
        case ConversionAction.HexToBytes:
            b.InputString = Ob2Input("hexString", "input"); break;
        case ConversionAction.ReadableSize:
            b.InputString = Ob2Input("input");
            if (seg.Properties.TryGetValue("outputBits", out string obits))
                b.ReadableSizeOutputBits = obits.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
            if (seg.Properties.TryGetValue("binaryUnit", out string bunit))
                b.ReadableSizeBinaryUnit = bunit.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
            if (seg.Properties.TryGetValue("decimalPlaces", out string dplaces))
                b.ReadableSizeDecimalPlaces = dplaces.Trim().Trim('"');
            break;
        }

        return b;
    }

    private static BlockUtility SegToImagesUtility(LoliCodeSegment seg, ImageAction action)
    {
        var b = new BlockUtility { Group = UtilityGroup.Images, ImageAct = action };
        if (!string.IsNullOrEmpty(seg.Label)) b.Label = seg.Label;
        b.Disabled = seg.Disabled;
        if (seg.OutputVar != null) { b.VariableName = seg.OutputVar; b.IsCapture = seg.IsCapture; }

        if (action == ImageAction.SvgToPng)
        {
            if (seg.Properties.TryGetValue("xml", out string xmlVal))
            {
                xmlVal = xmlVal.Trim();
                b.InputString = xmlVal.StartsWith("@") ? Ob2AtRefToSbInput(xmlVal) : FromOb2String(xmlVal);
            }
            if (seg.Properties.TryGetValue("width", out string svgW))
                b.ImageSvgWidth = svgW.Trim().Trim('"');
            if (seg.Properties.TryGetValue("height", out string svgH))
                b.ImageSvgHeight = svgH.Trim().Trim('"');
        }

        return b;
    }

    private static BlockBase SegToUtility(LoliCodeSegment seg)
    {
        // Reconstruct from the LoliScript text stored in the "ls" property
        if (seg.Properties.TryGetValue("ls", out string lsCode))
        {
            string raw = FromOb2String(lsCode.Trim());
            try
            {
                var b = BlockParser.Parse(raw);
                if (!string.IsNullOrEmpty(seg.Label)) b.Label = seg.Label;
                b.Disabled = seg.Disabled;
                return b;
            }
            catch { }
        }
        // Fallback: empty utility block
        var fallback = new BlockUtility();
        if (!string.IsNullOrEmpty(seg.Label)) fallback.Label = seg.Label;
        fallback.Disabled = seg.Disabled;
        return fallback;
    }

    private static BlockLSCode SegToLSCode(LoliCodeSegment seg)
    {
        // Preserve the raw BLOCK:...ENDBLOCK text so it can be converted back
        var sb = new StringBuilder();
        string disabledMark = seg.Disabled ? "!" : "";
        sb.AppendLine($"BLOCK:{disabledMark}{seg.BlockType}");
        if (!string.IsNullOrEmpty(seg.Label)) sb.AppendLine($"  LABEL:{seg.Label}");
        foreach (var kv in seg.Properties)
            sb.AppendLine($"  {kv.Key} = {kv.Value}");

        // Serialize any keychains so they survive the round-trip for unknown block types
        foreach (var chain in seg.KeyChains)
        {
            string chainType = chain.ChainType switch {
                LoliCodeKeyChainType.SUCCESS => "SUCCESS",
                LoliCodeKeyChainType.FAIL    => "FAIL",
                LoliCodeKeyChainType.BAN     => "BAN",
                LoliCodeKeyChainType.RETRY   => "RETRY",
                LoliCodeKeyChainType.CUSTOM  => "CUSTOM",
                _                            => "FAIL",
            };
            string chainMode = chain.Mode == LoliCodeKeyMode.AND ? "AND" : "OR";
            string chainHeader = chainType == "CUSTOM" && !string.IsNullOrEmpty(chain.CustomType)
                ? $"  KEYCHAIN {chainType} \"{chain.CustomType.Replace("\"", "\\\"")}\" {chainMode}"
                : $"  KEYCHAIN {chainType} {chainMode}";
            sb.AppendLine(chainHeader);
            foreach (var key in chain.Keys)
            {
                string kw = key.Comparer == "MatchesRegex" || key.Comparer == "DoesNotMatchRegex"
                    ? "REGEXKEY" : "STRINGKEY";
                if (string.IsNullOrEmpty(key.RightTerm))
                    sb.AppendLine($"    {kw} {key.LeftTerm} {key.Comparer}");
                else
                    sb.AppendLine($"    {kw} {key.LeftTerm} {key.Comparer} \"{key.RightTerm}\"");
            }
        }

        if (seg.OutputVar != null)
            sb.AppendLine(seg.IsCapture ? $"  => CAP @{seg.OutputVar}" : $"  => VAR @{seg.OutputVar}");
        sb.Append("ENDBLOCK");

        return new BlockLSCode { Script = sb.ToString(), Label = seg.BlockType };
    }

    private static string BypassCFToLoliCode(BlockBypassCF b)
    {
        var sb = new StringBuilder();
        sb.AppendLine(BlockHeader("BypassCF", b.Disabled));
        if (!string.IsNullOrEmpty(b.Label)) sb.AppendLine($"  LABEL:{b.Label}");
        sb.AppendLine($"  url = {ToOb2String(b.Url)}");
        const string defaultUA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.149 Safari/537.36";
        if (b.UserAgent != defaultUA)
            sb.AppendLine($"  userAgent = {ToOb2String(b.UserAgent)}");
        if (b.SecurityProtocol != SecurityProtocol.SystemDefault)
            sb.AppendLine($"  securityProtocol = {b.SecurityProtocol}");
        if (!b.PrintResponseInfo)
            sb.AppendLine("  printResponseInfo = false");
        if (b.AutoRedirect)
            sb.AppendLine("  autoRedirect = true");
        sb.Append("ENDBLOCK");
        return sb.ToString();
    }

    private static BlockBypassCF SegToBypassCF(LoliCodeSegment seg)
    {
        var b = new BlockBypassCF();
        if (!string.IsNullOrEmpty(seg.Label)) b.Label = seg.Label;
        b.Disabled = seg.Disabled;

        if (seg.Properties.TryGetValue("url", out string url))
            b.Url = FromOb2String(url);
        if (seg.Properties.TryGetValue("userAgent", out string ua))
            b.UserAgent = FromOb2String(ua);
        if (seg.Properties.TryGetValue("securityProtocol", out string sp) &&
            Enum.TryParse<SecurityProtocol>(sp.Trim(), true, out var proto))
            b.SecurityProtocol = proto;
        if (seg.Properties.TryGetValue("printResponseInfo", out string pri))
            b.PrintResponseInfo = !pri.Trim().Equals("false", StringComparison.OrdinalIgnoreCase);
        if (seg.Properties.TryGetValue("autoRedirect", out string ar))
            b.AutoRedirect = ar.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);

        return b;
    }

    private static string CfClearanceToLoliCode(BlockCfClearance b)
    {
        var sb = new StringBuilder();
        sb.AppendLine(BlockHeader("CfClearance", b.Disabled));
        if (!string.IsNullOrEmpty(b.Label)) sb.AppendLine($"  LABEL:{b.Label}");
        sb.AppendLine($"  url = {ToOb2String(b.Url)}");
        sb.AppendLine($"  outputVariable = {ToOb2String(b.OutputVariable)}");
        if (b.Timeout != 30)   sb.AppendLine($"  timeout = {b.Timeout}");
        if (b.Port    != 9516) sb.AppendLine($"  port = {b.Port}");
        if (b.StoreCookies)    sb.AppendLine($"  storeCookies = true");
        sb.Append("ENDBLOCK");
        return sb.ToString();
    }

    private static BlockCfClearance SegToCfClearance(LoliCodeSegment seg)
    {
        var b = new BlockCfClearance();
        if (!string.IsNullOrEmpty(seg.Label)) b.Label = seg.Label;
        b.Disabled = seg.Disabled;
        if (seg.Properties.TryGetValue("url",            out string url))    b.Url            = FromOb2String(url);
        if (seg.Properties.TryGetValue("outputVariable", out string outVar)) b.OutputVariable = FromOb2String(outVar);
        if (seg.Properties.TryGetValue("timeout",        out string tmo) && int.TryParse(tmo.Trim(), out int t))  b.Timeout = t;
        if (seg.Properties.TryGetValue("port",           out string prt) && int.TryParse(prt.Trim(), out int p))  b.Port    = p;
        if (seg.Properties.TryGetValue("storeCookies",   out string sc))     b.StoreCookies   = sc.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        return b;
    }

    private static string TurnstileToLoliCode(BlockTurnstile b)
    {
        var sb = new StringBuilder();
        sb.AppendLine(BlockHeader("Turnstile", b.Disabled));
        if (!string.IsNullOrEmpty(b.Label)) sb.AppendLine($"  LABEL:{b.Label}");
        sb.AppendLine($"  domain = {ToOb2String(b.Domain)}");
        sb.AppendLine($"  siteKey = {ToOb2String(b.SiteKey)}");
        sb.AppendLine($"  outputVariable = {ToOb2String(b.OutputVariable)}");
        if (!string.IsNullOrEmpty(b.Action)) sb.AppendLine($"  action = {ToOb2String(b.Action)}");
        if (!string.IsNullOrEmpty(b.Proxy)) sb.AppendLine($"  proxy = {ToOb2String(b.Proxy)}");
        if (b.Port != 8742) sb.AppendLine($"  port = {b.Port}");
        sb.Append("ENDBLOCK");
        return sb.ToString();
    }

    private static BlockTurnstile SegToTurnstile(LoliCodeSegment seg)
    {
        var b = new BlockTurnstile();
        if (!string.IsNullOrEmpty(seg.Label)) b.Label = seg.Label;
        b.Disabled = seg.Disabled;
        if (seg.Properties.TryGetValue("domain", out string domain))
            b.Domain = FromOb2String(domain);
        if (seg.Properties.TryGetValue("siteKey", out string siteKey))
            b.SiteKey = FromOb2String(siteKey);
        if (seg.Properties.TryGetValue("outputVariable", out string outVar))
            b.OutputVariable = FromOb2String(outVar);
        if (seg.Properties.TryGetValue("action", out string act))
            b.Action = FromOb2String(act);
        if (seg.Properties.TryGetValue("proxy", out string prx))
            b.Proxy = FromOb2String(prx);
        if (seg.Properties.TryGetValue("port", out string port) && int.TryParse(port.Trim(), out int p))
            b.Port = p;
        return b;
    }

    private static string RecaptchaV3ToLoliCode(BlockRecaptchaV3 b)
    {
        var sb = new StringBuilder();
        sb.AppendLine(BlockHeader("RecaptchaV3", b.Disabled));
        if (!string.IsNullOrEmpty(b.Label)) sb.AppendLine($"  LABEL:{b.Label}");
        sb.AppendLine($"  url = {ToOb2String(b.Url)}");
        sb.AppendLine($"  siteKey = {ToOb2String(b.SiteKey)}");
        sb.AppendLine($"  outputVariable = {ToOb2String(b.OutputVariable)}");
        if (b.Action != "submit") sb.AppendLine($"  action = {ToOb2String(b.Action)}");
        if (b.Port != 9512)      sb.AppendLine($"  port = {b.Port}");
        sb.Append("ENDBLOCK");
        return sb.ToString();
    }

    private static BlockRecaptchaV3 SegToRecaptchaV3(LoliCodeSegment seg)
    {
        var b = new BlockRecaptchaV3();
        if (!string.IsNullOrEmpty(seg.Label)) b.Label = seg.Label;
        b.Disabled = seg.Disabled;
        if (seg.Properties.TryGetValue("url",            out string url))     b.Url            = FromOb2String(url);
        if (seg.Properties.TryGetValue("siteKey",        out string siteKey)) b.SiteKey        = FromOb2String(siteKey);
        if (seg.Properties.TryGetValue("outputVariable", out string outVar))  b.OutputVariable = FromOb2String(outVar);
        if (seg.Properties.TryGetValue("action",         out string act))     b.Action         = FromOb2String(act);
        if (seg.Properties.TryGetValue("port",           out string prt) && int.TryParse(prt.Trim(), out int p)) b.Port    = p;
        return b;
    }

    private static string RecaptchaV2InvisibleToLoliCode(BlockRecaptchaV2Invisible b)
    {
        var sb = new StringBuilder();
        sb.AppendLine(BlockHeader("RecaptchaV2Invisible", b.Disabled));
        if (!string.IsNullOrEmpty(b.Label)) sb.AppendLine($"  LABEL:{b.Label}");
        sb.AppendLine($"  url = {ToOb2String(b.Url)}");
        sb.AppendLine($"  outputVariable = {ToOb2String(b.OutputVariable)}");
        if (b.Action != "submit")             sb.AppendLine($"  action = {ToOb2String(b.Action)}");
        if (!string.IsNullOrEmpty(b.Proxy))   sb.AppendLine($"  proxy = {ToOb2String(b.Proxy)}");
        if (b.Port != 9513)                   sb.AppendLine($"  port = {b.Port}");
        sb.Append("ENDBLOCK");
        return sb.ToString();
    }

    private static BlockRecaptchaV2Invisible SegToRecaptchaV2Invisible(LoliCodeSegment seg)
    {
        var b = new BlockRecaptchaV2Invisible();
        if (!string.IsNullOrEmpty(seg.Label)) b.Label = seg.Label;
        b.Disabled = seg.Disabled;
        if (seg.Properties.TryGetValue("url",            out string url))     b.Url            = FromOb2String(url);
        if (seg.Properties.TryGetValue("outputVariable", out string outVar))  b.OutputVariable = FromOb2String(outVar);
        if (seg.Properties.TryGetValue("action",         out string act))     b.Action         = FromOb2String(act);
        if (seg.Properties.TryGetValue("proxy",          out string prx))     b.Proxy          = FromOb2String(prx);
        if (seg.Properties.TryGetValue("port",           out string prt) && int.TryParse(prt.Trim(), out int p)) b.Port = p;
        return b;
    }

    private static string DataDomeToLoliCode(BlockDataDome b)
    {
        var sb = new StringBuilder();
        sb.AppendLine(BlockHeader("DataDome", b.Disabled));
        if (!string.IsNullOrEmpty(b.Label)) sb.AppendLine($"  LABEL:{b.Label}");
        sb.AppendLine($"  url = {ToOb2String(b.Url)}");
        sb.AppendLine($"  outputCookie = {ToOb2String(b.OutputCookie)}");
        sb.AppendLine($"  outputUserAgent = {ToOb2String(b.OutputUserAgent)}");
        if (!string.IsNullOrEmpty(b.Proxy)) sb.AppendLine($"  proxy = {ToOb2String(b.Proxy)}");
        if (b.Port != 9505) sb.AppendLine($"  port = {b.Port}");
        sb.Append("ENDBLOCK");
        return sb.ToString();
    }

    private static BlockDataDome SegToDataDome(LoliCodeSegment seg)
    {
        var b = new BlockDataDome();
        if (!string.IsNullOrEmpty(seg.Label)) b.Label = seg.Label;
        b.Disabled = seg.Disabled;
        if (seg.Properties.TryGetValue("url",             out string url))  b.Url             = FromOb2String(url);
        if (seg.Properties.TryGetValue("outputCookie",    out string oc))   b.OutputCookie    = FromOb2String(oc);
        if (seg.Properties.TryGetValue("outputUserAgent", out string oua))  b.OutputUserAgent = FromOb2String(oua);
        if (seg.Properties.TryGetValue("proxy",           out string prx))  b.Proxy           = FromOb2String(prx);
        if (seg.Properties.TryGetValue("port", out string prt) && int.TryParse(prt.Trim(), out int p)) b.Port = p;
        return b;
    }

    private static string AkmCookiesToLoliCode(BlockAkmCookies b)
    {
        var sb = new StringBuilder();
        sb.AppendLine(BlockHeader("AkmCookies", b.Disabled));
        if (!string.IsNullOrEmpty(b.Label)) sb.AppendLine($"  LABEL:{b.Label}");
        sb.AppendLine($"  url = {ToOb2String(b.Url)}");
        sb.AppendLine($"  outputCookies = {ToOb2String(b.OutputCookies)}");
        sb.AppendLine($"  outputUserAgent = {ToOb2String(b.OutputUserAgent)}");
        if (!string.IsNullOrEmpty(b.Proxy)) sb.AppendLine($"  proxy = {ToOb2String(b.Proxy)}");
        if (b.Port != 8085) sb.AppendLine($"  port = {b.Port}");
        sb.Append("ENDBLOCK");
        return sb.ToString();
    }

    private static BlockAkmCookies SegToAkmCookies(LoliCodeSegment seg)
    {
        var b = new BlockAkmCookies();
        if (!string.IsNullOrEmpty(seg.Label)) b.Label = seg.Label;
        b.Disabled = seg.Disabled;
        if (seg.Properties.TryGetValue("url",             out string url))   b.Url             = FromOb2String(url);
        if (seg.Properties.TryGetValue("outputCookies",   out string oc))    b.OutputCookies   = FromOb2String(oc);
        if (seg.Properties.TryGetValue("outputUserAgent", out string oua))   b.OutputUserAgent = FromOb2String(oua);
        if (seg.Properties.TryGetValue("proxy",           out string prx))   b.Proxy           = FromOb2String(prx);
        if (seg.Properties.TryGetValue("port", out string prt) && int.TryParse(prt.Trim(), out int p)) b.Port = p;
        return b;
    }

    private static string FriendlyCaptchaToLoliCode(BlockFriendlyCaptcha b)
    {
        var sb = new StringBuilder();
        sb.AppendLine(BlockHeader("FriendlyCaptcha", b.Disabled));
        if (!string.IsNullOrEmpty(b.Label)) sb.AppendLine($"  LABEL:{b.Label}");
        sb.AppendLine($"  siteKey = {ToOb2String(b.SiteKey)}");
        sb.AppendLine($"  useEuEndpoint = {(b.UseEuEndpoint ? "True" : "False")}");
        sb.AppendLine($"  outputVariable = {ToOb2String(b.OutputVariable)}");
        sb.Append("ENDBLOCK");
        return sb.ToString();
    }

    private static BlockFriendlyCaptcha SegToFriendlyCaptcha(LoliCodeSegment seg)
    {
        var b = new BlockFriendlyCaptcha();
        if (!string.IsNullOrEmpty(seg.Label)) b.Label = seg.Label;
        b.Disabled = seg.Disabled;
        if (seg.Properties.TryGetValue("siteKey",        out string key))    b.SiteKey       = FromOb2String(key);
        if (seg.Properties.TryGetValue("useEuEndpoint",  out string eu))     b.UseEuEndpoint = eu.Trim().Equals("True", System.StringComparison.OrdinalIgnoreCase);
        if (seg.Properties.TryGetValue("outputVariable", out string outVar)) b.OutputVariable = FromOb2String(outVar);
        return b;
    }

    private static string AltchaToLoliCode(BlockAltcha b)
    {
        var sb = new StringBuilder();
        sb.AppendLine(BlockHeader("Altcha", b.Disabled));
        if (!string.IsNullOrEmpty(b.Label)) sb.AppendLine($"  LABEL:{b.Label}");
        sb.AppendLine($"  challengeUrl = {ToOb2String(b.ChallengeUrl)}");
        sb.AppendLine($"  outputVariable = {ToOb2String(b.OutputVariable)}");
        sb.Append("ENDBLOCK");
        return sb.ToString();
    }

    private static BlockAltcha SegToAltcha(LoliCodeSegment seg)
    {
        var b = new BlockAltcha();
        if (!string.IsNullOrEmpty(seg.Label)) b.Label = seg.Label;
        b.Disabled = seg.Disabled;
        if (seg.Properties.TryGetValue("challengeUrl", out string url))
            b.ChallengeUrl = FromOb2String(url);
        if (seg.Properties.TryGetValue("outputVariable", out string outVar))
            b.OutputVariable = FromOb2String(outVar);
        return b;
    }

    private static string RecaptchaV3BypassToLoliCode(BlockRecaptchaV3Bypass b)
    {
        var sb = new StringBuilder();
        sb.AppendLine(BlockHeader("RecaptchaV3Bypass", b.Disabled));
        if (!string.IsNullOrEmpty(b.Label)) sb.AppendLine($"  LABEL:{b.Label}");
        sb.AppendLine($"  variableName = {ToOb2String(b.VariableName)}");
        sb.AppendLine($"  getUrl       = {ToOb2String(b.GetUrl)}");
        sb.AppendLine($"  bg           = {ToOb2String(b.Bg)}");
        sb.AppendLine($"  postUrl      = {ToOb2String(b.PostUrl)}");
        sb.AppendLine($"  referer      = {ToOb2String(b.Referer)}");
        sb.AppendLine($"  userAgent    = {ToOb2String(b.UserAgent)}");
        sb.Append("ENDBLOCK");
        return sb.ToString();
    }

    private static BlockRecaptchaV3Bypass SegToRecaptchaV3Bypass(LoliCodeSegment seg)
    {
        var b = new BlockRecaptchaV3Bypass();
        if (!string.IsNullOrEmpty(seg.Label)) b.Label = seg.Label;
        b.Disabled = seg.Disabled;
        if (seg.Properties.TryGetValue("variableName", out string varName)) b.VariableName = FromOb2String(varName);
        if (seg.Properties.TryGetValue("getUrl",       out string getUrl))  b.GetUrl       = FromOb2String(getUrl);
        if (seg.Properties.TryGetValue("bg",           out string bg))      b.Bg           = FromOb2String(bg);
        if (seg.Properties.TryGetValue("postUrl",      out string postUrl)) b.PostUrl      = FromOb2String(postUrl);
        if (seg.Properties.TryGetValue("referer",      out string referer)) b.Referer      = FromOb2String(referer);
        if (seg.Properties.TryGetValue("userAgent",    out string ua))      b.UserAgent    = FromOb2String(ua);
        return b;
    }

    // ─── OB2 value helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Convert a field value to OB2 string format.
    /// Uses $"..." (which supports \" and \\ escapes in OB2) whenever the string
    /// contains variable refs, backslashes, or embedded double-quotes.
    /// Simple literal strings with no special chars use plain "..." form.
    /// </summary>
    private static string ToOb2String(string s)
    {
        if (s == null) return "\"\"";
        // Map SilverBullet wordlist vars to OB2 input refs inside interpolated strings
        s = s.Replace("<USER>", "<input.USER>")
             .Replace("<PASS>", "<input.PASS>")
             .Replace("<USERNAME>", "<input.USER>")
             .Replace("<PASSWORD>", "<input.PASS>");
        // Match <VARNAME>, <@VARNAME>, and dotted refs like <input.USER>
        bool hasVar = Regex.IsMatch(s, @"<@?[A-Za-z0-9_ .]+>");
        string escaped = EscapeOb2(s);
        // OB2's plain "..." strings don't support escape sequences — \" is treated as end-of-string.
        // Use $"..." whenever the value contains \ or " or control chars so that OB2 can parse it correctly.
        bool needsInterpolated = hasVar || s.Contains('\\') || s.Contains('"') || s.Contains('\r') || s.Contains('\n') || s.Contains('\t');
        return needsInterpolated ? $"$\"{escaped}\"" : $"\"{escaped}\"";
    }

    /// <summary>Escapes a string for use inside OB2 $"..." or "..." literals.</summary>
    private static string EscapeOb2(string s)
    {
        if (s == null) return "";
        var escSb = new StringBuilder(s.Length + 4);
        foreach (char c in s)
        {
            switch (c)
            {
                case '\\': escSb.Append("\\\\"); break;
                case '"':  escSb.Append("\\\""); break;
                case '\r': escSb.Append("\\r");  break;
                case '\n': escSb.Append("\\n");  break;
                case '\t': escSb.Append("\\t");  break;
                default:   escSb.Append(c);      break;
            }
        }
        return escSb.ToString();
    }

    /// <summary>
    /// Strips OB2 $"..." or plain "..." markers and unescapes the inner value.
    /// Returns the raw string suitable for setting a block property.
    /// </summary>
    public static string FromOb2String(string value)
    {
        if (value == null) return "";
        value = value.Trim();

        if (value.StartsWith("$\"") && value.EndsWith("\"") && value.Length >= 3)
        {
            string inner = value.Substring(2, value.Length - 3);
            return UnescapeOb2(inner);
        }
        if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
        {
            return UnescapeOb2(value.Substring(1, value.Length - 2));
        }
        return value; // plain word (enum, bool, number)
    }

    /// <summary>Parses an OB2 ${("k","v"),...} dictionary literal into a C# Dictionary.</summary>
    public static Dictionary<string, string> ParseOb2Dict(string value)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(value)) return dict;

        string inner = value.Trim();
        if (inner.StartsWith("${")) inner = inner.Substring(2, inner.Length - 3);
        else if (inner.StartsWith("{")) inner = inner.Substring(1, inner.Length - 2);

        foreach (Match m in Regex.Matches(inner, @"\(\s*""((?:[^""\\]|\\.)*)""\s*,\s*""((?:[^""\\]|\\.)*)""\s*\)"))
        {
            string k = UnescapeOb2(m.Groups[1].Value);
            string v = UnescapeOb2(m.Groups[2].Value);
            dict[k] = v;
        }
        return dict;
    }

    /// <summary>Unescapes OB2 backslash-escaped chars inside a matched string group.</summary>
    public static string UnescapeOb2(string s)
    {
        if (s == null) return "";
        var unescSb = new StringBuilder(s.Length);
        for (int ui = 0; ui < s.Length; ui++)
        {
            if (s[ui] == '\\' && ui + 1 < s.Length)
            {
                switch (s[ui + 1])
                {
                    case '"':  unescSb.Append('"');  ui++; break;
                    case '\\': unescSb.Append('\\'); ui++; break;
                    case 'r':  unescSb.Append('\r'); ui++; break;
                    case 'n':  unescSb.Append('\n'); ui++; break;
                    case 't':  unescSb.Append('\t'); ui++; break;
                    default:   unescSb.Append(s[ui]); break;
                }
            }
            else
            {
                unescSb.Append(s[ui]);
            }
        }
        return unescSb.ToString();
    }
}
