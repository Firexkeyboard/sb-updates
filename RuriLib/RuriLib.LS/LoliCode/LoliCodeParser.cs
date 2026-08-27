using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RuriLib.LS.LoliCode;

/// <summary>
/// Parses a LoliCode script into a flat list of segments (Block or Code).
/// LoliCode format:
///   BLOCK:TypeName
///     LABEL:label
///     key = value
///     KEYCHAIN SUCCESS OR         ← KeyCheck-style nested structure
///       STRINGKEY @data.SOURCE Contains "text"
///       REGEXKEY  @data.SOURCE MatchesRegex "pattern"
///     KEYCHAIN FAIL OR
///       ...
///     => VAR @outputVar
///   ENDBLOCK
///   ... raw C# code ...
/// </summary>
public static class LoliCodeParser
{
    /// <summary>Returns true if the text looks like a LoliCode script (not LoliScript).</summary>
    public static bool IsLoliCode(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        // Pre-pass: BLOCK: is exclusive to LoliCode — scan the whole script before evaluating
        // other keywords. This handles serialized LoliCode that starts with LoliScript-style
        // keywords like SET USEPROXY, which would otherwise cause an early false return.
        {
            bool _inSb = false;
            foreach (string _ln in text.Split('\n'))
            {
                string _t = _ln.TrimStart();
                if (_inSb)
                {
                    if (_t.StartsWith("ENDIRONPYTHON", StringComparison.OrdinalIgnoreCase) ||
                        _t.StartsWith("ENDPYTHON",     StringComparison.OrdinalIgnoreCase) ||
                        _t.StartsWith("END SCRIPT",    StringComparison.OrdinalIgnoreCase))
                        _inSb = false;
                    continue;
                }
                if (_t.StartsWith("IRONPYTHON", StringComparison.Ordinal) ||
                    _t.StartsWith("PYTHON",     StringComparison.Ordinal) ||
                    _t.StartsWith("BEGIN SCRIPT", StringComparison.Ordinal))
                { _inSb = true; continue; }
                if (_t.StartsWith("BLOCK:")) return true;
            }
        }

        bool inScriptBlock = false;
        foreach (string line in text.Split('\n'))
        {
            string t = line.TrimStart();
            if (t.StartsWith("//") || t.StartsWith("/*")) continue;
            if (string.IsNullOrWhiteSpace(t)) continue;

            // Skip lines inside IRONPYTHON/PYTHON/BEGIN SCRIPT blocks
            if (inScriptBlock)
            {
                if (t.StartsWith("ENDIRONPYTHON", StringComparison.OrdinalIgnoreCase) ||
                    t.StartsWith("ENDPYTHON",     StringComparison.OrdinalIgnoreCase) ||
                    t.StartsWith("END SCRIPT",    StringComparison.OrdinalIgnoreCase))
                    inScriptBlock = false;
                continue;
            }

            if (t.StartsWith("BLOCK:")) return true;

            // IRONPYTHON/PYTHON appear in both LoliScript and LoliCode.
            // Don't return false here — skip the body and keep scanning for BLOCK:.
            if (t.StartsWith("IRONPYTHON", StringComparison.Ordinal) ||
                t.StartsWith("PYTHON",     StringComparison.Ordinal))
            { inScriptBlock = true; continue; }

            // BEGIN SCRIPT is LoliScript-style but may precede BLOCK: sections in a mixed config.
            if (t.StartsWith("BEGIN SCRIPT", StringComparison.Ordinal))
            { inScriptBlock = true; continue; }

            if (IsLoliScriptKeyword(t)) return false;
            // LoliScript "#label KEYWORD params" format: strip "#label " prefix and re-check.
            // Example: "#email FUNCTION Constant ..." → keyword is "FUNCTION ..." → LoliScript.
            // Do NOT classify as LoliScript if the # isn't followed by a LoliScript keyword,
            // because C# preprocessor directives (#nullable, #region) also start with #.
            if (t.StartsWith("#"))
            {
                // Labels can be multi-word ("DNS Lookup DNS ..."), so scan every word
                // position after # until we find a LoliScript keyword.
                int sp = t.IndexOf(' ');
                while (sp >= 0 && sp < t.Length - 1)
                {
                    string rest = t.Substring(sp + 1).TrimStart();
                    if (IsLoliScriptKeyword(rest))
                        return false;
                    sp = t.IndexOf(' ', sp + 1);
                }
            }
            // Disabled LoliScript block: "!REQUEST ...", "!PARSE ...", "!DNS ...", etc.
            if (t.StartsWith("!") && t.Length > 1)
            {
                string afterBang = t.Substring(1).TrimStart();
                if (IsLoliScriptKeyword(afterBang)) return false;
                // Disabled labeled block: "!#label REQUEST ...", etc.
                if (afterBang.StartsWith("#"))
                {
                    int sp = afterBang.IndexOf(' ');
                    while (sp >= 0 && sp < afterBang.Length - 1)
                    {
                        string rest = afterBang.Substring(sp + 1).TrimStart();
                        if (IsLoliScriptKeyword(rest)) return false;
                        sp = afterBang.IndexOf(' ', sp + 1);
                    }
                }
                // Disabled block continuation line ("!  CONTENT ...", "!  HEADER ...") —
                // not a LoliScript keyword on its own, but not LoliCode either. Keep scanning.
                continue;
            }
            // This line is not a LoliScript keyword, a comment, whitespace, or BLOCK: marker.
            // It could be a C# line (e.g. "{", "string proxy = ...") that appears in both
            // LoliCode AND LoliScript inline-C# blocks. Don't return true here — keep scanning.
            // Only BLOCK: markers are definitive LoliCode indicators; return false by default.
            continue;
        }
        return false;
    }

    private static readonly string[] LoliScriptKeywords = {
        "REQUEST ", "PARSE ", "FUNCTION ", "KEYCHECK", "UTILITY ",
        "DNS ", "SET ",
        "BEGIN SCRIPT", "IF ", "ELSE", "END IF", "WHILE ", "END WHILE",
        "ENDWHILE", "ENDIF", "END FOREACH", "ENDFOREACH",
        "JUMP ", "LABEL:", "FOREACH ", "TRY", "CATCH",
        "PYTHON", "ENDPYTHON", "IRONPYTHON", "ENDIRONPYTHON",
        "BROWSERACTION ", "CAPTCHA ", "OCRTEXTOCR", "TCP ", "WEBSOCKET ",
        "NAVIGATE ", "ELEMENTACTION ", "EXECUTEJS ",
        // Custom solver blocks — must be listed here so IsLoliCode() returns false for them
        "BYPASSCF ", "CF-CLEARANCE ", "TURNSTILE ", "RECAPTCHA-V3 ", "RECAPTCHA-V2-INVISIBLE ", "RECAPTCHA ",
        "AKM-COOKIES ", "DATADOME ", "ALTCHA ", "FRIENDLYCAPTCHA ", "RECAPTCHAV3-BYPASS ",
        "SOLVECAPTCHA ", "REPORTCAPTCHA ", "WS ", "OCR "
    };

    private static bool IsLoliScriptKeyword(string line)
    {
        // Case-SENSITIVE: LoliScript keywords are always ALL-CAPS (IF, WHILE, TRY, etc.)
        // whereas C# keywords are lowercase (if, while, try, etc.).
        // Using OrdinalIgnoreCase would wrongly classify C# `if (...)` as LoliScript IF.
        foreach (string kw in LoliScriptKeywords)
            if (line.StartsWith(kw, StringComparison.Ordinal)) return true;
        return false;
    }

    public static List<LoliCodeSegment> Parse(string lolicode)
    {
        var segments = new List<LoliCodeSegment>();
        // Normalize line endings so \r\n is treated as one separator, not two.
        // Split(char[]) on \r\n produces an extra empty entry per line on Windows.
        lolicode = lolicode.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines    = lolicode.Split('\n');
        int i        = 0;
        var codeBuf  = new List<string>();

        void FlushCode()
        {
            if (codeBuf.Any(l => !string.IsNullOrWhiteSpace(l)))
                segments.Add(new LoliCodeSegment { Type = LoliCodeSegmentType.Code,
                                                   Code = string.Join("\n", codeBuf) });
            codeBuf.Clear();
        }

        while (i < lines.Length)
        {
            string trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith("BLOCK:"))
            {
                FlushCode();
                string blockTypePart = trimmed.Substring("BLOCK:".Length).Trim().Trim('!').Trim();
                // Match "Script" alone OR "Script INTERPRETER:..." inline OB2 form
                bool isScriptBlock = blockTypePart.Equals("Script", StringComparison.OrdinalIgnoreCase)
                    || blockTypePart.StartsWith("Script ", StringComparison.OrdinalIgnoreCase)
                    || blockTypePart.StartsWith("Script\t", StringComparison.OrdinalIgnoreCase);
                if (isScriptBlock)
                    segments.Add(ParseScriptBlock(lines, ref i));
                else
                    segments.Add(ParseBlock(lines, ref i));
            }
            else if (trimmed.StartsWith("IF ", StringComparison.Ordinal))
            {
                string ifRest = trimmed.Substring(3).Trim();
                // OB2 inline key-check: IF INTKEY/STRINGKEY/FLOATKEY/BOOLKEY @var Comparer val
                if (Regex.IsMatch(ifRest, @"^(INT|STRING|FLOAT|BOOL|LIST|DICT)KEY\s+", RegexOptions.IgnoreCase))
                    codeBuf.Add(CompileInlineKeyCheck(ifRest));
                else
                    codeBuf.Add(CompileIfLine(ifRest));
                i++;
            }
            // OB2-style: if (@var Comparer "right") {
            // Only trigger when the condition starts with @variable — NOT for arbitrary C# if(...)
            else if (Regex.IsMatch(trimmed, @"^if\s*\(\s*@[\w.]+\s+\w", RegexOptions.IgnoreCase))
            {
                codeBuf.Add(CompileOb2CondLine(trimmed, "if"));
                i++;
            }
            else if (trimmed.StartsWith("ELSE IF ", StringComparison.Ordinal))
            {
                // OB2: ELSE IF STRINGKEY @var Comparer "val"  /  ELSE IF "left" Comparer "right"
                string elseIfRest = trimmed.Substring("ELSE IF ".Length).Trim();
                string elseIfLine;
                if (Regex.IsMatch(elseIfRest, @"^(INT|STRING|FLOAT|BOOL|LIST|DICT)KEY\s+", RegexOptions.IgnoreCase))
                    elseIfLine = "} else " + CompileInlineKeyCheck(elseIfRest);
                else
                    elseIfLine = "} else if (" + CompileLsCondition(elseIfRest) + ") {";
                codeBuf.Add(elseIfLine);
                i++;
            }
            else if (trimmed.Equals("ELSE", StringComparison.Ordinal))
            {
                codeBuf.Add("} else {");
                i++;
            }
            else if (trimmed.Equals("ENDIF", StringComparison.Ordinal) || trimmed.Equals("END IF", StringComparison.Ordinal))
            {
                codeBuf.Add("}");
                i++;
            }
            else if (trimmed.StartsWith("WHILE ", StringComparison.Ordinal))
            {
                codeBuf.Add(CompileWhileLine(trimmed.Substring(6).Trim()));
                i++;
            }
            // OB2-style: while (@var Comparer "right") {
            else if (Regex.IsMatch(trimmed, @"^while\s*\(\s*@[\w.]+\s+\w", RegexOptions.IgnoreCase))
            {
                codeBuf.Add(CompileOb2CondLine(trimmed, "while"));
                i++;
            }
            else if (trimmed.Equals("ENDWHILE", StringComparison.Ordinal) || trimmed.Equals("END WHILE", StringComparison.Ordinal))
            {
                codeBuf.Add("}");
                i++;
            }
            else if (trimmed.Equals("}", StringComparison.Ordinal))
            {
                codeBuf.Add("}");
                i++;
            }
            else if (trimmed.Equals("TRY", StringComparison.Ordinal))
            {
                codeBuf.Add("try {");
                i++;
            }
            else if (trimmed.Equals("CATCH", StringComparison.Ordinal) ||
                     trimmed.StartsWith("CATCH ", StringComparison.Ordinal) ||
                     trimmed.StartsWith("CATCH(", StringComparison.Ordinal))
            {
                codeBuf.Add("} catch (Exception ex) { data.Variables.Set(new CVar(\"ERROR\", ex.Message));");
                i++;
            }
            else if (trimmed.Equals("ENDTRY", StringComparison.Ordinal))
            {
                codeBuf.Add("}");
                i++;
            }
            // OB2 standalone END — closes an IF block (same as ENDIF)
            else if (trimmed.Equals("END", StringComparison.Ordinal))
            {
                codeBuf.Add("}");
                i++;
            }
            // OB2 JUMP #label — goto
            // OB2 JUMP — goto label (supports both #label and LABEL:name formats)
            else if (trimmed.StartsWith("JUMP ", StringComparison.OrdinalIgnoreCase))
            {
                string jumpTarget = trimmed.Substring(5).Trim();
                if (jumpTarget.StartsWith("#")) jumpTarget = jumpTarget.Substring(1);
                else if (jumpTarget.StartsWith("LABEL:", StringComparison.OrdinalIgnoreCase))
                    jumpTarget = jumpTarget.Substring("LABEL:".Length).Trim();
                string safeLabel = Regex.Replace(jumpTarget, @"[^\w]", "_");
                codeBuf.Add($"goto __lbl_{safeLabel};");
                i++;
            }
            // LOG @varName / LOG expression — write value to the bot log.
            // Converts to LOG(expr); where LOG is defined in the compiler preamble as Action<object>.
            // Using LOG(expr) instead of data.Log(expr.ToString()) works for both value types
            // (long, double) and reference types (string) without null-conditional issues.
            else if (trimmed.StartsWith("LOG ", StringComparison.OrdinalIgnoreCase)
                     || trimmed.Equals("LOG", StringComparison.OrdinalIgnoreCase))
            {
                string logExpr = trimmed.Length > 4 ? trimmed.Substring(4).Trim() : "\"\"";
                codeBuf.Add($"LOG({logExpr});");
                i++;
            }
            // PRINT @varName / PRINT expression — alias for LOG
            else if (trimmed.StartsWith("PRINT ", StringComparison.OrdinalIgnoreCase)
                     || trimmed.Equals("PRINT", StringComparison.OrdinalIgnoreCase))
            {
                string printExpr = trimmed.Length > 6 ? trimmed.Substring(6).Trim() : "\"\"";
                codeBuf.Add($"LOG({printExpr});");
                i++;
            }
            // OB2 MARK @varName — mark a variable as a capture.
            // Emit @varRef as a placeholder so the compiler's ResolveAtVarRefs can resolve it:
            //   - if varRef is a known C# local (declared in preceding code), it stays as a bare identifier
            //   - otherwise it becomes data.GetVar("varRef") (CVar from a Parse/Function block)
            // This handles both cases correctly without hardcoding one path here.
            else if (trimmed.StartsWith("MARK ", StringComparison.OrdinalIgnoreCase))
            {
                string varRef = trimmed.Substring(5).Trim().TrimStart('@');
                codeBuf.Add($"RuriLib.LS.LoliCode.Ob2Compat.MarkVar(data, \"{varRef}\", @{varRef});");
                i++;
            }
            // OB2 SET USEPROXY TRUE/FALSE — convert to direct assignment
            else if (trimmed.StartsWith("SET USEPROXY ", StringComparison.OrdinalIgnoreCase))
            {
                string boolVal = trimmed.Substring(13).Trim();
                codeBuf.Add(boolVal.Equals("TRUE", StringComparison.OrdinalIgnoreCase)
                    ? "data.UseProxy = true;"
                    : "data.UseProxy = false;");
                i++;
            }
            // SET STATUS <value> — LoliScript command used inside inline C# blocks.
            // Translates to a valid data.STATUS assignment so Roslyn can compile the block.
            else if (trimmed.StartsWith("SET STATUS ", StringComparison.OrdinalIgnoreCase))
            {
                string status = trimmed.Substring("SET STATUS ".Length).Trim().Trim('"');
                codeBuf.Add($"data.STATUS = \"{status}\";");
                i++;
            }
            // OB2 CLOG <Color> <message> — colored log entry.
            // Compiles to: data.Log(<message>, System.Windows.Media.Colors.<Color>);
            else if (trimmed.StartsWith("CLOG ", StringComparison.OrdinalIgnoreCase))
            {
                string clogRest = trimmed.Substring(5).Trim();
                var clogMatch = Regex.Match(clogRest, @"^(\w+)\s+(.+)$");
                if (clogMatch.Success)
                {
                    string color = clogMatch.Groups[1].Value;
                    string msg   = clogMatch.Groups[2].Value;
                    codeBuf.Add($"data.Log({msg}, System.Windows.Media.Colors.{color});");
                }
                else
                {
                    // No color specified — log with default white
                    codeBuf.Add($"data.Log({clogRest});");
                }
                i++;
            }
            else if (trimmed.Equals("PYTHON", StringComparison.Ordinal) || trimmed.Equals("IRONPYTHON", StringComparison.Ordinal))
            {
                FlushCode();
                bool isIronPy = trimmed.Equals("IRONPYTHON", StringComparison.Ordinal);
                string interpName = isIronPy ? "IronPython" : "Python";
                string endKw  = isIronPy ? "ENDIRONPYTHON" : "ENDPYTHON";
                i++;
                var pyLines = new List<string>();
                string outputs = "";
                while (i < lines.Length)
                {
                    string pt = lines[i].TrimStart();
                    if (pt.StartsWith(endKw, StringComparison.OrdinalIgnoreCase))
                    {
                        outputs = pt.Substring(endKw.Length).Trim();
                        i++;
                        break;
                    }
                    pyLines.Add(lines[i]);
                    i++;
                }
                var csSb = new System.Text.StringBuilder();
                csSb.AppendLine("{");
                csSb.Append("    var __pyCode = string.Join(\"\\n\", new string[] {");
                foreach (string pl in pyLines)
                    csSb.Append(" " + EscapeAsLiteral(pl) + ",");
                csSb.AppendLine(" });");
                csSb.AppendLine($"    RuriLib.LS.LoliScript.RunInlineScript(__pyCode, \"\", {EscapeAsLiteral(outputs)}, {EscapeAsLiteral(interpName)}, data._inner);");
                csSb.AppendLine("}");
                segments.Add(new LoliCodeSegment {
                    Type             = LoliCodeSegmentType.Code,
                    Code             = csSb.ToString(),
                    PythonLines      = pyLines,
                    PythonOutputs    = outputs,
                    IsIronPython     = isIronPy,
                    ScriptInterpreter = interpName
                });
            }
            else if (trimmed.StartsWith("BEGIN SCRIPT", StringComparison.OrdinalIgnoreCase))
            {
                // LoliScript-style BEGIN SCRIPT Language ... END SCRIPT -> VARS "outputs" block.
                // Handles configs that still contain this format inside a LoliCode context.
                FlushCode();
                string scriptLang = "IronPython";
                string afterKw = trimmed.Substring("BEGIN SCRIPT".Length).Trim();
                if (!string.IsNullOrEmpty(afterKw)) scriptLang = afterKw;
                bool isIronPy = scriptLang.Equals("IronPython", StringComparison.OrdinalIgnoreCase);
                i++;
                var pyLines = new List<string>();
                string outputs = "";
                while (i < lines.Length)
                {
                    string pt = lines[i].TrimStart();
                    if (pt.StartsWith("END SCRIPT", StringComparison.OrdinalIgnoreCase))
                    {
                        var m = Regex.Match(pt,
                            @"END\s+SCRIPT\s*->\s*VARS\s+""([^""]*)""\s*$",
                            RegexOptions.IgnoreCase);
                        if (m.Success) outputs = m.Groups[1].Value;
                        i++;
                        break;
                    }
                    pyLines.Add(lines[i]);
                    i++;
                }
                var bsSb = new System.Text.StringBuilder();
                bsSb.AppendLine("{");
                bsSb.Append("    var __pyCode = string.Join(\"\\n\", new string[] {");
                foreach (string pl in pyLines)
                    bsSb.Append(" " + EscapeAsLiteral(pl) + ",");
                bsSb.AppendLine(" });");
                bsSb.AppendLine($"    RuriLib.LS.LoliScript.RunInlineScript(__pyCode, \"\", {EscapeAsLiteral(outputs)}, {EscapeAsLiteral(scriptLang)}, data._inner);");
                bsSb.AppendLine("}");
                segments.Add(new LoliCodeSegment {
                    Type             = LoliCodeSegmentType.Code,
                    Code             = bsSb.ToString(),
                    PythonLines      = pyLines,
                    PythonOutputs    = outputs,
                    IsIronPython     = isIronPy,
                    ScriptInterpreter = scriptLang
                });
            }
            // OB2 standalone goto label: LABEL:identifier (between blocks, outside any BLOCK)
            else if (trimmed.StartsWith("LABEL:", StringComparison.OrdinalIgnoreCase))
            {
                string lblName   = trimmed.Substring("LABEL:".Length).Trim();
                string safeLabel = Regex.Replace(lblName, @"[^\w]", "_");
                codeBuf.Add($"__lbl_{safeLabel}: ;");
                i++;
            }
            else
            {
                string raw = lines[i];
                string tr  = raw.TrimStart();

                // OB2 jump label: #identifier at the start of a line (not a C# preprocessor directive
                // that would be invalid in Roslyn scripts anyway). Must be a plain word with no spaces.
                if (Regex.IsMatch(tr, @"^#[A-Za-z_][A-Za-z0-9_]*\s*$"))
                {
                    string lblName    = tr.Trim().Substring(1);
                    string safeLabel  = Regex.Replace(lblName, @"[^\w]", "_");
                    // C# label requires at least one statement after it; the semicolon is an empty one.
                    codeBuf.Add($"__lbl_{safeLabel}: ;");
                    i++;
                    continue;
                }

                // Unwrap "if (true) { /* unparseable: X */ " markers left by an old parser bug.
                // Extract X and restore the original code so Roslyn can compile it.
                var um = Regex.Match(tr, @"^if \(true\) \{ /\* unparseable: (.*) \*/$");
                if (um.Success)
                {
                    string original = um.Groups[1].Value;
                    string pfx      = raw.Substring(0, raw.Length - tr.Length); // preserve indent
                    string trimOrig = original.TrimEnd();
                    if (trimOrig.EndsWith(";") || trimOrig.EndsWith("}") || trimOrig.EndsWith("{"))
                    {
                        // Complete statement or already has opening brace — emit verbatim
                        codeBuf.Add(pfx + original);
                    }
                    else
                    {
                        // Condition ending with ) — check whether next non-blank line opens {
                        // If yes: the { on the next line IS the body opener; don't add another
                        // If no:  add { so a following } else { / body can close it properly
                        int ni = i + 1;
                        while (ni < lines.Length && string.IsNullOrWhiteSpace(lines[ni])) ni++;
                        bool nextIsBrace = ni < lines.Length && lines[ni].TrimStart().StartsWith("{");
                        codeBuf.Add(nextIsBrace ? pfx + original : pfx + original + " {");
                    }
                }
                else
                {
                    codeBuf.Add(raw);
                }
                i++;
            }
        }

        FlushCode();
        return segments;
    }

    private static string CompileIfLine(string condStr)
        => $"if ({CompileLsCondition(condStr)}) {{";

    private static string CompileWhileLine(string condStr)
        => $"while ({CompileLsCondition(condStr)}) {{";

    private static string CompileLsCondition(string condStr)
    {
        var m = Regex.Match(condStr, @"^""((?:[^""\\]|\\.)*)""\s+(\w+)(?:\s+""((?:[^""\\]|\\.)*)"")?$");
        if (!m.Success) return $"true /* unparseable */";
        string left       = m.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\");
        string comparerStr = m.Groups[2].Value;
        string right      = m.Groups[3].Success ? m.Groups[3].Value.Replace("\\\"", "\"").Replace("\\\\", "\\") : "";
        string csLeft  = left.Replace("\\", "\\\\").Replace("\"", "\\\"");
        string csRight = right.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"RuriLib.Functions.Conditions.Condition.ReplaceAndVerify(\"{csLeft}\", " +
               $"RuriLib.Functions.Conditions.Comparer.{comparerStr}, " +
               $"\"{csRight}\", data._inner)";
    }

    // Compiles an OB2-style condition line to C# ReplaceAndVerify call.
    // Handles: if (@varRef Comparer "right") {  and  while (@varRef Comparer "right") {
    private static string CompileOb2CondLine(string trimmed, string keyword)
    {
        var m = Regex.Match(trimmed,
            @"^(?:if|while)\s*\((@[\w.]+|""(?:[^""\\]|\\.)*"")\s+(\w+)(?:\s+\$?""((?:[^""\\]|\\.)*)"")?",
            RegexOptions.IgnoreCase);
        if (!m.Success)
            return trimmed; // not OB2 format — pass through as-is

        string leftRef   = m.Groups[1].Value;
        string comparer  = m.Groups[2].Value;
        string right     = m.Groups[3].Success ? m.Groups[3].Value.Replace("\\\"", "\"").Replace("\\\\", "\\") : "";

        // Convert @ref → SilverBullet <ref>
        string sbLeft;
        if (leftRef.StartsWith("@"))
        {
            string name = leftRef.Substring(1); // strip leading @
            if (name.Equals("input.USERNAME", StringComparison.OrdinalIgnoreCase)) sbLeft = "<USER>";
            else if (name.Equals("input.PASSWORD", StringComparison.OrdinalIgnoreCase)) sbLeft = "<PASS>";
            else if (name.StartsWith("input.", StringComparison.OrdinalIgnoreCase)) sbLeft = "<" + name.Substring(6).ToUpperInvariant() + ">";
            else if (name.StartsWith("data.", StringComparison.OrdinalIgnoreCase))  sbLeft = "<" + name.Substring(5).ToUpperInvariant() + ">";
            else sbLeft = "<" + name + ">";
        }
        else // quoted literal
        {
            sbLeft = leftRef.Substring(1, leftRef.Length - 2).Replace("\\\"", "\"").Replace("\\\\", "\\");
        }

        string csLeft  = sbLeft.Replace("\\", "\\\\").Replace("\"", "\\\"");
        string csRight = right.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"{keyword} (RuriLib.Functions.Conditions.Condition.ReplaceAndVerify(\"{csLeft}\", " +
               $"RuriLib.Functions.Conditions.Comparer.{comparer}, " +
               $"\"{csRight}\", data._inner)) {{";
    }

    // Returns just the boolean expression for a single XKEY condition (no "if (...) {" wrapper).
    // Used by CompileInlineKeyCheck for multi-condition support.
    private static string CompileSingleKeyCheckCondition(string rest)
    {
        var m = Regex.Match(rest,
            @"^(?:INT|STRING|FLOAT|BOOL|LIST|DICT)KEY\s+(@[\w.\[\]""]+)\s+(\w+)(?:\s+(""(?:[^""\\]|\\.)*""|[^\s]+))?",
            RegexOptions.IgnoreCase);
        if (!m.Success) return $"true /* unparseable KEY: {rest} */";

        string leftRef  = m.Groups[1].Value;
        string comparer = m.Groups[2].Value;
        string rightRaw = m.Groups[3].Success ? m.Groups[3].Value.Trim() : "";

        string sbLeft;
        if (leftRef.StartsWith("@data.",  StringComparison.OrdinalIgnoreCase))
            sbLeft = "<" + leftRef.Substring(6).ToUpperInvariant() + ">";
        else if (leftRef.StartsWith("@input.", StringComparison.OrdinalIgnoreCase))
            sbLeft = "<" + leftRef.Substring(7).ToUpperInvariant() + ">";
        else if (leftRef.StartsWith("@"))
            sbLeft = "<" + leftRef.Substring(1) + ">";
        else
            sbLeft = leftRef;

        string rightTerm = rightRaw;
        if (rightTerm.StartsWith("$\"", StringComparison.Ordinal)) rightTerm = rightTerm.Substring(1);
        if (rightTerm.StartsWith("\"") && rightTerm.EndsWith("\"") && rightTerm.Length >= 2)
            rightTerm = rightTerm.Substring(1, rightTerm.Length - 2)
                                 .Replace("\\\"", "\"").Replace("\\\\", "\\");

        string csLeft  = sbLeft.Replace("\\", "\\\\").Replace("\"", "\\\"");
        string csRight = rightTerm.Replace("\\", "\\\\").Replace("\"", "\\\"");

        return $"RuriLib.Functions.Conditions.Condition.ReplaceAndVerify(\"{csLeft}\", " +
               $"RuriLib.Functions.Conditions.Comparer.{comparer}, " +
               $"\"{csRight}\", data._inner)";
    }

    // Compiles OB2 inline key-check in an IF line, e.g.:
    //   IF INTKEY @data.RESPONSECODE NotEqualTo 200
    //   IF STRINGKEY @data.SOURCE Contains "text"
    //   IF STRINGKEY @data.SOURCE Contains "x" || IF STRINGKEY @data.SOURCE Contains "y"
    private static string CompileInlineKeyCheck(string rest)
    {
        // OB2 multi-condition: split on " || IF " / " && IF " separators.
        // Regex.Split with a capturing group keeps the operators in the result array:
        //   ["STRINGKEY x", "||", "STRINGKEY y", "&&", "STRINGKEY z"]
        var parts = Regex.Split(rest, @"\s*(\|\||&&)\s+IF\s+");

        if (parts.Length > 1)
        {
            // Multi-condition path
            var sb = new System.Text.StringBuilder("if (");
            for (int pi = 0; pi < parts.Length; pi++)
            {
                if (pi % 2 == 1)
                    sb.Append(parts[pi].Trim() == "||" ? " || " : " && ");
                else
                    sb.Append(CompileSingleKeyCheckCondition(parts[pi].Trim()));
            }
            sb.Append(") {");
            return sb.ToString();
        }

        // Single condition — original path
        var m = Regex.Match(rest,
            @"^(?:INT|STRING|FLOAT|BOOL|LIST|DICT)KEY\s+(@[\w.\[\]""]+)\s+(\w+)\s*(""(?:[^""\\]|\\.)*""|[^\s].*)?\s*$",
            RegexOptions.IgnoreCase);
        if (!m.Success) return $"if (true) /* unparseable IF KEY: {rest} */ {{";

        string leftRef  = m.Groups[1].Value;
        string comparer = m.Groups[2].Value;
        string rightRaw = m.Groups[3].Success ? m.Groups[3].Value.Trim() : "";

        string sbLeft;
        if (leftRef.StartsWith("@data.",  StringComparison.OrdinalIgnoreCase))
            sbLeft = "<" + leftRef.Substring(6).ToUpperInvariant() + ">";
        else if (leftRef.StartsWith("@input.", StringComparison.OrdinalIgnoreCase))
            sbLeft = "<" + leftRef.Substring(7).ToUpperInvariant() + ">";
        else if (leftRef.StartsWith("@"))
            sbLeft = "<" + leftRef.Substring(1) + ">";
        else
            sbLeft = leftRef;

        string rightTerm = rightRaw;
        if (rightTerm.StartsWith("$\"", StringComparison.Ordinal)) rightTerm = rightTerm.Substring(1);
        if (rightTerm.StartsWith("\"") && rightTerm.EndsWith("\"") && rightTerm.Length >= 2)
            rightTerm = rightTerm.Substring(1, rightTerm.Length - 2)
                                 .Replace("\\\"", "\"").Replace("\\\\", "\\");

        string csLeft  = sbLeft.Replace("\\", "\\\\").Replace("\"", "\\\"");
        string csRight = rightTerm.Replace("\\", "\\\\").Replace("\"", "\\\"");

        return $"if (RuriLib.Functions.Conditions.Condition.ReplaceAndVerify(\"{csLeft}\", " +
               $"RuriLib.Functions.Conditions.Comparer.{comparer}, " +
               $"\"{csRight}\", data._inner)) {{";
    }

    // ─── OB2 BLOCK:Script parser ─────────────────────────────────────────────
    // Handles the OB2-standard script block format:
    //   BLOCK:Script
    //   INTERPRETER:IronPython
    //   INPUT x,y
    //   BEGIN SCRIPT
    //   ... script lines ...
    //   END SCRIPT
    //   OUTPUT Int @result
    //   ENDBLOCK
    private static LoliCodeSegment ParseScriptBlock(string[] lines, ref int i)
    {
        // Parse INTERPRETER: and INPUT that OB2 puts inline on the BLOCK:Script header line
        // e.g.  BLOCK:Script INTERPRETER:NodeJS INPUT @input.USER,@input.PASS
        string headerLine = lines[i].Trim();
        string headerRest = headerLine.Length > "BLOCK:Script".Length
            ? headerLine.Substring("BLOCK:Script".Length).Trim('!').Trim()
            : "";

        string interpreter = "IronPython";
        string inputsLine  = "";

        if (!string.IsNullOrEmpty(headerRest))
        {
            var interpM = Regex.Match(headerRest, @"(?i)INTERPRETER:(\S+)");
            if (interpM.Success) interpreter = interpM.Groups[1].Value.Trim();

            var inputM = Regex.Match(headerRest, @"(?i)INPUT\s+(.+?)(?:\s+INTERPRETER:|\s*$)");
            if (inputM.Success) inputsLine = inputM.Groups[1].Value.Trim();
        }

        i++; // consume BLOCK:Script line
        var scriptLines    = new List<string>();
        var outputVars     = new List<string>();
        bool inScript      = false;

        while (i < lines.Length)
        {
            string raw     = lines[i];
            string trimmed = raw.Trim();

            if (trimmed.Equals("ENDBLOCK", StringComparison.OrdinalIgnoreCase))
            {
                i++;
                break;
            }
            if (trimmed.StartsWith("INTERPRETER:", StringComparison.OrdinalIgnoreCase))
                interpreter = trimmed.Substring("INTERPRETER:".Length).Trim();
            else if (trimmed.StartsWith("INPUT ", StringComparison.OrdinalIgnoreCase))
                inputsLine = trimmed.Substring("INPUT ".Length).Trim();
            else if (trimmed.Equals("BEGIN SCRIPT", StringComparison.OrdinalIgnoreCase))
                inScript = true;
            else if (trimmed.Equals("END SCRIPT", StringComparison.OrdinalIgnoreCase))
                inScript = false;
            else if (trimmed.StartsWith("OUTPUT ", StringComparison.OrdinalIgnoreCase))
            {
                // Format: OUTPUT Type @varName   (e.g. "OUTPUT Int @result" → "result")
                string rest = trimmed.Substring("OUTPUT ".Length).Trim();
                int sp = rest.LastIndexOf(' ');
                string varName = sp >= 0 ? rest.Substring(sp + 1).TrimStart('@') : rest.TrimStart('@');
                if (!string.IsNullOrEmpty(varName))
                    outputVars.Add(varName);
            }
            else if (inScript)
                scriptLines.Add(raw);

            i++;
        }

        string outputs = string.Join(",", outputVars);
        bool isIronPy = interpreter.Equals("IronPython", StringComparison.OrdinalIgnoreCase);


        // CSharp blocks: inject code directly into the Roslyn script so variables
        // declared inside (ke, requestString, etc.) are C# locals in the outer scope.
        if (interpreter.Equals("CSharp", StringComparison.OrdinalIgnoreCase) ||
            interpreter.Equals("C#",     StringComparison.OrdinalIgnoreCase))
        {
            var __csSb = new System.Text.StringBuilder();
            __csSb.AppendLine(string.Join("\n", scriptLines));
            // Export OUTPUT variables as CVars so subsequent blocks can reference them via @varName
            foreach (string __v in outputVars)
                __csSb.AppendLine($"data.Variables.Set(new RuriLib.Models.CVar(\"{__v}\", {__v}?.ToString() ?? \"\"));");
            return new LoliCodeSegment {
                Type             = LoliCodeSegmentType.Code,
                Code             = __csSb.ToString(),
                ScriptInterpreter = interpreter,
            };
        }
        var csSb = new System.Text.StringBuilder();
        // Pre-declare output vars at outer scope so downstream blocks can reference them
        // as C# locals via <varName> template syntax (not just as CVars via __rv).
        foreach (string ov in outputVars)
            csSb.AppendLine($"string {ov} = \"\";");
        csSb.AppendLine("{");
        csSb.Append("    var __pyCode = string.Join(\"\\n\", new string[] {");
        foreach (string pl in scriptLines)
            csSb.Append(" " + EscapeAsLiteral(pl) + ",");
        csSb.AppendLine(" });");
        csSb.AppendLine($"    RuriLib.LS.LoliScript.RunInlineScript(__pyCode, {EscapeAsLiteral(inputsLine)}, {EscapeAsLiteral(outputs)}, {EscapeAsLiteral(interpreter)}, data._inner);");
        foreach (string ov in outputVars)
            csSb.AppendLine($"    {ov} = data.GetVar(\"{ov}\");");
        csSb.AppendLine("}");

        return new LoliCodeSegment {
            Type             = LoliCodeSegmentType.Code,
            Code             = csSb.ToString(),
            PythonLines      = scriptLines,
            PythonOutputs    = outputs,
            PythonInputs     = inputsLine,
            ScriptInterpreter = interpreter,
            IsIronPython     = isIronPy
        };
    }

    private static LoliCodeSegment ParseBlock(string[] lines, ref int i)
    {
        string header    = lines[i].Trim();
        string typeRaw   = header.Substring("BLOCK:".Length).Trim();
        bool   disabled  = typeRaw.StartsWith("!") || typeRaw.EndsWith("!");
        string blockType = typeRaw.Trim('!').Trim();

        string label     = "";
        string outputVar = null;
        bool   isCapture = false;
        var    props     = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var    keychains = new List<LoliCodeKeyChain>();
        LoliCodeKeyChain currentChain = null;

        // State for OB2 positional body syntax: TYPE:STANDARD / $"body" / "content-type"
        bool   seenTypeDecl       = false;
        bool   bodyParsed         = false;
        string declaredRequestType = null; // tracks which TYPE: was declared

        i++; // consume BLOCK: line

        while (i < lines.Length)
        {
            string line = lines[i].Trim();

            if (line.Equals("ENDBLOCK", StringComparison.OrdinalIgnoreCase))
            {
                i++;
                break;
            }

            if (line.StartsWith("LABEL:", StringComparison.OrdinalIgnoreCase))
            {
                label = line.Substring("LABEL:".Length).Trim();
            }
            else if (line.StartsWith("=> CAP ", StringComparison.OrdinalIgnoreCase))
            {
                isCapture = true;
                outputVar = line.Substring("=> CAP ".Length).Trim().TrimStart('@');
            }
            else if (line.StartsWith("=> VAR ", StringComparison.OrdinalIgnoreCase))
            {
                outputVar = line.Substring("=> VAR ".Length).Trim().TrimStart('@');
            }
            else if (line.StartsWith("=> LIST ", StringComparison.OrdinalIgnoreCase))
            {
                isCapture = false;
                outputVar = line.Substring("=> LIST ".Length).Trim().TrimStart('@');
            }
            else if (line.StartsWith("KEYCHAIN ", StringComparison.OrdinalIgnoreCase))
            {
                currentChain = ParseKeyChainHeader(line);
                if (currentChain != null) keychains.Add(currentChain);
            }
            else if (currentChain != null &&
                     (line.StartsWith("STRINGKEY ", StringComparison.OrdinalIgnoreCase) ||
                      line.StartsWith("REGEXKEY ",  StringComparison.OrdinalIgnoreCase) ||
                      line.StartsWith("FLOATKEY ",  StringComparison.OrdinalIgnoreCase) ||
                      line.StartsWith("INTKEY ",    StringComparison.OrdinalIgnoreCase) ||
                      line.StartsWith("BOOLKEY ",   StringComparison.OrdinalIgnoreCase) ||
                      line.StartsWith("LISTKEY ",   StringComparison.OrdinalIgnoreCase) ||
                      line.StartsWith("DICTKEY ",   StringComparison.OrdinalIgnoreCase) ||
                      line.StartsWith("KEY ",       StringComparison.OrdinalIgnoreCase)))
            {
                var key = ParseKeyLine(line);
                if (key != null) currentChain.Keys.Add(key);
            }
            else if (line.Equals("DISABLED", StringComparison.OrdinalIgnoreCase))
            {
                // OB2 uses a "DISABLED" line inside the block (vs our BLOCK:! prefix)
                disabled = true;
            }
            else if (line.Equals("ENABLED", StringComparison.OrdinalIgnoreCase))
            {
                disabled = false;
            }
            else if (line.Equals("SAFE", StringComparison.OrdinalIgnoreCase))
            {
                // OB2 "SAFE" flag: don't fail if parse returns no match — stored as property
                props["safe"] = "true";
            }
            else if (line.Equals("RECURSIVE", StringComparison.OrdinalIgnoreCase))
            {
                props["recursive"] = "true";
            }
            else if (line.StartsWith("DISABLED:", StringComparison.OrdinalIgnoreCase) && !line.Contains('='))
            {
                // Disabled flag handled by !-prefix on BLOCK header; no-op here for compatibility
            }
            else if (line.StartsWith("TYPE:", StringComparison.OrdinalIgnoreCase) && !line.Contains('='))
            {
                // OB2 positional body type: TYPE:STANDARD, TYPE:RAW, TYPE:MULTIPART, etc.
                props["requestType"]  = line.Substring(5).Trim();
                seenTypeDecl          = true;
                bodyParsed            = false;
                declaredRequestType   = line.Substring(5).Trim().ToUpperInvariant();
            }
            else if (line.StartsWith("MODE:", StringComparison.OrdinalIgnoreCase) && !line.Contains('='))
            {
                // Parse block mode: MODE:LR, MODE:REGEX, MODE:CSS, MODE:JSON
                props["type"] = line.Substring(5).Trim();
            }
            else if (seenTypeDecl && !bodyParsed)
            {
                // First positional line after TYPE:xxx — interpretation depends on the declared type.
                bool isQuoted = line.Length >= 2 && line.StartsWith("\"", StringComparison.Ordinal) && line.EndsWith("\"", StringComparison.Ordinal);
                bool isInterp = line.StartsWith("$\"", StringComparison.Ordinal);

                bool consumed = false;
                switch (declaredRequestType)
                {
                    case "BASICAUTH":
                        if (isQuoted) { props["authUser"] = line.Substring(1, line.Length - 2); bodyParsed = true; consumed = true; }
                        break;
                    case "RAW":
                        // Raw body may be unquoted (OB2 allows bare data on that line)
                        props["rawData"] = isQuoted ? line.Substring(1, line.Length - 2) : line;
                        bodyParsed = true; consumed = true;
                        break;
                    case "MULTIPART":
                        if (isQuoted) { props["multipartBoundary"] = line.Substring(1, line.Length - 2); bodyParsed = true; consumed = true; }
                        break;
                    default: // STANDARD or unknown
                        // Also accept bare @ref (OB2 variable reference without quotes) as the body.
                        if (isInterp || isQuoted || line.StartsWith("@", StringComparison.Ordinal))
                        { props["postData"] = line; bodyParsed = true; consumed = true; }
                        break;
                }
                // If the line wasn't a body line, treat it as a regular key=value property.
                // This handles configs where TYPE: appears before some key=value properties.
                if (!consumed && line.Contains(" = "))
                {
                    int _eq = line.IndexOf(" = ", StringComparison.Ordinal);
                    string _pk = line.Substring(0, _eq).Trim();
                    string _pv = line.Substring(_eq + 3).Trim();
                    if (!string.IsNullOrEmpty(_pk)) props[_pk] = _pv;
                }
            }
            else if (seenTypeDecl && bodyParsed)
            {
                // Second (or subsequent) positional lines after the first was consumed.
                bool isQuoted = line.Length >= 2 && line.StartsWith("\"", StringComparison.Ordinal) && line.EndsWith("\"", StringComparison.Ordinal);

                bool consumed = false;
                switch (declaredRequestType)
                {
                    case "BASICAUTH":
                        if (isQuoted && !props.ContainsKey("authPass"))
                            { props["authPass"] = line.Substring(1, line.Length - 2); consumed = true; }
                        break;
                    case "RAW":
                        if (isQuoted && !props.ContainsKey("contentType"))
                            { props["contentType"] = line.Substring(1, line.Length - 2); consumed = true; }
                        break;
                    case "MULTIPART":
                        if (line.StartsWith("CONTENT:", StringComparison.OrdinalIgnoreCase))
                        {
                            if (props.TryGetValue("multipartPart", out string existMp) && existMp.Length > 0)
                                props["multipartPart"] = existMp + "\x1E" + line;
                            else
                                props["multipartPart"] = line;
                            consumed = true;
                        }
                        break;
                    default: // STANDARD or unknown
                        if (isQuoted && !props.ContainsKey("contentType"))
                            { props["contentType"] = line.Substring(1, line.Length - 2); consumed = true; }
                        break;
                }
                // If the line wasn't a content-type/auth line, treat it as a regular key=value property.
                if (!consumed && line.Contains(" = "))
                {
                    int _eq = line.IndexOf(" = ", StringComparison.Ordinal);
                    string _pk = line.Substring(0, _eq).Trim();
                    string _pv = line.Substring(_eq + 3).Trim();
                    if (!string.IsNullOrEmpty(_pk)) props[_pk] = _pv;
                }
            }
            else
            {
                // key = value  (split only on first " = " or "=")
                string pKey = null, pVal = null;
                if (line.Contains(" = "))
                {
                    int eq = line.IndexOf(" = ", StringComparison.Ordinal);
                    pKey = line.Substring(0, eq).Trim();
                    pVal = line.Substring(eq + 3).Trim();
                }
                else
                {
                    int eq = line.IndexOf('=');
                    if (eq >= 0)
                    {
                        pKey = line.Substring(0, eq).Trim();
                        pVal = line.Substring(eq + 1).Trim();
                    }
                }
                if (pKey != null)
                {
                    // "Header "name"" = "value" — hand-written custom header shorthand.
                    // Normalise into the ${("k","v")} format expected by ParseOb2Dict.
                    if (pKey.StartsWith("Header ", StringComparison.OrdinalIgnoreCase))
                    {
                        string hNamePart = pKey.Substring("Header ".Length).Trim();
                        if (hNamePart.Length >= 2 && hNamePart[0] == '"' && hNamePart[hNamePart.Length - 1] == '"')
                            hNamePart = hNamePart.Substring(1, hNamePart.Length - 2);
                        string hName = hNamePart.Replace("\\\"", "\"").Replace("\\\\", "\\");
                        string hValRaw = pVal.Trim();
                        string hVal = hValRaw.Length >= 2 && hValRaw[0] == '"' && hValRaw[hValRaw.Length - 1] == '"'
                            ? hValRaw.Substring(1, hValRaw.Length - 2).Replace("\\\"", "\"").Replace("\\\\", "\\")
                            : hValRaw;
                        string hNameEsc = hName.Replace("\\", "\\\\").Replace("\"", "\\\"");
                        string hValEsc  = hVal.Replace("\\", "\\\\").Replace("\"", "\\\"");
                        string pair = $"(\"{hNameEsc}\", \"{hValEsc}\")";
                        if (props.TryGetValue("customHeaders", out string prev)
                            && prev.StartsWith("${", StringComparison.Ordinal) && prev.EndsWith("}"))
                            props["customHeaders"] = prev.Substring(0, prev.Length - 1) + ", " + pair + "}";
                        else
                            props["customHeaders"] = "${" + pair + "}";
                    }
                    // multipartPart may appear multiple times; join with record separator \x1E
                    else if (pKey.Equals("multipartPart", StringComparison.OrdinalIgnoreCase)
                        && props.TryGetValue("multipartPart", out string existingMp)
                        && existingMp.Length > 0)
                        props["multipartPart"] = existingMp + "\x1E" + pVal;
                    else
                        props[pKey] = pVal;
                }
            }
            i++;
        }

        return new LoliCodeSegment {
            Type       = LoliCodeSegmentType.Block,
            BlockType  = blockType,
            Label      = label,
            Disabled   = disabled,
            Properties = props,
            KeyChains  = keychains,
            OutputVar  = outputVar,
            IsCapture  = isCapture,
        };
    }

    // ─── KEYCHAIN parsing ────────────────────────────────────────────────────

    // Formats:
    //   KEYCHAIN SUCCESS OR
    //   KEYCHAIN FAIL AND
    //   KEYCHAIN CUSTOM "MyStatus" OR
    private static LoliCodeKeyChain ParseKeyChainHeader(string line)
    {
        string rest  = line.Substring("KEYCHAIN ".Length).Trim();
        var    chain = new LoliCodeKeyChain();

        // Extract type token
        int sp1 = rest.IndexOf(' ');
        string typeToken = sp1 < 0 ? rest : rest.Substring(0, sp1);
        chain.ChainType = typeToken.ToUpperInvariant() switch {
            "SUCCESS"              => LoliCodeKeyChainType.SUCCESS,
            "FAIL" or "FAILURE"   => LoliCodeKeyChainType.FAIL,
            "BAN"                 => LoliCodeKeyChainType.BAN,
            "RETRY"               => LoliCodeKeyChainType.RETRY,
            "CUSTOM"              => LoliCodeKeyChainType.CUSTOM,
            "TOCHECK"             => LoliCodeKeyChainType.TOCHECK,
            "EXPIRED"             => LoliCodeKeyChainType.EXPIRED,
            "2FACTOR" or "TWOFACTOR" => LoliCodeKeyChainType.TWOFACTOR,
            _                     => LoliCodeKeyChainType.FAIL,
        };

        string modeStr = "OR";
        if (sp1 >= 0)
        {
            string after = rest.Substring(sp1 + 1).Trim();

            if (chain.ChainType == LoliCodeKeyChainType.CUSTOM)
            {
                // Optional quoted custom type name: KEYCHAIN CUSTOM "MyName" OR
                var m = Regex.Match(after, @"^""((?:[^""\\]|\\.)*)""\s+(\w+)");
                if (m.Success)
                {
                    chain.CustomType = m.Groups[1].Value.Replace("\\\"", "\"");
                    modeStr = m.Groups[2].Value;
                }
                else
                {
                    // unquoted: KEYCHAIN CUSTOM MyName OR
                    var parts2 = after.Split(new[]{' '}, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts2.Length >= 2) { chain.CustomType = parts2[0]; modeStr = parts2[1]; }
                    else if (parts2.Length == 1) modeStr = parts2[0];
                }
            }
            else
            {
                modeStr = after;
            }
        }

        chain.Mode = modeStr.Trim().Equals("AND", StringComparison.OrdinalIgnoreCase)
            ? LoliCodeKeyMode.AND : LoliCodeKeyMode.OR;

        return chain;
    }

    // ─── KEY / STRINGKEY / REGEXKEY parsing ─────────────────────────────────

    // Formats:
    //   STRINGKEY @data.SOURCE Contains "text"
    //   STRINGKEY @data.RESPONSECODE EqualTo "200"
    //   REGEXKEY  @data.SOURCE MatchesRegex "pattern"   ← SB legacy (STRINGKEY preferred)
    //   STRINGKEY @data.SOURCE Exists ""               ← OB2 requires "" even for Exists/DoesNotExist
    //   BOOLKEY / INTKEY / FLOATKEY / LISTKEY / DICTKEY → treated as STRINGKEY in SilverBullet
    //   KEY "literal text"                             ← OB1 shorthand (implicit Contains on SOURCE)
    private static LoliCodeKey ParseKeyLine(string line)
    {
        bool isRegex = line.StartsWith("REGEXKEY ", StringComparison.OrdinalIgnoreCase);
        bool isShort = line.StartsWith("KEY ",      StringComparison.OrdinalIgnoreCase);
        // OB2 typed keys — parsed identically to STRINGKEY in SilverBullet
        bool isFloat = line.StartsWith("FLOATKEY ", StringComparison.OrdinalIgnoreCase);
        bool isInt   = line.StartsWith("INTKEY ",   StringComparison.OrdinalIgnoreCase);
        bool isBool  = line.StartsWith("BOOLKEY ",  StringComparison.OrdinalIgnoreCase);
        bool isList  = line.StartsWith("LISTKEY ",  StringComparison.OrdinalIgnoreCase);
        bool isDict  = line.StartsWith("DICTKEY ",  StringComparison.OrdinalIgnoreCase);

        string prefix = isRegex ? "REGEXKEY " :
                        isShort ? "KEY "       :
                        isFloat ? "FLOATKEY "  :
                        isInt   ? "INTKEY "    :
                        isBool  ? "BOOLKEY "   :
                        isList  ? "LISTKEY "   :
                        isDict  ? "DICTKEY "   :
                                  "STRINGKEY ";
        string rest = line.Substring(prefix.Length).Trim();

        var key = new LoliCodeKey();

        if (isShort)
        {
            // KEY "literal" → implicit: LeftTerm=<SOURCE>, Comparer=Contains, RightTerm=literal
            key.LeftTerm  = "@data.SOURCE";
            key.Comparer  = "Contains";
            key.RightTerm = ParseQuotedOrBare(rest);
            return key;
        }

        // Read left term (until first space)
        int sp1 = rest.IndexOf(' ');
        if (sp1 < 0)
        {
            key.LeftTerm = rest;
            key.Comparer = isRegex ? "MatchesRegex" : "Contains";
            return key;
        }
        key.LeftTerm = rest.Substring(0, sp1).Trim();
        string remaining = rest.Substring(sp1 + 1).Trim();

        if (isRegex)
        {
            key.Comparer = "MatchesRegex";
            // Allow optional explicit keyword: REGEXKEY @data.SOURCE MatchesRegex "..."
            if (!remaining.StartsWith("\"", StringComparison.Ordinal))
            {
                int sp2 = remaining.IndexOf(' ');
                if (sp2 >= 0) remaining = remaining.Substring(sp2 + 1).Trim();
            }
        }
        else
        {
            // STRINGKEY @data.SOURCE <Comparer> "value"
            int sp2 = remaining.IndexOf(' ');
            if (sp2 < 0)
            {
                // No right term — Exists / DoesNotExist
                key.Comparer  = remaining;
                key.RightTerm = "";
                return key;
            }
            key.Comparer = remaining.Substring(0, sp2).Trim();
            remaining    = remaining.Substring(sp2 + 1).Trim();
        }

        key.RightTerm = ParseQuotedOrBare(remaining);
        return key;
    }

    // Parse a quoted string literal "..." (or OB2 $"..." interpolated literal) handling
    // \" and \\ escapes, or return the raw value if not quoted.
    private static string ParseQuotedOrBare(string s)
    {
        s = s.Trim();
        // Strip OB2 $"..." interpolated-string prefix: treat escape sequences the same
        // as a regular quoted string (no runtime {expr} interpolation in key values).
        if (s.StartsWith("$\"", StringComparison.Ordinal)) s = s.Substring(1);
        var m = Regex.Match(s, @"^""((?:[^""\\]|\\.)*)""");
        if (m.Success)
        {
            return m.Groups[1].Value
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
        }
        return s;
    }

    private static string EscapeAsLiteral(string s) =>
        "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n") + "\"";
}
