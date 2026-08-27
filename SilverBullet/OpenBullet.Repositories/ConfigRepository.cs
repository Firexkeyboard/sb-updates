using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using RuriLib;
using RuriLib.Interfaces;
using RuriLib.ViewModels;

namespace OpenBullet.Repositories;

public class ConfigRepository : IRepository<ConfigViewModel, (string, string)>
{
	public static string defaultCategory = "Default";

	private const string Suffix = "svb";

	public string BaseFolder { get; set; }

	public ConfigRepository(string baseFolder)
	{
		BaseFolder = baseFolder;
	}

	public void Add(ConfigViewModel entity)
	{
		string path = GetPath(entity);
		if (entity.Category != defaultCategory && !Directory.Exists(entity.Category))
		{
			Directory.CreateDirectory(Path.Combine(BaseFolder, entity.Category));
		}
		if (!File.Exists(path))
		{
			IOManager.SaveConfig(entity.Config, path);
			return;
		}
		throw new IOException("A config with the same name and category already exists");
	}

	public IEnumerable<ConfigViewModel> Get()
	{
		List<ConfigViewModel> list = new List<ConfigViewModel>();
		bool loliX = false;
		foreach (string item in from file in Directory.EnumerateFiles(SB.configFolder)
			where file.EndsWith(".svb") || file.EndsWith(".loli") || file.EndsWith(".anom") || file.EndsWith(".opk") || (loliX = file.EndsWith(".loliX"))
			select file)
		{
			try
			{
				Config cfg = loliX ? IOManager.LoadConfig(item, true) : LoadConfigRobust(item);
				list.Add(new ConfigViewModel(item, Path.GetFileNameWithoutExtension(item), defaultCategory, cfg, false));
				if (loliX)
				{
					FileInfo fileInfo = new FileInfo("Configs\\" + Path.GetFileName(item));
					IOManager.SaveConfig(list.Last().Config, "Configs\\" + Path.GetFileNameWithoutExtension(item) + ".svb");
					fileInfo.Delete();
				}
				loliX = false;
			}
			catch
			{
				loliX = false;
			}
		}
		foreach (string item2 in Directory.EnumerateDirectories(SB.configFolder))
		{
			foreach (string item3 in from file in Directory.EnumerateFiles(item2)
				where file.EndsWith(".loli") || file.EndsWith(".svb") || file.EndsWith(".anom") || file.EndsWith(".opk")
				select file)
			{
				try
				{
					list.Add(new ConfigViewModel(item3, Path.GetFileNameWithoutExtension(item3), Path.GetFileName(item2), LoadConfigRobust(item3), false));
				}
				catch
				{
				}
			}
		}
		return list;
	}

	public ConfigViewModel Get((string, string) id)
	{
		string item = id.Item1;
		string item2 = id.Item2;
		string path = GetPath(item, item2, string.Empty);
		return new ConfigViewModel(path, item2, item, IOManager.LoadConfig(path, false), false);
	}

	public void Remove(ConfigViewModel entity)
	{
		string path = GetPath(entity.Category, entity.FileName, Path.GetExtension(entity.Path));
		if (File.Exists(path))
		{
			File.Delete(path);
			return;
		}
		throw new IOException("File not found");
	}

	public void RemoveAll()
	{
		Directory.Delete(BaseFolder, recursive: true);
		Directory.CreateDirectory(BaseFolder);
	}

	public void Update(ConfigViewModel entity)
	{
		string text = GetPath(entity);
		if (!File.Exists(text))
		{
			text = PathExtensions.CreateFileName(text, entity.Name, true);
			using (File.Create(text))
			{
			}
		}
		IOManager.SaveConfig(entity.Config, text);
	}

	public void Add(IEnumerable<ConfigViewModel> entities)
	{
		foreach (ConfigViewModel entity in entities)
		{
			Add(entity);
		}
	}

	public void Remove(IEnumerable<ConfigViewModel> entities)
	{
		foreach (ConfigViewModel entity in entities)
		{
			Remove(entity);
		}
	}

	public void Update(IEnumerable<ConfigViewModel> entities)
	{
		foreach (ConfigViewModel entity in entities)
		{
			Update(entity);
		}
	}

	private string GetPath(ConfigViewModel config)
	{
		// Preserve the original extension so saving an OPK doesn't create a duplicate SVB file.
		string ext = Path.GetExtension(config.Path);
		return GetPath(config.Category, config.FileName, string.IsNullOrEmpty(ext) ? string.Empty : ext);
	}

	private string GetPath(string category, string fileName, string suffix)
	{
		string text = fileName + "." + (string.IsNullOrEmpty(suffix) ? "svb" : suffix.Remove(0, 1));
		if (category != defaultCategory)
		{
			return Path.Combine(BaseFolder, category, text);
		}
		return Path.Combine(BaseFolder, text);
	}

	// Tries standard format → loliX encrypted → OB2 ZIP pack
	private static Config LoadConfigRobust(string path)
	{
		try { return IOManager.LoadConfig(path, false); } catch { }
		try { return IOManager.LoadConfig(path, true); } catch { }
		try { return LoadFromZipPack(path); } catch { }
		throw new Exception($"Unrecognized config format: {Path.GetFileName(path)}");
	}

	private static Config LoadFromZipPack(string path)
	{
		using var archive = ZipFile.OpenRead(path);

		string script = "";
		foreach (string ext in new[] { ".cs", ".loli", ".ls", ".txt" })
		{
			var entry = archive.Entries.FirstOrDefault(e =>
				e.FullName.EndsWith(ext, System.StringComparison.OrdinalIgnoreCase));
			if (entry != null)
			{
				using var reader = new System.IO.StreamReader(entry.Open());
				script = reader.ReadToEnd();
				break;
			}
		}

		ConfigSettings settings = new ConfigSettings { Name = Path.GetFileNameWithoutExtension(path) };
		var settingsEntry = archive.Entries.FirstOrDefault(e =>
			e.Name.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase));
		if (settingsEntry != null)
		{
			try
			{
				using var reader = new System.IO.StreamReader(settingsEntry.Open());
				settings = IOManager.DeserializeObject<ConfigSettings>(reader.ReadToEnd()) ?? settings;
				if (string.IsNullOrEmpty(settings.Name))
					settings.Name = Path.GetFileNameWithoutExtension(path);
			}
			catch { }
		}

		return new Config(settings, script);
	}
}
