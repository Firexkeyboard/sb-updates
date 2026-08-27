using System;
using System.Collections.Generic;
using System.Linq;

namespace RuriLib.Models;

public class CVar
{
	public enum VarType
	{
		Single,
		List,
		Dictionary
	}

	public string Name { get; set; }

	public dynamic Value { get; set; }

	public bool IsCapture { get; set; }

	public VarType Type { get; set; }

	public bool Hidden { get; set; }

	public CVar()
	{
	}

	public CVar(string name, string value, bool isCapture = false, bool hidden = false)
	{
		Name = name;
		Value = value;
		IsCapture = isCapture;
		Type = VarType.Single;
		Hidden = hidden;
	}

	public CVar(string name, List<string> value, bool isCapture = false, bool hidden = false)
	{
		Name = name;
		Value = value;
		IsCapture = isCapture;
		Type = VarType.List;
		Hidden = hidden;
	}

	public CVar(string name, Dictionary<string, string> value, bool isCapture = false, bool hidden = false)
	{
		Name = name;
		Value = value;
		IsCapture = isCapture;
		Type = VarType.Dictionary;
		Hidden = hidden;
	}

	public CVar(string name, VarType type, dynamic value, bool isCapture = false, bool hidden = false)
	{
		Name = name;
		Value = (object)value;
		IsCapture = isCapture;
		Type = type;
		Hidden = hidden;
	}

	public override string ToString()
	{
		switch (Type)
		{
		case VarType.Single:
			return Value;
		case VarType.List:
			if (Value.GetType() == typeof(List<string>))
			{
				return "[" + string.Join(", ", (List<string>)Value) + "]";
			}
			if (Value.GetType() == typeof(object[]))
			{
				return "[" + string.Join(", ", Value) + "]";
			}
			return "";
		case VarType.Dictionary:
			return "{" + string.Join(", ", ((Dictionary<string, string>)Value).Select((KeyValuePair<string, string> d) => "(" + d.Key + ", " + d.Value + ")")) + "}";
		default:
			return base.ToString();
		}
	}

	public string GetListItem(int index)
	{
		if (Type != VarType.List)
		{
			return null;
		}
		List<string> list = Value as List<string>;
		if (index < 0)
		{
			index = list.Count + index;
		}
		if (index > list.Count - 1 || index < 0)
		{
			return null;
		}
		return list[index];
	}

	public string GetDictValue(string key)
	{
		Dictionary<string, string> dictionary = Value as Dictionary<string, string>;
		if (dictionary.ContainsKey(key))
		{
			return dictionary.First((KeyValuePair<string, string> d) => d.Key == key).Value;
		}
		throw new Exception("Key not in dictionary");
	}

	public string GetDictKey(string value)
	{
		Dictionary<string, string> dictionary = Value as Dictionary<string, string>;
		if (dictionary.ContainsValue(value))
		{
			return dictionary.First((KeyValuePair<string, string> d) => d.Value == value).Key;
		}
		throw new Exception("Value not in dictionary");
	}
}
