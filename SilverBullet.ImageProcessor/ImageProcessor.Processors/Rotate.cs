using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging.Helpers;
using ImageProcessor.Imaging.MetaData;

namespace ImageProcessor.Processors;

public class Rotate : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Rotate()
	{
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		Image image = factory.Image;
		try
		{
			float angle = DynamicParameter;
			float rotateAtX = Math.Abs(image.Width / 2);
			float rotateAtY = Math.Abs(image.Height / 2);
			image = RotateImage(image, rotateAtX, rotateAtY, angle);
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

	private Bitmap RotateImage(Image image, float rotateAtX, float rotateAtY, float angle)
	{
		Rectangle boundingRotatedRectangle = ImageMaths.GetBoundingRotatedRectangle(image.Width, image.Height, angle);
		int num = (boundingRotatedRectangle.Width - image.Width) / 2;
		int num2 = (boundingRotatedRectangle.Height - image.Height) / 2;
		Bitmap bitmap = new Bitmap(boundingRotatedRectangle.Width, boundingRotatedRectangle.Height, PixelFormat.Format32bppPArgb);
		bitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			GraphicsHelper.SetGraphicsOptions(graphics);
			graphics.TranslateTransform(rotateAtX + (float)num, rotateAtY + (float)num2);
			graphics.RotateTransform(angle);
			graphics.TranslateTransform(0f - rotateAtX - (float)num, 0f - rotateAtY - (float)num2);
			graphics.DrawImage(image, new PointF(num, num2));
		}
		image.Dispose();
		return bitmap;
	}
}
