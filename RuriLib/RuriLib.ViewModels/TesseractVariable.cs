namespace RuriLib.ViewModels;

public class TesseractVariable
{
	public string Name { get; set; }

	public string Value { get; set; }

	public VariableValueType ValueType { get; set; }

	public override string ToString()
	{
		return $"{Name}:{Value}:{ValueType}";
	}
}
