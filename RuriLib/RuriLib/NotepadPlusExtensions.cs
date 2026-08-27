using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RuriLib;

public static class NotepadPlusExtensions
{
	private static IntPtr notepadPlus;

	private static Process notepadPlusProc;

	[DllImport("User32.dll")]
	private static extern int SendMessageW(IntPtr hWnd, int uMsg, int wParam, string lParam);

	[DllImport("user32.dll")]
	public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

	public static void ShowText(string text)
	{
		Process process = Start();
		if (process != null && (notepadPlus = process.MainWindowHandle) != IntPtr.Zero)
		{
			SendMessageW(FindWindowEx(notepadPlus, new IntPtr(0), "Scintilla", null), 194, 0, text);
		}
	}

	public static void Clear()
	{
		_ = notepadPlus;
		SendMessageW(FindWindowEx(notepadPlus, new IntPtr(0), "Scintilla", null), 12, 0, string.Empty);
	}

	private static Process Start()
	{
		if (notepadPlusProc == null || notepadPlusProc.HasExited || notepadPlusProc?.MainWindowHandle == IntPtr.Zero)
		{
			notepadPlusProc = Process.Start(new ProcessStartInfo("notepad++")
			{
				WindowStyle = ProcessWindowStyle.Minimized,
				Arguments = "-multiInst -nosession"
			});
			notepadPlusProc.WaitForInputIdle();
		}
		return notepadPlusProc;
	}

	public static void Close()
	{
		try
		{
			notepadPlusProc?.Kill();
		}
		catch
		{
		}
	}
}
