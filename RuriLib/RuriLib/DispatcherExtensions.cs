using System;
using System.Windows.Threading;

namespace RuriLib;

public static class DispatcherExtensions
{
	public static void InvokeIfRequired(this Dispatcher dispatcher, Action action)
	{
		if (dispatcher != null)
		{
			if (!dispatcher.CheckAccess())
			{
				dispatcher.BeginInvoke(action, DispatcherPriority.ContextIdle);
			}
			else
			{
				action();
			}
		}
	}
}
