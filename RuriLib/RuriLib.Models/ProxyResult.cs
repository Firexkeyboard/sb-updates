namespace RuriLib.Models;

public struct ProxyResult
{
	public CProxy proxy;

	public bool working;

	public int ping;

	public string country;

	public ProxyResult(CProxy proxy, bool working, int ping, string country = "Unknown")
	{
		this.proxy = proxy;
		this.working = working;
		this.ping = ping;
		this.country = country;
	}
}
