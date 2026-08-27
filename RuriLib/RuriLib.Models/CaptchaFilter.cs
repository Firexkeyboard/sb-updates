using System;
using System.Reflection;

namespace RuriLib.Models;

public class CaptchaFilter
{
	public MethodInfo Method { get; set; }

	public object Parameter { get; set; }

	public Type ParameterType { get; set; }

	public string Name { get; set; }
}
