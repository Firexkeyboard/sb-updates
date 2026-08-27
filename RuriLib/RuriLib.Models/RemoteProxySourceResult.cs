using System.Collections.Generic;

namespace RuriLib.Models;

public class RemoteProxySourceResult
{
	public bool Successful { get; set; }

	public string Error { get; set; }

	public string Url { get; set; }

	public List<CProxy> Proxies { get; set; }
}
