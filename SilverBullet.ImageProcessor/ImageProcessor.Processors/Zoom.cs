using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using ImageProcessor.Common.Exceptions;
using OpenBullet.ImageProcessor;

namespace ImageProcessor.Processors;

public class Zoom : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Image ProcessImage(ImageFactory factory)
	{
		Size size = factory.Bitmap.Size;
		ZoomLayer zoomLayer = (ZoomLayer)DynamicParameter;
		Bitmap bitmap = null;
		try
		{
			bitmap = new Bitmap(size.Width * zoomLayer.ZoomFactor, size.Height * zoomLayer.ZoomFactor);
			using Graphics graphics = Graphics.FromImage(bitmap);
			if (zoomLayer.NearestNeighbor)
			{
				graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			}
			graphics.DrawImage(factory.Bitmap, new Rectangle(Point.Empty, bitmap.Size));
			return bitmap;
		}
		catch (Exception innerException)
		{
			bitmap?.Dispose();
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
	}
}
