using System;

namespace RuriLib.Functions.Conversion;

// OB2 compat: configs may reference RuriLib.Functions.Conversion.HexConverter.ToHexString(bytes)
public static class HexConverter
{
	public static string ToHexString(byte[] bytes) =>
		Convert.ToHexString(bytes).ToLowerInvariant();

	public static byte[] FromHexString(string hex) =>
		Convert.FromHexString(hex);
}
