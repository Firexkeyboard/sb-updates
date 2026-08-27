using System.Collections.Generic;

namespace RuriLib.LS.LoliCode;

public enum LoliCodeSegmentType { Block, Code }

/// <summary>
/// Represents a parsed segment of a LoliCode script: either a BLOCK:Type...ENDBLOCK
/// declaration or raw inline C# code.
/// </summary>
public sealed class LoliCodeSegment
{
    public LoliCodeSegmentType Type { get; init; }

    // ── Inline C# code segment ──────────────────────────────────────────────
    public string Code { get; init; } = "";

    // ── Script source preservation (for round-trip LoliCode ↔ LoliScript) ──
    // Non-null only when this Code segment originated from an IRONPYTHON/PYTHON,
    // BEGIN SCRIPT, or BLOCK:Script block. SegmentsToBlocks() uses these to
    // reconstruct "BEGIN SCRIPT ... END SCRIPT -> VARS" on conversion.
    public List<string> PythonLines      { get; init; }
    public string       PythonOutputs    { get; init; }
    public string       PythonInputs     { get; init; }  // from OB2 BLOCK:Script INPUT line
    public string       ScriptInterpreter { get; init; } // "IronPython","Python","JavaScript","NodeJS"
    public bool         IsIronPython     { get; init; }  // kept for backward compat

    // ── Block segment ───────────────────────────────────────────────────────
    public string BlockType  { get; init; } = "";
    public string Label      { get; init; } = "";
    public bool   Disabled   { get; init; }

    /// <summary>OB2-style block properties (key → raw value string)</summary>
    public Dictionary<string, string> Properties { get; init; } = new();

    /// <summary>Parsed KEYCHAIN/STRINGKEY/REGEXKEY structure (for Keycheck blocks).</summary>
    public List<LoliCodeKeyChain> KeyChains { get; init; } = new();

    /// <summary>From "=> VAR @name" or "=> CAP @name" after the last property</summary>
    public string OutputVar { get; init; }

    /// <summary>True when "=> CAP @name" (captured variable, will appear in output)</summary>
    public bool IsCapture { get; init; }
}
