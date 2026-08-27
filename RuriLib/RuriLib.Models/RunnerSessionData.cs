using System;
using RuriLib.Runner;

namespace RuriLib.Models;

public class RunnerSessionData : Persistable<Guid>
{
	public string Config { get; set; } = "";

	public string Wordlist { get; set; } = "";

	public int Bots { get; set; } = 1;

	public ProxyMode ProxyMode { get; set; }
}
