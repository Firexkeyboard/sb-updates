using System;

namespace RuriLib;

public static class SizeExtensions
{
	private static readonly string[] SizeSuffixes = new string[2] { "MB", "GB" };

	public static string SizeSuffix(long value, int decimalPlaces = 0)
	{
		if (value < 0)
		{
			throw new ArgumentException("Bytes should not be negative", "value");
		}
		int num = (int)Math.Max(0.0, Math.Log(value, 1024.0));
		double num2 = Math.Round((double)value / Math.Pow(1024.0, num), decimalPlaces);
		return $"{num2} {SizeSuffixes[num]}";
	}
}
