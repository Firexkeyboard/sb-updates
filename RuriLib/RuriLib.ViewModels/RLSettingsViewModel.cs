namespace RuriLib.ViewModels;

public class RLSettingsViewModel
{
	public SettingsGeneral General { get; set; } = new SettingsGeneral();

	public SettingsProxies Proxies { get; set; } = new SettingsProxies();

	public SettingsCaptchas Captchas { get; set; } = new SettingsCaptchas();

	public SettingsSelenium Selenium { get; set; } = new SettingsSelenium();

	public SettingsOcr Ocr { get; set; } = new SettingsOcr();

	public SettingsCefSharp CefSharp { get; set; } = new SettingsCefSharp();

	public void Reset()
	{
		General.Reset();
		Proxies.Reset();
		Captchas.Reset();
		Selenium.Reset();
		Ocr.Reset();
	}
}
