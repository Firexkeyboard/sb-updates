using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using ImageProcessor.Common.Extensions;

namespace ImageProcessor.Common.Helpers;

internal static class IOHelper
{
	private static string rootDirectory;

	public static string MapPath(string virtualPath)
	{
		if ((virtualPath.Length >= 2 && virtualPath[1] == Path.VolumeSeparatorChar) || virtualPath.StartsWith("\\\\"))
		{
			return virtualPath;
		}
		char directorySeparatorChar = Path.DirectorySeparatorChar;
		string rootDirectorySafe = GetRootDirectorySafe();
		string text = virtualPath.TrimStart('~', '/').Replace('/', directorySeparatorChar);
		return rootDirectorySafe + directorySeparatorChar.ToString(CultureInfo.InvariantCulture) + text;
	}

	public static string GetRootDirectoryBinFolder()
	{
		string result = string.Empty;
		if (string.IsNullOrEmpty(rootDirectory))
		{
			DirectoryInfo directory = Assembly.GetExecutingAssembly().GetAssemblyFile().Directory;
			if (directory != null)
			{
				result = directory.FullName;
			}
			return result;
		}
		result = Path.Combine(GetRootDirectorySafe(), "bin");
		string text = Path.Combine(result, "release");
		if (Directory.Exists(text))
		{
			return text;
		}
		if (Directory.Exists(result))
		{
			return result;
		}
		return rootDirectory;
	}

	internal static string GetRootDirectorySafe()
	{
		if (!string.IsNullOrEmpty(rootDirectory))
		{
			return rootDirectory;
		}
		string directoryName = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
		if (string.IsNullOrEmpty(directoryName))
		{
			throw new Exception("No root directory could be resolved. Please ensure that your solution is correctly configured.");
		}
		rootDirectory = (directoryName.Contains("bin") ? directoryName.Substring(0, directoryName.LastIndexOf("bin", StringComparison.OrdinalIgnoreCase) - 1) : directoryName);
		return rootDirectory;
	}
}
