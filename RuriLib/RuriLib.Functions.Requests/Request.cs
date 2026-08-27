using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using RuriLib.Functions.Conversions;
using RuriLib.Functions.Files;
using RuriLib.Functions.Formats;
using RuriLib.Models;
using RuriLib.ViewModels;

namespace RuriLib.Functions.Requests;

public class Request : IDisposable
{
    // ── RuriLibHttp: shared handler pool keyed by (proxy, redirect, encoding, ssl, protocol) ─
    private static readonly ConcurrentDictionary<string, SocketsHttpHandler> _handlerPool = new();

    // Request configuration gathered via Setup / Set* fluent methods
    private CProxy _proxy;
    private Dictionary<string, string> _headers = new();
    private System.Net.Http.HttpContent _content;
    private Dictionary<string, string> _cookies = new();
    private string _contentType = "";
    private string _authorization = "";
    private int _timeout = 60000;
    private bool _autoRedirect = true;
    private int _maxRedirects = 8;
    private bool _acceptEncoding = true;
    private SslProtocols _sslProtocols = SslProtocols.None;
    private Version _protocolVersion = null;

    // New options
    private HttpLibrary _httpLibrary = HttpLibrary.SystemNet;
    private bool _ignoreCertValidation = true;
    private bool _alwaysSendContent = false;
    private bool _allowEmptyHeaderValues = false;
    private string _codePagesEncoding = "";
    private CurlImpersonateBrowserProfile _curlProfile = CurlImpersonateBrowserProfile.Chrome142;

    // Last response state
    private HttpResponseMessage _response;
    private MemoryStream _memoryStream;
    private bool _hasContentLength;
    private byte[] _rawResponseBytes; // raw body bytes before any string conversion

    public static bool CanContainRequestBody(HttpMethod method)
        => method == HttpMethod.POST || method == HttpMethod.PUT || method == HttpMethod.PATCH;

    public Request Setup(RLSettingsViewModel settings = null,
        SecurityProtocol securityProtocol = SecurityProtocol.SystemDefault,
        bool autoRedirect = true, int maxRedirects = 8,
        bool acceptEncoding = true, Version protocolVersion = null,
        bool allowEmptyHeaderValues = false,
        HttpLibrary httpLibrary = HttpLibrary.SystemNet,
        bool ignoreCertValidation = true,
        bool alwaysSendContent = false,
        string codePagesEncoding = "",
        int timeoutOverrideMs = 0,
        CurlImpersonateBrowserProfile curlProfile = CurlImpersonateBrowserProfile.Chrome142)
    {
        if (settings != null)
            _timeout = settings.General.RequestTimeout * 1000;
        if (timeoutOverrideMs > 0)
            _timeout = timeoutOverrideMs;
        _autoRedirect = autoRedirect;
        _maxRedirects = maxRedirects;
        _acceptEncoding = acceptEncoding;
        _protocolVersion = protocolVersion;
        _sslProtocols = securityProtocol.ToSslProtocols();
        _httpLibrary = httpLibrary;
        _ignoreCertValidation = ignoreCertValidation;
        _alwaysSendContent = alwaysSendContent;
        _allowEmptyHeaderValues = allowEmptyHeaderValues;
        _codePagesEncoding = codePagesEncoding ?? "";
        _curlProfile = curlProfile;
        return this;
    }

    public Request SetStandardContent(string postData, string contentType,
        HttpMethod method = HttpMethod.POST, bool encodeContent = false,
        List<LogEntry> log = null, bool alwaysSendContent = false)
    {
        _contentType = contentType;
        string text = Regex.Replace(postData, "(?<!\\\\)\\\\n", Environment.NewLine).Unescape();
        if (alwaysSendContent || CanContainRequestBody(method))
        {
            if (encodeContent)
            {
                int num = Random.Shared.Next(1000000, 9999999);
                text = text.Replace("&", $"{num}&{num}").Replace("=", $"{num}={num}");
                text = string.Join("",
                    from s in BlockFunction.SplitInChunks(text, 2080)
                    select Uri.EscapeDataString(s))
                    .Replace($"{num}%26{num}", "&").Replace($"{num}%3D{num}", "=");
            }
            // StringContent(string, Encoding, string) requires a bare media-type without
            // parameters, e.g. "application/json" not "application/json; charset=UTF-8".
            // MediaTypeHeaderValue.Parse handles the full value including parameters.
            _content = new StringContent(text);
            if (!string.IsNullOrEmpty(contentType))
                _content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
            log?.Add(new LogEntry("Post Data: " + text, Colors.MediumTurquoise));
        }
        return this;
    }

    public Request SetRawContent(byte[] rawBytes, string contentType,
        HttpMethod method = HttpMethod.POST, List<LogEntry> log = null,
        bool alwaysSendContent = false)
    {
        _contentType = contentType;
        if (alwaysSendContent || CanContainRequestBody(method))
        {
            _content = new ByteArrayContent(rawBytes);
            if (!string.IsNullOrEmpty(contentType))
                _content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
            log?.Add(new LogEntry($"Raw Data: [{rawBytes.Length} bytes]", Colors.MediumTurquoise));
        }
        return this;
    }

    public Request SetRawContent(string rawData, string contentType,
        HttpMethod method = HttpMethod.POST, List<LogEntry> log = null,
        bool alwaysSendContent = false)
    {
        _contentType = contentType;
        byte[] buffer = rawData.ConvertFrom(RuriLib.Functions.Conversions.Encoding.HEX);
        if (alwaysSendContent || CanContainRequestBody(method))
        {
            _content = new ByteArrayContent(buffer);
            if (!string.IsNullOrEmpty(contentType))
                _content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
            log?.Add(new LogEntry("Raw Data: " + rawData, Colors.MediumTurquoise));
        }
        return this;
    }


    public Request SetBasicAuth(string username, string password)
    {
        _authorization = "Basic " + (username + ":" + password).ToBase64();
        return this;
    }

    public Request SetMultipartContent(IEnumerable<MultipartContent> contents,
        string boundary = "", List<LogEntry> log = null)
    {
        string b = boundary != string.Empty ? boundary : GenerateMultipartBoundary();
        var mpContent = new MultipartFormDataContent(b);

        log?.Add(new LogEntry($"Content-Type: multipart/form-data; boundary={b}", Colors.MediumTurquoise));
        log?.Add(new LogEntry("Multipart Data:", Colors.MediumTurquoise));
        log?.Add(new LogEntry(b, Colors.MediumTurquoise));

        foreach (var part in contents)
        {
            if (part.Type == MultipartContentType.String)
            {
                var partVal = (part.Value ?? string.Empty).Unescape();
                mpContent.Add(new StringContent(partVal), part.Name);
                log?.Add(new LogEntry(
                    $"Content-Disposition: form-data; name=\"{part.Name}\"\r\n\r\n{partVal}",
                    Colors.MediumTurquoise));
            }
            else if (part.Type == MultipartContentType.File)
            {
                // Buffer the file into memory so retries can re-send it without a seek.
                // StreamContent backed by a FileStream would be at EOF on the second attempt.
                var fileContent = new ByteArrayContent(File.ReadAllBytes(part.Value));
                if (!string.IsNullOrEmpty(part.ContentType))
                    fileContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(part.ContentType);
                mpContent.Add(fileContent, part.Name, Path.GetFileName(part.Value));
                log?.Add(new LogEntry(
                    $"Content-Disposition: form-data; name=\"{part.Name}\"; filename=\"{part.Value}\"\r\nContent-Type: {part.ContentType}\r\n\r\n[FILE CONTENT OMITTED]",
                    Colors.MediumTurquoise));
            }
            log?.Add(new LogEntry(b, Colors.MediumTurquoise));
        }

        _content = mpContent;
        _contentType = $"multipart/form-data; boundary={b}";
        return this;
    }

    public Request SetProxy(CProxy proxy)
    {
        _proxy = proxy;
        return this;
    }

    public Request SetCookies(Dictionary<string, string> cookies, List<LogEntry> log = null)
    {
        _cookies = cookies ?? new Dictionary<string, string>();
        foreach (var kv in _cookies)
            log?.Add(new LogEntry($"{kv.Key}: {kv.Value}", Colors.MediumTurquoise));
        return this;
    }

    public Request SetHeaders(Dictionary<string, string> headers,
        bool acceptEncoding = true, List<LogEntry> log = null)
    {
        _headers = new Dictionary<string, string>(headers ?? new Dictionary<string, string>());
        if (_authorization != string.Empty)
            _headers["Authorization"] = _authorization;
        foreach (var kv in _headers)
            log?.Add(new LogEntry($"{kv.Key}: {kv.Value}", Colors.MediumTurquoise));
        if (_contentType != string.Empty)
            log?.Add(new LogEntry("Content-Type: " + _contentType, Colors.MediumTurquoise));
        return this;
    }

    // Used by BlockRequest.Analyze() for a raw GET without proxy.
    public static HttpRequestMessage BuildRawGetRequest(string url, Dictionary<string, string> headers)
    {
        var msg = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
        msg.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
        foreach (var h in headers)
        {
            if (h.Key.Replace("-", "").ToLower() != "acceptencoding")
                msg.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }
        return msg;
    }

    public static HttpClient BuildRawClient(bool ignoreSsl = true)
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            SslOptions = ignoreSsl
                ? new SslClientAuthenticationOptions
                  { RemoteCertificateValidationCallback = (_, _, _, _) => true }
                : new SslClientAuthenticationOptions()
        };
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
    }

    private HttpClient BuildClient()
    {
        RemoteCertificateValidationCallback certCallback = _ignoreCertValidation
            ? (_, _, _, _) => true
            : null;

        if (_httpLibrary == HttpLibrary.CurlImpersonate)
            return BuildCurlImpersonateClient();

        if (_httpLibrary == HttpLibrary.RuriLibHttp)
            return BuildPooledClient(certCallback);

        // SystemNet: BouncyCastle TLS with Chrome142 profile — matches original Extreme.Net behavior.
        // SocketsHttpHandler (.NET 8 pure stack) sends a non-browser TLS fingerprint that gets
        // blocked by Imperva/Cloudflare on cross-domain OAuth redirects. BouncyCastle sends a
        // browser-like ClientHello that works with TLS-fingerprinting anti-bot systems, exactly
        // as the original SilverBullet did via Extreme.Net.
        var bcHandlerSN = new BouncyCastleTlsHandler(
            CurlImpersonateData.GetCipherSuites(CurlImpersonateBrowserProfile.Chrome142),
            _ignoreCertValidation,
            _autoRedirect,
            _maxRedirects,
            _acceptEncoding,
            _proxy?.GetWebProxy(),
            _timeout,
            CurlImpersonateBrowserProfile.Chrome142);
        return new HttpClient(bcHandlerSN)
        {
            Timeout = _timeout > 0 && _timeout != int.MaxValue
                ? TimeSpan.FromMilliseconds((long)_timeout + 5000)
                : System.Threading.Timeout.InfiniteTimeSpan
        };
    }

    // RuriLibHttp: pooled SocketsHttpHandler for direct connections only.
    // Proxy requests always get a fresh handler so each request opens a new TCP session
    // to the proxy — required for rotating/residential proxies to assign a new exit IP.
    private HttpClient BuildPooledClient(RemoteCertificateValidationCallback certCallback)
    {
        var timeout = _timeout > 0
            ? TimeSpan.FromMilliseconds(_timeout)
            : System.Threading.Timeout.InfiniteTimeSpan;

        if (_proxy != null)
        {
            // Fresh handler per request → new TCP connection to proxy → new exit IP
            var freshHandler = new SocketsHttpHandler
            {
                AllowAutoRedirect    = _autoRedirect,
                MaxAutomaticRedirections = _maxRedirects,
                AutomaticDecompression = _acceptEncoding
                    ? (DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli)
                    : DecompressionMethods.None,
                UseCookies  = false,
                UseProxy    = true,
                Proxy       = _proxy.GetWebProxy(),
                SslOptions  = new SslClientAuthenticationOptions
                {
                    EnabledSslProtocols              = _sslProtocols,
                    RemoteCertificateValidationCallback = certCallback
                }
            };
            return new HttpClient(freshHandler, disposeHandler: true) { Timeout = timeout };
        }

        // No proxy: pool connections for performance (avoids repeated TCP + TLS handshakes)
        string poolKey = $"direct|{_autoRedirect}|{_maxRedirects}|{_acceptEncoding}|{(int)_sslProtocols}|{_ignoreCertValidation}";
        var handler = _handlerPool.GetOrAdd(poolKey, _ => new SocketsHttpHandler
        {
            AllowAutoRedirect    = _autoRedirect,
            MaxAutomaticRedirections = _maxRedirects,
            AutomaticDecompression = _acceptEncoding
                ? (DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli)
                : DecompressionMethods.None,
            UseCookies  = false,
            UseProxy    = false,
            PooledConnectionLifetime    = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer     = 16,
            SslOptions = new SslClientAuthenticationOptions
            {
                EnabledSslProtocols              = _sslProtocols,
                RemoteCertificateValidationCallback = certCallback
            }
        });

        return new HttpClient(handler, disposeHandler: false) { Timeout = timeout };
    }

    // CurlImpersonate: BouncyCastle TLS with browser-specific cipher suite order
    private HttpClient BuildCurlImpersonateClient()
    {
        int[] cipherSuites = CurlImpersonateData.GetCipherSuites(_curlProfile);
        var bcHandler = new BouncyCastleTlsHandler(
            cipherSuites,
            _ignoreCertValidation,
            _autoRedirect,
            _maxRedirects,
            _acceptEncoding,
            _proxy?.GetWebProxy(),
            _timeout,
            _curlProfile);
        return new HttpClient(bcHandler)
        {
            Timeout = _timeout > 0 && _timeout != int.MaxValue
                ? TimeSpan.FromMilliseconds((long)_timeout + 5000)
                : System.Threading.Timeout.InfiniteTimeSpan
        };
    }

    private static System.Net.Http.HttpMethod ToSystemMethod(HttpMethod method) =>
        method switch
        {
            HttpMethod.GET     => System.Net.Http.HttpMethod.Get,
            HttpMethod.POST    => System.Net.Http.HttpMethod.Post,
            HttpMethod.PUT     => System.Net.Http.HttpMethod.Put,
            HttpMethod.DELETE  => System.Net.Http.HttpMethod.Delete,
            HttpMethod.HEAD    => System.Net.Http.HttpMethod.Head,
            HttpMethod.OPTIONS => System.Net.Http.HttpMethod.Options,
            HttpMethod.PATCH   => System.Net.Http.HttpMethod.Patch,
            _                  => System.Net.Http.HttpMethod.Get
        };

    public (string address, string statusCode,
            Dictionary<string, string> headers,
            Dictionary<string, string> cookies)
        Perform(string url, HttpMethod method, List<LogEntry> log = null, bool resToMemoryStream = false)
    {
        // Dispose any state from a previous Perform() call (e.g. a retry that got a partial response).
        _response?.Dispose();
        _response = null;
        _memoryStream?.Dispose();
        _memoryStream = null;

        string address = "";
        string statusCode = "0";
        var responseHeaders = new Dictionary<string, string>();
        var responseCookies = new Dictionary<string, string>(_cookies);

        HttpClient clientBuilt;
        try { clientBuilt = BuildClient(); }
        catch (Exception buildEx)
        {
            log?.Add(new LogEntry($"[{buildEx.GetType().Name}@BuildClient] {buildEx.Message}", Colors.Orange));
            throw;
        }
        using var client = clientBuilt;

        HttpRequestMessage requestMsgBuilt;
        try { requestMsgBuilt = new HttpRequestMessage(ToSystemMethod(method), url); }
        catch (Exception urlEx)
        {
            log?.Add(new LogEntry($"[{urlEx.GetType().Name}@URL] {urlEx.Message}", Colors.Orange));
            throw;
        }
        using var requestMsg = requestMsgBuilt;
        if (_protocolVersion != null)
            requestMsg.Version = _protocolVersion;

        // Attach body content
        if (_content != null && (_alwaysSendContent || CanContainRequestBody(method)))
            requestMsg.Content = _content;

        // Attach headers (skip content-type and accept-encoding managed elsewhere)
        foreach (var h in _headers)
        {
            string normalised = h.Key.Replace("-", "").ToLower();
            if (normalised == "contenttype" && _content != null) continue;
            if (normalised == "acceptencoding" && _acceptEncoding) continue;
            if (!_allowEmptyHeaderValues && string.IsNullOrEmpty(h.Value)) continue;
            requestMsg.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }

        // Fallback User-Agent: if the config doesn't set one, use the active profile's browser UA.
        // SystemNet uses Chrome142; CurlImpersonate uses the selected profile.
        if (!requestMsg.Headers.Contains("User-Agent"))
        {
            var uaProfile = _httpLibrary == HttpLibrary.CurlImpersonate
                ? _curlProfile
                : CurlImpersonateBrowserProfile.Chrome142;
            requestMsg.Headers.TryAddWithoutValidation("User-Agent", CurlImpersonateData.GetUserAgent(uaProfile));
        }

        // Attach cookies: merge global cookie jar with any explicit Cookie header from config
        // Config template Cookie values override jar values; jar supplies any extra cookies not in config (e.g. JSESSIONID)
        if (_cookies.Count > 0)
        {
            var merged = new Dictionary<string, string>(_cookies, StringComparer.OrdinalIgnoreCase);
            if (requestMsg.Headers.TryGetValues("Cookie", out var existingCookies))
            {
                requestMsg.Headers.Remove("Cookie");
                foreach (var cookieStr in existingCookies)
                    foreach (var pair in cookieStr.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var kv = pair.Trim().Split('=', 2);
                        if (kv.Length == 2) merged[kv[0].Trim()] = kv[1].Trim();
                    }
            }
            requestMsg.Headers.TryAddWithoutValidation("Cookie",
                string.Join("; ", merged.Select(kv => $"{kv.Key}={kv.Value}")));
        }

        try
        {
            _response = client.SendAsync(requestMsg).GetAwaiter().GetResult();

            address = _response.RequestMessage?.RequestUri?.ToString() ?? url;
            log?.Add(new LogEntry("Address: " + address, Colors.Cyan));

            statusCode = ((int)_response.StatusCode).ToString();
            log?.Add(new LogEntry($"Response code: {statusCode} ({_response.StatusCode})", Colors.Cyan));

            log?.Add(new LogEntry("Received headers:", Colors.DeepPink));
            // Combine response headers + content headers
            var allHeaders = _response.Headers
                .Concat(_response.Content.Headers)
                .ToList();
            foreach (var h in allHeaders)
            {
                string val = string.Join(", ", h.Value);
                responseHeaders[h.Key] = val;
                log?.Add(new LogEntry($"{h.Key}: {val}", Colors.LightPink));
            }

            _hasContentLength = responseHeaders.ContainsKey("Content-Length");

            // Parse Set-Cookie headers
            log?.Add(new LogEntry("Received cookies:", Colors.Goldenrod));
            if (_response.Headers.TryGetValues("Set-Cookie", out var setCookies))
            {
                foreach (var sc in setCookies)
                {
                    var parts = sc.Split(';')[0].Split('=', 2);
                    if (parts.Length == 2)
                    {
                        string k = parts[0].Trim(), v = parts[1].Trim();
                        if (!_cookies.TryGetValue(k, out var old) || old != v)
                            log?.Add(new LogEntry($"{k}: {v}", Colors.LightGoldenrodYellow));
                        responseCookies[k] = v;
                    }
                }
            }

            if (resToMemoryStream)
            {
                _memoryStream = new MemoryStream();
                using var cs = _response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                cs.CopyTo(_memoryStream);
                _memoryStream.Position = 0;
            }
        }
        catch (HttpRequestException ex)
        {
            string msg = ex.Message;
            if (ex.StatusCode.HasValue)
                statusCode = ((int)ex.StatusCode.Value).ToString();
            log?.Add(new LogEntry(msg, Colors.Orange));
            log?.Add(new LogEntry("Status code: " + statusCode, Colors.Cyan));
            throw;
        }
        catch (Exception ex)
        {
            log?.Add(new LogEntry($"[{ex.GetType().Name}] {ex.Message}", Colors.Orange));
            throw;
        }
        finally
        {
            // Always detach _content from the request message before `using var requestMsg`
            // disposes it — _content must survive intact for retries in BlockRequest.
            requestMsg.Content = null;
        }

        return (address, statusCode, responseHeaders, responseCookies);
    }

    /// <summary>Raw response body bytes captured by the last SaveString call.</summary>
    public byte[] GetRawBytes() => _rawResponseBytes;

    public string SaveString(bool readResponseSource,
        Dictionary<string, string> headers = null, List<LogEntry> log = null)
    {
        string body;
        if (_memoryStream != null)
        {
            _memoryStream.Position = 0;
            _rawResponseBytes = _memoryStream.ToArray();
            body = GetSourceFromStream();
        }
        else if (_response != null)
        {
            // Read bytes once; derive string from them (preserves binary for RAWSOURCE)
            _rawResponseBytes = _response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(_codePagesEncoding))
            {
                try { body = System.Text.Encoding.GetEncoding(_codePagesEncoding).GetString(_rawResponseBytes); }
                catch { body = System.Text.Encoding.Latin1.GetString(_rawResponseBytes); }
            }
            else
            {
                // Use Latin-1 (ISO-8859-1): maps each byte 0x00-0xFF to exactly one char,
                // so the round-trip string→bytes is lossless for any binary content.
                body = System.Text.Encoding.Latin1.GetString(_rawResponseBytes);
            }
        }
        else
        {
            _rawResponseBytes = Array.Empty<byte>();
            body = "";
        }

        log?.Add(new LogEntry("Response Source:", Colors.Green));
        string result = "";
        if (readResponseSource)
        {
            result = body;
            log?.Add(new LogEntry(result, Colors.GreenYellow));
        }
        else
        {
            log?.Add(new LogEntry("[SKIPPED]", Colors.GreenYellow));
        }

        if (!_hasContentLength && headers != null)
        {
            // Use the same encoding that decoded the body so the byte count matches the wire length.
            var cl = !string.IsNullOrEmpty(_codePagesEncoding)
                ? System.Text.Encoding.GetEncoding(_codePagesEncoding).GetByteCount(body)
                : System.Text.Encoding.UTF8.GetByteCount(body);
            headers["Content-Length"] = cl.ToString();
            log?.Add(new LogEntry("Calculated header: Content-Length: " + headers["Content-Length"], Colors.LightPink));
        }
        return result;
    }

    public string GetSourceFromStream()
    {
        if (_memoryStream == null) return "";
        _memoryStream.Position = 0;
        using var reader = new StreamReader(_memoryStream, leaveOpen: true);
        return reader.ReadToEnd();
    }

    public MemoryStream GetMemoryStream() => _memoryStream;

    public MemoryStream GetResponseStream()
    {
        // If content was already buffered into _memoryStream (resToMemoryStream path),
        // return a fresh copy — the underlying response stream is already exhausted.
        if (_memoryStream != null)
        {
            _memoryStream.Position = 0;
            var copy = new MemoryStream();
            _memoryStream.CopyTo(copy);
            copy.Position = 0;
            return copy;
        }
        if (_response == null) return new MemoryStream();
        var ms = new MemoryStream();
        using var cs = _response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
        cs.CopyTo(ms);
        ms.Position = 0;
        return ms;
    }

    public void SaveFile(string path, List<LogEntry> log = null)
    {
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) dir += Path.DirectorySeparatorChar;
        string noExt = Path.GetFileNameWithoutExtension(path);
        string ext = Path.GetExtension(path);
        string dest = dir + RuriLib.Functions.Files.Files.MakeValidFileName(noExt) + ext;
        using var fs = File.Create(dest);
        using var responseStream = GetResponseStream();
        responseStream.CopyTo(fs);
        log?.Add(new LogEntry("File saved as " + dest, Colors.Green));
    }

    public void Dispose()
    {
        _response?.Dispose();
        _memoryStream?.Dispose();
    }

    internal static string GenerateMultipartBoundary()
        => "------WebKitFormBoundary" +
           Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(12)).ToLower();
}
