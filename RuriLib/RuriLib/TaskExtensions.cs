using System;
using System.Threading.Tasks;

namespace RuriLib;

public static class TaskExtensions
{
	public static async Task WaitWhile(Func<bool> condition, int frequency = 25, int timeout = -1)
	{
		Task task = Task.Run(async delegate
		{
			while (condition())
			{
				await Task.Delay(frequency);
			}
		});
		object obj = task;
		if (obj != await Task.WhenAny(task, Task.Delay(timeout)))
		{
			throw new TimeoutException();
		}
	}

	public static async Task WaitUntil(Func<bool> condition, int frequency = 25, int timeout = -1)
	{
		Task task = Task.Run(async delegate
		{
			while (!condition())
			{
				await Task.Delay(frequency);
			}
		});
		object obj = task;
		if (obj != await Task.WhenAny(task, Task.Delay(timeout)))
		{
			throw new TimeoutException();
		}
	}
}
