using System;

namespace RuriLib.Models;

public class Record : Persistable<Guid>
{
	public string ConfigName { get; set; }

	public string WordlistLocation { get; set; }

	public int Checkpoint { get; set; }

	public Record()
	{
	}

	public Record(string configName, string wordlistLocation, int checkpoint)
	{
		ConfigName = configName;
		WordlistLocation = wordlistLocation;
		Checkpoint = checkpoint;
	}
}
