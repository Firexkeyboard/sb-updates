using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Processors;

public class RoundedCorners : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public RoundedCorners()
	{
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		Image image = factory.Image;
		try
		{
			RoundedCornerLayer obj = DynamicParameter;
			int radius = obj.Radius;
			bool topLeft = obj.TopLeft;
			bool topRight = obj.TopRight;
			bool bottomLeft = obj.BottomLeft;
			bool bottomRight = obj.BottomRight;
			factory.CurrentBitDepth = 32L;
			return RoundCornerImage(image, radius, topLeft, topRight, bottomLeft, bottomRight);
		}
		catch (Exception innerException)
		{
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
	}

	private Bitmap RoundCornerImage(Image image, int cornerRadius, bool topLeft = false, bool topRight = false, bool bottomLeft = false, bool bottomRight = false)
	{
		int width = image.Width;
		int height = image.Height;
		int num = cornerRadius * 2;
		Bitmap bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppPArgb);
		bitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			GraphicsHelper.SetGraphicsOptions(graphics, blending: true, smoothing: true);
			using GraphicsPath graphicsPath = new GraphicsPath();
			if (topLeft)
			{
				graphicsPath.AddArc(0, 0, num, num, 180f, 90f);
			}
			else
			{
				graphicsPath.AddLine(0, 0, 0, 0);
			}
			if (topRight)
			{
				graphicsPath.AddArc(width - num, 0, num, num, 270f, 90f);
			}
			else
			{
				graphicsPath.AddLine(width, 0, width, 0);
			}
			if (bottomRight)
			{
				graphicsPath.AddArc(width - num, height - num, num, num, 0f, 90f);
			}
			else
			{
				graphicsPath.AddLine(width, height, width, height);
			}
			if (bottomLeft)
			{
				graphicsPath.AddArc(0, height - num, num, num, 90f, 90f);
			}
			else
			{
				graphicsPath.AddLine(0, height, 0, height);
			}
			using Brush brush = new TextureBrush(image);
			graphics.FillPath(brush, graphicsPath);
		}
		image.Dispose();
		return bitmap;
	}
}
