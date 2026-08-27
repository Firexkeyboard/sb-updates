using System;
using RuriLib.Models;

namespace RuriLib.Runner;

public interface IRunnerMessaging
{
	event Action<IRunnerMessaging, LogLevel, string, bool, int> MessageArrived;

	event Action<IRunnerMessaging> WorkerStatusChanged;

	event Action<IRunnerMessaging, Hit> FoundHit;

	event Action<IRunnerMessaging> ReloadProxies;

	event Action<IRunnerMessaging, Action> DispatchAction;

	event Action<IRunnerMessaging> SaveProgress;

	event Action<IRunnerMessaging> AskCustomInputs;
}
