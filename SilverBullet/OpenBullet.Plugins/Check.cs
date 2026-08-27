using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PluginFramework;
using PluginFramework.Attributes;
using RuriLib.Interfaces;
using RuriLib.Models;
using RuriLib.ViewModels;

namespace OpenBullet.Plugins;

public static class Check
{
	private static Dictionary<Type, Type> _requiredPropertyTypes = new Dictionary<Type, Type>
	{
		{
			typeof(InfoText),
			typeof(string)
		},
		{
			typeof(Text),
			typeof(string)
		},
		{
			typeof(Numeric),
			typeof(int)
		},
		{
			typeof(Checkbox),
			typeof(bool)
		},
		{
			typeof(TextMulti),
			typeof(string[])
		},
		{
			typeof(FilePicker),
			typeof(string)
		},
		{
			typeof(Dropdown),
			typeof(string)
		},
		{
			typeof(WordlistPicker),
			typeof(Wordlist)
		},
		{
			typeof(ConfigPicker),
			typeof(ConfigViewModel)
		}
	};

	public static bool InputProperty(PropertyInfo property)
	{
		if (property.GetCustomAttributes().Count((Attribute a) => a is InputField) != 1)
		{
			return false;
		}
		InputField customAttribute = ((MemberInfo)property).GetCustomAttribute<InputField>();
		if (!_requiredPropertyTypes.ContainsKey(((object)customAttribute).GetType()))
		{
			throw new Exception($"Unknown attribute type {((object)customAttribute).GetType()}");
		}
		Type type = _requiredPropertyTypes[((object)customAttribute).GetType()];
		if (property.PropertyType != type)
		{
			throw new Exception($"The property {property.Name} must be of type {type}");
		}
		return true;
	}

	public static bool Method(IPlugin plugin, MethodInfo method)
	{
		if (method.GetCustomAttributes().Count((Attribute a) => a is Button) != 1)
		{
			return false;
		}
		((MemberInfo)method).GetCustomAttribute<Button>();
		ParameterInfo[] parameters = method.GetParameters();
		if (parameters.Length > 1)
		{
			return false;
		}
		if (parameters.Length == 1 && !parameters.Any((ParameterInfo p) => p.ParameterType == typeof(IApplication)))
		{
			return false;
		}
		return true;
	}
}
