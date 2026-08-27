using System;
using System.Linq;
using System.Text.RegularExpressions;
using RuriLib.LS;
using RuriLib.ViewModels;

namespace RuriLib;

public class Config : ViewModelBase
{
	public ConfigSettings Settings { get; set; }

	public string Script { get; set; }

	public bool SeleniumPresent
	{
		get
		{
			if (!Script.Contains("NAVIGATE") && !Script.Contains("BROWSERACTION") && !Script.Contains("ELEMENTACTION") && !Script.Contains("EXECUTEJS"))
			{
				return Script.Contains("MOUSEACTION");
			}
			return true;
		}
	}

	public bool DangerousScriptPresent => Script.ToUpper().Contains("BEGIN SCRIPT");

	public bool CaptchasNeeded => Regex.Match(Script, "(^|\n)(#[^ ]* )?(RE)?CAPTCHA").Success;

	public bool HasCFBypass => Script.Contains("BYPASSCF");

	public bool OcrNeeded => ScriptExtension.HasBlock(Script, "OCR");

	public bool JsNeeded
	{
		get
		{
			if (ScriptExtension.HasScript(Script, "JavaScript") && Script.Contains("BEGIN SCRIPT"))
			{
				return Script.Contains("END SCRIPT");
			}
			return false;
		}
	}

	public int BlocksAmount => (from l in Script.Split(new string[1] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
		where BlockParser.IsBlock(l)
		select l).Count();

	public string AllowedWordlists => Settings.AllowedWordlist1 + " | " + Settings.AllowedWordlist2;

	public string LastModifiedString => Settings.LastModified.ToShortDateString() + " " + Settings.LastModified.ToShortTimeString();

	public Config(ConfigSettings settings, string script)
	{
		Settings = settings;
		Script = script;
	}
}
