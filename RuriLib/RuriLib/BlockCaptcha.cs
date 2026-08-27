using System;
using System.Windows.Media;
using Newtonsoft.Json;
using RuriLib.Functions.Captchas;

namespace RuriLib;

[Obsolete]
public abstract class BlockCaptcha : BlockBase
{
	[JsonIgnore]
	public decimal Balance { get; set; }

	public override void Process(BotData data)
	{
		base.Process(data);
		if (!data.GlobalSettings.Captchas.BypassBalanceCheck)
		{
			Balance = 0m;
			data.Log(new LogEntry("Checking balance...", Colors.White));
			Balance = Captchas.GetService(data.GlobalSettings.Captchas).GetBalanceAsync().Result;
			if (Balance <= 0m)
			{
				throw new Exception($"[{data.GlobalSettings.Captchas.CurrentService}] Bad token/credentials or zero balance!");
			}
			data.Log(new LogEntry($"[{data.GlobalSettings.Captchas.CurrentService}] Current Balance: ${Balance}", Colors.GreenYellow));
			data.Balance = Balance;
		}
	}
}
