namespace RuriLib.Models.Stats;

public struct RunnerStatsData
{
	public int total;

	public int hits;

	public int custom;

	public int bad;

	public int retries;

	public int toCheck;

	public RunnerStatsData(int total, int hits, int custom, int bad, int retries, int toCheck)
	{
		this.total = total;
		this.hits = hits;
		this.custom = custom;
		this.bad = bad;
		this.retries = retries;
		this.toCheck = toCheck;
	}
}
