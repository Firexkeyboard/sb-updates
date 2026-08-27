using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace RuriLib.Models;

public class EnvironmentSettings
{
	public List<WordlistType> WordlistTypes { get; set; } = new List<WordlistType>();

	public List<CustomKeychain> CustomKeychains { get; set; } = new List<CustomKeychain>();

	public List<ExportFormat> ExportFormats { get; set; } = new List<ExportFormat>();

	// Built-in pseudo-custom keychains always available regardless of Environment.ini
	private static readonly List<CustomKeychain> _builtinKeychains = new List<CustomKeychain>
	{
		new CustomKeychain { Name = "FREE",    Color = Colors.DodgerBlue  },
		new CustomKeychain { Name = "2FACTOR", Color = Colors.MediumOrchid },
		new CustomKeychain { Name = "EXPIRED", Color = Colors.DarkOrange   },
		new CustomKeychain { Name = "CUSTOM",  Color = Colors.OrangeRed    },
	};

	public List<string> GetWordlistTypeNames()
	{
		return WordlistTypes.Select((WordlistType w) => w.Name).ToList();
	}

	public string RecognizeWordlistType(string data)
	{
		foreach (WordlistType wordlistType in WordlistTypes)
		{
			if (Regex.Match(data, wordlistType.Regex).Success)
			{
				return wordlistType.Name;
			}
		}
		return WordlistTypes.First().Name;
	}

	public List<string> GetCustomKeychainNames()
	{
		// Built-ins first, then any user-defined ones that aren't already listed
		var builtinNames = _builtinKeychains.Select(c => c.Name).ToList();
		var userNames = CustomKeychains
			.Select(c => c.Name)
			.Where(n => !builtinNames.Contains(n, StringComparer.OrdinalIgnoreCase))
			.ToList();
		return builtinNames.Concat(userNames).ToList();
	}

	public CustomKeychain GetCustomKeychain(string name)
	{
		// Check built-ins first
		var builtin = _builtinKeychains.FirstOrDefault(
			c => string.Equals(c.Name, name, System.StringComparison.OrdinalIgnoreCase));
		if (builtin != null) return builtin;
		try
		{
			return CustomKeychains.First((CustomKeychain w) => w.Name == name);
		}
		catch
		{
			return new CustomKeychain();
		}
	}

	public WordlistType GetWordlistType(string name)
	{
		try
		{
			return WordlistTypes.FirstOrDefault((WordlistType w) => w.Name == name);
		}
		catch
		{
			return new WordlistType();
		}
	}
}
