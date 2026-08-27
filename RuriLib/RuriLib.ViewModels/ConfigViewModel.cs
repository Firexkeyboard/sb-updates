namespace RuriLib.ViewModels;

public class ConfigViewModel : ViewModelBase
{
	private string category = "Default";

	private string fileName = "";

	public Config Config { get; set; }

	public string Category
	{
		get
		{
			return category;
		}
		set
		{
			category = value;
			OnPropertyChanged("Category");
		}
	}

	public string FileName
	{
		get
		{
			return fileName;
		}
		set
		{
			fileName = value;
			OnPropertyChanged("FileName");
		}
	}

	public string Path { get; set; }

	public bool Remote { get; set; }

	public string Name => Config.Settings.Name;

	public string FileType => System.IO.Path.GetExtension(Path)?.TrimStart('.').ToUpperInvariant() ?? "";

	public ConfigViewModel(string path, string fileName, string category, Config config, bool remote = false)
	{
		FileName = fileName;
		Category = category;
		Config = config;
		Remote = remote;
		Path = path;
	}
}
