using System;
using System.Diagnostics;

namespace RuriLib.Models;

public class Usage
{
	private static string instance;

	private static int pid;

	private static DateTime lastTime;

	private static TimeSpan lastTotalProcessorTime;

	private static DateTime curTime;

	private static TimeSpan curTotalProcessorTime;

	public double Cpu { get; private set; }

	public double Ram { get; private set; }

	private Usage()
	{
	}

	public static Usage Get()
	{
		Process currentProcess = Process.GetCurrentProcess();
		if (currentProcess.ProcessName != instance || pid != currentProcess.Id)
		{
			string[] instanceNames = new PerformanceCounterCategory("Process").GetInstanceNames();
			foreach (string text in instanceNames)
			{
				if (text == currentProcess.ProcessName)
				{
					using PerformanceCounter performanceCounter = new PerformanceCounter("Process", "ID Process", text, readOnly: true);
					if ((pid = currentProcess.Id) == (int)performanceCounter.RawValue)
					{
						instance = text;
						break;
					}
				}
				instance = string.Empty;
			}
		}
		if (instance == string.Empty)
		{
			return null;
		}
		double num = 0.0;
		_ = lastTime;
		if (lastTime == default(DateTime))
		{
			lastTime = DateTime.Now;
			lastTotalProcessorTime = currentProcess.TotalProcessorTime;
		}
		else
		{
			curTime = DateTime.Now;
			curTotalProcessorTime = currentProcess.TotalProcessorTime;
			num = (curTotalProcessorTime.TotalMilliseconds - lastTotalProcessorTime.TotalMilliseconds) / curTime.Subtract(lastTime).TotalMilliseconds / Convert.ToDouble(Environment.ProcessorCount);
			lastTime = curTime;
			lastTotalProcessorTime = curTotalProcessorTime;
		}
		return new Usage
		{
			Cpu = Math.Round((num > 0.0) ? (num * 100.0) : 0.0, 2),
			Ram = Math.Round((double)currentProcess.PagedMemorySize64 / 1024.0 / 1024.0, 2)
		};
	}

	public override string ToString()
	{
		return $"{Cpu}/{SizeExtensions.SizeSuffix((long)Ram, 2)}";
	}
}
