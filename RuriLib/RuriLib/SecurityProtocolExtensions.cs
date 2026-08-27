using System.Security.Authentication;
using RuriLib.Functions.Requests;

namespace RuriLib;

public static class SecurityProtocolExtensions
{
	public static SslProtocols ToSslProtocols(this SecurityProtocol protocol)
	{
		return protocol switch
		{
			SecurityProtocol.SystemDefault => SslProtocols.None,
			SecurityProtocol.SSL3          => SslProtocols.Ssl3,
			SecurityProtocol.TLS10         => SslProtocols.Tls,
			SecurityProtocol.TLS11         => SslProtocols.Tls11,
			SecurityProtocol.TLS12         => SslProtocols.Tls12,
			SecurityProtocol.TLS13         => SslProtocols.Tls13,
			_                              => SslProtocols.None,
		};
	}
}
