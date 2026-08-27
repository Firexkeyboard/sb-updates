using System;

namespace RuriLib;

public static class EnumExtensions
{
	public static TEnum ToEnum<TEnum>(this string strEnumValue, TEnum defaultValue, bool ignoreCase = true)
	{
		try
		{
			return (TEnum)Enum.Parse(typeof(TEnum), strEnumValue, ignoreCase);
		}
		catch (Exception)
		{
			return defaultValue;
		}
	}
}
