using System;
using System.IO;

namespace RuriLib.LS;

public class BlockWriter : StringWriter
{
	private bool Indented { get; set; }

	private Type Type { get; set; }

	private object Block { get; set; }

	private bool Disabled { get; set; }

	public BlockWriter(Type blockType, bool indented = true, bool disabled = false)
	{
		Type = blockType;
		Block = Activator.CreateInstance(blockType);
		Indented = indented;
		Disabled = disabled;
		if (Disabled)
		{
			Write("!");
		}
	}

	public BlockWriter Token(dynamic token, string property = "")
	{
		if (property != string.Empty && CheckDefault(token, property))
		{
			return this;
		}
		Write($"{(object)token.ToString()} ");
		return this;
	}

	public BlockWriter Integer(int integer, string property = "")
	{
		if (property != string.Empty && CheckDefault(integer, property))
		{
			return this;
		}
		Write($"{integer} ");
		return this;
	}

	public BlockWriter Literal(string literal, string property = "")
	{
		if (property != string.Empty && CheckDefault(literal, property))
		{
			return this;
		}
		Write("\"" + literal.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\" ");
		return this;
	}

	public BlockWriter Float(float floatVar, string property = "")
	{
		if (property != "" && CheckDefault(floatVar, property))
		{
			return this;
		}
		Write($"{floatVar} ");
		return this;
	}

	public BlockWriter Arrow()
	{
		Write("-> ");
		return this;
	}

	public BlockWriter Label(string label)
	{
		if (CheckDefault(label, "Label"))
		{
			return this;
		}
		Write("#" + label.Replace(" ", "_") + " ");
		return this;
	}

	public BlockWriter Boolean(bool boolean, string property)
	{
		if (property != string.Empty && CheckDefault(boolean, property))
		{
			return this;
		}
		Write(property + "=" + boolean.ToString().ToUpper() + " ");
		return this;
	}

	public BlockWriter Indent(int spacing = 1)
	{
		if (Indented)
		{
			WriteLine();
			if (Disabled)
			{
				Write("!");
			}
			for (int i = 0; i < spacing; i++)
			{
				Write("  ");
			}
		}
		return this;
	}

	public BlockWriter Return()
	{
		WriteLine();
		return this;
	}

	public bool CheckDefault(object value, string property)
	{
		object value2 = Type.GetProperty(property).GetValue(Block);
		return value.Equals(value2);
	}
}
