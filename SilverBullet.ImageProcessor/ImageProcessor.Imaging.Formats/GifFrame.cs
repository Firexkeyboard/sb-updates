using System;
using System.Drawing;

namespace ImageProcessor.Imaging.Formats;

public class GifFrame
{
	public Image Image { get; set; }

	public TimeSpan Delay { get; set; }

	public int X { get; set; }

	public int Y { get; set; }
}
