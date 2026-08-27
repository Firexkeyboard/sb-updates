using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ImageProcessor.Common.Extensions;

public static class AssemblyExtensions
{
	public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
	{
		if (assembly == null)
		{
			throw new ArgumentNullException("assembly");
		}
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			return ex.Types.Where((Type t) => t != null);
		}
	}

	public static string GetResourceAsString(this Assembly assembly, string resource, Encoding encoding = null)
	{
		encoding = encoding ?? Encoding.UTF8;
		using MemoryStream memoryStream = new MemoryStream();
		using (Stream stream = assembly.GetManifestResourceStream(resource))
		{
			stream?.CopyTo(memoryStream);
		}
		return encoding.GetString(memoryStream.GetBuffer()).Replace('\0', ' ').Trim();
	}

	public static FileInfo GetAssemblyFile(this Assembly assembly)
	{
		return new FileInfo(new Uri(assembly.CodeBase).LocalPath);
	}

	public static FileInfo GetAssemblyFile(this AssemblyName assemblyName)
	{
		return new FileInfo(new Uri(assemblyName.CodeBase).LocalPath);
	}
}
