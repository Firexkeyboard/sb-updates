using System;
using System.Collections.Generic;
using RuriLib.Models;
using RuriLib.Models.Stats;
using RuriLib.Runner;

namespace RuriLib.Interfaces;

public interface IRunner
{
	bool Busy { get; }

	int Progress { get; }

	IEnumerable<ValidData> Checked { get; }

	RunnerStats Stats { get; }

	Config Config { get; }

	Wordlist Wordlist { get; }

	int BotsAmount { get; set; }

	ProxyMode ProxyMode { get; set; }

	int StartingPoint { get; set; }

	event Action<IRunnerMessaging, LogLevel, string, bool, int> MessageArrived;

	event Action<IRunnerMessaging> WorkerStatusChanged;

	event Action<IRunnerMessaging, Hit> FoundHit;

	event Action<IRunnerMessaging> ReloadProxies;

	event Action<IRunnerMessaging, Action> DispatchAction;

	event Action<IRunnerMessaging> SaveProgress;

	event Action<IRunnerMessaging> AskCustomInputs;

	event Action<IRunnerMessaging> ConfigChanged;

	event Action<IRunnerMessaging> WordlistChanged;

	void Start();

	void Stop();

	void SetConfig(Config config, bool setRecommended);

	void SetWordlist(Wordlist wordlist);
}
