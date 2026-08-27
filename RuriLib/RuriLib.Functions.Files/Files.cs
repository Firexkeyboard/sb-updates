using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using OpenQA.Selenium;

namespace RuriLib.Functions.Files;

public static class Files
{
	public static void SaveScreenshot(Screenshot screenshot, BotData data)
	{
		string text = MakeScreenshotPath(data);
		data.Screenshots.Add(text);
		screenshot.SaveAsFile(text);
	}

	public static void SaveScreenshot(Bitmap screenshot, BotData data)
	{
		string text = MakeScreenshotPath(data);
		data.Screenshots.Add(text);
		screenshot.Save(text);
	}

	public static void SaveScreenshot(MemoryStream stream, BotData data)
	{
		string text = MakeScreenshotPath(data);
		using (FileStream destination = File.Create(text))
		{
			stream.CopyTo(destination);
		}
		data.Screenshots.Add(text);
	}

	private static string MakeScreenshotPath(BotData data)
	{
		string text = MakeValidFileName(data.ConfigSettings.Name);
		string fileName = MakeValidFileName(data.Data.Data);
		if (!Directory.Exists("UserData\\Screenshots\\" + text))
		{
			Directory.CreateDirectory("UserData\\Screenshots\\" + text);
		}
		string firstAvailableFileName = GetFirstAvailableFileName("UserData\\Screenshots\\" + text + "\\", fileName, "bmp");
		return "UserData\\Screenshots\\" + text + "\\" + firstAvailableFileName;
	}

	public static string GetFirstAvailableFileName(string basePath, string fileName, string extension)
	{
		int num = 1;
		while (File.Exists(basePath + fileName + num + "." + extension))
		{
			num++;
		}
		return fileName + num + "." + extension;
	}

	public static string MakeValidFileName(string name, bool underscore = true)
	{
		string arg = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
		string pattern = string.Format("([{0}]*\\.+$)|([{0}]+)", arg);
		return Regex.Replace(name, pattern, underscore ? "_" : "");
	}

	public static void ThrowIfNotInCWD(string path)
	{
		if (!path.IsSubPathOf(Directory.GetCurrentDirectory()))
		{
			throw new UnauthorizedAccessException("For security reasons, you cannot interact with paths outside of the current working directory");
		}
	}

	public static void CreatePath(string file)
	{
		string directoryName = Path.GetDirectoryName(file);
		if (!string.IsNullOrWhiteSpace(directoryName) && !Directory.Exists(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
	}
}
