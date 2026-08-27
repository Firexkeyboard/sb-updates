namespace RuriLib.Models;

public struct ProxyCheckResult<T>
{
	public bool success;

	public T result;

	public string error;

	public ProxyCheckResult(bool success, T result, string error = "")
	{
		this.success = success;
		this.result = result;
		this.error = error;
	}
}
