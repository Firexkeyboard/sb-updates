namespace RuriLib.ViewModels;

public class CustomInput : ViewModelBase
{
	private string description = "";

	private string variableName = "";

	public string Description
	{
		get
		{
			return description;
		}
		set
		{
			description = value;
			OnPropertyChanged("Description");
		}
	}

	public string VariableName
	{
		get
		{
			return variableName;
		}
		set
		{
			variableName = value;
			OnPropertyChanged("VariableName");
		}
	}

	public int Id { get; set; }

	public CustomInput(int id)
	{
		Id = id;
	}
}
