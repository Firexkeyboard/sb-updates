namespace RuriLib;

public static class ConvertTimeExtensions
{
	public static double ConvertMillisecondsToNanoseconds(double milliseconds)
	{
		return milliseconds * 1000000.0;
	}

	public static double ConvertMicrosecondsToNanoseconds(double microseconds)
	{
		return microseconds * 1000.0;
	}

	public static double ConvertMillisecondsToMicroseconds(double milliseconds)
	{
		return milliseconds * 1000.0;
	}

	public static double ConvertNanosecondsToMilliseconds(double nanoseconds)
	{
		return nanoseconds * 1E-06;
	}

	public static double ConvertMicrosecondsToMilliseconds(double microseconds)
	{
		return microseconds * 0.001;
	}

	public static double ConvertNanosecondsToMicroseconds(double nanoseconds)
	{
		return nanoseconds * 0.001;
	}
}
