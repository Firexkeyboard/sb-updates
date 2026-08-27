using System;
using System.Collections.Generic;
using System.Windows.Media;
using RuriLib.Models;

namespace RuriLib.LS.LoliCode;

/// <summary>
/// OB2-style wrapper for BotData, exposed as "data" in LoliCode scripts.
/// Properties use the familiar ALL-CAPS names from OpenBullet 2.
/// </summary>
public sealed class LoliCodeData
{
    /// <summary>Direct access to the underlying BotData (used by block Process() calls).</summary>
    public BotData _inner { get; }

    public LoliCodeData(BotData botData) { _inner = botData; Logger = new Ob2Logger(this); }

    /// <summary>OB2: data.Logger.Log(message, LogColors.X)</summary>
    public Ob2Logger Logger { get; }

    // ── Status ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Get/set the bot status as a string (SUCCESS, FAIL, RETRY, BAN, ERROR, CUSTOM, or any custom value).
    /// </summary>
    public string STATUS
    {
        get => _inner.StatusString;
        set
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            string upper = value.Trim().ToUpperInvariant();
            switch (upper)
            {
                case "SUCCESS": _inner.Status = BotStatus.SUCCESS; break;
                case "FAIL":    _inner.Status = BotStatus.FAIL;    break;
                case "RETRY":   _inner.Status = BotStatus.RETRY;   break;
                case "BAN":     _inner.Status = BotStatus.BAN;     break;
                case "ERROR":   _inner.Status = BotStatus.ERROR;   break;
                case "NONE":    _inner.Status = BotStatus.NONE;    break;
                default:
                    _inner.Status       = BotStatus.CUSTOM;
                    _inner.CustomStatus = value;
                    break;
            }
        }
    }

    // ── HTTP response ────────────────────────────────────────────────────────
    /// <summary>HTTP response code as int (OB2-compatible: data.RESPONSECODE == 200).</summary>
    public int RESPONSECODE =>
        int.TryParse(_inner.ResponseCode, out int c) ? c : 0;

    /// <summary>HTTP response code as string (for Contains checks etc.).</summary>
    public string RESPONSECODESTR => _inner.ResponseCode ?? "";

    public string SOURCE   => _inner.ResponseSource;
    public string ADDRESS  => _inner.Address;

    public Dictionary<string, string> COOKIES
    {
        get => _inner.Cookies;
        set => _inner.Cookies = value;
    }

    public Dictionary<string, string> HEADERS
    {
        get => _inner.ResponseHeaders;
        set => _inner.ResponseHeaders = value;
    }

    // ── Input data ────────────────────────────────────────────────────────────
    /// <summary>Raw input line (user:pass or whatever Data contains)</summary>
    public string INPUT => _inner.Data?.Data ?? "";

    /// <summary>Slice named USER from the input line (e.g. email in email:pass format).</summary>
    public string USER     => GetVar("USER");

    /// <summary>OB2 alias for USER.</summary>
    public string USERNAME => GetVarFallback("USER", "USERNAME");

    /// <summary>Slice named PASS from the input line.</summary>
    public string PASS     => GetVar("PASS");

    /// <summary>OB2 alias for PASS.</summary>
    public string PASSWORD => GetVarFallback("PASS", "PASSWORD");

    /// <summary>Slice named EMAIL from the input line (alias for USER in email:pass wordlists).</summary>
    public string EMAIL    => GetVarFallback("EMAIL", "USER");

    // ── Captures ─────────────────────────────────────────────────────────────
    // OB2: data.Captures["VarName"] = "value"  →  stores as capture CVar (isCapture=true)
    private CaptureProxy _capturesProxy;
    public CaptureProxy Captures => _capturesProxy ??= new CaptureProxy(_inner);

    // ── Variables ─────────────────────────────────────────────────────────────
    public VariableList Variables => _inner.Variables;

    /// <summary>Read a bot variable as string.</summary>
    public string GetVar(string name) => _inner.Variables.Get(name)?.Value?.ToString() ?? "";

    /// <summary>Read first non-empty variable from the candidates (OB2 alias resolution).</summary>
    public string GetVarFallback(params string[] names)
    {
        foreach (var n in names)
        {
            var v = _inner.Variables.Get(n)?.Value?.ToString();
            if (!string.IsNullOrEmpty(v)) return v;
        }
        return "";
    }

    /// <summary>Read a bot variable as raw byte[] (for TYPE:RAW @byteVar).</summary>
    public byte[] GetByteVar(string name) => _inner.Variables.Get(name)?.Value as byte[];

    /// <summary>Read a bot list variable as List&lt;string&gt; (for RECURSIVE Parse output).</summary>
    public System.Collections.Generic.List<string> GetListVar(string name)
    {
        var cv = _inner.Variables.Get(name);
        if (cv?.Value is System.Collections.Generic.List<string> l) return l;
        if (cv?.Value != null) return new System.Collections.Generic.List<string> { cv.Value.ToString() };
        return new System.Collections.Generic.List<string>();
    }

    /// <summary>OB2 captcha credit balance (informational; set by inline C# code in configs).</summary>
    public decimal CaptchaCredit { get; set; }

    /// <summary>Write a string bot variable.</summary>
    public void SetVar(string name, string value) =>
        _inner.Variables.Set(new CVar(name, value));

    /// <summary>Write a list bot variable.</summary>
    public void SetVar(string name, List<string> value) =>
        _inner.Variables.Set(new CVar(name, value));

    // ── OB2 block API ────────────────────────────────────────────────────────
    // OB2 generates these calls around every LoliCode block. We need them to
    // compile; _blockOutputValue is a temp slot shared with Ob2Compat helpers
    // so MarkForCapture can retrieve the value set by ConstantString / similar.
    internal string _blockOutputValue = null;

    /// <summary>OB2 internal: logs which block type is executing.</summary>
    public void ExecutingBlock(string blockName) =>
        _inner.Log(new LogEntry($"<--- Executing Block {blockName} --->", Color.FromRgb(0xFF, 0xA5, 0x00)));

    /// <summary>OB2 internal: marks the named variable as a capture output.</summary>
    public void MarkForCapture(string varName)
    {
        string val = _blockOutputValue ?? GetVar(varName);
        _inner.Variables.Set(new CVar(varName, val ?? "", isCapture: true));
        _blockOutputValue = null;
    }

    /// <summary>OB2-compatible: data.SetObject(name, value) — stores any value as a bot variable.</summary>
    public void SetObject(string name, object value)
    {
        switch (value)
        {
            case string s:
                _inner.Variables.Set(new CVar(name, s));
                break;
            case List<string> lst:
                _inner.Variables.Set(new CVar(name, lst));
                break;
            case Dictionary<string, string> dict:
                _inner.Variables.Set(new CVar(name, dict));
                break;
            default:
                _inner.Variables.Set(new CVar(name, value?.ToString() ?? ""));
                break;
        }
    }

    // ── Proxy ─────────────────────────────────────────────────────────────────
    public string PROXY => _inner.Proxy?.Proxy ?? "";

    // OB2-compatible proxy access: data.UseProxy (bool) and data.Proxy (CProxy)
    public bool UseProxy
    {
        get => _inner.UseProxies;
        set => _inner.UseProxies = value;
    }
    public RuriLib.Models.CProxy Proxy => _inner.Proxy;

    public byte[] RAWSOURCE =>
        _inner.RawSourceBytes ?? System.Text.Encoding.Latin1.GetBytes(_inner.ResponseSource ?? "");

    // ── Logging ───────────────────────────────────────────────────────────────
    public void Log(string message) =>
        _inner.Log(new LogEntry(message, Colors.White));

    public void Log(string message, Color color) =>
        _inner.Log(new LogEntry(message, color));

    public void Flush() => _inner.Flush();

    // OB2: data.LogVariableAssignment(nameof(myVar))
    public void LogVariableAssignment(string name) =>
        _inner.Log(new LogEntry($"Assigned variable | Name: {name}", Color.FromRgb(0x7E, 0xC8, 0xFF)));

    // Overload with value (e.g. data.LogVariableAssignment(nameof(x), x))
    public void LogVariableAssignment(string name, object value)
    {
        string display = value is byte[] b
            ? $"[{b.Length} bytes] {System.Convert.ToHexString(b).ToLower()}"
            : value?.ToString() ?? "null";
        _inner.Log(new LogEntry($"Assigned variable | Name: {name} | Value: {display}", Color.FromRgb(0x7E, 0xC8, 0xFF)));
    }

    // ── Error storage ─────────────────────────────────────────────────────────
    // OB2: data.ERROR = ex.PrettyPrint()
    private string _error = "";
    public string ERROR
    {
        get => _error;
        set
        {
            _error = value ?? "";
            _inner.Variables.Set(new CVar("ERROR", _error));
        }
    }
}

/// <summary>
/// Dictionary-like proxy that stores key/value pairs as capture CVars in BotData.
/// Accessed via <c>data.Captures["VarName"] = "value"</c> in LoliCode scripts.
/// </summary>
public sealed class CaptureProxy
{
    private readonly BotData _data;
    internal CaptureProxy(BotData data) => _data = data;

    public string this[string name]
    {
        get => _data.Variables.Get(name)?.Value?.ToString() ?? "";
        set => _data.Variables.Set(new CVar(name, value ?? "", isCapture: true));
    }
}

/// <summary>
/// Roslyn scripting globals object. Public fields become directly accessible
/// in the script by name (so the user writes "data.STATUS" not "__globals.data.STATUS").
/// </summary>
public sealed class LoliCodeGlobals
{
    public LoliCodeData data;
    /// <summary>OB2 alias: scripts can use input.USER, input.PASS, etc.</summary>
    public LoliCodeData input;

    /// <summary>
    /// OB2 global: scripts can read/write STATUS as a bare name (e.g. if (STATUS.Contains("NONE"))).
    /// Forwards to data.STATUS so it always reflects the current bot status.
    /// </summary>
    public string STATUS
    {
        get => data?.STATUS ?? "NONE";
        set { if (data != null) data.STATUS = value; }
    }
}
