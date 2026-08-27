using System;
using System.Threading;

namespace ImageProcessor.Common.Helpers;

internal sealed class WriteLock : IDisposable
{
	private readonly ReaderWriterLockSlim locker;

	private bool isDisposed;

	public WriteLock(ReaderWriterLockSlim locker)
	{
		this.locker = locker;
		this.locker.EnterWriteLock();
	}

	~WriteLock()
	{
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (!isDisposed)
		{
			if (disposing)
			{
				locker.ExitWriteLock();
			}
			isDisposed = true;
		}
	}
}
