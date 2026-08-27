using System;
using System.Linq;
using System.Text;

namespace RuriLib.Functions.Conversions;

public static class Conversion
{
	public static byte[] ConvertFrom(this string input, Encoding encoding)
	{
		switch (encoding)
		{
		case Encoding.BASE64:
			return Convert.FromBase64String(input);
		case Encoding.HEX:
			input = new string((from c in input.ToCharArray()
				where !char.IsWhiteSpace(c)
				select c).ToArray()).Replace("0x", "");
			if (input.Length % 2 != 0) input = "0" + input;
			return (from x in Enumerable.Range(0, input.Length)
				where x % 2 == 0
				select Convert.ToByte(input.Substring(x, 2), 16)).ToArray();
		case Encoding.BIN:
		{
			int num = input.Length / 8;
			byte[] array = new byte[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = Convert.ToByte(input.Substring(8 * i, 8), 2);
			}
			return array;
		}
		case Encoding.ASCII:
			return System.Text.Encoding.ASCII.GetBytes(input);
		case Encoding.UTF8:
			return System.Text.Encoding.UTF8.GetBytes(input);
		case Encoding.UNICODE:
			return System.Text.Encoding.Unicode.GetBytes(input);
		default:
			return new byte[0];
		}
	}

	public static string ConvertTo(this byte[] input, Encoding encoding)
	{
		StringBuilder stringBuilder = new StringBuilder();
		switch (encoding)
		{
		case Encoding.BASE64:
			return Convert.ToBase64String(input);
		case Encoding.HEX:
			foreach (byte b2 in input)
			{
				stringBuilder.AppendFormat("{0:x2}", b2);
			}
			return stringBuilder.ToString().ToUpper();
		case Encoding.BIN:
			return string.Concat(input.Select((byte b) => Convert.ToString(b, 2).PadLeft(8, '0')));
		case Encoding.ASCII:
			return System.Text.Encoding.ASCII.GetString(input);
		case Encoding.UTF8:
			return System.Text.Encoding.UTF8.GetString(input);
		case Encoding.UNICODE:
			return System.Text.Encoding.Unicode.GetString(input);
		default:
			return "";
		}
	}
}
