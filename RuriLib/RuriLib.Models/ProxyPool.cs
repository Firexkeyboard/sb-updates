using System;
using System.Collections.Generic;
using System.Linq;
using RuriLib.Models;

namespace RuriLib.Models;

public class ProxyPool
{
	private static readonly Random rng = new Random();
	private static readonly object _rngLock = new object();

	public List<CProxy> Proxies { get; } = new List<CProxy>();

	public List<CProxy> Alive => Proxies.Where((CProxy p) => p.Status == Status.AVAILABLE || p.Status == Status.BUSY).ToList();

	public List<CProxy> Available => Proxies.Where((CProxy p) => p.Status == Status.AVAILABLE).ToList();

	public List<CProxy> Banned => Proxies.Where((CProxy p) => p.Status == Status.BANNED).ToList();

	public List<CProxy> Bad => Proxies.Where((CProxy p) => p.Status == Status.BAD).ToList();

	private readonly object _proxyLock = new object();

	public ProxyPool(IEnumerable<string> proxies, ProxyType type, bool shuffle)
	{
		Proxies = proxies.Select((string p) => new CProxy(p, type)).ToList();
		if (shuffle)
		{
			Shuffle(Proxies);
		}
	}

	public ProxyPool(List<CProxy> proxies, bool shuffle = false)
	{
		Proxies = IOManager.CloneProxies(proxies);
		UnbanAll();
		if (shuffle)
		{
			Shuffle(Proxies);
		}
	}

	public void ClearCF()
	{
		foreach (CProxy proxy in Proxies)
		{
			proxy.Clearance = "";
			proxy.Cfduid = "";
		}
	}

	public void UnbanAll()
	{
		foreach (CProxy proxy in Proxies)
		{
			if (proxy.Status == Status.BANNED || proxy.Status == Status.BAD)
			{
				proxy.Status = Status.AVAILABLE;
				proxy.Hooked = 0;
				proxy.Uses = 0;
			}
		}
		ClearCF();
	}

	public CProxy GetProxy(bool evenBusy, int maxUses, bool neverBan = false)
	{
		lock (_proxyLock)
		{
			CProxy cProxy = (evenBusy ? ((maxUses != 0) ? Alive.Where((CProxy p) => p.Uses < maxUses).FirstOrDefault() : Alive.FirstOrDefault()) : ((maxUses != 0) ? Available.Where((CProxy p) => p.Uses < maxUses).FirstOrDefault() : Available.FirstOrDefault()));
			if (cProxy != null)
			{
				if (maxUses > 0 && cProxy.Uses > maxUses && !neverBan)
				{
					cProxy.Status = Status.BANNED;
					return null;
				}
				cProxy.Status = Status.BUSY;
				cProxy.Hooked++;
			}
			return cProxy;
		}
	}

	public void RemoveDuplicates()
	{
		foreach (CProxy item in from p in Proxies
			group p by p.Proxy into grp
			where grp.Count() > 1
			select grp.First())
		{
			Proxies.Remove(item);
		}
	}

	private static void Shuffle<T>(IList<T> list)
	{
		int num = list.Count;
		while (num > 1)
		{
			num--;
			int index;
			lock (_rngLock) { index = rng.Next(num + 1); }
			T value = list[index];
			list[index] = list[num];
			list[num] = value;
		}
	}
}
