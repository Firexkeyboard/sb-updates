using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RuriLib.LS;

#pragma warning disable CS0612 // BlockImageCaptcha and BlockRecaptcha are marked Obsolete but still referenced by the legacy LoliScript parser
public static class BlockParser
{
	public static Dictionary<string, Type> BlockMappings { get; set; } = new Dictionary<string, Type>
	{
		{
			"BYPASSCF",
			typeof(BlockBypassCF)
		},
		{
			"CF-CLEARANCE",
			typeof(BlockCfClearance)
		},
		{
			"TURNSTILE",
			typeof(BlockTurnstile)
		},
		{
			"CAPMONSTER",
			typeof(BlockCapMonster)
		},
		{
			"RECAPTCHA-V3",
			typeof(BlockRecaptchaV3)
		},
		{
			"RECAPTCHA-V2-INVISIBLE",
			typeof(BlockRecaptchaV2Invisible)
		},
		{
			"AKM-COOKIES",
			typeof(BlockAkmCookies)
		},
		{
			"DATADOME",
			typeof(BlockDataDome)
		},
		{
			"ALTCHA",
			typeof(BlockAltcha)
		},
		{
			"RECAPTCHAV3BYPASS",       // legacy alias — kept for backward compat
			typeof(BlockRecaptchaV3Bypass)
		},
		{
			"RecaptchaV3Bypass",       // new canonical name
			typeof(BlockRecaptchaV3Bypass)
		},
		{
			"FRIENDLYCAPTCHA",
			typeof(BlockFriendlyCaptcha)
		},
		{
			"SOLVECAPTCHA",
			typeof(BlockSolveCaptcha)
		},
		{
			"REPORTCAPTCHA",
			typeof(BlockReportCaptcha)
		},
		{
			"CAPTCHA",
			typeof(BlockImageCaptcha)
		},
		{
			"FUNCTION",
			typeof(BlockFunction)
		},
		{
			"KEYCHECK",
			typeof(BlockKeycheck)
		},
		{
			"PARSE",
			typeof(BlockParse)
		},
		{
			"RECAPTCHA",
			typeof(BlockRecaptcha)
		},
		{
			"REQUEST",
			typeof(BlockRequest)
		},
		{
			"TCP",
			typeof(BlockTCP)
		},
		{
			"OCR",
			typeof(BlockOcr)
		},
		{
			"WS",
			typeof(BlockWebSocket)
		},
		{
			"UTILITY",
			typeof(BlockUtility)
		},
		{
			"BROWSERACTION",
			typeof(SBlockBrowserAction)
		},
		{
			"ELEMENTACTION",
			typeof(SBlockElementAction)
		},
		{
			"EXECUTEJS",
			typeof(SBlockExecuteJS)
		},
		{
			"NAVIGATE",
			typeof(SBlockNavigate)
		},
		{
			"DNS",
			typeof(BlockDns)
		}
	};

	public static bool IsBlock(string line)
	{
		return BlockMappings.Keys.Select((string n) => n.ToUpper()).Contains(GetBlockType(line).ToUpper());
	}

	public static string GetBlockType(string line)
	{
		return Regex.Match(line, "^!?(#[^ ]* )?([^ ]*)").Groups[2].Value;
	}

	public static BlockBase Parse(string line)
	{
		string input = line.Trim();
		if (input == string.Empty)
		{
			throw new ArgumentNullException();
		}
		bool flag = input.StartsWith("!");
		if (flag)
		{
			input = input.Substring(1).Trim();
		}
		string text = LineParser.ParseToken(ref input, TokenType.Label, essential: false);
		string text2 = "";
		try
		{
			text2 = LineParser.ParseToken(ref input, TokenType.Parameter, essential: true);
		}
		catch
		{
			throw new ArgumentException("Missing identifier");
		}
		BlockBase blockBase = (Activator.CreateInstance(BlockMappings[text2]) as BlockBase).FromLS(input);
		if (blockBase != null)
		{
			blockBase.Disabled = flag;
		}
		if (blockBase != null && text != string.Empty)
		{
			blockBase.Label = text.Replace("#", "");
		}
		return blockBase;
	}
}
