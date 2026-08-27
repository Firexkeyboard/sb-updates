using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using OpenBullet.Repositories;
using RuriLib.Interfaces;
using RuriLib.Models;
using RuriLib.Models.Stats;
using RuriLib.ViewModels;

namespace OpenBullet.ViewModels;

public class ProxyManagerViewModel : ViewModelBase, IProxyManager, IProxyChecker
{
	private LiteDBRepository<CProxy> _repo;

	private ObservableCollection<CProxy> _proxiesCollection;

	public object totalLock = new object();

	private int total;

	public object testedLock = new object();

	private int tested;

	public object httpLock = new object();

	private int http;

	public object socks4Lock = new object();

	private int socks4;

	public object socks4aLock = new object();

	private int socks4a;

	public object socks5Lock = new object();

	private int socks5;

	public object chainLock = new object();

	private int chain;

	public object workingLock = new object();

	private int working;

	public object notWorkingLock = new object();

	private int notWorking;

	private int botsAmount = 1;

	private bool onlyUntested = false;

	private int timeout = 2;

	public static readonly int maximumBots = 200;

	public ObservableCollection<CProxy> ProxiesCollection
	{
		get
		{
			return _proxiesCollection;
		}
		private set
		{
			if (!object.Equals(_proxiesCollection, value))
			{
				_proxiesCollection = value;
				OnPropertyChanged("Proxies");
				OnPropertyChanged("ProxiesCollection");
				UpdateProperties();
			}
		}
	}

	public IEnumerable<CProxy> Proxies => ProxiesCollection;

	public int Total
	{
		get
		{
			return total;
		}
		set
		{
			if (total != value)
			{
				total = value;
				OnPropertyChanged("Stats");
				OnPropertyChanged("Total");
			}
		}
	}

	public int Tested
	{
		get
		{
			return tested;
		}
		set
		{
			if (tested != value)
			{
				tested = value;
				OnPropertyChanged("Stats");
				OnPropertyChanged("Tested");
			}
		}
	}

	public int Http
	{
		get
		{
			return http;
		}
		set
		{
			if (http != value)
			{
				http = value;
				OnPropertyChanged("Stats");
				OnPropertyChanged("Http");
			}
		}
	}

	public int Socks4
	{
		get
		{
			return socks4;
		}
		set
		{
			if (socks4 != value)
			{
				socks4 = value;
				OnPropertyChanged("Stats");
				OnPropertyChanged("Socks4");
			}
		}
	}

	public int Socks4a
	{
		get
		{
			return socks4a;
		}
		set
		{
			if (socks4a != value)
			{
				socks4a = value;
				OnPropertyChanged("Stats");
				OnPropertyChanged("Socks4a");
			}
		}
	}

	public int Socks5
	{
		get
		{
			return socks5;
		}
		set
		{
			if (socks5 != value)
			{
				socks5 = value;
				OnPropertyChanged("Stats");
				OnPropertyChanged("Socks5");
			}
		}
	}

	public int Chain
	{
		get
		{
			return chain;
		}
		set
		{
			if (chain != value)
			{
				chain = value;
				OnPropertyChanged("Chain");
			}
		}
	}

	public int Working
	{
		get
		{
			return working;
		}
		set
		{
			if (working != value)
			{
				working = value;
				OnPropertyChanged("Stats");
				OnPropertyChanged("Working");
			}
		}
	}

	public int NotWorking
	{
		get
		{
			return notWorking;
		}
		set
		{
			if (notWorking != value)
			{
				notWorking = value;
				OnPropertyChanged("NotWorking");
			}
		}
	}

	public ProxyManagerStats Stats => new ProxyManagerStats(Total, Tested, Working, Http, Socks4, Socks4a, Socks5);

	public int BotsAmount
	{
		get
		{
			return botsAmount;
		}
		set
		{
			if (botsAmount != value)
			{
				botsAmount = value;
				OnPropertyChanged("BotsAmount");
			}
		}
	}

	public string TestSite
	{
		get
		{
			return SB.Settings.ProxyManagerSettings.ActiveProxySiteUrl;
		}
		set
		{
			if (!string.Equals(TestSite, value, StringComparison.Ordinal))
			{
				SB.Settings.ProxyManagerSettings.ActiveProxySiteUrl = value;
				OnPropertyChanged("TestSite");
			}
		}
	}

	public string SuccessKey
	{
		get
		{
			return SB.Settings.ProxyManagerSettings.ActiveProxyKey;
		}
		set
		{
			if (!string.Equals(SuccessKey, value, StringComparison.Ordinal))
			{
				SB.Settings.ProxyManagerSettings.ActiveProxyKey = value;
				OnPropertyChanged("SuccessKey");
			}
		}
	}

	public bool OnlyUntested
	{
		get
		{
			return onlyUntested;
		}
		set
		{
			if (onlyUntested != value)
			{
				onlyUntested = value;
				OnPropertyChanged("OnlyUntested");
			}
		}
	}

	public int Timeout
	{
		get
		{
			return timeout;
		}
		set
		{
			if (timeout != value)
			{
				timeout = value;
				OnPropertyChanged("Timeout");
			}
		}
	}

	public ProxyManagerViewModel()
	{
		_repo = new LiteDBRepository<CProxy>(SB.dataBaseFile, "proxies");
		ProxiesCollection = new ObservableCollection<CProxy>();
	}

	public void UpdateProperties()
	{
		lock (totalLock)
		{
			Total = ProxiesCollection.Count();
		}
		lock (testedLock)
		{
			Tested = ProxiesCollection.Count((CProxy x) => (int)x.Working != 2);
		}
		lock (httpLock)
		{
			Http = ProxiesCollection.Count((CProxy x) => (int)x.Type == 0);
		}
		lock (socks4Lock)
		{
			Socks4 = ProxiesCollection.Count((CProxy x) => (int)x.Type == 1);
		}
		lock (socks4aLock)
		{
			Socks4a = ProxiesCollection.Count((CProxy x) => (int)x.Type == 2);
		}
		lock (socks5Lock)
		{
			Socks5 = ProxiesCollection.Count((CProxy x) => (int)x.Type == 3);
		}
		lock (chainLock)
		{
			Chain = ProxiesCollection.Count((CProxy x) => (int)x.Type == 4);
		}
		lock (workingLock)
		{
			Working = ProxiesCollection.Count((CProxy x) => (int)x.Working == 0);
		}
		lock (notWorkingLock)
		{
			NotWorking = ProxiesCollection.Count((CProxy x) => (int)x.Working == 1);
		}
	}

	public async Task CheckAllAsync(IEnumerable<CProxy> proxies, CancellationToken cancellationToken, Action<ProxyCheckResult<ProxyResult>> onResult = null, IProgress<float> progress = null)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return;
		}
		SemaphoreSlim ss = new SemaphoreSlim(BotsAmount, BotsAmount);
		try
		{
			int total = proxies.Count();
			int current = 0;
			IEnumerable<Task> tasks = proxies.Select((Func<CProxy, Task>)async delegate(CProxy proxy)
			{
				await ss.WaitAsync();
				ProxyCheckResult<ProxyResult> checkResult = default(ProxyCheckResult<ProxyResult>);
				ProxyResult proxyResult = default(ProxyResult);
				proxyResult.proxy = proxy;
				try
				{
					proxyResult = await CheckProxy(proxy);
					checkResult = new ProxyCheckResult<ProxyResult>(true, proxyResult, "");
				}
				catch (Exception ex)
				{
					checkResult = new ProxyCheckResult<ProxyResult>(false, proxyResult, ex.Message);
				}
				finally
				{
					onResult?.Invoke(checkResult);
					progress?.Report((float)(++current) / (float)total);
					ss.Release();
				}
			});
			await Task.WhenAny(Task.WhenAll(tasks), AsTask(cancellationToken));
			cancellationToken.ThrowIfCancellationRequested();
		}
		finally
		{
			if (ss != null)
			{
				((IDisposable)ss).Dispose();
			}
		}
	}

	public async Task<ProxyResult> CheckAsync(CProxy proxy, CancellationToken cancellationToken)
	{
		Task<ProxyResult> task = CheckProxy(proxy);
		await Task.WhenAny(task, AsTask(cancellationToken));
		cancellationToken.ThrowIfCancellationRequested();
		return task.Result;
	}

	private async Task<ProxyResult> CheckProxy(CProxy proxy)
	{
		ProxyResult result = default(ProxyResult);
		result.proxy = proxy;
		Stopwatch sw = new Stopwatch();
		sw.Start();
		result.working = await CheckWorking(proxy);
		sw.Stop();
		result.ping = (int)sw.ElapsedMilliseconds;
		try
		{
			result.country = await CheckCountry(proxy);
		}
		catch
		{
		}
		// Si el check principal falló pero el proxy sí enrutó tráfico (detectó país),
		// el proxy está vivo — posiblemente el test site lo bloqueó o el timeout era muy justo.
		if (!result.working && !string.IsNullOrEmpty(result.country) && result.country != "Unknown")
			result.working = true;
		return result;
	}

	private async Task<bool> CheckWorking(CProxy proxy)
	{
		int timeout = Timeout * 1000;
		var handler = new SocketsHttpHandler
		{
			UseProxy       = true,
			Proxy          = proxy.GetWebProxy(),
			ConnectTimeout = System.TimeSpan.FromMilliseconds(timeout),
			SslOptions = new System.Net.Security.SslClientAuthenticationOptions
			{
				RemoteCertificateValidationCallback = (_, _, _, _) => true
			}
		};
		using var client = new HttpClient(handler) { Timeout = System.TimeSpan.FromMilliseconds(timeout) };
		try
		{
			var response = await client.GetAsync(TestSite);
				if (!response.IsSuccessStatusCode) return false;
				if (string.IsNullOrEmpty(SuccessKey)) return true;
				string result = await response.Content.ReadAsStringAsync();
				return result.Contains(SuccessKey);
		}
		catch { return false; }
	}

	private async Task<string> CheckCountry(CProxy proxy)
	{
		// Route through the proxy so ip-api.com sees the actual exit IP, not the gateway hostname.
		// For rotating/residential proxies the gateway resolves to different IPs on each lookup,
		// so querying by hostname without the proxy gives wrong or inconsistent countries.
		var handler = new SocketsHttpHandler
		{
			UseProxy = true,
			Proxy    = proxy.GetWebProxy(),
			ConnectTimeout = System.TimeSpan.FromSeconds(Timeout),
			SslOptions = new System.Net.Security.SslClientAuthenticationOptions
			{
				RemoteCertificateValidationCallback = (_, _, _, _) => true
			}
		};
		using var client = new HttpClient(handler) { Timeout = System.TimeSpan.FromSeconds(Timeout) };
		try
		{
			// No IP in the URL → ip-api returns data for the connection's source IP (the proxy exit)
			JObject val = JObject.Parse(await client.GetStringAsync("http://ip-api.com/json/"));
			if (((JToken)val).Value<string>((object)"status") == "success")
				return ((JToken)val).Value<string>((object)"country");
			return "Unknown";
		}
		catch { return "Unknown"; }
	}

	public static Task AsTask(CancellationToken cancellationToken)
	{
		TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
		cancellationToken.Register(delegate
		{
			tcs.TrySetCanceled();
		}, useSynchronizationContext: false);
		return tcs.Task;
	}

	public void Add(CProxy proxy)
	{
		ProxiesCollection.Add(proxy);
		_repo.Add(proxy);
	}

	public void AddRange(IEnumerable<CProxy> proxies)
	{
		foreach (CProxy proxy in proxies)
		{
			ProxiesCollection.Add(proxy);
		}
		_repo.Add(proxies);
	}

	public void RefreshList()
	{
		ProxiesCollection = new ObservableCollection<CProxy>(_repo.Get());
	}

	public void Update(CProxy proxy)
	{
		_repo.Update(proxy);
	}

	public void Remove(CProxy proxy)
	{
		ProxiesCollection.Remove(proxy);
		_repo.Remove(proxy);
	}

	public void Remove(IEnumerable<CProxy> proxies)
	{
		CProxy[] array = proxies.ToArray();
		CProxy[] array2 = array;
		foreach (CProxy item in array2)
		{
			ProxiesCollection.Remove(item);
		}
		_repo.Remove((IEnumerable<CProxy>)array);
	}

	public void RemoveAll()
	{
		ProxiesCollection.Clear();
		_repo.RemoveAll();
	}

	public void RemoveNotWorking()
	{
		Remove(Proxies.Where((CProxy p) => (int)p.Working == 1));
	}

	public void RemoveDuplicates()
	{
		IEnumerable<CProxy> proxies = (from p in Proxies
			group p by p.Proxy into g
			where g.Count() > 1
			select g).SelectMany((IGrouping<string, CProxy> g) => g.OrderBy((CProxy p) => p.Proxy).Reverse().Skip(1));
		Remove(proxies);
	}

	public void RemoveUntested()
	{
		Remove(Proxies.Where((CProxy p) => (int)p.Working == 2));
	}
}



