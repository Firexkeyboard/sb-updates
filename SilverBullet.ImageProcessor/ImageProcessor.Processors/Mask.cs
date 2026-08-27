using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Colors;
using ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Processors;

public class Mask : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Mask()
	{
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		Bitmap bitmap = null;
		Bitmap bitmap2 = null;
		Bitmap bitmap3 = null;
		Bitmap bitmap4 = null;
		Image image = factory.Image;
		try
		{
			int width = image.Width;
			int height = image.Height;
			ImageLayer obj = DynamicParameter;
			bitmap2 = new Bitmap(obj.Image);
			bitmap2.SetResolution(image.HorizontalResolution, image.VerticalResolution);
			Point? position = obj.Position;
			if (bitmap2.Size != image.Size)
			{
				Rectangle parent = new Rectangle(0, 0, width, height);
				Rectangle filteredBoundingRectangle = ImageMaths.GetFilteredBoundingRectangle(bitmap2, 0, RgbaComponent.A);
				bitmap3 = new Bitmap(filteredBoundingRectangle.Width, filteredBoundingRectangle.Height, PixelFormat.Format32bppPArgb);
				bitmap3.SetResolution(image.HorizontalResolution, image.VerticalResolution);
				using (Graphics graphics = Graphics.FromImage(bitmap3))
				{
					GraphicsHelper.SetGraphicsOptions(graphics);
					graphics.Clear(Color.Transparent);
					graphics.DrawImage(bitmap2, new Rectangle(0, 0, filteredBoundingRectangle.Width, filteredBoundingRectangle.Height), filteredBoundingRectangle.X, filteredBoundingRectangle.Y, filteredBoundingRectangle.Width, filteredBoundingRectangle.Height, GraphicsUnit.Pixel);
				}
				bitmap4 = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
				bitmap4.SetResolution(image.HorizontalResolution, image.VerticalResolution);
				using (Graphics graphics2 = Graphics.FromImage(bitmap4))
				{
					GraphicsHelper.SetGraphicsOptions(graphics2, blending: true);
					graphics2.Clear(Color.Transparent);
					if (position.HasValue)
					{
						graphics2.DrawImage(bitmap3, position.Value);
					}
					else
					{
						RectangleF rectangleF = ImageMaths.CenteredRectangle(parent, filteredBoundingRectangle);
						graphics2.DrawImage(bitmap3, new PointF(rectangleF.X, rectangleF.Y));
					}
				}
				bitmap = Effects.ApplyMask(image, bitmap4);
				bitmap3.Dispose();
				bitmap4.Dispose();
			}
			else
			{
				bitmap = Effects.ApplyMask(image, bitmap2);
				bitmap2.Dispose();
			}
			factory.CurrentBitDepth = 32L;
			image.Dispose();
			return bitmap;
		}
		catch (Exception innerException)
		{
			bitmap2?.Dispose();
			bitmap3?.Dispose();
			bitmap4?.Dispose();
			bitmap?.Dispose();
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
	}
}
