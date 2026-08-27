using System;

namespace RuriLib.LS;

internal class SysConsole
{
	public void log(string o)
	{
		Console.WriteLine(o);
	}

	public void Print(object o)
	{
		Console.WriteLine(o);
	}

	public void log(params object[] o)
	{
		Console.WriteLine(o);
	}
}
