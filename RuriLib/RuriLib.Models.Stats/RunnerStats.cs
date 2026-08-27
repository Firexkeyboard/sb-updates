namespace RuriLib.Models.Stats;

public struct RunnerStats
{
	public RunnerStatsData data;

	public RunnerStatsProxies proxies;

	public double cpm;

	public decimal credit;

	public RunnerStats(RunnerStatsData data, RunnerStatsProxies proxies, double cpm, decimal credit)
	{
		this.data = data;
		this.proxies = proxies;
		this.cpm = cpm;
		this.credit = credit;
	}
}
