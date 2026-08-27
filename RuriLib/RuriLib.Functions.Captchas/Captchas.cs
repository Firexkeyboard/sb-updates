using System;
using CaptchaSharp;
using CaptchaSharp.Services;
using CaptchaSharp.Services.More;
using RuriLib.Enums;
using RuriLib.ViewModels;

namespace RuriLib.Functions.Captchas;

public static class Captchas
{
	public static CaptchaService GetService(SettingsCaptchas settings)
	{
		CaptchaService captchaService = settings.CurrentService switch
		{
			CaptchaServiceType.AntiCaptcha => new AntiCaptchaService(settings.AntiCapToken), 
			CaptchaServiceType.AzCaptcha => new AzCaptchaService(settings.AZCapToken), 
			CaptchaServiceType.CaptchasIO => new CaptchasIOService(settings.CIOToken), 
			CaptchaServiceType.CustomTwoCaptcha => new CustomTwoCaptchaService(settings.CustomTwoCapToken, new Uri($"http://{settings.CustomTwoCapDomain}:{settings.CustomTwoCapPort}")), 
			CaptchaServiceType.DeathByCaptcha => new DeathByCaptchaService(settings.DBCUser, settings.DBCPass), 
			CaptchaServiceType.DeCaptcher => new DeCaptcherService(settings.DCUser, settings.DCPass), 
			CaptchaServiceType.ImageTyperz => new ImageTyperzService(settings.ImageTypToken), 
			CaptchaServiceType.CapMonster => new CapMonsterService(settings.CustomTwoCapToken, new Uri($"http://{settings.CustomTwoCapDomain}:{settings.CustomTwoCapPort}")), 
			CaptchaServiceType.RuCaptcha => new RuCaptchaService(settings.RuCapToken), 
			CaptchaServiceType.SolveCaptcha => new SolveCaptchaService(settings.SCToken), 
			CaptchaServiceType.SolveRecaptcha => new SolveRecaptchaService(settings.SRToken), 
			CaptchaServiceType.TrueCaptcha => new TrueCaptchaService(settings.TrueCapUser, settings.TrueCapToken), 
			CaptchaServiceType.TwoCaptcha => new TwoCaptchaService(settings.TwoCapToken), 
			_ => throw new NotSupportedException(), 
		};
		captchaService.Timeout = TimeSpan.FromSeconds(settings.Timeout);
		return captchaService;
	}
}
