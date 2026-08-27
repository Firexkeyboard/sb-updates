using System;
using System.Runtime.InteropServices;

namespace ImageProcessor.Configuration;

internal static class NativeMethods
{
	[DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
	public static extern IntPtr LoadLibrary(string libname);

	[DllImport("kernel32", SetLastError = true)]
	public static extern bool FreeLibrary(IntPtr hModule);

	[DllImport("libdl")]
	public static extern IntPtr dlopen(string libname, int flags);

	[DllImport("libdl")]
	public static extern int dlclose(IntPtr hModule);
}
