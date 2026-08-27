using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RuriLib.ViewModels;

namespace RuriLib.Models;

public class DataRule : ViewModelBase
{
	private string sliceName = "";

	private RuleType ruleType;

	private string ruleString = "Lowercase";

	[JsonIgnore]
	public List<RuleType> RuleTypes { get; set; } = new List<RuleType>(Enum.GetValues(typeof(RuleType)).Cast<RuleType>());

	[JsonIgnore]
	public List<string> RuleStrings { get; set; } = new List<string>(new string[4] { "Lowercase", "Uppercase", "Digit", "Symbol" });

	public string SliceName
	{
		get
		{
			return sliceName;
		}
		set
		{
			sliceName = value;
			OnPropertyChanged("SliceName");
		}
	}

	public RuleType RuleType
	{
		get
		{
			return ruleType;
		}
		set
		{
			ruleType = value;
			OnPropertyChanged("RuleType");
		}
	}

	public string RuleString
	{
		get
		{
			return ruleString;
		}
		set
		{
			ruleString = value;
			OnPropertyChanged("RuleString");
		}
	}

	public int Id { get; set; }

	[JsonIgnore]
	public bool TypeInitialized { get; set; }

	[JsonIgnore]
	public bool StringInitialized { get; set; }

	public DataRule(int id)
	{
		Id = id;
	}
}
