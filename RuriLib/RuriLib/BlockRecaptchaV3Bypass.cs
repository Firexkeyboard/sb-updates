using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using RuriLib.LS;
using RuriLib.Models;

namespace RuriLib;

public class BlockRecaptchaV3Bypass : BlockBase
{
    private string variableName = "RECAPTCHA_TOKEN";
    private string getUrl       = "";
    private string bg           = "!q62grYxHRvVxjUIjSFNd0mlvrZ-iCgIHAAAB6FcAAAANnAkBySdqTJGFRK7SirleWAwPVhv9-XwP8ugGSTJJgQ46-0IMBKN8HUnfPqm4sCefwxOOEURND35prc9DJYG0pbmg_jD18qC0c-lQzuPsOtUhHTtfv3--SVCcRvJWZ0V3cia65HGfUys0e1K-IZoArlxM9qZfUMXJKAFuWqZiBn-Qi8VnDqI2rRnAQcIB8Wra6xWzmFbRR2NZqF7lDPKZ0_SZBEc99_49j07ISW4X65sMHL139EARIOipdsj5js5JyM19a2TCZJtAu4XL1h0ZLfomM8KDHkcl_b0L-jW9cvAe2K2uQXKRPzruAvtjdhMdODzVWU5VawKhpmi2NCKAiCRUlJW5lToYkR_X-07AqFLY6qi4ZbJ_sSrD7fCNNYFKmLfAaxPwPmp5Dgei7KKvEQmeUEZwTQAS1p2gaBmt6SCOgId3QBfF_robIkJMcXFzj7R0G-s8rwGUSc8EQzT_DCe9SZsJyobu3Ps0-YK-W3MPWk6a69o618zPSIIQtSCor9w_oUYTLiptaBAEY03NWINhc1mmiYu2Yz5apkW_KbAp3HD3G0bhzcCIYZOGZxyJ44HdGsCJ-7ZFTcEAUST-aLbS-YN1AyuC7ClFO86CMICVDg6aIDyCJyIcaJXiN-bN5xQD_NixaXatJy9Mx1XEnU4Q7E_KISDJfKUhDktK5LMqBJa-x1EIOcY99E-eyry7crf3-Hax3Uj-e-euzRwLxn2VB1Uki8nqJQVYUgcjlVXQhj1X7tx4jzUb0yB1TPU9uMBtZLRvMCRKvFdnn77HgYs5bwOo2mRECiFButgigKXaaJup6NM4KRUevhaDtnD6aJ8ZWQZTXz_OJ74a_OvPK9eD1_5pTG2tUyYNSyz-alhvHdMt5_MAdI3op4ZmcvBQBV9VC2JLjphDuTW8eW_nuK9hN17zin6vjEL8YIm_MekB_dIUK3T1Nbyqmyzigy-Lg8tRL6jSinzdwOTc9hS5SCsPjMeiblc65aJC8AKmA5i80f-6Eg4BT305UeXKI3QwhI3ZJyyQAJTata41FoOXl3EF9Pyy8diYFK2G-CS8lxEpV7jcRYduz4tEPeCpBxU4O_KtM2iv4STkwO4Z_-c-fMLlYu9H7jiFnk6Yh8XlPE__3q0FHIBFf15zVSZ3qroshYiHBMxM5BVQBOExbjoEdYKx4-m9c23K3suA2sCkxHytptG-6yhHJR3EyWwSRTY7OpX_yvhbFri0vgchw7U6ujyoXeCXS9N4oOoGYpS5OyFyRPLxJH7yjXOG2Play5HJ91LL6J6qg1iY8MIq9XQtiVZHadVpZVlz3iKcX4vXcQ3rv_qQwhntObGXPAGJWEel5OiJ1App7mWy961q3mPg9aDEp9VLKU5yDDw1xf6tOFMwg2Q-PNDaKXAyP_FOkxOjnu8dPhuKGut6cJr449BKDwbnA9BOomcVSztEzHGU6HPXXyNdZbfA6D12f5lWxX2B_pobw3a1gFLnO6mWaNRuK1zfzZcfGTYMATf6d7sj9RcKNS230XPHWGaMlLmNxsgXkEN7a9PwsSVwcKdHg_HU4vYdRX6vkEauOIwVPs4dS7yZXmtvbDaX1zOU4ZYWg0T42sT3nIIl9M2EeFS5Rqms_YzNp8J-YtRz1h5RhtTTNcA5jX4N-xDEVx-vD36bZVzfoMSL2k85PKv7pQGLH-0a3DsR0pePCTBWNORK0g_RZCU_H898-nT1syGzNKWGoPCstWPRvpL9cnHRPM1ZKemRn0nPVm9Bgo0ksuUijgXc5yyrf5K49UU2J5JgFYpSp7aMGOUb1ibrj2sr-D63d61DtzFJ2mwrLm_KHBiN_ECpVhDsRvHe5iOx_APHtImevOUxghtkj-8RJruPgkTVaML2MEDOdL_UYaldeo-5ckZo3VHss7IpLArGOMTEd0bSH8tA8CL8RLQQeSokOMZ79Haxj8yE0EAVZ-k9-O72mmu5I0wH5IPgapNvExeX6O1l3mC4MqLhKPdOZOnTiEBlSrV4ZDH_9fhLUahe5ocZXvXqrud9QGNeTpZsSPeIYubeOC0sOsuqk10sWB7NP-lhifWeDob-IK1JWcgFTytVc99RkZTjUcdG9t8prPlKAagZIsDr1TiX3dy8sXKZ7d9EXQF5P_rHJ8xvmUtCWqbc3V5jL-qe8ANypwHsuva75Q6dtqoBR8vCE5xWgfwB0GzR3Xi_l7KDTsYAQIrDZVyY1UxdzWBwJCrvDrtrNsnt0S7BhBJ4ATCrW5VFPqXyXRiLxHCIv9zgo-NdBZQ4hEXXxMtbem3KgYUB1Rals1bbi8X8MsmselnHfY5LdOseyXWIR2QcrANSAypQUAhwVpsModw7HMdXgV9Uc-HwCMWafOChhBr88tOowqVHttPtwYorYrzriXNRt9LkigESMy1bEDx79CJguitwjQ9IyIEu8quEQb_-7AEXrfDzl_FKgASnnZLrAfZMtgyyddIhBpgAvgR_c8a8Nuro-RGV0aNuunVg8NjL8binz9kgmZvOS38QaP5anf2vgzJ9wC0ZKDg2Ad77dPjBCiCRtVe_dqm7FDA_cS97DkAwVfFawgce1wfWqsrjZvu4k6x3PAUH1UNzQUxVgOGUbqJsaFs3GZIMiI8O6-tZktz8i8oqpr0RjkfUhw_I2szHF3LM20_bFwhtINwg0rZxRTrg4il-_q7jDnVOTqQ7fdgHgiJHZw_OOB7JWoRW6ZlJmx3La8oV93fl1wMGNrpojSR0b6pc8SThsKCUgoY6zajWWa3CesX1ZLUtE7Pfk9eDey3stIWf2acKolZ9fU-gspeACUCN20EhGT-HvBtNBGr_xWk1zVJBgNG29olXCpF26eXNKNCCovsILNDgH06vulDUG_vR5RrGe5LsXksIoTMYsCUitLz4HEehUOd9mWCmLCl00eGRCkwr9EB557lyr7mBK2KPgJkXhNmmPSbDy6hPaQ057zfAd5s_43UBCMtI-aAs5NN4TXHd6IlLwynwc1zsYOQ6z_HARlcMpCV9ac-8eOKsaepgjOAX4YHfg3NekrxA2ynrvwk9U-gCtpxMJ4f1cVx3jExNlIX5LxE46FYIhQ";
    private string postUrl      = "";
    private string referer      = "";
    private string userAgent    = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    public string VariableName { get => variableName; set { variableName = value; OnPropertyChanged(); } }
    public string GetUrl       { get => getUrl;       set { getUrl       = value; OnPropertyChanged(); } }
    public string Bg           { get => bg;           set { bg           = value; OnPropertyChanged(); } }
    public string PostUrl      { get => postUrl;      set { postUrl      = value; OnPropertyChanged(); } }
    public string Referer      { get => referer;      set { referer      = value; OnPropertyChanged(); } }
    public string UserAgent    { get => userAgent;    set { userAgent    = value; OnPropertyChanged(); } }

    public BlockRecaptchaV3Bypass()
    {
        Label = "RECAPTCHAV3-BYPASS";
    }

    public override BlockBase FromLS(string line)
    {
        string input = line.Trim();
        if (input.StartsWith("#")) Label = LineParser.ParseLabel(ref input);
        GetUrl  = LineParser.ParseLiteral(ref input, "GetUrl");
        Bg      = LineParser.ParseLiteral(ref input, "Bg");
        PostUrl = LineParser.ParseLiteral(ref input, "PostUrl");

        // Old format had Referer + UserAgent before the variable name — skip them
        if (input.TrimStart().StartsWith("\""))
        {
            LineParser.ParseLiteral(ref input, "Referer");   // discard
            if (input.TrimStart().StartsWith("\""))
                LineParser.ParseLiteral(ref input, "UserAgent"); // discard
            if (input.TrimStart().StartsWith("\""))
                VariableName = LineParser.ParseLiteral(ref input, "VariableName");
            return this;
        }

        // New format: -> VAR "name"
        if (LineParser.ParseToken(ref input, TokenType.Arrow, false) == "->")
        {
            LineParser.ParseToken(ref input, TokenType.Parameter, true); // consume VAR
            VariableName = LineParser.ParseLiteral(ref input, "VariableName");
        }
        return this;
    }

    public override string ToLS(bool indent = true)
    {
        var bw = new BlockWriter(GetType(), indent, Disabled);
        bw.Label(Label)
          .Token("RecaptchaV3Bypass")
          .Literal(GetUrl)
          .Literal(Bg)
          .Literal(PostUrl)
          .Arrow().Token("VAR")
          .Literal(VariableName);
        return bw.ToString();
    }

    public override void Process(BotData data)
    {
        base.Process(data);

        string resolvedGetUrl  = ReplaceValues(getUrl,       data);
        string resolvedBg      = ReplaceValues(bg,           data);
        string resolvedPostUrl = ReplaceValues(postUrl,      data);
        string resolvedReferer = ReplaceValues(referer,      data);
        string resolvedUa      = ReplaceValues(userAgent,    data);
        string resolvedVar     = ReplaceValues(variableName, data);

        // Parse query params from GET URL to reuse in POST body
        var qp = new Uri(resolvedGetUrl).Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(
                p => Uri.UnescapeDataString(p[0]),
                p => Uri.UnescapeDataString(p[1]),
                StringComparer.OrdinalIgnoreCase);

        qp.TryGetValue("v",    out string qV);    qV    ??= "";
        qp.TryGetValue("k",    out string qK);    qK    ??= "";
        qp.TryGetValue("co",   out string qCo);   qCo   ??= "";
        qp.TryGetValue("hl",   out string qHl);   qHl   ??= "en";
        qp.TryGetValue("size", out string qSize); qSize ??= "invisible";

        using var handler = new System.Net.Http.HttpClientHandler { UseCookies = false, AllowAutoRedirect = true };
        using var http    = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };

        // ── Step 1: GET anchor (silent — just parse TK1) ─────────────────────────
        string anchorHtml;
        try
        {
            using var getReq = new HttpRequestMessage(HttpMethod.Get, resolvedGetUrl);
            getReq.Headers.TryAddWithoutValidation("User-Agent",      resolvedUa);
            getReq.Headers.TryAddWithoutValidation("Accept",          "*/*");
            getReq.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
            if (!string.IsNullOrEmpty(resolvedReferer))
                getReq.Headers.TryAddWithoutValidation("Referer", resolvedReferer);

            var getResp = http.SendAsync(getReq).GetAwaiter().GetResult();
            anchorHtml  = getResp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            data.Log(new LogEntry($"GET Error: {ex.Message}", Colors.Tomato));
            throw new Exception($"[RecaptchaV3Bypass] GET failed: {ex.Message}");
        }

        var tk1Match = Regex.Match(anchorHtml,
            @"<input[^>]+id=""recaptcha-token""[^>]+value=""([^""]+)""", RegexOptions.IgnoreCase);
        if (!tk1Match.Success)
            tk1Match = Regex.Match(anchorHtml,
                @"id=""recaptcha-token""[^>]*value=""([^""]+)""", RegexOptions.IgnoreCase);
        if (!tk1Match.Success)
        {
            data.Log(new LogEntry("Could not find recaptcha-token in anchor response", Colors.Tomato));
            throw new Exception("[RecaptchaV3Bypass] recaptcha-token not found");
        }

        string tk1 = tk1Match.Groups[1].Value;

        // ── Step 2: POST reload ───────────────────────────────────────────────────
        var parts = new System.Collections.Generic.List<string>
        {
            $"v={Uri.EscapeDataString(qV)}",
            "reason=q",
            $"c={Uri.EscapeDataString(tk1)}",
            $"k={Uri.EscapeDataString(qK)}",
            $"co={Uri.EscapeDataString(qCo)}",
            $"hl={Uri.EscapeDataString(qHl)}",
            $"size={Uri.EscapeDataString(qSize)}",
            "chr=%5B89%2C64%2C27%5D",
            "vh=13599012192"
        };
        if (!string.IsNullOrWhiteSpace(resolvedBg))
            parts.Add($"bg={resolvedBg}");

        string postBodyRaw = string.Join("&", parts);

        data.Log(new LogEntry($"Calling URL: {resolvedPostUrl}", Colors.MediumTurquoise));
        data.Log(new LogEntry($"Post Data: {postBodyRaw}", Colors.MediumTurquoise));
        data.Log(new LogEntry("Sent Headers:", Colors.DarkTurquoise));
        data.Log(new LogEntry($"UserAgent: {resolvedUa}", Colors.MediumTurquoise));
        data.Log(new LogEntry("Accept: */*", Colors.MediumTurquoise));
        data.Log(new LogEntry("accept-language: fa,en;q=0.9,en-GB;q=0.8,en-US;q=0.7", Colors.MediumTurquoise));
        data.Log(new LogEntry("Connection: keep-alive", Colors.MediumTurquoise));
        data.Log(new LogEntry("origin: https://www.google.com", Colors.MediumTurquoise));
        data.Log(new LogEntry($"referer: {resolvedGetUrl}", Colors.MediumTurquoise));
        data.Log(new LogEntry("sec-fetch-dest: empty", Colors.MediumTurquoise));
        data.Log(new LogEntry("sec-fetch-mode: cors", Colors.MediumTurquoise));
        data.Log(new LogEntry("sec-fetch-site: same-origin", Colors.MediumTurquoise));
        data.Log(new LogEntry("Content-Type: application/x-www-form-urlencoded", Colors.MediumTurquoise));
        data.Log(new LogEntry("Sent Cookies:", Colors.MediumTurquoise));

        System.Net.Http.HttpResponseMessage postRespMsg;
        string reloadResp;
        try
        {
            using var postReq = new HttpRequestMessage(HttpMethod.Post, resolvedPostUrl)
            {
                Content = new StringContent(postBodyRaw, Encoding.UTF8, "application/x-www-form-urlencoded")
            };
            postReq.Headers.TryAddWithoutValidation("User-Agent",      resolvedUa);
            postReq.Headers.TryAddWithoutValidation("Accept",          "*/*");
            postReq.Headers.TryAddWithoutValidation("Accept-Language", "fa,en;q=0.9,en-GB;q=0.8,en-US;q=0.7");
            postReq.Headers.TryAddWithoutValidation("Connection",      "keep-alive");
            postReq.Headers.TryAddWithoutValidation("Origin",          "https://www.google.com");
            postReq.Headers.TryAddWithoutValidation("Referer",         resolvedGetUrl);
            postReq.Headers.TryAddWithoutValidation("sec-fetch-dest",  "empty");
            postReq.Headers.TryAddWithoutValidation("sec-fetch-mode",  "cors");
            postReq.Headers.TryAddWithoutValidation("sec-fetch-site",  "same-origin");

            postRespMsg = http.SendAsync(postReq).GetAwaiter().GetResult();
            reloadResp  = postRespMsg.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            data.Log(new LogEntry($"POST Error: {ex.Message}", Colors.Tomato));
            throw new Exception($"[RecaptchaV3Bypass] POST failed: {ex.Message}");
        }

        // Log response details — same colors as BlockRequest
        data.Log(new LogEntry($"Address: {resolvedPostUrl}", Colors.Cyan));
        data.Log(new LogEntry(
            $"Response code: {(int)postRespMsg.StatusCode} ({postRespMsg.StatusCode})",
            Colors.Cyan));

        data.Log(new LogEntry("Received headers:", Colors.DeepPink));
        foreach (var h in postRespMsg.Headers.Concat(postRespMsg.Content.Headers))
            data.Log(new LogEntry($"{h.Key}: {string.Join(", ", h.Value)}", Colors.LightPink));

        // Collect cookies
        data.Log(new LogEntry("Received cookies:", Colors.Goldenrod));
        if (postRespMsg.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var sc in setCookies)
            {
                var cp = sc.Split(';')[0].Split('=', 2);
                if (cp.Length == 2)
                {
                    data.Cookies[cp[0].Trim()] = cp[1].Trim();
                    data.Log(new LogEntry($"{cp[0].Trim()}: {cp[1].Trim()}", Colors.LightGoldenrodYellow));
                }
            }
        }

        data.Log(new LogEntry("Response Source:", Colors.Green));
        data.Log(new LogEntry(reloadResp, Colors.GreenYellow));
        data.Log(new LogEntry($"Calculated header: Content-Length: {Encoding.UTF8.GetByteCount(reloadResp)}", Colors.LightPink));

        // Parse rresp
        var rrespMatch = Regex.Match(reloadResp, @"""rresp"",""([^""]+)""");
        if (!rrespMatch.Success)
        {
            data.Log(new LogEntry("Could not parse rresp from reload response", Colors.Tomato));
            throw new Exception("[RecaptchaV3Bypass] Could not parse rresp");
        }

        string token = rrespMatch.Groups[1].Value;
        data.Log(new LogEntry($"Parsed variable | Name: {resolvedVar} | Value: {token}" + Environment.NewLine, Colors.Gold));

        data.Variables.Set(new CVar(resolvedVar, token));
        data.ResponseSource = token;
    }
}
