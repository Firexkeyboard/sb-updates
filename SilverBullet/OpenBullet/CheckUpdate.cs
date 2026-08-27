using System;
using System.Net.Http;
using Newtonsoft.Json;

namespace OpenBullet;

public static class CheckUpdate
{
	private static readonly HttpClient _http = new HttpClient();

	public static T Run<T>(string url = "https://raw.githubusercontent.com/mohamm4dx/SilverBullet/master/SilverBulletUpdater/SBUpdate.json")
	{
		try
		{
			_http.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:87.0) Gecko/20100101 Firefox/87.0");
			var response = _http.GetAsync(url).GetAwaiter().GetResult();
			if (!response.IsSuccessStatusCode)
				return default;
			string text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
			return JsonConvert.DeserializeObject<T>(text);
		}
		catch
		{
			return default;
		}
	}
}
