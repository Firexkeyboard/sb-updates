using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;

namespace RuriLib.LS;

public static class FilterParser
{
	public static object ParseObject(ref object[] values, Type parameterType, bool convertOneVal = true)
	{
		if (parameterType.GetTypeInfo().IsEnum)
		{
			return Enum.Parse(parameterType, values[0].ToString(), ignoreCase: true);
		}
		if (values.Length == 1 && convertOneVal)
		{
			object result = null;
			try
			{
				result = Convert.ChangeType(values[0], parameterType);
			}
			catch
			{
				if (parameterType.IsClass || parameterType.IsEnum || parameterType.IsValueType)
				{
					result = ParseObject(ref values, parameterType, convertOneVal: false);
				}
			}
			try
			{
				values = values.RemoveAt(0);
			}
			catch
			{
			}
			return result;
		}
		if ((parameterType.IsClass || parameterType.IsValueType) && !parameterType.IsAbstract && !parameterType.IsInterface && !parameterType.IsPrimitive)
		{
			ConstructorInfo[] constructors = parameterType.GetConstructors();
			ConstructorInfo constructorInfo = null;
			ParameterInfo[] array = null;
			int num = 0;
			while (num < constructors.Length)
			{
				constructorInfo = constructors[num];
				array = constructorInfo.GetParameters();
				if (array.Length == values.Length || (array.Length == 1 && ParameterIsForValue(array, values[0])))
				{
					break;
				}
				num++;
				constructorInfo = null;
			}
			if (constructorInfo == null)
			{
				constructorInfo = constructors.FirstOrDefault();
			}
			List<object> list = new List<object>();
			object value = null;
			if (array != null && array.Length != 0)
			{
				for (int i = 0; i < array.Length; i++)
				{
					try
					{
						list.Add(ChangeType(values[i], array[i].ParameterType));
					}
					catch (IndexOutOfRangeException)
					{
						list.Add(ChangeType(values.First(), array[i].ParameterType));
					}
					catch (InvalidCastException)
					{
						if (array[i].ParameterType.IsClass || array[i].ParameterType.IsEnum || array[i].ParameterType.IsValueType)
						{
							list.Add(ParseObject(ref values, array[i].ParameterType));
						}
					}
				}
				for (int j = 0; j < list.Count; j++)
				{
					try
					{
						RemoveValue(ref values);
					}
					catch
					{
					}
				}
				value = constructorInfo.Invoke(list.ToArray());
			}
			else if (parameterType.IsColor())
			{
				value = CreateColor(ref values);
			}
			return ChangeType(value, parameterType);
		}
		object result2 = ChangeType(values[0], parameterType);
		RemoveValue(ref values);
		return result2;
	}

	private static Color CreateColor(ref object[] values, bool alpha = false)
	{
		int alpha2 = 255;
		if (values.Length == 4 && alpha)
		{
			try
			{
				alpha2 = int.Parse(values[0].ToString());
				RemoveValue(ref values);
			}
			catch
			{
			}
		}
		int red = int.Parse(values[0].ToString());
		RemoveValue(ref values);
		int green = int.Parse(values[0].ToString());
		RemoveValue(ref values);
		int blue = int.Parse(values[0].ToString());
		RemoveValue(ref values);
		return Color.FromArgb(alpha2, red, green, blue);
	}

	private static object ChangeType(object value, Type parameterType)
	{
		if (parameterType.GetTypeInfo().IsEnum)
		{
			return Enum.Parse(parameterType, value.ToString(), ignoreCase: true);
		}
		if (parameterType.IsNullableEnum(out var enumType))
		{
			try
			{
				return Enum.Parse(enumType, value.ToString());
			}
			catch (ArgumentException)
			{
				return null;
			}
		}
		if (parameterType.IsNullable(out var nullableType))
		{
			if (nullableType.IsValueType)
			{
				object[] values = new object[1] { value };
				try
				{
					return ParseObject(ref values, nullableType);
				}
				catch (ArgumentException)
				{
					return null;
				}
			}
			try
			{
				return Convert.ChangeType(value, nullableType);
			}
			catch (ArgumentException)
			{
				return null;
			}
		}
		try
		{
			return Convert.ChangeType(value, parameterType);
		}
		catch (ArgumentException)
		{
			return null;
		}
		catch (FormatException)
		{
			return null;
		}
	}

	private static object ChangeType(object[] values, Type parameterType)
	{
		if (parameterType.GetTypeInfo().IsEnum)
		{
			return Enum.Parse(parameterType, values[0].ToString(), ignoreCase: true);
		}
		List<object> list = new List<object>();
		int num = 0;
		if (num < values.Length)
		{
			return Convert.ChangeType(values[num], parameterType);
		}
		return list.ToArray();
	}

	private static void RemoveValue(ref object[] values, int index = 0)
	{
		values = values.RemoveAt(index);
	}

	private static bool ParameterIsForValue(ParameterInfo[] parameters, object value)
	{
		try
		{
			Type parameterType = parameters.FirstOrDefault().ParameterType;
			return parameterType == ChangeType(value, parameterType).GetType();
		}
		catch (Exception)
		{
			return false;
		}
	}
}
