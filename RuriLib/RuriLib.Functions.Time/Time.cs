using System;
using System.Globalization;

namespace RuriLib.Functions.Time;

public static class Time
{
	private static readonly DateTime _utcEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	public static long ToUnixTimeSeconds(this DateTime dateTime)
	{
		return (long)dateTime.ToUniversalTime().Subtract(_utcEpoch).TotalSeconds;
	}

	public static long ToUnixTimeMilliseconds(this DateTime dateTime)
	{
		return (long)dateTime.ToUniversalTime().Subtract(_utcEpoch).TotalMilliseconds;
	}

	public static DateTime ToDateTime(this string time, string format)
	{
		return DateTime.ParseExact(time, format, new CultureInfo("en-US"), DateTimeStyles.AllowWhiteSpaces);
	}

	public static DateTime ToDateTime(this double unixTime)
	{
		if (unixTime < 10000000000.0)
		{
			unixTime *= 1000.0;
		}
		return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(unixTime).ToUniversalTime();
	}

	public static string ToISO8601(this DateTime dateTime)
	{
		return dateTime.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffZ");
	}
}

// OB2 compat: configs may reference RuriLib.Functions.Time.TimeConverter.*
public static class TimeConverter
{
	public static DateTime ToDateTimeUtc(long unixTimestamp, bool asUtc = true)
	{
		var dt = DateTimeOffset.FromUnixTimeMilliseconds(unixTimestamp).UtcDateTime;
		return asUtc ? dt : dt.ToLocalTime();
	}

	// OB2 compat: RuriLib.Functions.Time.TimeConverter.ToUnixTime(DateTime.Now, true)
	// Returns Unix timestamp in seconds (inSeconds=true) or milliseconds (inSeconds=false).
	public static long ToUnixTime(DateTime dt, bool inSeconds = true)
	{
		var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		long secs = (long)dt.ToUniversalTime().Subtract(epoch).TotalSeconds;
		return inSeconds ? secs : secs * 1000L;
	}
}
