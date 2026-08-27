using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RuriLib;

public static class NotepadExtensions
{
	[DllImport("user32.dll")]
	private static extern int SetWindowText(IntPtr hWnd, string text);

	[DllImport("user32.dll")]
	private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

	[DllImport("User32.dll")]
	private static extern int SendMessage(IntPtr hWnd, int uMsg, int wParam, string lParam);

	public static void ShowText(string text = null, string title = "Log")
	{
		Process process = Process.Start(new ProcessStartInfo("notepad.exe"));
		if (process != null)
		{
			process.WaitForInputIdle();
			if (!string.IsNullOrEmpty(title))
			{
				SetWindowText(process.MainWindowHandle, title);
			}
			if (!string.IsNullOrEmpty(text))
			{
				SendMessage(FindWindowEx(process.MainWindowHandle, new IntPtr(0), "Edit", null), 12, 0, text);
			}
		}
	}
}
