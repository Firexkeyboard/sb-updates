using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RuriLib.LS;
using RuriLib.Models;

namespace RuriLib;

public class BlockAltcha : BlockBase
{
    private string challengeUrl = "";
    private string outputVariable = "ALTCHA_TOKEN";

    public string ChallengeUrl
    {
        get => challengeUrl;
        set { challengeUrl = value; OnPropertyChanged("ChallengeUrl"); }
    }

    public string OutputVariable
    {
        get => outputVariable;
        set { outputVariable = value; OnPropertyChanged("OutputVariable"); }
    }

    public BlockAltcha()
    {
        Label = "ALTCHA";
    }

    public override BlockBase FromLS(string line)
    {
        string input = line.Trim();
        if (input.StartsWith("#"))
            Label = LineParser.ParseLabel(ref input);

        ChallengeUrl = LineParser.ParseLiteral(ref input, "ChallengeUrl");

        if (input != "" && LineParser.Lookahead(ref input) == TokenType.Literal)
            OutputVariable = LineParser.ParseLiteral(ref input, "OutputVariable");

        return this;
    }

    public override string ToLS(bool indent = true)
    {
        var bw = new BlockWriter(GetType(), indent, Disabled);
        bw.Label(Label).Token("ALTCHA").Literal(ChallengeUrl).Literal(OutputVariable);
        return bw.ToString();
    }

    public override void Process(BotData data)
    {
        base.Process(data);

        string resolvedUrl = ReplaceValues(challengeUrl, data);
        string resolvedVar = ReplaceValues(outputVariable, data);

        var G  = Colors.GreenYellow;
        var G2 = Colors.GreenYellow;

        data.Log(new LogEntry("ALTCHA Solver", G));
        data.Log(new LogEntry($"Challenge URL : {resolvedUrl}", G));

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0");
        http.DefaultRequestHeaders.TryAddWithoutValidation("Referer", new Uri(resolvedUrl).GetLeftPart(UriPartial.Authority) + "/");
        http.DefaultRequestHeaders.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");

        if (data.Cookies != null && data.Cookies.Count > 0)
        {
            string cookieHeader = string.Join("; ", data.Cookies.Select(kv => $"{kv.Key}={kv.Value}"));
            http.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", cookieHeader);
            data.Log(new LogEntry($"Cookies       : {data.Cookies.Count} forwarded", G2));
        }

        string challengeJson;
        System.Net.HttpStatusCode statusCode;
        try
        {
            var resp = http.GetAsync(resolvedUrl).GetAwaiter().GetResult();
            statusCode = resp.StatusCode;

            if (resp.Headers.TryGetValues("Set-Cookie", out var setCookies))
            {
                foreach (var sc in setCookies)
                {
                    var parts = sc.Split(';')[0].Split('=', 2);
                    if (parts.Length == 2)
                        data.Cookies[parts[0].Trim()] = parts[1].Trim();
                }
            }

            challengeJson = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (!resp.IsSuccessStatusCode)
            {
                data.Log(new LogEntry($"Status        : {(int)statusCode} {statusCode}", Colors.Tomato));
                data.Log(new LogEntry($"Response      : {challengeJson.Trim().Split('\n')[0].Trim()}", Colors.Tomato));
                data.Log(new LogEntry($"Hint          : Verify the Challenge URL is correct", Colors.Orange));
                data.Log(new LogEntry($"{{\"solution\":null,\"status\":\"error\",\"errorId\":1,\"errorCode\":\"ERROR_CHALLENGE_FETCH\",\"errorDescription\":\"{(int)statusCode} {statusCode}\"}}", Colors.Tomato));
                throw new Exception($"[Altcha] Challenge endpoint returned {(int)statusCode} {statusCode}");
            }
        }
        catch (Exception ex) when (ex.Message.Contains("Challenge endpoint"))
        {
            throw;
        }
        catch (Exception ex)
        {
            data.Log(new LogEntry($"Status        : Network Error", Colors.Tomato));
            data.Log(new LogEntry($"Response      : {ex.Message}", Colors.Tomato));
            data.Log(new LogEntry($"{{\"solution\":null,\"status\":\"error\",\"errorId\":1,\"errorCode\":\"ERROR_NETWORK\",\"errorDescription\":\"{ex.Message.Replace("\"", "'")}\" }}", Colors.Tomato));
            throw new Exception($"[Altcha] Network error: {ex.Message}");
        }

        JObject ch;
        try { ch = JObject.Parse(challengeJson); }
        catch
        {
            string preview = challengeJson.Length > 120 ? challengeJson[..120].Trim() + "..." : challengeJson.Trim();
            data.Log(new LogEntry($"Status        : Invalid Response", Colors.Tomato));
            data.Log(new LogEntry($"Response      : {preview}", Colors.Tomato));
            data.Log(new LogEntry($"Hint          : Endpoint did not return valid JSON", Colors.Orange));
            data.Log(new LogEntry($"{{\"solution\":null,\"status\":\"error\",\"errorId\":1,\"errorCode\":\"ERROR_INVALID_JSON\",\"errorDescription\":\"Expected JSON, got non-JSON response\"}}", Colors.Tomato));
            throw new Exception($"[Altcha] Expected JSON from challenge endpoint");
        }

        string token;

        // Detectar variante PBKDF2 por presencia de campo "parameters"
        if (ch["parameters"] is JObject parameters)
        {
            token = SolvePbkdf2(data, ch, parameters);
        }
        else
        {
            token = SolveSha256(data, ch);
        }

        string urlToken = Uri.EscapeDataString(token);
        data.Variables.Set(new CVar(resolvedVar, urlToken));

        string result = $"{{\"solution\":{{\"altchaToken\":\"{token}\"}},\"status\":\"ready\",\"errorId\":0,\"errorCode\":null,\"errorDescription\":null}}";
        data.Log(new LogEntry(result, G));
        data.ResponseSource = result;
        data.Log(new LogEntry($"Saved to <{resolvedVar}>", Colors.Lime));
    }

    // ── Variante estándar: SHA-256(salt + n) == challenge ─────────────────────

    private static string SolveSha256(BotData data, JObject ch)
    {
        string algorithm = ch["algorithm"]?.ToString() ?? "SHA-256";
        string challenge  = ch["challenge"]?.ToString()
            ?? throw new Exception("[Altcha] Falta 'challenge' en la respuesta");
        string salt       = ch["salt"]?.ToString()
            ?? throw new Exception("[Altcha] Falta 'salt' en la respuesta");
        long maxnumber    = ch["maxnumber"]?.ToObject<long>() ?? 1_000_000L;
        string signature  = ch["signature"]?.ToString() ?? "";

        var G  = Colors.GreenYellow;
        var G2 = Colors.GreenYellow;
        data.Log(new LogEntry($"Algorithm     : {algorithm}", G));
        data.Log(new LogEntry($"Max Number    : {maxnumber:N0}", G2));

        var sw = System.Diagnostics.Stopwatch.StartNew();
        long? found = null;

        for (long n = 0; n <= maxnumber; n++)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(salt + n.ToString()));
            if (Convert.ToHexString(hash).ToLowerInvariant() == challenge)
            {
                found = n;
                break;
            }
        }

        sw.Stop();

        if (found == null)
            throw new Exception($"[Altcha] Solution not found in range 0–{maxnumber}");

        long took = sw.ElapsedMilliseconds;
        data.Log(new LogEntry($"Number        : {found}", G));
        data.Log(new LogEntry($"Elapsed       : {took} ms", G2));

        var solution = new JObject
        {
            ["algorithm"] = algorithm,
            ["challenge"] = challenge,
            ["number"]    = found.Value,
            ["salt"]      = salt,
            ["signature"] = signature,
            ["took"]      = took
        };

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(solution.ToString(Formatting.None)));
    }

    // ── Variante PBKDF2/SHA-256 ────────────────────────────────────────────────
    // Formato real del token (base64 del JSON):
    //   { "challenge": { "parameters": {...}, "signature": "..." },
    //     "solution":  { "counter": N, "derivedKey": "00ab...", "time": ms } }

    private static string SolvePbkdf2(BotData data, JObject ch, JObject parameters)
    {
        int    cost      = parameters["cost"]?.ToObject<int>()     ?? 10000;
        int    keyLength = parameters["keyLength"]?.ToObject<int>() ?? 32;
        string keyPrefix = (parameters["keyPrefix"]?.ToString() ?? "").ToLowerInvariant();
        string salt      = parameters["salt"]?.ToString()
            ?? throw new Exception("[Altcha] Falta 'parameters.salt'");
        string nonce     = parameters["nonce"]?.ToString()
            ?? throw new Exception("[Altcha] Falta 'parameters.nonce'");
        string signature = ch["signature"]?.ToString() ?? "";

        byte[] saltBytes  = Convert.FromHexString(salt);
        byte[] nonceBytes = Convert.FromHexString(nonce);

        var G  = Colors.GreenYellow;
        var G2 = Colors.GreenYellow;
        data.Log(new LogEntry($"Algorithm     : PBKDF2/SHA-256", G));
        data.Log(new LogEntry($"Iterations    : {cost:N0}  |  Key Length: {keyLength} bytes  |  Prefix: \"{keyPrefix}\"", G2));

        var sw = System.Diagnostics.Stopwatch.StartNew();
        long   counter    = -1;
        string derivedKey = "";

        for (long n = 0; n < 5_000_000L; n++)
        {
            // password = nonce_bytes + counter.to_bytes(4, 'big')
            byte[] counterBytes = new byte[4];
            counterBytes[0] = (byte)((n >> 24) & 0xFF);
            counterBytes[1] = (byte)((n >> 16) & 0xFF);
            counterBytes[2] = (byte)((n >> 8)  & 0xFF);
            counterBytes[3] = (byte)( n         & 0xFF);
            byte[] passwordBytes = nonceBytes.Concat(counterBytes).ToArray();

            byte[] key = Rfc2898DeriveBytes.Pbkdf2(
                passwordBytes,
                saltBytes, cost, HashAlgorithmName.SHA256, keyLength);
            string keyHex = Convert.ToHexString(key).ToLowerInvariant();
            if (keyHex.StartsWith(keyPrefix))
            {
                counter    = n;
                derivedKey = keyHex;
                break;
            }
        }

        sw.Stop();

        if (counter < 0)
            throw new Exception("[Altcha] PBKDF2 solution not found in search space");

        long took = sw.ElapsedMilliseconds;
        data.Log(new LogEntry($"Counter       : {counter}", G));
        data.Log(new LogEntry($"Derived Key   : {derivedKey}", G2));
        data.Log(new LogEntry($"Elapsed       : {took} ms", G2));

        // Estructura exacta que espera el servidor
        var solution = new JObject
        {
            ["challenge"] = new JObject
            {
                ["parameters"] = parameters.DeepClone(),
                ["signature"]  = signature
            },
            ["solution"] = new JObject
            {
                ["counter"]    = counter,
                ["derivedKey"] = derivedKey,
                ["time"]       = took
            }
        };

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(solution.ToString(Formatting.None)));
    }
}
