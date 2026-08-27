using System.Collections.Generic;
using RuriLib.Models;
using RuriLib.Models.Stats;

namespace RuriLib.Interfaces;

public interface IProxyManager
{
	IEnumerable<CProxy> Proxies { get; }

	ProxyManagerStats Stats { get; }

	void Add(CProxy proxy);

	void AddRange(IEnumerable<CProxy> proxies);

	void Remove(CProxy proxy);

	void Remove(IEnumerable<CProxy> proxies);

	void Update(CProxy proxy);

	void RemoveAll();

	void RemoveNotWorking();

	void RemoveDuplicates();

	void RemoveUntested();
}
