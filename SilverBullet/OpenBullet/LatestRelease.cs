using System;
using System.Text.RegularExpressions;

namespace OpenBullet;

public class LatestRelease
{
	public string Name { get; set; }

	public Assets[] Assets { get; set; }

	public string Body { get; set; }

	public Version Ver => Version.Parse(Regex.Match(Name, "\\d+(\\.\\d+)+").Value);

	public bool Available => Ver > Version.Parse(SB.Version);
}
