using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace RuriLib;

public static class TypeExtension
{
	public static MethodInfo[] GetExtensionMethods<T>(this Type t)
	{
		List<Type> list = new List<Type>();
		list.AddRange(typeof(T).Assembly.GetTypes());
		return (from type in list
			where type.IsSealed && !type.IsGenericType && !type.IsNested
			from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
			where method.IsDefined(typeof(ExtensionAttribute), inherit: false)
			where method.GetParameters()[0].ParameterType == t
			select method).ToArray();
	}

	public static ConstructorInfo GetByParamsCount(this ConstructorInfo[] constructorInfos, int paramsCount)
	{
		return constructorInfos.FirstOrDefault((ConstructorInfo c) => c.GetParameters().Length == paramsCount);
	}

	public static bool IsColor(this Type type)
	{
		return type == typeof(Color);
	}

	public static bool IsNullableEnum(this Type t, out Type enumType)
	{
		Type type = (enumType = Nullable.GetUnderlyingType(t));
		if (type != null)
		{
			return type.IsEnum;
		}
		return false;
	}

	public static bool IsNullable(this Type type, out Type nullableType)
	{
		return (nullableType = Nullable.GetUnderlyingType(type)) != null;
	}

	public static IEnumerable<Type> GetEnumTypes(this Type type, BindingFlags bindingAttr, bool getCanWrite = true)
	{
		return from p in type.GetProperties(bindingAttr)
			where p.PropertyType.IsEnum && p.CanWrite == getCanWrite
			select p into e
			select e.PropertyType;
	}
}
