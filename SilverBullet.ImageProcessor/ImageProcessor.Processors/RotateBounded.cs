using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging.Helpers;
using ImageProcessor.Imaging.MetaData;

namespace ImageProcessor.Processors;

public class RotateBounded : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Image ProcessImage(ImageFactory factory)
	{
		Image image = factory.Image;
		try
		{
			Tuple<float, bool> tuple = DynamicParameter;
			image = RotateImage(image, tuple.Item1, tuple.Item2);
			if (factory.PreserveExifData && factory.ExifPropertyItems.Count > 0)
			{
				factory.SetPropertyItem(ExifPropertyTag.ImageWidth, (ushort)image.Width);
				factory.SetPropertyItem(ExifPropertyTag.ImageHeight, (ushort)image.Height);
			}
			factory.CurrentBitDepth = 32L;
			return image;
		}
		catch (Exception innerException)
		{
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
	}

	private Bitmap RotateImage(Image image, float angleInDegrees, bool keepSize)
	{
		Size size = new Size(image.Width, image.Height);
		float num = ImageMaths.ZoomAfterRotation(image.Width, image.Height, angleInDegrees);
		if (!keepSize)
		{
			size.Width = Math.Max(1, (int)Math.Floor((float)size.Width / num));
			size.Height = Math.Max(1, (int)Math.Floor((float)size.Height / num));
		}
		float num2 = Math.Abs(image.Width / 2);
		float num3 = Math.Abs(image.Height / 2);
		Bitmap bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
		bitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			GraphicsHelper.SetGraphicsOptions(graphics);
			if (keepSize)
			{
				graphics.TranslateTransform(num2, num3);
				graphics.RotateTransform(angleInDegrees);
				graphics.ScaleTransform(num, num);
				graphics.TranslateTransform(0f - num2, 0f - num3);
				graphics.DrawImage(image, new PointF(0f, 0f));
			}
			else
			{
				float num4 = num2;
				float num5 = num3;
				num2 = Math.Abs(bitmap.Width / 2);
				num3 = Math.Abs(bitmap.Height / 2);
				graphics.TranslateTransform(num2, num3);
				graphics.RotateTransform(angleInDegrees);
				graphics.DrawImage(image, new PointF(0f - num4, 0f - num5));
			}
		}
		image.Dispose();
		return bitmap;
	}
}
