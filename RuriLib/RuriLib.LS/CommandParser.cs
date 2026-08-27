using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace RuriLib.LS;

public class CommandParser
{
	public enum CommandName
	{
		PRINT,
		SET,
		DELETE,
		MOUSEACTION,
		STOP,
		ForceStop
	}

	public static bool IsCommand(string line)
	{
		GroupCollection groups = Regex.Match(line, "^([^ ]*)").Groups;
		return (from n in Enum.GetNames(typeof(CommandName))
			select n.ToUpper()).Contains(groups[1].Value.ToUpper());
	}

	public static Action Parse(string line, BotData data)
	{
		string input = line.Trim();
		if (input == string.Empty)
		{
			throw new ArgumentNullException();
		}
		LineParser.ParseToken(ref input, TokenType.Label, essential: false);
		string text = "";
		try
		{
			text = LineParser.ParseToken(ref input, TokenType.Parameter, essential: true);
		}
		catch
		{
			throw new ArgumentException("Missing identifier");
		}
		return (CommandName)Enum.Parse(typeof(CommandName), text, ignoreCase: true) switch
		{
			CommandName.PRINT => delegate
			{
				data.Log(new LogEntry(BlockBase.ReplaceValues(input, data), Colors.White));
			}, 
			CommandName.SET => SetParser.Parse(input, data), 
			CommandName.DELETE => DeleteParser.Parse(input, data), 
			CommandName.MOUSEACTION => MouseActionParser.Parse(input, data), 
			CommandName.STOP => delegate
			{
				data.Worker?.CancelAsync();
				data.Log(new LogEntry("Stoped!", Colors.OrangeRed));
			}, 
			CommandName.ForceStop => delegate
			{
				data.Worker?.Abort();
				data.Log(new LogEntry("Force Stoped!", Colors.OrangeRed));
			}, 
			_ => throw new ArgumentException("Invalid identifier '" + text + "'"), 
		};
	}
}
