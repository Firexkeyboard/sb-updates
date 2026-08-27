using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessor.Common.Exceptions;

namespace ImageProcessor.Processors;

public class ColorThreshold : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Image ProcessImage(ImageFactory factory)
	{
		Image image = factory.Image;
		Bitmap bitmap = null;
		try
		{
			bitmap = new Bitmap(image.Width, image.Height);
			ImageAttributes imageAttributes = new ImageAttributes();
			imageAttributes.SetThreshold((float)DynamicParameter);
			Point[] destPoints = new Point[3]
			{
				new Point(0, 0),
				new Point(image.Width, 0),
				new Point(0, image.Height)
			};
			Rectangle srcRect = new Rectangle(0, 0, image.Width, image.Height);
			using Graphics graphics = Graphics.FromImage(bitmap);
			graphics.DrawImage(image, destPoints, srcRect, GraphicsUnit.Pixel, imageAttributes);
			return bitmap;
		}
		catch (Exception innerException)
		{
			bitmap?.Dispose();
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
	}
}
