using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RuriLib;

public static class StringExtensions
{
	public static string Unescape(this string str, bool useEnvNewLine = false)
	{
		str = Regex.Replace(str, "(?<!\\\\)\\\\r\\\\n", useEnvNewLine ? Environment.NewLine : "\r\n");
		str = Regex.Replace(str, "(?<!\\\\)\\\\n", useEnvNewLine ? Environment.NewLine : "\n");
		str = Regex.Replace(str, "(?<!\\\\)\\\\t", "\t");
		return new StringBuilder(str).Replace("\\\\r\\\\n", "\\r\\n").Replace("\\\\n", "\\n").Replace("\\\\t", "\\t")
			.ToString();
	}

	public static bool IsSubPathOf(this string path, string baseDirPath)
	{
		string fullPath = Path.GetFullPath(path.Replace('/', '\\').WithEnding("\\"));
		string fullPath2 = Path.GetFullPath(baseDirPath.Replace('/', '\\').WithEnding("\\"));
		return fullPath.StartsWith(fullPath2, StringComparison.OrdinalIgnoreCase);
	}

	public static string WithEnding(this string str, string ending)
	{
		if (str == null)
		{
			return ending;
		}
		for (int i = 0; i <= ending.Length; i++)
		{
			string text = str + ending.Right(i);
			if (text.EndsWith(ending))
			{
				return text;
			}
		}
		return str;
	}

	public static string Right(this string value, int length)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		if (length < 0)
		{
			throw new ArgumentOutOfRangeException("length", length, "Length is less than zero");
		}
		if (length >= value.Length)
		{
			return value;
		}
		return value.Substring(value.Length - length);
	}

	public static bool IsUrl(this string str)
	{
		if (Uri.TryCreate(str, UriKind.Absolute, out var result))
		{
			if (!(result.Scheme == Uri.UriSchemeHttp))
			{
				return result.Scheme == Uri.UriSchemeHttps;
			}
			return true;
		}
		return false;
	}

	public static string TrySpaceSplit(this string value, int returnIndex = 0)
	{
		try
		{
			return value.Split(' ')[returnIndex];
		}
		catch
		{
			return value;
		}
	}

	public static string DoFormat(this double myNumber)
	{
		string text = $"{myNumber:0.00}";
		if (text.EndsWith("00"))
		{
			return ((int)myNumber).ToString();
		}
		return text;
	}

	public static byte[] ConvertToByteArray(this string input)
	{
		return input.Select(Convert.ToByte).ToArray();
	}

	public static string ConvertToString(this byte[] bytes)
	{
		return new string(bytes.Select(Convert.ToChar).ToArray());
	}

	public static string GetUntilOrEmpty(this string text, string stopAt)
	{
		if (!string.IsNullOrWhiteSpace(text))
		{
			int num = text.IndexOf(stopAt, StringComparison.Ordinal);
			if (num > 0)
			{
				return text.Substring(0, num);
			}
		}
		return string.Empty;
	}
}
