using System;
using System.Linq;

namespace RuriLib;

public static class ScriptExtension
{
	public static bool HasBlock(string script, string blockName, bool contains = false)
	{
		if (string.IsNullOrWhiteSpace(script) || string.IsNullOrWhiteSpace(blockName))
		{
			return false;
		}
		if (contains)
		{
			return script.Contains(blockName);
		}
		try
		{
			return script.Split(new char[1] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Any((string s) => s.TrySpaceSplit(s.StartsWith("#") ? 1 : 0) == blockName);
		}
		catch
		{
		}
		return false;
	}

	public static bool HasScript(string script, string code, bool contains = false)
	{
		if (string.IsNullOrWhiteSpace(script) || string.IsNullOrWhiteSpace(code))
		{
			return false;
		}
		if (contains)
		{
			return script.Contains(code);
		}
		try
		{
			return script.Split(new char[1] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Any((string s) => s.TrySpaceSplit(s.StartsWith("#") ? 3 : 2) == code);
		}
		catch
		{
		}
		return false;
	}
}
