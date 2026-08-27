using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using ImageProcessor.Common.Extensions;
using ImageProcessor.Imaging.Filters.Photo;

namespace ImageProcessor.Imaging.Filters.EdgeDetection;

public class ConvolutionFilter
{
	private readonly IEdgeFilter edgeFilter;

	private readonly bool greyscale;

	public ConvolutionFilter(IEdgeFilter edgeFilter, bool greyscale)
	{
		this.edgeFilter = edgeFilter;
		this.greyscale = greyscale;
	}

	public Bitmap ProcessFilter(Image source)
	{
		int width = source.Width;
		int height = source.Height;
		int maxWidth = width + 1;
		int maxHeight = height + 1;
		int bufferedWidth = width + 2;
		int bufferedHeight = height + 2;
		Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
		Bitmap bitmap2 = new Bitmap(bufferedWidth, bufferedHeight, PixelFormat.Format32bppPArgb);
		bitmap.SetResolution(source.HorizontalResolution, source.VerticalResolution);
		bitmap2.SetResolution(source.HorizontalResolution, source.VerticalResolution);
		using (Graphics graphics = Graphics.FromImage(bitmap2))
		{
			graphics.Clear(Color.Transparent);
			Rectangle rect = new Rectangle(0, 0, bufferedWidth, bufferedHeight);
			Rectangle dstRect = new Rectangle(0, 0, width, height);
			using ImageAttributes imageAttributes = new ImageAttributes();
			if (greyscale)
			{
				imageAttributes.SetColorMatrix(ColorMatrixes.GreyScale);
			}
			using TextureBrush textureBrush = new TextureBrush(source, dstRect, imageAttributes);
			textureBrush.WrapMode = WrapMode.TileFlipXY;
			textureBrush.TranslateTransform(1f, 1f);
			graphics.FillRectangle(textureBrush, rect);
		}
		try
		{
			double[,] horizontalFilter = edgeFilter.HorizontalGradientOperator;
			int kernelLength = horizontalFilter.GetLength(0);
			int radius = kernelLength >> 1;
			FastBitmap sourceBitmap = new FastBitmap(bitmap2);
			try
			{
				FastBitmap destinationBitmap = new FastBitmap(bitmap);
				try
				{
					Parallel.For(0, bufferedHeight, delegate(int y)
					{
						for (int i = 0; i < bufferedWidth; i++)
						{
							double num = 0.0;
							double num2 = 0.0;
							double num3 = 0.0;
							for (int j = 0; j < kernelLength; j++)
							{
								int num4 = j - radius;
								int num5 = y + num4;
								if (num5 >= 0)
								{
									if (num5 >= bufferedHeight)
									{
										break;
									}
									for (int k = 0; k < kernelLength; k++)
									{
										int num6 = k - radius;
										int num7 = i + num6;
										if (num7 >= 0 && num7 < bufferedWidth)
										{
											Color pixel = sourceBitmap.GetPixel(num7, num5);
											double num8 = (int)pixel.R;
											double num9 = (int)pixel.G;
											double num10 = (int)pixel.B;
											num += horizontalFilter[j, k] * num8;
											num2 += horizontalFilter[j, k] * num9;
											num3 += horizontalFilter[j, k] * num10;
										}
									}
								}
							}
							byte red = num.ToByte();
							byte green = num2.ToByte();
							byte blue = num3.ToByte();
							Color color = Color.FromArgb(red, green, blue);
							if (y > 0 && i > 0 && y < maxHeight && i < maxWidth)
							{
								destinationBitmap.SetPixel(i - 1, y - 1, color);
							}
						}
					});
					return bitmap;
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
				if (sourceBitmap != null)
				{
					((IDisposable)sourceBitmap).Dispose();
				}
			}
		}
		finally
		{
			bitmap2.Dispose();
		}
	}

	public Bitmap Process2DFilter(Image source)
	{
		int width = source.Width;
		int height = source.Height;
		int maxWidth = width + 1;
		int maxHeight = height + 1;
		int bufferedWidth = width + 2;
		int bufferedHeight = height + 2;
		Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
		Bitmap bitmap2 = new Bitmap(bufferedWidth, bufferedHeight, PixelFormat.Format32bppPArgb);
		bitmap.SetResolution(source.HorizontalResolution, source.VerticalResolution);
		bitmap2.SetResolution(source.HorizontalResolution, source.VerticalResolution);
		using (Graphics graphics = Graphics.FromImage(bitmap2))
		{
			graphics.Clear(Color.Transparent);
			Rectangle rect = new Rectangle(0, 0, bufferedWidth, bufferedHeight);
			Rectangle dstRect = new Rectangle(0, 0, width, height);
			using ImageAttributes imageAttributes = new ImageAttributes();
			if (greyscale)
			{
				imageAttributes.SetColorMatrix(ColorMatrixes.GreyScale);
			}
			using TextureBrush textureBrush = new TextureBrush(source, dstRect, imageAttributes);
			textureBrush.WrapMode = WrapMode.TileFlipXY;
			textureBrush.TranslateTransform(1f, 1f);
			graphics.FillRectangle(textureBrush, rect);
		}
		try
		{
			double[,] horizontalFilter = edgeFilter.HorizontalGradientOperator;
			double[,] verticalFilter = ((I2DEdgeFilter)edgeFilter).VerticalGradientOperator;
			int kernelLength = horizontalFilter.GetLength(0);
			int radius = kernelLength >> 1;
			FastBitmap sourceBitmap = new FastBitmap(bitmap2);
			try
			{
				FastBitmap destinationBitmap = new FastBitmap(bitmap);
				try
				{
					Parallel.For(0, bufferedHeight, delegate(int y)
					{
						for (int i = 0; i < bufferedWidth; i++)
						{
							double num = 0.0;
							double num2 = 0.0;
							double num3 = 0.0;
							double num4 = 0.0;
							double num5 = 0.0;
							double num6 = 0.0;
							for (int j = 0; j < kernelLength; j++)
							{
								int num7 = j - radius;
								int num8 = y + num7;
								if (num8 >= 0)
								{
									if (num8 >= bufferedHeight)
									{
										break;
									}
									for (int k = 0; k < kernelLength; k++)
									{
										int num9 = k - radius;
										int num10 = i + num9;
										if (num10 >= 0 && num10 < bufferedWidth)
										{
											Color pixel = sourceBitmap.GetPixel(num10, num8);
											double num11 = (int)pixel.R;
											double num12 = (int)pixel.G;
											double num13 = (int)pixel.B;
											num += horizontalFilter[j, k] * num11;
											num2 += verticalFilter[j, k] * num11;
											num3 += horizontalFilter[j, k] * num12;
											num4 += verticalFilter[j, k] * num12;
											num5 += horizontalFilter[j, k] * num13;
											num6 += verticalFilter[j, k] * num13;
										}
									}
								}
							}
							byte red = Math.Sqrt(num * num + num2 * num2).ToByte();
							byte green = Math.Sqrt(num3 * num3 + num4 * num4).ToByte();
							byte blue = Math.Sqrt(num5 * num5 + num6 * num6).ToByte();
							Color color = Color.FromArgb(red, green, blue);
							if (y > 0 && i > 0 && y < maxHeight && i < maxWidth)
							{
								destinationBitmap.SetPixel(i - 1, y - 1, color);
							}
						}
					});
					return bitmap;
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
				if (sourceBitmap != null)
				{
					((IDisposable)sourceBitmap).Dispose();
				}
			}
		}
		finally
		{
			bitmap2.Dispose();
		}
	}
}
