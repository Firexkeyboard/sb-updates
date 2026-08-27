using System;
using System.Text.RegularExpressions;
using RuriLib.Models;

namespace RuriLib.LS.LoliCode;

// OB2-compatible enum for CheckCondition calls.
public enum BoolComparison { Is = 0, IsNot = 1 }

/// <summary>
/// Static helpers that match the OB2 LoliCode global API so that configs copied
/// from OpenBullet 2 compile without modification in SilverBullet.
/// The class is imported via "using static RuriLib.LS.LoliCode.Ob2Compat;" that
/// the compiler preamble injects, making every method directly callable as a
/// top-level name (ConstantString, CheckCondition, MatchRegexGroups, etc.).
/// </summary>
public static class Ob2Compat
{
    // OB2: string varName = ConstantString(data, "@VarName");
    // Resolves @VarName / @input.VarName (OB2 syntax) and <VarName> (SilverBullet syntax)
    // from data.Variables, then stores the result in data._blockOutputValue so
    // MarkForCapture() can retrieve it without access to the Roslyn local variable.
    public static string ConstantString(LoliCodeData data, string value)
    {
        if (value == null) { data._blockOutputValue = ""; return ""; }
        // ReplaceValues handles both @VAR (OB2) and <VAR> (SilverBullet) in one pass.
        // We do NOT do a separate @-resolution here first — that caused double-resolution
        // when a variable's value itself contained an @-reference (e.g. PART = "@TOKEN").
        string resolved = RuriLib.BlockBase.ReplaceValues(value, data._inner);
        data._blockOutputValue = resolved;
        return resolved;
    }

    // OB2: CheckCondition(data, data.UseProxy.AsBool(), BoolComparison.Is, true)
    public static bool CheckCondition(LoliCodeData data, bool value, BoolComparison comparison, bool right)
        => comparison == BoolComparison.Is ? value == right : value != right;

    // Overload for object left-hand side (OB2 uses dynamic typing)
    public static bool CheckCondition(LoliCodeData data, object value, BoolComparison comparison, bool right)
        => CheckCondition(data, value.AsBool(), comparison, right);

    // OB2: string result = MatchRegexGroups(data, input, pattern, "[1]", false, "", "", false);
    public static string MatchRegexGroups(
        LoliCodeData data,
        string input,
        string pattern,
        string outputFormat,
        bool caseSensitive = false,
        string prefix = "",
        string suffix = "",
        bool encode = false)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(pattern)) return "";
        try
        {
            var opts = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            var m = Regex.Match(input, pattern, opts);
            if (!m.Success) return "";
            string result = outputFormat ?? "";
            for (int i = 1; i < m.Groups.Count; i++)
                result = result.Replace($"[{i}]", m.Groups[i].Value);
            return prefix + result + suffix;
        }
        catch { return ""; }
    }

    // OB2 extension: marks a variable as a capture (MARK @varName)
    public static void MarkVar(LoliCodeData data, string name, object value)
    {
        if (value is System.Collections.Generic.List<string> list)
            data._inner.Variables.Set(new CVar(name, list, true));
        else
            data._inner.Variables.Set(new CVar(name, value?.ToString() ?? "", true));
    }

    // ── OB2 extension methods on object ─────────────────────────────────────

    // OB2: data.UseProxy.AsBool()  / prux.AsString()
    public static string AsString(this object obj) => obj?.ToString() ?? "";

    public static bool AsBool(this object obj)
    {
        if (obj is bool b) return b;
        string s = obj?.ToString() ?? "";
        return s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1";
    }

    public static int AsInt(this object obj)
    {
        if (obj is int i) return i;
        if (int.TryParse(obj?.ToString(), out int v)) return v;
        return 0;
    }

    public static float AsFloat(this object obj)
    {
        if (obj is float f) return f;
        if (float.TryParse(obj?.ToString(),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out float v)) return v;
        return 0f;
    }

    public static double AsDouble(this object obj)
    {
        if (obj is double d) return d;
        if (double.TryParse(obj?.ToString(),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out double v)) return v;
        return 0.0;
    }

    public static System.Collections.Generic.List<string> AsList(this object obj)
    {
        if (obj is System.Collections.Generic.List<string> l) return l;
        return new System.Collections.Generic.List<string> { obj?.ToString() ?? "" };
    }

    // OB2 compat: some generated code calls bool.Contains("True") / bool.Contains("False")
    // (e.g. from IF BOOLKEY @solved IsNot True compiled to if (solved.Contains("True"))).
    // This extension makes that pattern compile and evaluate correctly.
    public static bool Contains(this bool b, string value) =>
        b.ToString().Equals(value, StringComparison.OrdinalIgnoreCase);

    // ── OB2 byte array helpers ───────────────────────────────────────────────

    // byte[].AsBytes() — identity; lets OB2 code call .AsBytes() on byte[] locals.
    public static byte[] AsBytes(this byte[] b) => b;

    // string.AsBytes() — Latin-1 (preserves raw byte values for crypto inputs).
    public static byte[] AsBytes(this string s) => System.Text.Encoding.Latin1.GetBytes(s ?? "");

    // object.AsBytes() — dispatches to typed overloads.
    public static byte[] AsBytes(this object o)
    {
        if (o is byte[] b) return b;
        if (o is string s) return AsBytes(s);
        return System.Text.Encoding.Latin1.GetBytes(o?.ToString() ?? "");
    }

    // ── OB2 crypto helpers ───────────────────────────────────────────────────

    // OB2: string result = AESDecryptString(data, inputBytes, keyBytes, ivBytes, mode, padding, keySize)
    public static string AESDecryptString(
        LoliCodeData data,
        byte[] input, byte[] key, byte[] iv,
        System.Security.Cryptography.CipherMode mode,
        System.Security.Cryptography.PaddingMode padding,
        int keySize)
    {
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.KeySize   = keySize;
        aes.BlockSize = 128;
        aes.Mode      = mode;
        aes.Padding   = padding;
        aes.Key       = key;
        if (iv != null && mode != System.Security.Cryptography.CipherMode.ECB)
            aes.IV = iv;
        using var dec = aes.CreateDecryptor();
        byte[] result = dec.TransformFinalBlock(input, 0, input.Length);
        return System.Text.Encoding.Latin1.GetString(result);
    }

    // OB2: byte[] result = AESEncrypt(data, inputBytes, keyBytes, ivBytes, mode, padding, keySize)
    public static byte[] AESEncrypt(
        LoliCodeData data,
        byte[] input, byte[] key, byte[] iv,
        System.Security.Cryptography.CipherMode mode,
        System.Security.Cryptography.PaddingMode padding,
        int keySize)
    {
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.KeySize   = keySize;
        aes.BlockSize = 128;
        aes.Mode      = mode;
        aes.Padding   = padding;
        aes.Key       = key;
        if (iv != null && mode != System.Security.Cryptography.CipherMode.ECB)
            aes.IV = iv;
        using var enc = aes.CreateEncryptor();
        return enc.TransformFinalBlock(input, 0, input.Length);
    }

    // ── OB2 utility helpers ──────────────────────────────────────────────────

    // OB2: string hex = ByteArrayToHexString(data, bytes)
    public static string ByteArrayToHexString(LoliCodeData data, byte[] input)
        => System.Convert.ToHexString(input ?? Array.Empty<byte>()).ToLower();

    // OB2: string sub = Substring(data, str, start, length)
    public static string Substring(LoliCodeData data, string input, int start, int length)
    {
        if (string.IsNullOrEmpty(input) || start >= input.Length) return "";
        return input.Substring(start, Math.Min(length, input.Length - start));
    }

    // OB2: exception.PrettyPrint() — returns a readable error string
    public static string PrettyPrint(this Exception ex)
        => ex == null ? "" : $"{ex.GetType().Name}: {ex.Message}";
}

/// <summary>
/// OB2-compatible color constants for data.Logger.Log(msg, LogColors.X) calls.
/// Maps OB2 LogColors names to WPF Colors used by SilverBullet's log system.
/// </summary>
public static class LogColors
{
    public static System.Windows.Media.Color White      => System.Windows.Media.Colors.White;
    public static System.Windows.Media.Color Black      => System.Windows.Media.Colors.Black;
    public static System.Windows.Media.Color Red        => System.Windows.Media.Colors.Red;
    public static System.Windows.Media.Color DarkRed    => System.Windows.Media.Colors.DarkRed;
    public static System.Windows.Media.Color Green      => System.Windows.Media.Colors.Green;
    public static System.Windows.Media.Color LimeGreen  => System.Windows.Media.Colors.LimeGreen;
    public static System.Windows.Media.Color GreenYellow=> System.Windows.Media.Colors.GreenYellow;
    public static System.Windows.Media.Color Yellow     => System.Windows.Media.Colors.Yellow;
    public static System.Windows.Media.Color Orange     => System.Windows.Media.Colors.Orange;
    public static System.Windows.Media.Color OrangeRed  => System.Windows.Media.Colors.OrangeRed;
    public static System.Windows.Media.Color Tomato     => System.Windows.Media.Colors.Tomato;
    public static System.Windows.Media.Color Cyan       => System.Windows.Media.Colors.Cyan;
    public static System.Windows.Media.Color Blue       => System.Windows.Media.Colors.Blue;
    public static System.Windows.Media.Color DodgerBlue => System.Windows.Media.Colors.DodgerBlue;
    public static System.Windows.Media.Color LightBlue  => System.Windows.Media.Colors.LightBlue;
    public static System.Windows.Media.Color Purple     => System.Windows.Media.Colors.Purple;
    public static System.Windows.Media.Color Violet     => System.Windows.Media.Colors.Violet;
    public static System.Windows.Media.Color Magenta    => System.Windows.Media.Colors.Magenta;
    public static System.Windows.Media.Color Pink       => System.Windows.Media.Colors.Pink;
    public static System.Windows.Media.Color Gray       => System.Windows.Media.Colors.Gray;
    public static System.Windows.Media.Color Silver     => System.Windows.Media.Colors.Silver;
    public static System.Windows.Media.Color Transparent=> System.Windows.Media.Colors.Transparent;
}

/// <summary>
/// OB2-compatible logger shim: data.Logger.Log(message, LogColors.X).
/// </summary>
public sealed class Ob2Logger
{
    private readonly LoliCodeData _data;
    public Ob2Logger(LoliCodeData data) => _data = data;

    public void Log(string message, System.Windows.Media.Color color)
        => _data.Log(message, color);

    public void Log(string message)
        => _data.Log(message);

    public void Log(string message, object color)
        => _data.Log(message, color is System.Windows.Media.Color c ? c : System.Windows.Media.Colors.White);
}
