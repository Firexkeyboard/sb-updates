using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using RuriLib.Functions.Requests;
using RuriLib.Models;
using PluginFramework;
using PluginFramework.Attributes;
using RuriLib;
using RuriLib.LS;
using RuriLib.ViewModels;

namespace SilverBullet.Plugin;

internal class RecaptchaV3Bypass : BlockBase, IBlockPlugin
{
	private string variableName;

	private string recaptchaUrlGet = "";

	private string bg = "!q62grYxHRvVxjUIjSFNd0mlvrZ-iCgIHAAAB6FcAAAANnAkBySdqTJGFRK7SirleWAwPVhv9-XwP8ugGSTJJgQ46-0IMBKN8HUnfPqm4sCefwxOOEURND35prc9DJYG0pbmg_jD18qC0c-lQzuPsOtUhHTtfv3--SVCcRvJWZ0V3cia65HGfUys0e1K-IZoArlxM9qZfUMXJKAFuWqZiBn-Qi8VnDqI2rRnAQcIB8Wra6xWzmFbRR2NZqF7lDPKZ0_SZBEc99_49j07ISW4X65sMHL139EARIOipdsj5js5JyM19a2TCZJtAu4XL1h0ZLfomM8KDHkcl_b0L-jW9cvAe2K2uQXKRPzruAvtjdhMdODzVWU5VawKhpmi2NCKAiCRUlJW5lToYkR_X-07AqFLY6qi4ZbJ_sSrD7fCNNYFKmLfAaxPwPmp5Dgei7KKvEQmeUEZwTQAS1p2gaBmt6SCOgId3QBfF_robIkJMcXFzj7R0G-s8rwGUSc8EQzT_DCe9SZsJyobu3Ps0-YK-W3MPWk6a69o618zPSIIQtSCor9w_oUYTLiptaBAEY03NWINhc1mmiYu2Yz5apkW_KbAp3HD3G0bhzcCIYZOGZxyJ44HdGsCJ-7ZFTcEAUST-aLbS-YN1AyuC7ClFO86CMICVDg6aIDyCJyIcaJXiN-bN5xQD_NixaXatJy9Mx1XEnU4Q7E_KISDJfKUhDktK5LMqBJa-x1EIOcY99E-eyry7crf3-Hax3Uj-e-euzRwLxn2VB1Uki8nqJQVYUgcjlVXQhj1X7tx4jzUb0yB1TPU9uMBtZLRvMCRKvFdnn77HgYs5bwOo2mRECiFButgigKXaaJup6NM4KRUevhaDtnD6aJ8ZWQZTXz_OJ74a_OvPK9eD1_5pTG2tUyYNSyz-alhvHdMt5_MAdI3op4ZmcvBQBV9VC2JLjphDuTW8eW_nuK9hN17zin6vjEL8YIm_MekB_dIUK3T1Nbyqmyzigy-Lg8tRL6jSinzdwOTc9hS5SCsPjMeiblc65aJC8AKmA5i80f-6Eg4BT305UeXKI3QwhI3ZJyyQAJTata41FoOXl3EF9Pyy8diYFK2G-CS8lxEpV7jcRYduz4tEPeCpBxU4O_KtM2iv4STkwO4Z_-c-fMLlYu9H7jiFnk6Yh8XlPE__3q0FHIBFf15zVSZ3qroshYiHBMxM5BVQBOExbjoEdYKx4-m9c23K3suA2sCkxHytptG-6yhHJR3EyWwSRTY7OpX_yvhbFri0vgchw7U6ujyoXeCXS9N4oOoGYpS5OyFyRPLxJH7yjXOG2Play5HJ91LL6J6qg1iY8MIq9XQtiVZHadVpZVlz3iKcX4vXcQ3rv_qQwhntObGXPAGJWEel5OiJ1App7mWy961q3mPg9aDEp9VLKU5yDDw1xf6tOFMwg2Q-PNDaKXAyP_FOkxOjnu8dPhuKGut6cJr449BKDwbnA9BOomcVSztEzHGU6HPXXyNdZbfA6D12f5lWxX2B_pobw3a1gFLnO6mWaNRuK1zfzZcfGTYMATf6d7sj9RcKNS230XPHWGaMlLmNxsgXkEN7a9PwsSVwcKdHg_HU4vYdRX6vkEauOIwVPs4dS7yZXmtvbDaX1zOU4ZYWg0T42sT3nIIl9M2EeFS5Rqms_YzNp8J-YtRz1h5RhtTTNcA5jX4N-xDEVx-vD36bZVzfoMSL2k85PKv7pQGLH-0a3DsR0pePCTBWNORK0g_RZCU_H898-nT1syGzNKWGoPCstWPRvpL9cnHRPM1ZKemRn0nPVm9Bgo0ksuUijgXc5yyrf5K49UU2J5JgFYpSp7aMGOUb1ibrj2sr-D63d61DtzFJ2mwrLm_KHBiN_ECpVhDsRvHe5iOx_APHtImevOUxghtkj-8RJruPgkTVaML2MEDOdL_UYaldeo-5ckZo3VHss7IpLArGOMTEd0bSH8tA8CL8RLQQeSokOMZ79Haxj8yE0EAVZ-k9-O72mmu5I0wH5IPgapNvExeX6O1l3mC4MqLhKPdOZOnTiEBlSrV4ZDH_9fhLUahe5ocZXvXqrud9QGNeTpZsSPeIYubeOC0sOsuqk10sWB7NP-lhifWeDob-IK1JWcgFTytVc99RkZTjUcdG9t8prPlKAagZIsDr1TiX3dy8sXKZ7d9EXQF5P_rHJ8xvmUtCWqbc3V5jL-qe8ANypwHsuva75Q6dtqoBR8vCE5xWgfwB0GzR3Xi_l7KDTsYAQIrDZVyY1UxdzWBwJCrvDrtrNsnt0S7BhBJ4ATCrW5VFPqXyXRiLxHCIv9zgo-NdBZQ4hEXXxMtbem3KgYUB1Rals1bbi8X8MsmselnHfY5LdOseyXWIR2QcrANSAypQUAhwVpsModw7HMdXgV9Uc-HwCMWafOChhBr88tOowqVHttPtwYorYrzriXNRt9LkigESMy1bEDx79CJguitwjQ9IyIEu8quEQb_-7AEXrfDzl_FKgASnnZLrAfZMtgyyddIhBpgAvgR_c8a8Nuro-RGV0aNuunVg8NjL8binz9kgmZvOS38QaP5anf2vgzJ9wC0ZKDg2Ad77dPjBCiCRtVe_dqm7FDA_cS97DkAwVfFawgce1wfWqsrjZvu4k6x3PAUH1UNzQUxVgOGUbqJsaFs3GZIMiI8O6-tZktz8i8oqpr0RjkfUhw_I2szHF3LM20_bFwhtINwg0rZxRTrg4il-_q7jDnVOTqQ7fdgHgiJHZw_OOB7JWoRW6ZlJmx3La8oV93fl1wMGNrpojSR0b6pc8SThsKCUgoY6zajWWa3CesX1ZLUtE7Pfk9eDey3stIWf2acKolZ9fU-gspeACUCN20EhGT-HvBtNBGr_xWk1zVJBgNG29olXCpF26eXNKNCCovsILNDgH06vulDUG_vR5RrGe5LsXksIoTMYsCUitLz4HEehUOd9mWCmLCl00eGRCkwr9EB557lyr7mBK2KPgJkXhNmmPSbDy6hPaQ057zfAd5s_43UBCMtI-aAs5NN4TXHd6IlLwynwc1zsYOQ6z_HARlcMpCV9ac-8eOKsaepgjOAX4YHfg3NekrxA2ynrvwk9U-gCtpxMJ4f1cVx3jExNlIX5LxE46FYIhQ";

	private string recaptchaUrlPost = "";

	private string referer = "";

	private string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.149 Safari/537.36";

	public string Name => "RecaptchaV3Bypass";

	public LinearGradientBrush LinearGradientBrush => new LinearGradientBrush
	{
		StartPoint = new Point(0.0, 0.0),
		EndPoint = new Point(1.0, 1.0),
		GradientStops = new GradientStopCollection
		{
			new GradientStop
			{
				Offset = 0.3,
				Color = ColorConverter("#DB4437")
			},
			new GradientStop
			{
				Offset = 0.1,
				Color = ColorConverter("#F4B400")
			},
			new GradientStop
			{
				Offset = 0.87,
				Color = ColorConverter("#0F9D58")
			}
		}
	};

	public bool LightForeground => false;

	[Text("VariableName:", "")]
	public string VariableName
	{
		get
		{
			return variableName;
		}
		set
		{
			variableName = value;
			OnPropertyChanged("VariableName");
		}
	}

	[Text("Recaptcha Url (GET):", "")]
	public string RecaptchaUrlGet
	{
		get
		{
			return recaptchaUrlGet;
		}
		set
		{
			recaptchaUrlGet = value;
			OnPropertyChanged("RecaptchaUrlGet");
		}
	}

	[Text("BG:", "")]
	public string BG
	{
		get
		{
			return bg;
		}
		set
		{
			bg = value;
			OnPropertyChanged("BG");
		}
	}

	[Text("Recaptcha Url (POST):", "")]
	public string RecaptchaUrlPost
	{
		get
		{
			return recaptchaUrlPost;
		}
		set
		{
			recaptchaUrlPost = value;
			OnPropertyChanged("RecaptchaUrlPost");
		}
	}

	[Text("Referer:", "")]
	public string Referer
	{
		get
		{
			return referer;
		}
		set
		{
			referer = value;
			OnPropertyChanged("Referer");
		}
	}

	[Text("User-Agent:", "")]
	public string UserAgent
	{
		get
		{
			return userAgent;
		}
		set
		{
			userAgent = value;
			OnPropertyChanged("UserAgent");
		}
	}

	public RecaptchaV3Bypass()
	{
		((BlockBase)this).Label = "RecaptchaV3Bypass";
	}

	public override void Process(BotData data)
	{
		BlockRequest val = new BlockRequest();
		val.Url = recaptchaUrlGet;
		val.SetCustomHeaders(new string[6]
		{
			"UserAgent: " + UserAgent,
			"Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8",
			"Accept-Language: en-US,en;q=0.9",
			"Accept-Encoding: gzip, deflate",
			"Upgrade-Insecure-Requests: 1",
			"Connection: keep-alive"
		});
		((BlockBase)val).Process(data);
		string value = Regex.Match(BlockBase.ReplaceValues("<SOURCE>", data), "id=\"recaptcha-token\" value=\"(.*?)\">").Groups[1].Value;
		val.PostData = "v=" + Regex.Match(val.Url, "v=(.*?)&").Groups[1].Value + "&reason=q&c=" + value + "&k=" + Regex.Match(val.Url, "&k=(.*?)&").Groups[1].Value + "&co=" + Regex.Match(val.Url, "&co=(.*?)&").Groups[1].Value + "&hl=en&size=invisible&chr=%5B89%2C64%2C27%5D&vh=13599012192&bg=" + bg;
		string text = BlockBase.ReplaceValues(Referer, data);
		val.SetCustomHeaders(new string[11]
		{
			"UserAgent: " + UserAgent,
			"Accept: */*",
			"Accept-encoding: gzip, deflate, br",
			"accept-language: fa,en;q=0.9,en-GB;q=0.8,en-US;q=0.7",
			$"Content-Length: {val.PostData.Length}",
			"Connection: keep-alive",
			"origin: https://www.google.com",
			"referer: " + (string.IsNullOrEmpty(text) ? val.Url : text),
			"sec-fetch-dest: empty",
			"sec-fetch-mode: cors",
			"sec-fetch-site: same-origin"
		});
		val.Url = recaptchaUrlPost;
		val.Method = HttpMethod.POST;
		((BlockBase)val).Process(data);
		string value2 = Regex.Match(BlockBase.ReplaceValues("<SOURCE>", data), "\"rresp\",\"(.*?)\"").Groups[1].Value;
		BlockBase.InsertVariable(data, false, value2, VariableName, "", "", false, true);
	}

	public override string ToLS(bool indent = true)
	{
		BlockWriter val = new BlockWriter(((object)this).GetType(), indent, ((BlockBase)this).Disabled);
		val.Label(((BlockBase)this).Label).Token((object)"RecaptchaV3Bypass", "").Literal(RecaptchaUrlGet, "")
			.Literal(BG, "")
			.Literal(RecaptchaUrlPost, "");
		if (!val.CheckDefault((object)VariableName, "VariableName"))
		{
			val.Arrow().Token((object)"VAR", "").Literal(VariableName, "")
				.Indent(1);
		}
		return ((object)val).ToString();
	}

	public override BlockBase FromLS(string line)
	{
		string text = line.Trim();
		if (text.StartsWith("#"))
		{
			((BlockBase)this).Label = LineParser.ParseLabel(ref text);
		}
		RecaptchaUrlGet = LineParser.ParseLiteral(ref text, "RecaptchaUrlGet", false, (BotData)null);
		BG = LineParser.ParseLiteral(ref text, "BG", false, (BotData)null);
		RecaptchaUrlPost = LineParser.ParseLiteral(ref text, "RecaptchaUrlPost", false, (BotData)null);

		// Old format had Referer + UserAgent before the variable name; skip them if present
		if (text.TrimStart().StartsWith("\""))
		{
			LineParser.ParseLiteral(ref text, "Referer", false, (BotData)null);   // discard Referer
			if (text.TrimStart().StartsWith("\""))
				LineParser.ParseLiteral(ref text, "UserAgent", false, (BotData)null); // discard UserAgent
			// Variable name in old format is a plain literal at the end
			if (text.TrimStart().StartsWith("\""))
				VariableName = LineParser.ParseLiteral(ref text, "VariableName", false, (BotData)null);
			return (BlockBase)(object)this;
		}

		// New format: -> VAR "name"
		if (LineParser.ParseToken(ref text, (TokenType)3, false, true) == "")
		{
			return (BlockBase)(object)this;
		}
		try
		{
			VariableName = LineParser.ParseToken(ref text, (TokenType)2, true, true);
			return (BlockBase)(object)this;
		}
		catch
		{
			throw new ArgumentException("Variable name not specified");
		}
	}

	private Color ColorConverter(string color)
	{
		return (Color)System.Windows.Media.ColorConverter.ConvertFromString(color);
	}
}
