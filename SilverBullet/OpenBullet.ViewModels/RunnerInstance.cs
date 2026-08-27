using System;
using OpenBullet.Views.Main.Runner;
using RuriLib.Runner;

namespace OpenBullet.ViewModels;

public class RunnerInstance
{
	public Runner View { get; private set; }

	public RunnerViewModel ViewModel { get; private set; }

	public int Id { get; set; }

	public RunnerInstance(int id)
	{
		Id = id;
		ViewModel = new RunnerViewModel(SB.Settings.Environment, SB.Settings.RLSettings, (Random)null);
		View = new Runner(ViewModel);
	}
}
