using System;
using System.Text;

namespace RuriLib.Functions.Formats;

public static class Encode
{
	public static string ToBase64(this string plainText)
	{
		return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
	}

	public static string FromBase64(this string base64EncodedData)
	{
		string text = base64EncodedData.Replace(".", "");
		int num = text.Length % 4;
		if (num != 0)
		{
			text = text.PadRight(text.Length + (4 - num), '=');
		}
		byte[] bytes = Convert.FromBase64String(text);
		return Encoding.UTF8.GetString(bytes);
	}
}
