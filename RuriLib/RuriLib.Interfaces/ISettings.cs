using RuriLib.Models;
using RuriLib.ViewModels;

namespace RuriLib.Interfaces;

public interface ISettings
{
	EnvironmentSettings Environment { get; }

	RLSettingsViewModel RLSettings { get; }
}
