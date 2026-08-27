using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using RuriLib.LS;

namespace RuriLib;

/// <summary>
/// Container block for raw LoliScript (or LoliCode) code embedded in the Stacker.
/// When executed, it runs the script through the appropriate engine based on the
/// script content (LoliCode if it starts with BLOCK:, otherwise LoliScript).
/// </summary>
public class BlockLSCode : BlockBase
{
    private string script = "";

    public string Script
    {
        get => script;
        set
        {
            script = value;
            OnPropertyChanged("Script");
        }
    }

    public BlockLSCode()
    {
        base.Label = "LS";
    }

    public override BlockBase FromLS(List<string> lines)
    {
        Script = string.Join(Environment.NewLine, lines);
        return this;
    }

    // Single-line FromLS for inline use
    public override BlockBase FromLS(string line)
    {
        Script = line ?? "";
        return this;
    }

    public override string ToLS(bool indent = true)
    {
        // When the script contains compiled LoliCode patterns (Condition.ReplaceAndVerify calls,
        // data.Captures assignments, etc.) convert them back to LoliScript syntax so the output
        // is both readable and executable by the LoliScript engine.
        return ConvertCSharpToLoliScript(script);
    }

    public override void Process(BotData data)
    {
        if (Disabled) return;
        if (string.IsNullOrWhiteSpace(script)) return;

        // Run as LoliCode if it contains BLOCK: markers, otherwise as LoliScript
        if (LS.LoliCode.LoliCodeRunner.IsLoliCode(script))
        {
            LS.LoliCode.LoliCodeRunner.Run(script, data);
        }
        else
        {
            // Execute line-by-line through the LoliScript engine
            var ls = new LoliScript(script);
            while (ls.CanProceed)
            {
                try { ls.TakeStep(data); }
                catch (Exception ex) when (!(ex is System.Threading.ThreadAbortException))
                {
                    data.Log(new LogEntry($"[LSCode] Error: {ex.Message}", Colors.Tomato));
                    break;
                }
                if (data.Status != BotStatus.NONE && data.Status != BotStatus.SUCCESS
                    && !(data.Status == BotStatus.CUSTOM && data.ConfigSettings.ContinueOnCustom))
                    break;
            }
        }
    }

    // ── C# → LoliScript conversion ────────────────────────────────────────────
    // Converts a code segment compiled from LoliCode back into LoliScript syntax.
    //
    // Patterns converted:
    //   if (Condition.ReplaceAndVerify("left", Comparer.X, "right", data._inner)) {
    //       → IF "left" X "right"
    //   } else if (Condition.ReplaceAndVerify(...)) {
    //       → ELSE + nested IF "left" X "right"  (extra ENDIF emitted at closing })
    //   } else {   → ELSE
    //   }          → ENDIF (×N when ELSE IFs were nested)
    //   data.Captures["var"] = "value";          → SET CAP "var" "value"
    //   data.Variables.Set(new CVar("v","val")); → SET VAR "v" "val"
    //   data.STATUS = "SUCCESS";                 → SET STATUS SUCCESS
    //   Multi-condition OR  if (c1 || c2) {  expands with a temporary flag variable.
    //
    // Raw C# if-blocks that don't match Condition.ReplaceAndVerify are passed through
    // verbatim, and everything inside them is also passed through verbatim so that
    // converted LoliScript keywords don't appear at the wrong nesting level.
    private static string ConvertCSharpToLoliScript(string code)
    {
        if (string.IsNullOrEmpty(code)) return code;

        // Quick bailout: only process when compiled LoliCode markers are present.
        bool hasMarkers = code.Contains("data.Captures[")
            || code.Contains("data.Variables.Set(")
            || code.Contains("Condition.ReplaceAndVerify(")
            || code.Contains("data.STATUS");
        if (!hasMarkers) return code;

        const string RAV = "RuriLib.Functions.Conditions.Condition.ReplaceAndVerify";
        const string CMP = "RuriLib.Functions.Conditions.Comparer.";

        // Matches a single ReplaceAndVerify(...) boolean expression.
        // Groups: 1=left, 2=comparer, 3=right
        string ravPat = Regex.Escape(RAV) + @"\(""((?:[^""\\]|\\.)*)"",\s*"
                      + Regex.Escape(CMP) + @"(\w+),\s*""((?:[^""\\]|\\.)*)"",\s*data\._inner\)";

        var rxSingleIf     = new Regex(@"^if\s*\(" + ravPat + @"\)\s*\{$");
        var rxSingleElseIf = new Regex(@"^\}\s*else\s+if\s*\(" + ravPat + @"\)\s*\{$");
        var rxAnyIf        = new Regex(@"^if\s*\(.+\)\s*\{$");
        var rxAnyElseIf    = new Regex(@"^\}\s*else\s+if\s*\(.+\)\s*\{$");
        var rxElse         = new Regex(@"^\}\s*else\s*\{$");

        var rxCapStr    = new Regex(@"^data\.Captures\[""([^""]+)""\]\s*=\s*""((?:[^""\\]|\\.)*)""\s*;$");
        var rxCapVar    = new Regex(@"^data\.Captures\[""([^""]+)""\]\s*=\s*data\.GetVar\(""([^""]+)""\)\s*;$");
        var rxVarStr    = new Regex(@"^data\.Variables\.Set\(new\s+(?:RuriLib\.Models\.)?CVar\(""([^""]+)"",\s*""((?:[^""\\]|\\.)*)""\)\)\s*;$");
        var rxVarStrCap = new Regex(@"^data\.Variables\.Set\(new\s+(?:RuriLib\.Models\.)?CVar\(""([^""]+)"",\s*""((?:[^""\\]|\\.)*)"",\s*(?:true|isCapture:\s*true)\)\)\s*;$");
        var rxStatus    = new Regex(@"^data\.STATUS\s*=\s*""([^""]*)""\s*;$");

        // Single ReplaceAndVerify expression finder (for multi-OR splitting)
        var rxRav = new Regex(ravPat);

        var lines = code.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var result = new StringBuilder();

        // ifStack: tracks convertible IF blocks.
        // Each entry: (endifCount) — how many ENDIF lines to emit when the closing } is seen.
        // We only push to this stack for Condition.ReplaceAndVerify-based ifs.
        var ifStack = new Stack<int>();

        // rawDepth: how many non-convertible raw C# { blocks we are currently inside.
        // When rawDepth > 0, all lines are passed through verbatim (no conversion).
        int rawDepth = 0;

        int orFlagIdx = 0;

        foreach (string rawLine in lines)
        {
            string trimmed = rawLine.Trim();

            if (string.IsNullOrEmpty(trimmed)) { result.AppendLine(); continue; }

            // ── Inside a raw C# block: pass everything through ────────────────
            if (rawDepth > 0)
            {
                // Track additional { openings / } closings inside raw blocks
                if (rxAnyIf.IsMatch(trimmed) || rxAnyElseIf.IsMatch(trimmed))
                {
                    // } else if counts as one closing + one opening → net depth unchanged.
                    // Plain if (...) { adds one level.
                    if (rxAnyIf.IsMatch(trimmed)) rawDepth++;
                    // } else if: the } closes one level, the { opens one → net 0
                    // (rawDepth stays the same)
                    result.AppendLine(rawLine);
                    continue;
                }
                if (rxElse.IsMatch(trimmed))
                {
                    result.AppendLine(rawLine);
                    continue;
                }
                if (trimmed == "}")
                {
                    rawDepth--;
                    result.AppendLine(rawLine);
                    continue;
                }
                result.AppendLine(rawLine);
                continue;
            }

            // ── } else if (single Condition.ReplaceAndVerify) { ──────────────
            {
                var m = rxSingleElseIf.Match(trimmed);
                if (m.Success)
                {
                    int prevCount = ifStack.Count > 0 ? ifStack.Pop() : 1;
                    result.AppendLine("ELSE");
                    result.AppendLine($"IF \"{m.Groups[1].Value}\" {m.Groups[2].Value} \"{m.Groups[3].Value}\"");
                    ifStack.Push(prevCount + 1);
                    continue;
                }
            }

            // ── } else if (multi-OR Condition.ReplaceAndVerify) { ────────────
            if (rxAnyElseIf.IsMatch(trimmed) && trimmed.Contains(RAV))
            {
                string[] parts = ExtractOrConditionParts(trimmed, RAV, rxRav);
                if (parts != null)
                {
                    int prevCount = ifStack.Count > 0 ? ifStack.Pop() : 1;
                    string flag = $"__ls_or_{orFlagIdx++}";
                    result.AppendLine("ELSE");
                    result.AppendLine($"SET VAR \"{flag}\" \"0\"");
                    foreach (string part in parts)
                    {
                        var mc = rxRav.Match(part.Trim());
                        if (mc.Success)
                        {
                            result.AppendLine($"IF \"{mc.Groups[1].Value}\" {mc.Groups[2].Value} \"{mc.Groups[3].Value}\"");
                            result.AppendLine($"SET VAR \"{flag}\" \"1\"");
                            result.AppendLine("ENDIF");
                        }
                    }
                    result.AppendLine($"IF \"<{flag}>\" EqualTo \"1\"");
                    ifStack.Push(prevCount + 1);
                    continue;
                }
            }

            // ── } else if (raw C# — non-convertible) { ───────────────────────
            if (rxAnyElseIf.IsMatch(trimmed))
            {
                // Close the convertible if-block on our stack (if any), then enter raw depth
                if (ifStack.Count > 0)
                {
                    // The } closes the convertible block — emit its ENDIFs
                    int count = ifStack.Pop();
                    for (int e = 0; e < count; e++)
                        result.AppendLine("ENDIF");
                }
                // The else if (...) { opens a new raw block
                rawDepth++;
                result.AppendLine(rawLine);
                continue;
            }

            // ── if (single Condition.ReplaceAndVerify) { ─────────────────────
            {
                var m = rxSingleIf.Match(trimmed);
                if (m.Success)
                {
                    result.AppendLine($"IF \"{m.Groups[1].Value}\" {m.Groups[2].Value} \"{m.Groups[3].Value}\"");
                    ifStack.Push(1);
                    continue;
                }
            }

            // ── if (multi-OR Condition.ReplaceAndVerify) { ───────────────────
            if (rxAnyIf.IsMatch(trimmed) && trimmed.Contains(RAV))
            {
                string[] parts = ExtractOrConditionParts(trimmed, RAV, rxRav);
                if (parts != null)
                {
                    string flag = $"__ls_or_{orFlagIdx++}";
                    result.AppendLine($"SET VAR \"{flag}\" \"0\"");
                    foreach (string part in parts)
                    {
                        var mc = rxRav.Match(part.Trim());
                        if (mc.Success)
                        {
                            result.AppendLine($"IF \"{mc.Groups[1].Value}\" {mc.Groups[2].Value} \"{mc.Groups[3].Value}\"");
                            result.AppendLine($"SET VAR \"{flag}\" \"1\"");
                            result.AppendLine("ENDIF");
                        }
                    }
                    result.AppendLine($"IF \"<{flag}>\" EqualTo \"1\"");
                    ifStack.Push(1);
                    continue;
                }
            }

            // ── if (raw C# — non-convertible) { ──────────────────────────────
            if (rxAnyIf.IsMatch(trimmed))
            {
                rawDepth++;
                result.AppendLine(rawLine);
                continue;
            }

            // ── } else { ─────────────────────────────────────────────────────
            if (rxElse.IsMatch(trimmed))
            {
                if (ifStack.Count > 0)
                    result.AppendLine("ELSE");
                else
                    result.AppendLine(rawLine);
                continue;
            }

            // ── } (bare closing brace) ────────────────────────────────────────
            if (trimmed == "}")
            {
                if (ifStack.Count > 0)
                {
                    int count = ifStack.Pop();
                    for (int e = 0; e < count; e++)
                        result.AppendLine("ENDIF");
                }
                else
                {
                    result.AppendLine(rawLine);
                }
                continue;
            }

            // ── Variable / capture / status assignments ───────────────────────
            {
                var m = rxCapStr.Match(trimmed);
                if (m.Success)
                {
                    result.AppendLine($"SET CAP \"{m.Groups[1].Value}\" \"{m.Groups[2].Value}\"");
                    continue;
                }
            }
            {
                var m = rxCapVar.Match(trimmed);
                if (m.Success)
                {
                    result.AppendLine($"SET CAP \"{m.Groups[1].Value}\" \"<{m.Groups[2].Value}>\"");
                    continue;
                }
            }
            {
                var m = rxVarStrCap.Match(trimmed);
                if (m.Success)
                {
                    result.AppendLine($"SET CAP \"{m.Groups[1].Value}\" \"{m.Groups[2].Value}\"");
                    continue;
                }
            }
            {
                var m = rxVarStr.Match(trimmed);
                if (m.Success)
                {
                    result.AppendLine($"SET VAR \"{m.Groups[1].Value}\" \"{m.Groups[2].Value}\"");
                    continue;
                }
            }
            {
                var m = rxStatus.Match(trimmed);
                if (m.Success)
                {
                    result.AppendLine($"SET STATUS {m.Groups[1].Value.ToUpper()}");
                    continue;
                }
            }

            // ── Pass through unrecognized lines ───────────────────────────────
            result.AppendLine(rawLine);
        }

        return result.ToString().TrimEnd();
    }

    // Extracts individual ReplaceAndVerify(...) parts from a multi-OR/AND if/else-if line.
    // Returns null if there's only one condition or if any part isn't a ReplaceAndVerify call.
    private static string[] ExtractOrConditionParts(string ifLine, string rav, Regex rxRav)
    {
        // Strip outer "if (" / "} else if (" prefix and ") {" suffix.
        var mOuter = Regex.Match(ifLine, @"^(?:\}\s*else\s+)?if\s*\((.+)\)\s*\{$");
        if (!mOuter.Success) return null;

        string condsPart = mOuter.Groups[1].Value;

        // Split on " || " or " && " — safe because ReplaceAndVerify arg strings don't contain || or &&
        string[] parts = Regex.Split(condsPart, @"\s*\|\|\s*|\s*&&\s*");
        if (parts.Length < 2) return null;

        // Verify every part is a ReplaceAndVerify expression.
        foreach (string p in parts)
            if (!rxRav.IsMatch(p.Trim())) return null;

        return parts;
    }
}
