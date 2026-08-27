using RuriLib.Functions.Conditions;
using RuriLib.ViewModels;

namespace RuriLib.Models;

public class Key : ViewModelBase
{
	private string leftTerm = "<SOURCE>";

	private Comparer comparer = Comparer.Contains;

	private string rightTerm = "";

	public string LeftTerm
	{
		get
		{
			return leftTerm;
		}
		set
		{
			leftTerm = value;
			OnPropertyChanged("LeftTerm");
		}
	}

	public Comparer Comparer
	{
		get
		{
			return comparer;
		}
		set
		{
			comparer = value;
			OnPropertyChanged("Comparer");
		}
	}

	public string RightTerm
	{
		get
		{
			return rightTerm;
		}
		set
		{
			rightTerm = value;
			OnPropertyChanged("RightTerm");
		}
	}

	public bool CheckKey(BotData data)
	{
		try
		{
			return Condition.ReplaceAndVerify(LeftTerm, Comparer, RightTerm, data);
		}
		catch
		{
			return false;
		}
	}
}
