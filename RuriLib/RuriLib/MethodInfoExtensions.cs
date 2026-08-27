using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RuriLib;

public static class MethodInfoExtensions
{
	public static Delegate CreateDelegate(this MethodInfo method)
	{
		if (method == null)
		{
			throw new ArgumentNullException("method");
		}
		if (!method.IsStatic)
		{
			throw new ArgumentException("The provided method must be static.", "method");
		}
		if (method.IsGenericMethod)
		{
			throw new ArgumentException("The provided method must not be generic.", "method");
		}
		return method.CreateDelegate(Expression.GetDelegateType((from parameter in method.GetParameters()
			select parameter.ParameterType).Concat(new Type[1] { method.ReturnType }).ToArray()));
	}
}
