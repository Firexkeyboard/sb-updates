using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Common.Extensions;
using ImageProcessor.Imaging.Formats;
using ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Imaging;

public class Resizer
{
	public ResizeLayer ResizeLayer { get; set; }

	public ISupportedImageFormat ImageFormat { get; set; }

	public AnimationProcessMode AnimationProcessMode { get; set; }

	public Resizer(Size size)
	{
		ResizeLayer = new ResizeLayer(size, ResizeMode.Pad, AnchorPosition.Center, upscale: true, null, null, null, null);
	}

	public Resizer(ResizeLayer resizeLayer)
	{
		ResizeLayer = resizeLayer;
	}

	public Bitmap ResizeImage(Image source, bool linear)
	{
		Bitmap bitmap = null;
		try
		{
			Size size = source.Size;
			int num = ResizeLayer.Size.Width;
			int num2 = ResizeLayer.Size.Height;
			int num3 = ResizeLayer.MaxSize?.Width ?? int.MaxValue;
			int num4 = ResizeLayer.MaxSize?.Height ?? int.MaxValue;
			if (num == 0 && num2 > 0)
			{
				num = (int)Math.Max(1.0, Math.Round((float)(size.Width * num2) / (float)size.Height));
			}
			if (num2 == 0 && num > 0)
			{
				num2 = (int)Math.Max(1.0, Math.Round((float)(size.Height * num) / (float)size.Width));
			}
			(Size, Rectangle) tuple = ResizeHelper.CalculateTargetLocationAndBounds(source.Size, ResizeLayer, num, num2);
			Size item = tuple.Item1;
			Rectangle item2 = tuple.Item2;
			int width = source.Width;
			int height = source.Height;
			int width2 = item.Width;
			int height2 = item.Height;
			bool flag = ResizeLayer.Upscale;
			if (ResizeLayer.ResizeMode == ResizeMode.Min)
			{
				num4 = height;
				num3 = width;
				flag = false;
			}
			num3 = ((num3 > 0) ? num3 : int.MaxValue);
			num4 = ((num4 > 0) ? num4 : int.MaxValue);
			List<Size> restrictedSizes = ResizeLayer.RestrictedSizes;
			if (restrictedSizes != null && restrictedSizes.Count > 0)
			{
				bool flag2 = true;
				foreach (Size item3 in restrictedSizes)
				{
					if (item3.Height == 0 || item3.Width == 0)
					{
						if (item3.Width == width2 || item3.Height == height2)
						{
							flag2 = false;
						}
					}
					else if (item3.Width == width2 && item3.Height == height2)
					{
						flag2 = false;
					}
				}
				if (flag2)
				{
					return (Bitmap)source;
				}
			}
			if (width2 > 0 && height2 > 0 && width2 <= num3 && height2 <= num4)
			{
				if ((width2 > width || height2 > height) && !flag && ResizeLayer.ResizeMode != ResizeMode.Stretch)
				{
					return (Bitmap)source;
				}
				bitmap = (linear ? ResizeLinear(source, width2, height2, item2, AnimationProcessMode) : ResizeComposite(source, width2, height2, item2));
				source.Dispose();
				source = bitmap;
			}
		}
		catch (Exception innerException)
		{
			bitmap?.Dispose();
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
		return (Bitmap)source;
	}

	protected virtual Bitmap ResizeComposite(Image source, int width, int height, Rectangle destination)
	{
		Bitmap bitmap = new Bitmap(width, height, source.PixelFormat);
		bitmap.SetResolution(source.HorizontalResolution, source.VerticalResolution);
		using Graphics graphics = Graphics.FromImage(bitmap);
		GraphicsHelper.SetGraphicsOptions(graphics);
		using ImageAttributes imageAttributes = new ImageAttributes();
		imageAttributes.SetWrapMode(WrapMode.TileFlipXY);
		graphics.DrawImage(source, destination, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, imageAttributes);
		return bitmap;
	}

	protected virtual Bitmap ResizeLinear(Image source, int targetWidth, int targetHeight, Rectangle destination)
	{
		return ResizeLinear(source, targetWidth, targetHeight, destination, AnimationProcessMode);
	}

	protected virtual Bitmap ResizeLinear(Image source, int width, int height, Rectangle destination, AnimationProcessMode animationProcessMode)
	{
		Bitmap bitmap = Adjustments.ToLinear(source.Copy(animationProcessMode));
		Bitmap bitmap2 = new Bitmap(width, height, source.PixelFormat);
		bitmap2.SetResolution(source.HorizontalResolution, source.VerticalResolution);
		using (Graphics graphics = Graphics.FromImage(bitmap2))
		{
			GraphicsHelper.SetGraphicsOptions(graphics);
			using ImageAttributes imageAttributes = new ImageAttributes();
			imageAttributes.SetWrapMode(WrapMode.TileFlipXY);
			graphics.DrawImage(bitmap, destination, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, imageAttributes);
		}
		bitmap2 = Adjustments.ToSRGB(bitmap2);
		bitmap.Dispose();
		return bitmap2;
	}
}
