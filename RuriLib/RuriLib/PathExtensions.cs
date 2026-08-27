using System.IO;
using System.Text.RegularExpressions;

namespace RuriLib;

public static class PathExtensions
{
	public static string RemoveIllegalCharacters(this string illegal)
	{
		string str = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
		return new Regex($"[{Regex.Escape(str)}]").Replace(illegal, "");
	}

	public static string CreateFileName(this string fileName, string newFileName, bool removeIllegalCharacters = false)
	{
		if (removeIllegalCharacters)
		{
			newFileName = newFileName.RemoveIllegalCharacters();
		}
		string text = Path.GetDirectoryName(fileName);
		if (!text.EndsWith("\\"))
		{
			text += "\\";
		}
		return text + newFileName + Path.GetExtension(fileName);
	}

	public static string Rename(this string fullPath)
	{
		int num = 1;
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullPath);
		string extension = Path.GetExtension(fullPath);
		string directoryName = Path.GetDirectoryName(fullPath);
		string text = fullPath;
		while (File.Exists(text))
		{
			string text2 = $"{fileNameWithoutExtension}({num++})";
			text = Path.Combine(directoryName, text2 + extension);
		}
		return text;
	}
}
