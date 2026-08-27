using System;

namespace RuriLib;

public static class DateTimeExtensionMethods
{
	public const int TicksPerMicrosecond = 10;

	public const int NanosecondsPerTick = 100;

	public static int Microseconds(this DateTime self)
	{
		return (int)Math.Floor((double)(self.Ticks % 10000) / 10.0);
	}

	public static int Nanoseconds(this DateTime self)
	{
		return (int)(self.Ticks % 10000 % 10) * 100;
	}

	public static DateTime AddMicroseconds(this DateTime self, int microseconds)
	{
		return self.AddTicks(microseconds * 10);
	}

	public static DateTime AddNanoseconds(this DateTime self, int nanoseconds)
	{
		return self.AddTicks((int)Math.Round((double)nanoseconds / 100.0));
	}
}
