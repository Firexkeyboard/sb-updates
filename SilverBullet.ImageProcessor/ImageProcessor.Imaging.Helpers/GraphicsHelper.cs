using System.Drawing;
using System.Drawing.Drawing2D;

namespace ImageProcessor.Imaging.Helpers;

internal static class GraphicsHelper
{
	public static void SetGraphicsOptions(Graphics graphics, bool blending = false, bool smoothing = false)
	{
		graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
		graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
		graphics.CompositingQuality = CompositingQuality.HighQuality;
		if (smoothing)
		{
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
		}
		if (blending || smoothing)
		{
			graphics.CompositingMode = CompositingMode.SourceOver;
			graphics.CompositingQuality = CompositingQuality.GammaCorrected;
		}
		else
		{
			graphics.CompositingMode = CompositingMode.SourceCopy;
		}
	}
}
