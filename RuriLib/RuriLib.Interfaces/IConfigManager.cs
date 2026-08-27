using System.Collections.Generic;
using RuriLib.ViewModels;

namespace RuriLib.Interfaces;

public interface IConfigManager
{
	IEnumerable<ConfigViewModel> Configs { get; }

	void Add(ConfigViewModel config);

	void Rescan();

	void Remove(ConfigViewModel config);

	void Remove(IEnumerable<ConfigViewModel> configs);

	void Update(ConfigViewModel config);
}
