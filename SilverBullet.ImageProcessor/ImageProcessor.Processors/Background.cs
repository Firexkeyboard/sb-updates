using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Processors;

public class Background : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Background()
	{
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		Image image = factory.Image;
		Bitmap bitmap = null;
		Bitmap bitmap2 = null;
		try
		{
			ImageLayer obj = DynamicParameter;
			bitmap = new Bitmap(obj.Image);
			bitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);
			Size size = obj.Size;
			int width = image.Width;
			int height = image.Height;
			int num = ((size != Size.Empty) ? Math.Min(width, size.Width) : Math.Min(width, bitmap.Width));
			int num2 = ((size != Size.Empty) ? Math.Min(height, size.Height) : Math.Min(height, bitmap.Height));
			Point? position = obj.Position;
			int opacity = obj.Opacity;
			if (image.Size != bitmap.Size || image.Size != new Size(num, num2))
			{
				bitmap = new Resizer(new ResizeLayer(new Size(num, num2), ResizeMode.Max, AnchorPosition.Center, upscale: true, null, null, null, null))
				{
					AnimationProcessMode = factory.AnimationProcessMode
				}.ResizeImage(bitmap, factory.FixGamma);
				num = bitmap.Width;
				num2 = bitmap.Height;
			}
			Rectangle parent = new Rectangle(0, 0, width, height);
			Rectangle child = new Rectangle(0, 0, num, num2);
			if (opacity < 100)
			{
				bitmap = Adjustments.Alpha(bitmap, opacity, null);
			}
			bitmap2 = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
			bitmap2.SetResolution(image.HorizontalResolution, image.VerticalResolution);
			using (Graphics graphics = Graphics.FromImage(bitmap2))
			{
				GraphicsHelper.SetGraphicsOptions(graphics, blending: true);
				if (position.HasValue)
				{
					graphics.DrawImage(bitmap, new Point(Math.Min(position.Value.X, width - num), Math.Min(position.Value.Y, height - num2)));
				}
				else
				{
					RectangleF rectangleF = ImageMaths.CenteredRectangle(parent, child);
					graphics.DrawImage(bitmap, new PointF(rectangleF.X, rectangleF.Y));
				}
				graphics.DrawImage(image, 0, 0, width, height);
			}
			image.Dispose();
			return bitmap2;
		}
		catch (Exception innerException)
		{
			bitmap2?.Dispose();
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
		finally
		{
			bitmap?.Dispose();
		}
	}
}
