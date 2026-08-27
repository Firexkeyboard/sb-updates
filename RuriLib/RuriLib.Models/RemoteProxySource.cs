using Newtonsoft.Json;
using RuriLib.Models;

namespace RuriLib.Models;

public class RemoteProxySource
{
	public int Id { get; set; }

	public bool Active { get; set; } = true;

	public string Url { get; set; } = "";

	public ProxyType Type { get; set; }

	[JsonIgnore]
	public bool TypeInitialized { get; set; }

	public string Pattern { get; set; } = "([0-9]+\\.[0-9]+\\.[0-9]+\\.[0-9]+:[0-9]+)";

	public string Output { get; set; } = "[1]";

	public RemoteProxySource(int id)
	{
		Id = id;
	}
}
