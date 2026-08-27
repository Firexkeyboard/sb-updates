using System;
using System.Threading;

namespace ImageProcessor.Common.Helpers;

internal sealed class UpgradeableReadLock : IDisposable
{
	private readonly ReaderWriterLockSlim locker;

	private bool upgraded;

	private bool isDisposed;

	public UpgradeableReadLock(ReaderWriterLockSlim locker)
	{
		this.locker = locker;
		this.locker.EnterUpgradeableReadLock();
	}

	~UpgradeableReadLock()
	{
		Dispose(disposing: false);
	}

	public void UpgradeToWriteLock()
	{
		locker.EnterWriteLock();
		upgraded = true;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (isDisposed)
		{
			return;
		}
		if (disposing)
		{
			if (upgraded)
			{
				locker.ExitWriteLock();
			}
			locker.ExitUpgradeableReadLock();
		}
		isDisposed = true;
	}
}
