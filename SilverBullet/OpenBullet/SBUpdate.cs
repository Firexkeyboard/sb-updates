using System.Collections.Generic;

namespace OpenBullet;

public class SBUpdate
{
	public string Version { get; set; }

	public bool Available { get; set; }

	public string Message { get; set; }

	public string DownloadUrl { get; set; }

	public List<ReleaseNotes> ReleaseNotes { get; set; } = new List<ReleaseNotes>();

	public Donate[] Donate { get; set; }
}
