using System.Collections.Generic;

namespace RuriLib.ViewModels;

public class SettingsCefSharp : ViewModelBase
{
	private bool packLoadingDisabled;

	private bool ignoreCertificateErrors = true;

	private string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.89 Safari/537.36";

	public bool PackLoadingDisabled
	{
		get
		{
			return packLoadingDisabled;
		}
		set
		{
			packLoadingDisabled = value;
			OnPropertyChanged("PackLoadingDisabled");
		}
	}

	public bool IgnoreCertificateErrors
	{
		get
		{
			return ignoreCertificateErrors;
		}
		set
		{
			ignoreCertificateErrors = value;
			OnPropertyChanged("IgnoreCertificateErrors");
		}
	}

	public string UserAgent
	{
		get
		{
			return userAgent;
		}
		set
		{
			userAgent = value;
			OnPropertyChanged("UserAgent");
		}
	}

	public Dictionary<string, string> CmdLineArgs { get; }
}
