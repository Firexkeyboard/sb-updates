using OpenQA.Selenium;
using RuriLib.ViewModels;

namespace RuriLib.Runner;

public class RunnerBotViewModel : ViewModelBase
{
	private int id;

	private string data = "";

	private string proxy = "";

	private string status = "";

	public int Id
	{
		get
		{
			return id;
		}
		set
		{
			id = value;
			OnPropertyChanged("Id");
		}
	}

	public string Data
	{
		get
		{
			return data;
		}
		set
		{
			data = value;
			OnPropertyChanged("Data");
		}
	}

	public string Proxy
	{
		get
		{
			return proxy;
		}
		set
		{
			proxy = value;
			OnPropertyChanged("Proxy");
		}
	}

	public string Status
	{
		get
		{
			return status;
		}
		set
		{
			status = value;
			OnPropertyChanged("Status");
		}
	}

	public AbortableBackgroundWorker Worker { get; set; }

	public WebDriver Driver { get; set; }

	public bool IsDriverOpen { get; set; }

	public RunnerBotViewModel(int id)
	{
		Id = id;
		Worker = new AbortableBackgroundWorker();
		Worker.WorkerSupportsCancellation = true;
		Worker.Id = id;
	}
}
