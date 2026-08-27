using System.Collections.Generic;

namespace RuriLib.LS.LoliCode;

/// <summary>Types map to KeyChain.KeychainType in SilverBullet.</summary>
public enum LoliCodeKeyChainType { SUCCESS, FAIL, BAN, RETRY, CUSTOM, TOCHECK, EXPIRED, TWOFACTOR }

/// <summary>OR = any key matches; AND = all keys must match.</summary>
public enum LoliCodeKeyMode { OR, AND }

/// <summary>Parsed KEYCHAIN block from LoliCode text.</summary>
public sealed class LoliCodeKeyChain
{
    public LoliCodeKeyChainType ChainType { get; set; } = LoliCodeKeyChainType.FAIL;
    public LoliCodeKeyMode Mode { get; set; } = LoliCodeKeyMode.OR;
    public string CustomType { get; set; } = "CUSTOM";
    public List<LoliCodeKey> Keys { get; } = new();
}

/// <summary>
/// Parsed STRINGKEY / REGEXKEY line.
/// LeftTerm is in OB2 form (@data.SOURCE) or SilverBullet form (&lt;SOURCE&gt;).
/// </summary>
public sealed class LoliCodeKey
{
    public string LeftTerm  { get; set; } = "@data.SOURCE";
    public string Comparer  { get; set; } = "Contains";
    public string RightTerm { get; set; } = "";
}
