using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using ImageProcessor.Imaging.Filters.EdgeDetection;
using ImageProcessor.Imaging.Filters.Photo;

namespace ImageProcessor.Imaging.Helpers;

public static class Effects
{
	public static Bitmap Vignette(Image source, Color baseColor, Rectangle? rectangle = null, bool invert = false)
	{
		using (Graphics graphics = Graphics.FromImage(source))
		{
			Rectangle rectangle2 = rectangle ?? new Rectangle(0, 0, source.Width, source.Height);
			Rectangle rect = rectangle2;
			rect.Offset(-rect.X, -rect.Y);
			int width = rect.Width - (int)Math.Floor(0.70712 * (double)rect.Width);
			int height = rect.Height - (int)Math.Floor(0.70712 * (double)rect.Height);
			rect.Inflate(width, height);
			using GraphicsPath graphicsPath = new GraphicsPath();
			graphicsPath.AddEllipse(rect);
			using PathGradientBrush pathGradientBrush = new PathGradientBrush(graphicsPath);
			Color centerColor;
			Color color;
			if (invert)
			{
				centerColor = Color.FromArgb(50, baseColor.R, baseColor.G, baseColor.B);
				color = Color.FromArgb(0, baseColor.R, baseColor.G, baseColor.B);
			}
			else
			{
				centerColor = Color.FromArgb(0, baseColor.R, baseColor.G, baseColor.B);
				color = Color.FromArgb(255, baseColor.R, baseColor.G, baseColor.B);
			}
			pathGradientBrush.WrapMode = WrapMode.Tile;
			pathGradientBrush.CenterColor = centerColor;
			pathGradientBrush.SurroundColors = new Color[1] { color };
			Blend blend = new Blend();
			blend.Positions = new float[6] { 0f, 0.2f, 0.4f, 0.6f, 0.8f, 1f };
			blend.Factors = new float[6] { 0f, 0.5f, 1f, 1f, 1f, 1f };
			Blend blend2 = blend;
			pathGradientBrush.Blend = blend2;
			Region clip = graphics.Clip;
			graphics.Clip = new Region(rectangle2);
			graphics.FillRectangle(pathGradientBrush, rect);
			graphics.Clip = clip;
		}
		return (Bitmap)source;
	}

	public static Bitmap Glow(Image source, Color baseColor, Rectangle? rectangle = null)
	{
		return Vignette(source, baseColor, rectangle, invert: true);
	}

	public static Bitmap ApplyMask(Image source, Image mask)
	{
		if (mask.Size != source.Size)
		{
			throw new ArgumentException();
		}
		int width = mask.Width;
		int height = mask.Height;
		Bitmap bitmap = new Bitmap(source);
		bitmap.SetResolution(source.HorizontalResolution, source.VerticalResolution);
		FastBitmap maskBitmap = new FastBitmap(mask);
		try
		{
			FastBitmap sourceBitmap = new FastBitmap(bitmap);
			try
			{
				Parallel.For(0, height, delegate(int y)
				{
					for (int i = 0; i < width; i++)
					{
						Color pixel = maskBitmap.GetPixel(i, y);
						Color pixel2 = sourceBitmap.GetPixel(i, y);
						if (pixel2.A != 0)
						{
							sourceBitmap.SetPixel(i, y, Color.FromArgb(pixel.A, pixel2.R, pixel2.G, pixel2.B));
						}
					}
				});
			}
			finally
			{
				if (sourceBitmap != null)
				{
					((IDisposable)sourceBitmap).Dispose();
				}
			}
		}
		finally
		{
			if (maskBitmap != null)
			{
				((IDisposable)maskBitmap).Dispose();
			}
		}
		Bitmap bitmap2 = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
		bitmap2.SetResolution(source.HorizontalResolution, source.VerticalResolution);
		using (Graphics graphics = Graphics.FromImage(bitmap2))
		{
			GraphicsHelper.SetGraphicsOptions(graphics, blending: true);
			graphics.Clear(Color.Transparent);
			graphics.DrawImageUnscaled(bitmap, 0, 0, width, height);
		}
		bitmap.Dispose();
		return bitmap2;
	}

	public static Bitmap Trace(Image source, Image destination, byte threshold = 0)
	{
		int width = source.Width;
		int height = source.Height;
		using (Bitmap source2 = new ConvolutionFilter(new SobelEdgeFilter(), greyscale: true).Process2DFilter(source))
		{
			destination = new InvertMatrixFilter().TransformImage(source2, destination);
			destination = Adjustments.Brightness(destination, -5, null);
		}
		FastBitmap destinationBitmap = new FastBitmap(destination);
		try
		{
			Parallel.For(0, height, delegate(int y)
			{
				for (int i = 0; i < width; i++)
				{
					if (destinationBitmap.GetPixel(i, y).B >= threshold)
					{
						destinationBitmap.SetPixel(i, y, Color.Transparent);
					}
				}
			});
		}
		finally
		{
			if (destinationBitmap != null)
			{
				((IDisposable)destinationBitmap).Dispose();
			}
		}
		destination = Adjustments.Brightness(destination, -5, null);
		return (Bitmap)destination;
	}
}
