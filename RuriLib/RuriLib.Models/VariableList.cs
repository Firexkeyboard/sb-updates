using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RuriLib.Functions.Conditions;

namespace RuriLib.Models;

public class VariableList
{
	private readonly object _lock = new object();

	public List<CVar> All { get; set; }

	[JsonIgnore]
	public List<CVar> Captures { get { lock (_lock) return All.Where((CVar v) => v.IsCapture && !v.Hidden).ToList(); } }

	[JsonIgnore]
	public List<CVar> Singles { get { lock (_lock) return All.Where((CVar v) => v.Type == CVar.VarType.Single).ToList(); } }

	[JsonIgnore]
	public List<CVar> Lists { get { lock (_lock) return All.Where((CVar v) => v.Type == CVar.VarType.List).ToList(); } }

	[JsonIgnore]
	public List<CVar> Dictionaries { get { lock (_lock) return All.Where((CVar v) => v.Type == CVar.VarType.Dictionary).ToList(); } }

	public VariableList()
	{
		All = new List<CVar>();
	}

	public VariableList(List<CVar> list)
	{
		All = list;
	}

	public CVar Get(string name)
	{
		lock (_lock) return All.FirstOrDefault((CVar v) => v.Name == name);
	}

	public CVar Get(string name, CVar.VarType type)
	{
		lock (_lock) return All.FirstOrDefault((CVar v) => v.Name == name && v.Type == type);
	}

	public string GetSingle(string name)
	{
		try
		{
			return (string)Get(name, CVar.VarType.Single).Value;
		}
		catch
		{
			return null;
		}
	}

	public List<string> GetList(string name)
	{
		try
		{
			return (List<string>)Get(name, CVar.VarType.List).Value;
		}
		catch
		{
			return null;
		}
	}

	public Dictionary<string, string> GetDictionary(string name)
	{
		try
		{
			return (Dictionary<string, string>)Get(name, CVar.VarType.Dictionary).Value;
		}
		catch
		{
			return null;
		}
	}

	public bool VariableExists(string name)
	{
		lock (_lock) return All.Any((CVar v) => v.Name == name);
	}

	public bool VariableExists(string name, CVar.VarType type)
	{
		lock (_lock) return All.Any((CVar v) => v.Name == name && v.Type == type);
	}

	public void Set(CVar variable)
	{
		lock (_lock)
		{
			All.RemoveAll((CVar v) => v.Name == variable.Name && !v.Hidden);
			All.Add(variable);
		}
	}

	public void SetHidden(string name, dynamic value)
	{
		lock (_lock)
		{
			var existing = All.FirstOrDefault((CVar v) => v.Name == name && v.Hidden);
			if (existing != null)
				existing.Value = (object)value;
			else
				All.Add(new CVar(name, value, false, true));
		}
	}

	public void SetNew(CVar variable)
	{
		lock (_lock)
		{
			if (!All.Any((CVar v) => v.Name == variable.Name))
			{
				All.RemoveAll((CVar v) => v.Name == variable.Name && !v.Hidden);
				All.Add(variable);
			}
		}
	}

	public void Remove(string name)
	{
		lock (_lock) All.RemoveAll((CVar v) => v.Name == name && !v.Hidden);
	}

	public void Remove(Comparer comparer, string name, BotData data)
	{
		lock (_lock) All.RemoveAll((CVar v) => Condition.ReplaceAndVerify(v.Name, comparer, name, data) && !v.Hidden);
	}

	public string ToCaptureString()
	{
		return string.Join(" | ", Captures.Select((CVar c) => c.Name + " = " + c.ToString()));
	}
}
