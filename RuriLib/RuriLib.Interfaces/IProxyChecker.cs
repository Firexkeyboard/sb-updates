using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RuriLib.Models;

namespace RuriLib.Interfaces;

public interface IProxyChecker
{
	string TestSite { get; set; }

	string SuccessKey { get; set; }

	bool OnlyUntested { get; set; }

	int Timeout { get; set; }

	int BotsAmount { get; set; }

	Task CheckAllAsync(IEnumerable<CProxy> proxies, CancellationToken cancellationToken, Action<ProxyCheckResult<ProxyResult>> onResult = null, IProgress<float> progress = null);

	Task<ProxyResult> CheckAsync(CProxy proxy, CancellationToken cancellationToken);
}
