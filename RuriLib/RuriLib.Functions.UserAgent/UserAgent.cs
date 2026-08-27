using System;

namespace RuriLib.Functions.UserAgent;

public static class UserAgent
{
    public enum Browser
    {
        Chrome,
        Firefox,
        InternetExplorer,
        Opera,
        OperaMini,
        Android,
        IOS
    }

    private static readonly Random _rnd = new Random();

    public static string ForBrowser(Browser browser) =>
        browser switch
        {
            Browser.Chrome          => ChromeUserAgent(),
            Browser.Firefox         => FirefoxUserAgent(),
            Browser.InternetExplorer => IEUserAgent(),
            Browser.Opera           => OperaUserAgent(),
            Browser.OperaMini       => OperaMiniUserAgent(),
            Browser.Android         => AndroidUserAgent(),
            Browser.IOS             => IOSUserAgent(),
            _                       => throw new Exception("Browser not supported")
        };

    public static string Random(Random rand)
    {
        int n = rand.Next(99) + 1;
        if (n <= 70) return ChromeUserAgent();
        if (n <= 85) return FirefoxUserAgent();
        if (n <= 91) return IEUserAgent();
        if (n <= 96) return OperaUserAgent();
        return OperaMiniUserAgent();
    }

    public static string ChromeUserAgent()
    {
        int major = _rnd.Next(90, 120);
        int build = _rnd.Next(4000, 6000);
        int patch = _rnd.Next(100, 200);
        return $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{major}.0.{build}.{patch} Safari/537.36";
    }

    public static string FirefoxUserAgent()
    {
        int major = _rnd.Next(90, 115);
        return $"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:{major}.0) Gecko/20100101 Firefox/{major}.0";
    }

    public static string IEUserAgent()
    {
        int trident = _rnd.Next(6, 8);
        return $"Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/{trident}.0; rv:11.0) like Gecko";
    }

    public static string OperaUserAgent()
    {
        int opera = _rnd.Next(70, 90);
        int chrome = _rnd.Next(90, 110);
        return $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chrome}.0.0.0 Safari/537.36 OPR/{opera}.0.0.0";
    }

    public static string OperaMiniUserAgent()
    {
        int v = _rnd.Next(40, 55);
        return $"Opera/9.80 (Android; Opera Mini/{v}.0/28.2555; U; en) Presto/2.12.407 Version/12.50";
    }

    public static string AndroidUserAgent()
    {
        string apkVer = _rnd.Next(500) + "." + _rnd.Next(400);
        string os = _rnd.Next(1, 10) + "." + _rnd.Next(1, 10) + "." + _rnd.Next(1, 10);
        string chrome = _rnd.Next(25, 80) + ".0";
        return $"Mozilla/5.0 (Linux; Android {os}; SM-G{_rnd.Next(800)}S Build/MMB29K; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/{apkVer} Chrome/{chrome} Mobile Safari/537.36";
    }

    public static string IOSUserAgent()
    {
        string safariVer = _rnd.Next(500) + "." + _rnd.Next(400);
        string ios = _rnd.Next(3, 15) + "_0";
        string build = _rnd.Next(25, 500).ToString();
        return $"Mozilla/5.0 (iPhone; CPU iPhone OS {ios} like Mac OS X) AppleWebKit/604.1.38 (KHTML, like Gecko) Version/{safariVer} Mobile/15A{build} Safari/604.1";
    }

    public static string RandomFromList(string localInputString)
    {
        string[] array = localInputString.Split('|');
        return array[_rnd.Next(array.Length)];
    }
}
