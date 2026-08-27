using System.Collections.Generic;

namespace RuriLib.Interfaces;

public interface IRunnerManager
{
	IEnumerable<IRunner> Runners { get; }

	IRunner Create();

	void Remove(IRunner runner);

	void RemoveAll();
}
