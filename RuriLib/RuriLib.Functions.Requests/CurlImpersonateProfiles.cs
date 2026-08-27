using System.Collections.Generic;
using Org.BouncyCastle.Tls;

namespace RuriLib.Functions.Requests;

public enum CurlImpersonateBrowserProfile
{
    Chrome99 = 0,
    Chrome99Android,
    Chrome100,
    Chrome101,
    Chrome104,
    Chrome107,
    Chrome110,
    Chrome116,
    Chrome119,
    Chrome120,
    Chrome123,
    Chrome124,
    Chrome131,
    Chrome131Android,
    Chrome133a,
    Chrome136,
    Chrome142,
    Chrome145,
    Chrome146,
    Edge99,
    Edge101,
    Firefox133,
    Firefox135,
    Firefox144,
    Firefox147,
    Safari153,
    Safari155,
    Safari170,
    Safari172Ios,
    Safari180,
    Safari180Ios,
    Safari184,
    Safari184Ios,
    Safari260,
    Safari260Ios,
    SafariIos155,
    SafariIos156,
    SafariIos160,
    SafariIos17,
    SafariIos170,
    SafariIos18,
    SafariIos185,
    SafariIpad156,

    // Tor Browser (Firefox ESR 128 base, no session_ticket, no SCT)
    Tor145,

    // Android OkHttp 4.x (Conscrypt/BoringSSL — no GREASE, MODERN_TLS ConnectionSpec)
    OkhttpAndroid10,
    OkhttpAndroid11,
    OkhttpAndroid12,
    OkhttpAndroid13,
}

internal static class CurlImpersonateData
{
    // Chrome cipher suites (Chrome 99+)
    private static readonly int[] Chrome = new[]
    {
        CipherSuite.TLS_AES_128_GCM_SHA256,
        CipherSuite.TLS_AES_256_GCM_SHA384,
        CipherSuite.TLS_CHACHA20_POLY1305_SHA256,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256,
        CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384,
        CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256,
        CipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256,
        CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA,
        CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA,
        CipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256,
        CipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384,
        CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA,
        CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA,
    };

    // Firefox cipher suites
    private static readonly int[] Firefox = new[]
    {
        CipherSuite.TLS_AES_128_GCM_SHA256,
        CipherSuite.TLS_CHACHA20_POLY1305_SHA256,
        CipherSuite.TLS_AES_256_GCM_SHA384,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256,
        CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256,
        CipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384,
        CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA,
        CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA,
        CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA,
        CipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256,
        CipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384,
        CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA,
        CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA,
        CipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA,
    };

    // Safari cipher suites
    private static readonly int[] Safari = new[]
    {
        CipherSuite.TLS_AES_128_GCM_SHA256,
        CipherSuite.TLS_AES_256_GCM_SHA384,
        CipherSuite.TLS_CHACHA20_POLY1305_SHA256,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256,
        CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256,
        CipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256,
        CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384,
        CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA,
        CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA,
        CipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384,
        CipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256,
        CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA256,
        CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256,
        CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256,
        CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA,
        CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA,
        CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA,
        CipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA,
    };

    // Edge uses the same suites as Chrome
    private static readonly int[] Edge = Chrome;

    // OkHttp 4.x MODERN_TLS ConnectionSpec via Android Conscrypt (BoringSSL)
    // No 3DES, no CBC-only in later Android versions
    private static readonly int[] OkHttp4 = new[]
    {
        CipherSuite.TLS_AES_128_GCM_SHA256,
        CipherSuite.TLS_AES_256_GCM_SHA384,
        CipherSuite.TLS_CHACHA20_POLY1305_SHA256,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256,
        CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384,
        CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256,
        CipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA,
        CipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA,
        CipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA,
        CipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA,
        CipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256,
        CipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384,
        CipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA,
        CipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA,
    };

    public static int[] GetCipherSuites(CurlImpersonateBrowserProfile profile)
    {
        return profile switch
        {
            CurlImpersonateBrowserProfile.Firefox133 or
            CurlImpersonateBrowserProfile.Firefox135 or
            CurlImpersonateBrowserProfile.Firefox144 or
            CurlImpersonateBrowserProfile.Firefox147 or
            CurlImpersonateBrowserProfile.Tor145 => Firefox,  // Tor uses Firefox cipher suites

            CurlImpersonateBrowserProfile.Safari153 or
            CurlImpersonateBrowserProfile.Safari155 or
            CurlImpersonateBrowserProfile.Safari170 or
            CurlImpersonateBrowserProfile.Safari172Ios or
            CurlImpersonateBrowserProfile.Safari180 or
            CurlImpersonateBrowserProfile.Safari180Ios or
            CurlImpersonateBrowserProfile.Safari184 or
            CurlImpersonateBrowserProfile.Safari184Ios or
            CurlImpersonateBrowserProfile.Safari260 or
            CurlImpersonateBrowserProfile.Safari260Ios or
            CurlImpersonateBrowserProfile.SafariIos155 or
            CurlImpersonateBrowserProfile.SafariIos156 or
            CurlImpersonateBrowserProfile.SafariIos160 or
            CurlImpersonateBrowserProfile.SafariIos17 or
            CurlImpersonateBrowserProfile.SafariIos170 or
            CurlImpersonateBrowserProfile.SafariIos18 or
            CurlImpersonateBrowserProfile.SafariIos185 or
            CurlImpersonateBrowserProfile.SafariIpad156 => Safari,

            CurlImpersonateBrowserProfile.Edge99 or
            CurlImpersonateBrowserProfile.Edge101 => Edge,

            CurlImpersonateBrowserProfile.OkhttpAndroid10 or
            CurlImpersonateBrowserProfile.OkhttpAndroid11 or
            CurlImpersonateBrowserProfile.OkhttpAndroid12 or
            CurlImpersonateBrowserProfile.OkhttpAndroid13 => OkHttp4,

            _ => Chrome, // Chrome and all Android variants
        };
    }

    // Browser-default User-Agent strings for each profile
    public static string GetUserAgent(CurlImpersonateBrowserProfile profile)
    {
        return profile switch
        {
            // ── Chrome desktop ─────────────────────────────────────────────────
            CurlImpersonateBrowserProfile.Chrome99
                => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.51 Safari/537.36",
            CurlImpersonateBrowserProfile.Chrome100
                => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.4896.127 Safari/537.36",
            CurlImpersonateBrowserProfile.Chrome101
                => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.67 Safari/537.36",
            CurlImpersonateBrowserProfile.Chrome104
                => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.5112.102 Safari/537.36",
            CurlImpersonateBrowserProfile.Chrome107
                => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/107.0.0.0 Safari/537.36",
            CurlImpersonateBrowserProfile.Chrome110
                => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Safari/537.36",
            CurlImpersonateBrowserProfile.Chrome116
                => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/116.0.0.0 Safari/537.36",
            CurlImpersonateBrowserProfile.Chrome119
                => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
            CurlImpersonateBrowserProfile.Chrome120
                => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
            CurlImpersonateBrowserProfile.Chrome123
                => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36",
            CurlImpersonateBrowserProfile.Chrome124
                => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
            CurlImpersonateBrowserProfile.Chrome131
                => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            CurlImpersonateBrowserProfile.Chrome133a
                => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/133.0.0.0 Safari/537.36",
            CurlImpersonateBrowserProfile.Chrome136
                => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36",
            CurlImpersonateBrowserProfile.Chrome142
                => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.36",
            CurlImpersonateBrowserProfile.Chrome145
                => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36",
            CurlImpersonateBrowserProfile.Chrome146
                => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/146.0.0.0 Safari/537.36",

            // ── Chrome Android ──────────────────────────────────────────────────
            CurlImpersonateBrowserProfile.Chrome99Android
                => "Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.73 Mobile Safari/537.36",
            CurlImpersonateBrowserProfile.Chrome131Android
                => "Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Mobile Safari/537.36",

            // ── Edge ────────────────────────────────────────────────────────────
            CurlImpersonateBrowserProfile.Edge99
                => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.51 Safari/537.36 Edg/99.0.1150.46",
            CurlImpersonateBrowserProfile.Edge101
                => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.64 Safari/537.36 Edg/101.0.1210.47",

            // ── Firefox ─────────────────────────────────────────────────────────
            CurlImpersonateBrowserProfile.Firefox133
                => "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:133.0) Gecko/20100101 Firefox/133.0",
            CurlImpersonateBrowserProfile.Firefox135
                => "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:135.0) Gecko/20100101 Firefox/135.0",
            CurlImpersonateBrowserProfile.Firefox144
                => "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:144.0) Gecko/20100101 Firefox/144.0",
            CurlImpersonateBrowserProfile.Firefox147
                => "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:147.0) Gecko/20100101 Firefox/147.0",

            // ── Safari macOS ────────────────────────────────────────────────────
            CurlImpersonateBrowserProfile.Safari153
                => "Mozilla/5.0 (Macintosh; Intel Mac OS X 12_3) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.3 Safari/605.1.15",
            CurlImpersonateBrowserProfile.Safari155
                => "Mozilla/5.0 (Macintosh; Intel Mac OS X 12_5) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.5 Safari/605.1.15",
            CurlImpersonateBrowserProfile.Safari170
                => "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_0) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15",
            CurlImpersonateBrowserProfile.Safari180
                => "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_6) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Safari/605.1.15",
            CurlImpersonateBrowserProfile.Safari184
                => "Mozilla/5.0 (Macintosh; Intel Mac OS X 15_4) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.4 Safari/605.1.15",
            CurlImpersonateBrowserProfile.Safari260
                => "Mozilla/5.0 (Macintosh; Intel Mac OS X 26_0) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/26.0 Safari/605.1.15",

            // ── Safari iOS (iPhone) ─────────────────────────────────────────────
            CurlImpersonateBrowserProfile.SafariIos155
                => "Mozilla/5.0 (iPhone; CPU iPhone OS 15_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.5 Mobile/15E148 Safari/604.1",
            CurlImpersonateBrowserProfile.SafariIos156
                => "Mozilla/5.0 (iPhone; CPU iPhone OS 15_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.6 Mobile/15E148 Safari/604.1",
            CurlImpersonateBrowserProfile.SafariIos160
                => "Mozilla/5.0 (iPhone; CPU iPhone OS 16_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.0 Mobile/15E148 Safari/604.1",
            CurlImpersonateBrowserProfile.SafariIos17 or CurlImpersonateBrowserProfile.SafariIos170
                => "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1",
            CurlImpersonateBrowserProfile.Safari172Ios
                => "Mozilla/5.0 (iPhone; CPU iPhone OS 17_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.2 Mobile/15E148 Safari/604.1",
            CurlImpersonateBrowserProfile.SafariIos18 or CurlImpersonateBrowserProfile.Safari180Ios
                => "Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1",
            CurlImpersonateBrowserProfile.Safari184Ios
                => "Mozilla/5.0 (iPhone; CPU iPhone OS 18_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.4 Mobile/15E148 Safari/604.1",
            CurlImpersonateBrowserProfile.SafariIos185
                => "Mozilla/5.0 (iPhone; CPU iPhone OS 18_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Mobile/15E148 Safari/604.1",
            CurlImpersonateBrowserProfile.Safari260Ios
                => "Mozilla/5.0 (iPhone; CPU iPhone OS 26_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/26.0 Mobile/15E148 Safari/604.1",

            // ── Safari iPad ─────────────────────────────────────────────────────
            CurlImpersonateBrowserProfile.SafariIpad156
                => "Mozilla/5.0 (iPad; CPU OS 15_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.6 Mobile/15E148 Safari/604.1",

            // ── Tor Browser (Firefox ESR 128) ───────────────────────────────────
            // UA omits the OS-specific "Win64; x64" to resist fingerprinting
            CurlImpersonateBrowserProfile.Tor145
                => "Mozilla/5.0 (Windows NT 10.0; rv:128.0) Gecko/20100101 Firefox/128.0",

            // ── OkHttp 4.x on Android ───────────────────────────────────────────
            // Default OkHttp UA when the app does not override it
            CurlImpersonateBrowserProfile.OkhttpAndroid10
                => "okhttp/4.9.0",
            CurlImpersonateBrowserProfile.OkhttpAndroid11
                => "okhttp/4.9.1",
            CurlImpersonateBrowserProfile.OkhttpAndroid12
                => "okhttp/4.10.0",
            CurlImpersonateBrowserProfile.OkhttpAndroid13
                => "okhttp/4.11.0",

            // ── Default (Chrome latest) ─────────────────────────────────────────
            _ => "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.36",
        };
    }
}
