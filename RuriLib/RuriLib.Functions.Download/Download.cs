using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using RuriLib.Models;

namespace RuriLib.Functions.Download;

public static class Download
{
    public static void RemoteFile(string fileName, string url, bool useProxies, CProxy proxy,
        Dictionary<string, string> cookies, out Dictionary<string, string> newCookies,
        int timeout, string userAgent = "")
    {
        var handler = new SocketsHttpHandler
        {
            UseCookies = false,
            SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (_, _, _, _) => true
            }
        };
        if (useProxies && proxy != null)
            handler.Proxy = proxy.GetWebProxy();

        using var client = new HttpClient(handler);
        client.Timeout = System.TimeSpan.FromMilliseconds(timeout > 0 ? timeout : 30000);
        if (!string.IsNullOrEmpty(userAgent))
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
        if (cookies != null && cookies.Count > 0)
            client.DefaultRequestHeaders.TryAddWithoutValidation("Cookie",
                string.Join("; ", System.Linq.Enumerable.Select(cookies, kv => $"{kv.Key}={kv.Value}")));

        var response = client.GetAsync(url).GetAwaiter().GetResult();
        using var src = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
        using var dest = File.OpenWrite(fileName);
        src.CopyTo(dest);

        newCookies = new Dictionary<string, string>(cookies ?? new Dictionary<string, string>());
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var sc in setCookies)
            {
                var parts = sc.Split(';')[0].Split('=', 2);
                if (parts.Length == 2)
                    newCookies[parts[0].Trim()] = parts[1].Trim();
            }
        }
    }
}
