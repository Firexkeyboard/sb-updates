using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace OpenBullet;

public class WpfScreen
{
	private readonly Screen screen;

	public static WpfScreen Primary => new WpfScreen(Screen.PrimaryScreen);

	public Rect DeviceBounds => GetRect(screen.Bounds);

	public Rect WorkingArea => GetRect(screen.WorkingArea);

	public bool IsPrimary => screen.Primary;

	public string DeviceName => screen.DeviceName;

	public static IEnumerable<WpfScreen> AllScreens()
	{
		Screen[] allScreens = Screen.AllScreens;
		foreach (Screen screen in allScreens)
		{
			yield return new WpfScreen(screen);
		}
	}

	public static WpfScreen GetScreenFrom(Window window)
	{
		return new WpfScreen(Screen.FromHandle(new WindowInteropHelper(window).Handle));
	}

	public static WpfScreen GetScreenFrom(System.Windows.Point point)
	{
		int x = (int)Math.Round(point.X);
		int y = (int)Math.Round(point.Y);
		return new WpfScreen(Screen.FromPoint(new System.Drawing.Point(x, y)));
	}

	internal WpfScreen(Screen screen)
	{
		this.screen = screen;
	}

	private Rect GetRect(Rectangle value)
	{
		Rect result = default(Rect);
		result.X = value.X;
		result.Y = value.Y;
		result.Width = value.Width;
		result.Height = value.Height;
		return result;
	}

	public Tuple<double, double> CenterWindowOnScreen(Rect workArea, double width, double height)
	{
		double item = (workArea.Width - width) / 2.0 + workArea.Left;
		double item2 = (workArea.Height - height) / 2.0 + workArea.Top;
		return new Tuple<double, double>(item, item2);
	}
}
