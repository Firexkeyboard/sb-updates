using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using ImageProcessor.Imaging.Colors;
using ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Imaging.Filters.Artistic;

public class HalftoneFilter
{
	public float CyanAngle { get; set; } = 15f;

	public float MagentaAngle { get; set; } = 75f;

	public float YellowAngle { get; set; }

	public float KeylineAngle { get; set; } = 45f;

	public int Distance { get; set; } = 4;

	public HalftoneFilter()
	{
	}

	public HalftoneFilter(int distance)
	{
		Distance = distance;
	}

	public Bitmap ApplyFilter(Bitmap source)
	{
		Bitmap bitmap = null;
		Bitmap bitmap2 = null;
		Bitmap bitmap3 = null;
		Bitmap bitmap4 = null;
		Bitmap bitmap5 = null;
		Bitmap bitmap6 = null;
		try
		{
			int width = source.Width;
			int height = source.Height;
			int width2 = source.Width + Distance;
			int num = source.Height + Distance;
			bitmap = new Bitmap(width2, num, PixelFormat.Format32bppPArgb);
			bitmap.SetResolution(source.HorizontalResolution, source.VerticalResolution);
			using (Graphics graphics = Graphics.FromImage(bitmap))
			{
				graphics.Clear(Color.White);
				Rectangle rect = new Rectangle(0, 0, width + Distance, source.Height + Distance);
				using TextureBrush textureBrush = new TextureBrush(source);
				textureBrush.WrapMode = WrapMode.TileFlipXY;
				textureBrush.TranslateTransform(Distance, Distance);
				graphics.FillRectangle(textureBrush, rect);
			}
			Rectangle boundingRectangle = GetBoundingRectangle(width2, num);
			int num2 = -(boundingRectangle.Height + num);
			int num3 = boundingRectangle.Height + num;
			int num4 = -(boundingRectangle.Width + width2);
			int num5 = boundingRectangle.Width + width2;
			Point empty = Point.Empty;
			int offset = Distance;
			float num6 = (float)Distance * 1.587f;
			float num7 = (float)Distance * 2.176f;
			float num8 = (float)Distance * 2.2f;
			float num9 = (float)Distance * (float)Math.Sqrt(2.0);
			float val = (float)Distance * (float)Math.Sqrt(1.4545);
			float val2 = num9 * (float)Math.Sqrt(2.0);
			Brush brush = new SolidBrush(Color.FromArgb(0, 183, 235));
			Brush brush2 = new SolidBrush(Color.FromArgb(255, 0, 144));
			Brush brush3 = new SolidBrush(Color.FromArgb(255, 239, 0));
			bitmap2 = new Bitmap(width2, num, PixelFormat.Format32bppPArgb);
			bitmap3 = new Bitmap(width2, num, PixelFormat.Format32bppPArgb);
			bitmap4 = new Bitmap(width2, num, PixelFormat.Format32bppPArgb);
			bitmap5 = new Bitmap(width2, num, PixelFormat.Format32bppPArgb);
			bitmap6 = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
			bitmap2.SetResolution(source.HorizontalResolution, source.VerticalResolution);
			bitmap3.SetResolution(source.HorizontalResolution, source.VerticalResolution);
			bitmap4.SetResolution(source.HorizontalResolution, source.VerticalResolution);
			bitmap5.SetResolution(source.HorizontalResolution, source.VerticalResolution);
			bitmap6.SetResolution(source.HorizontalResolution, source.VerticalResolution);
			Rectangle rectangle = new Rectangle(0, 0, width2, num);
			using (Graphics graphics2 = Graphics.FromImage(bitmap2))
			{
				using Graphics graphics3 = Graphics.FromImage(bitmap3);
				using Graphics graphics4 = Graphics.FromImage(bitmap4);
				using Graphics graphics5 = Graphics.FromImage(bitmap5);
				graphics2.PixelOffsetMode = PixelOffsetMode.Half;
				graphics3.PixelOffsetMode = PixelOffsetMode.Half;
				graphics4.PixelOffsetMode = PixelOffsetMode.Half;
				graphics5.PixelOffsetMode = PixelOffsetMode.Half;
				graphics2.SmoothingMode = SmoothingMode.AntiAlias;
				graphics3.SmoothingMode = SmoothingMode.AntiAlias;
				graphics4.SmoothingMode = SmoothingMode.AntiAlias;
				graphics5.SmoothingMode = SmoothingMode.AntiAlias;
				graphics2.CompositingQuality = CompositingQuality.HighQuality;
				graphics3.CompositingQuality = CompositingQuality.HighQuality;
				graphics4.CompositingQuality = CompositingQuality.HighQuality;
				graphics5.CompositingQuality = CompositingQuality.HighQuality;
				graphics2.Clear(Color.White);
				graphics3.Clear(Color.White);
				graphics4.Clear(Color.White);
				graphics5.Clear(Color.White);
				using (FastBitmap fastBitmap = new FastBitmap(bitmap))
				{
					for (int i = num2; i < num3; i += offset)
					{
						for (int j = num4; j < num5; j += offset)
						{
							Point point = ImageMaths.RotatePoint(new Point(j, i), CyanAngle, empty);
							int x = point.X;
							int y2 = point.Y;
							if (rectangle.Contains(new Point(x, y2)))
							{
								float num10 = Math.Min(((CmykColor)fastBitmap.GetPixel(x, y2)).C / 100f * num8, num9);
								graphics2.FillEllipse(brush, x, y2, num10, num10);
							}
							point = ImageMaths.RotatePoint(new Point(j, i), MagentaAngle, empty);
							x = point.X;
							y2 = point.Y;
							if (rectangle.Contains(new Point(x, y2)))
							{
								float num10 = Math.Min(((CmykColor)fastBitmap.GetPixel(x, y2)).M / 100f * num7, val);
								graphics3.FillEllipse(brush2, x, y2, num10, num10);
							}
							point = ImageMaths.RotatePoint(new Point(j, i), YellowAngle, empty);
							x = point.X;
							y2 = point.Y;
							if (rectangle.Contains(new Point(x, y2)))
							{
								float num10 = Math.Min(((CmykColor)fastBitmap.GetPixel(x, y2)).Y / 100f * num6, num9);
								graphics4.FillEllipse(brush3, x, y2, num10, num10);
							}
							point = ImageMaths.RotatePoint(new Point(j, i), KeylineAngle, empty);
							x = point.X;
							y2 = point.Y;
							if (rectangle.Contains(new Point(x, y2)))
							{
								CmykColor cmykColor = fastBitmap.GetPixel(x, y2);
								float num10 = Math.Min(cmykColor.K / 100f * num8, val2);
								Brush brush4 = new SolidBrush(CmykColor.FromCmykColor(0f, 0f, 0f, cmykColor.K));
								graphics5.FillEllipse(brush4, x, y2, num10, num10);
							}
						}
					}
				}
				using (Graphics graphics6 = Graphics.FromImage(bitmap6))
				{
					graphics6.Clear(Color.White);
				}
				FastBitmap cyanBitmap = new FastBitmap(bitmap2);
				try
				{
					FastBitmap magentaBitmap = new FastBitmap(bitmap3);
					try
					{
						FastBitmap yellowBitmap = new FastBitmap(bitmap4);
						try
						{
							FastBitmap keylineBitmap = new FastBitmap(bitmap5);
							try
							{
								FastBitmap destinationBitmap = new FastBitmap(bitmap6);
								try
								{
									Parallel.For(offset, num, delegate(int y)
									{
										for (int k = offset; k < width2; k++)
										{
											Color pixel = cyanBitmap.GetPixel(k, y);
											Color pixel2 = magentaBitmap.GetPixel(k, y);
											Color pixel3 = yellowBitmap.GetPixel(k, y);
											Color pixel4 = keylineBitmap.GetPixel(k, y);
											int x2 = k - offset;
											int y3 = y - offset;
											CmykColor cmykColor2 = pixel.AddAsCmykColor(pixel2, pixel3, pixel4);
											if (rectangle.Contains(new Point(x2, y3)))
											{
												destinationBitmap.SetPixel(x2, y3, cmykColor2);
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
							}
							finally
							{
								if (keylineBitmap != null)
								{
									((IDisposable)keylineBitmap).Dispose();
								}
							}
						}
						finally
						{
							if (yellowBitmap != null)
							{
								((IDisposable)yellowBitmap).Dispose();
							}
						}
					}
					finally
					{
						if (magentaBitmap != null)
						{
							((IDisposable)magentaBitmap).Dispose();
						}
					}
				}
				finally
				{
					if (cyanBitmap != null)
					{
						((IDisposable)cyanBitmap).Dispose();
					}
				}
			}
			bitmap.Dispose();
			bitmap2.Dispose();
			bitmap3.Dispose();
			bitmap4.Dispose();
			bitmap5.Dispose();
			source.Dispose();
			source = bitmap6;
		}
		catch
		{
			bitmap?.Dispose();
			bitmap2?.Dispose();
			bitmap3?.Dispose();
			bitmap4?.Dispose();
			bitmap5?.Dispose();
			bitmap6?.Dispose();
		}
		return source;
	}

	private Rectangle GetBoundingRectangle(int width, int height)
	{
		int num = 0;
		int num2 = 0;
		foreach (float item in new List<float> { CyanAngle, MagentaAngle, YellowAngle, KeylineAngle })
		{
			Size size = ImageMaths.GetBoundingRotatedRectangle(width, height, item).Size;
			num = Math.Max(num, size.Width);
			num2 = Math.Max(num2, size.Height);
		}
		return new Rectangle(0, 0, num, num2);
	}
}
