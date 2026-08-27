using System;
using System.ComponentModel;
using System.Threading;

namespace RuriLib.Runner;

public class AbortableBackgroundWorker : BackgroundWorker
{
	private Thread workerThread;

	public WorkerStatus Status { get; set; }

	public int Id { get; set; }

#pragma warning disable SYSLIB0006 // Thread.Abort/ResetAbort not supported on .NET Core; kept for API compatibility
	protected override void OnDoWork(DoWorkEventArgs e)
	{
		workerThread = Thread.CurrentThread;
		try
		{
			base.OnDoWork(e);
		}
		catch (ThreadAbortException)
		{
			e.Cancel = true;
			Thread.ResetAbort();
		}
	}

	public void Abort()
	{
		if (workerThread != null)
		{
			try
			{
				workerThread.Abort();
			}
			catch (PlatformNotSupportedException)
			{
				// .NET 5+: Thread.Abort not supported; graceful cancel instead
				CancelAsync();
			}
			workerThread = null;
		}
	}
#pragma warning restore SYSLIB0006
}
