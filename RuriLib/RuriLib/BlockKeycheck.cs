using System.Collections.Generic;
using RuriLib.Functions.Conditions;
using RuriLib.LS;
using RuriLib.Models;

namespace RuriLib;

public class BlockKeycheck : BlockBase
{
	private bool banOn4XX;

	private bool banOnToCheck = true;

	public List<KeyChain> KeyChains = new List<KeyChain>();

	public bool BanOn4XX
	{
		get
		{
			return banOn4XX;
		}
		set
		{
			banOn4XX = value;
			OnPropertyChanged("BanOn4XX");
		}
	}

	public bool BanOnToCheck
	{
		get
		{
			return banOnToCheck;
		}
		set
		{
			banOnToCheck = value;
			OnPropertyChanged("BanOnToCheck");
		}
	}

	public BlockKeycheck()
	{
		base.Label = "KEY CHECK";
	}

	public override BlockBase FromLS(string line)
	{
		string input = line.Trim();
		if (input.StartsWith("#"))
		{
			base.Label = LineParser.ParseLabel(ref input);
		}
		while (LineParser.Lookahead(ref input) == TokenType.Boolean)
		{
			LineParser.SetBool(ref input, this);
		}
		while (input != string.Empty)
		{
			LineParser.EnsureIdentifier(ref input, "KEYCHAIN");
			KeyChain keyChain = new KeyChain();
			keyChain.Type = (KeyChain.KeychainType)LineParser.ParseEnum(ref input, "Keychain Type", typeof(KeyChain.KeychainType));
			if (keyChain.Type == KeyChain.KeychainType.Custom && LineParser.Lookahead(ref input) == TokenType.Literal)
			{
				keyChain.CustomType = LineParser.ParseLiteral(ref input, "Custom Type");
			}
			keyChain.Mode = (KeyChain.KeychainMode)LineParser.ParseEnum(ref input, "Keychain Mode", typeof(KeyChain.KeychainMode));
			while (!input.StartsWith("KEYCHAIN") && input != string.Empty)
			{
				Key key = new Key();
				LineParser.EnsureIdentifier(ref input, "KEY");
				string text = LineParser.ParseLiteral(ref input, "Left Term");
				if (LineParser.CheckIdentifier(ref input, "KEY") || LineParser.CheckIdentifier(ref input, "KEYCHAIN") || input == string.Empty)
				{
					key.LeftTerm = "<SOURCE>";
					key.Comparer = Comparer.Contains;
					key.RightTerm = text;
				}
				else
				{
					key.LeftTerm = text;
					key.Comparer = (Comparer)LineParser.ParseEnum(ref input, "Condition", typeof(Comparer));
					if (key.Comparer != Comparer.Exists && key.Comparer != Comparer.DoesNotExist)
					{
						key.RightTerm = LineParser.ParseLiteral(ref input, "Right Term");
					}
				}
				keyChain.Keys.Add(key);
			}
			KeyChains.Add(keyChain);
		}
		return this;
	}

	public override string ToLS(bool indent = true)
	{
		BlockWriter blockWriter = new BlockWriter(GetType(), indent, base.Disabled);
		blockWriter.Label(base.Label).Token("KEYCHECK").Boolean(BanOn4XX, "BanOn4XX")
			.Boolean(BanOnToCheck, "BanOnToCheck");
		foreach (KeyChain keyChain in KeyChains)
		{
			blockWriter.Indent().Token("KEYCHAIN").Token(keyChain.Type);
			if (keyChain.Type == KeyChain.KeychainType.Custom)
			{
				blockWriter.Literal(keyChain.CustomType);
			}
			blockWriter.Token(keyChain.Mode);
			foreach (Key key in keyChain.Keys)
			{
				if (key.LeftTerm == "<SOURCE>" && key.Comparer == Comparer.Contains)
				{
					blockWriter.Indent(2).Token("KEY").Literal(key.RightTerm);
					continue;
				}
				blockWriter.Indent(2).Token("KEY").Literal(key.LeftTerm)
					.Token(key.Comparer);
				if (key.Comparer != Comparer.Exists && key.Comparer != Comparer.DoesNotExist)
				{
					blockWriter.Literal(key.RightTerm);
				}
			}
		}
		return blockWriter.ToString();
	}

	public override void Process(BotData data)
	{
		base.Process(data);
		if (data.ResponseCode.StartsWith("4") && banOn4XX)
		{
			data.Status = BotStatus.BAN;
			return;
		}
		string[] globalBanKeys = data.GlobalSettings.Proxies.GlobalBanKeys;
		foreach (string text in globalBanKeys)
		{
			if (text != string.Empty && data.ResponseSource.Contains(text))
			{
				data.Status = BotStatus.BAN;
				return;
			}
		}
		bool flag = false;
		foreach (KeyChain keyChain in KeyChains)
		{
			if (keyChain.CheckKeys(data))
			{
				flag = true;
				switch (keyChain.Type)
				{
				case KeyChain.KeychainType.Success:
					data.Status = BotStatus.SUCCESS;
					break;
				case KeyChain.KeychainType.Failure:
					data.Status = BotStatus.FAIL;
					break;
				case KeyChain.KeychainType.Ban:
					data.Status = BotStatus.BAN;
					break;
				case KeyChain.KeychainType.Retry:
					data.Status = BotStatus.RETRY;
					break;
				case KeyChain.KeychainType.Custom:
					data.Status = BotStatus.CUSTOM;
					data.CustomStatus = keyChain.CustomType;
					break;
				}
			}
		}
		if (!flag && banOnToCheck)
		{
			data.Status = BotStatus.BAN;
		}
	}
}
