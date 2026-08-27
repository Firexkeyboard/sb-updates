using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ImageProcessor.Configuration;

public class NativeBinaryFactory : IDisposable
{
	private static readonly bool Is64Bit = Environment.Is64BitProcess;

	private static ConcurrentDictionary<string, IntPtr> nativeBinaries;

	private bool isDisposed;

	public bool Is64BitEnvironment => Is64Bit;

	public NativeBinaryFactory()
	{
		nativeBinaries = new ConcurrentDictionary<string, IntPtr>();
	}

	~NativeBinaryFactory()
	{
		Dispose(disposing: false);
	}

	public void RegisterNativeBinary(string name, byte[] resourceBytes)
	{
		nativeBinaries.GetOrAdd(name, delegate(string b)
		{
			string text = (Is64BitEnvironment ? "x64" : "x86");
			string fullPath = Path.GetFullPath(Path.Combine(new Uri(Assembly.GetExecutingAssembly().Location).LocalPath, "..\\" + text + "\\" + b));
			FileInfo fileInfo = new FileInfo(fullPath);
			bool flag = true;
			if (fileInfo.Exists)
			{
				byte[] second = File.ReadAllBytes(fullPath);
				if (resourceBytes.SequenceEqual(second))
				{
					flag = false;
				}
			}
			if (flag)
			{
				DirectoryInfo directoryInfo = new DirectoryInfo(Path.GetDirectoryName(fullPath));
				if (!directoryInfo.Exists)
				{
					directoryInfo.Create();
				}
				File.WriteAllBytes(fullPath, resourceBytes);
			}
			IntPtr intPtr;
			try
			{
				intPtr = NativeMethods.LoadLibrary(fullPath);
			}
			catch (Exception ex)
			{
				throw new ApplicationException(ex.Message);
			}
			if (intPtr == IntPtr.Zero)
			{
				throw new ApplicationException("Cannot load " + b);
			}
			return intPtr;
		});
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!isDisposed)
		{
			FreeNativeBinaries();
			isDisposed = true;
		}
	}

	private void FreeNativeBinaries()
	{
		foreach (KeyValuePair<string, IntPtr> nativeBinary in nativeBinaries)
		{
			IntPtr value = nativeBinary.Value;
			NativeMethods.FreeLibrary(value);
			NativeMethods.FreeLibrary(value);
		}
	}
}
