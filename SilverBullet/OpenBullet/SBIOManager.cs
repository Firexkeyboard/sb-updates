using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OpenBullet.ViewModels;
using RuriLib;

namespace OpenBullet;

public class SBIOManager
{
	private static JsonSerializerSettings settings = new JsonSerializerSettings
	{
		TypeNameHandling = (TypeNameHandling)3
	};

	public static void SaveSettings(string settingsFile, SBSettingsViewModel settings)
	{
		File.WriteAllText(settingsFile, JsonConvert.SerializeObject((object)settings, (Formatting)1));
	}

	public static SBSettingsViewModel LoadSettings(string settingsFile)
	{
		return JsonConvert.DeserializeObject<SBSettingsViewModel>(File.ReadAllText(settingsFile));
	}

	public static void CheckRequiredPlugins(IEnumerable<string> available, Config config)
	{
		string[] requiredPlugins = config.Settings.RequiredPlugins;
		foreach (string text in requiredPlugins)
		{
			if (!available.Contains(text))
			{
				throw new Exception("This config requires the plugin " + text + " which is missing from the Plugins folder and hence cannot be opened!");
			}
		}
	}
}
