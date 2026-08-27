using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging.Filters.Artistic;
using ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Processors;

public class Halftone : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Halftone()
	{
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		Image image = factory.Image;
		int width = image.Width;
		int height = image.Height;
		Bitmap bitmap = null;
		Bitmap bitmap2 = null;
		try
		{
			HalftoneFilter halftoneFilter = new HalftoneFilter(5);
			bitmap = new Bitmap(image);
			bitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);
			bitmap = halftoneFilter.ApplyFilter(bitmap);
			if ((bool)DynamicParameter)
			{
				bitmap2 = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
				bitmap2.SetResolution(image.HorizontalResolution, image.VerticalResolution);
				bitmap2 = Effects.Trace(image, bitmap2, 120);
				using (Graphics graphics = Graphics.FromImage(bitmap))
				{
					graphics.DrawImage(bitmap2, 0, 0);
					Rectangle rect = new Rectangle(0, 0, width, height);
					using Pen pen = new Pen(Color.Black);
					pen.Width = 4f;
					graphics.DrawRectangle(pen, rect);
				}
				bitmap2.Dispose();
			}
			image.Dispose();
			return bitmap;
		}
		catch (Exception innerException)
		{
			bitmap2?.Dispose();
			bitmap?.Dispose();
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
	}
}
