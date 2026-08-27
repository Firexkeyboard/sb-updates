namespace RuriLib.Interfaces;

public interface IApplication
{
	IRunnerManager RunnerManager { get; }

	IProxyManager ProxyManager { get; }

	IProxyChecker ProxyChecker { get; }

	IWordlistManager WordlistManager { get; }

	IConfigManager ConfigManager { get; }

	IHitsDB HitsDB { get; }

	IAlerter Alerter { get; }

	ILogger Logger { get; }

	ISettings Settings { get; }
}
