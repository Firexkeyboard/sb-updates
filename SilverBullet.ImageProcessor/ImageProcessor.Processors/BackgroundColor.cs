using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Processors;

public class BackgroundColor : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public BackgroundColor()
	{
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		Bitmap bitmap = null;
		Image image = factory.Image;
		try
		{
			int width = image.Width;
			int height = image.Height;
			Color color = DynamicParameter;
			bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
			bitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);
			using (Graphics graphics = Graphics.FromImage(bitmap))
			{
				GraphicsHelper.SetGraphicsOptions(graphics, blending: true);
				graphics.Clear(color);
				graphics.DrawImage(image, 0, 0, width, height);
			}
			if (color.A < byte.MaxValue)
			{
				factory.CurrentBitDepth = 32L;
			}
			image.Dispose();
			return bitmap;
		}
		catch (Exception innerException)
		{
			bitmap?.Dispose();
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
	}
}
