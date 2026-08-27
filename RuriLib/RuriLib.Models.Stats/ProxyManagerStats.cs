namespace RuriLib.Models.Stats;

public struct ProxyManagerStats
{
	public int Total { get; }

	public int Tested { get; }

	public int Working { get; }

	public int Http { get; }

	public int Socks4 { get; }

	public int Socks4a { get; }

	public int Socks5 { get; }

	public ProxyManagerStats(int total, int tested, int working, int http, int socks4, int socks4a, int socks5)
	{
		Total = total;
		Tested = tested;
		Working = working;
		Http = http;
		Socks4 = socks4;
		Socks4a = socks4a;
		Socks5 = socks5;
	}
}
