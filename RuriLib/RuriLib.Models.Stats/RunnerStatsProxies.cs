namespace RuriLib.Models.Stats;

public struct RunnerStatsProxies
{
	public int total;

	public int alive;

	public int banned;

	public int bad;

	public RunnerStatsProxies(int total, int alive, int banned, int bad)
	{
		this.total = total;
		this.alive = alive;
		this.banned = banned;
		this.bad = bad;
	}
}
