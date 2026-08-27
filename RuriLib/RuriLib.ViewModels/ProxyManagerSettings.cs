using System.Collections.ObjectModel;

namespace RuriLib.ViewModels;

public class ProxyManagerSettings : ViewModelBase
{
	private ObservableCollection<string> proxySiteUrls = new ObservableCollection<string>();

	private ObservableCollection<string> proxyKeys = new ObservableCollection<string>();

	private string activeProxySiteUrl;

	private string activeProxyKey;

	public ObservableCollection<string> ProxySiteUrls
	{
		get
		{
			return proxySiteUrls;
		}
		set
		{
			proxySiteUrls = value;
			OnPropertyChanged("ProxySiteUrls");
		}
	}

	public ObservableCollection<string> ProxyKeys
	{
		get
		{
			return proxyKeys;
		}
		set
		{
			proxyKeys = value;
			OnPropertyChanged("ProxyKeys");
		}
	}

	public string ActiveProxySiteUrl
	{
		get
		{
			return activeProxySiteUrl;
		}
		set
		{
			activeProxySiteUrl = value;
			OnPropertyChanged("ActiveProxySiteUrl");
		}
	}

	public string ActiveProxyKey
	{
		get
		{
			return activeProxyKey;
		}
		set
		{
			activeProxyKey = value;
			OnPropertyChanged("ActiveProxyKey");
		}
	}
}
