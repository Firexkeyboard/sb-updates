using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessor.Imaging.Filters.Artistic;
using ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Imaging.Filters.Photo;

internal class ComicMatrixFilter : MatrixFilterBase
{
	public override ColorMatrix Matrix => ColorMatrixes.ComicLow;

	public override Bitmap TransformImage(Image source, Image destination)
	{
		Bitmap bitmap = null;
		Bitmap bitmap2 = null;
		Bitmap bitmap3 = null;
		Bitmap bitmap4 = null;
		int width = source.Width;
		int height = source.Height;
		try
		{
			using (ImageAttributes imageAttributes = new ImageAttributes())
			{
				Rectangle rectangle = new Rectangle(0, 0, source.Width, source.Height);
				imageAttributes.SetColorMatrix(ColorMatrixes.ComicHigh);
				bitmap = new Bitmap(rectangle.Width, rectangle.Height, PixelFormat.Format32bppPArgb);
				bitmap.SetResolution(source.HorizontalResolution, source.VerticalResolution);
				bitmap = new OilPaintingFilter(3, 5).ApplyFilter((Bitmap)source);
				bitmap4 = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
				bitmap4.SetResolution(source.HorizontalResolution, source.VerticalResolution);
				bitmap4 = Effects.Trace(source, bitmap4, 120);
				using (Graphics graphics = Graphics.FromImage(bitmap))
				{
					graphics.DrawImage(bitmap, rectangle, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, imageAttributes);
				}
				bitmap2 = new Bitmap(rectangle.Width, rectangle.Height, PixelFormat.Format32bppPArgb);
				bitmap2.SetResolution(source.HorizontalResolution, source.VerticalResolution);
				imageAttributes.SetColorMatrix(Matrix);
				using (Graphics graphics2 = Graphics.FromImage(bitmap2))
				{
					graphics2.DrawImage(bitmap, rectangle, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, imageAttributes);
				}
				bitmap3 = new Bitmap(rectangle.Width, rectangle.Height, PixelFormat.Format32bppPArgb);
				bitmap3.SetResolution(source.HorizontalResolution, source.VerticalResolution);
				using (Graphics graphics3 = Graphics.FromImage(bitmap3))
				{
					graphics3.Clear(Color.Transparent);
					for (int i = 0; i < height; i += 8)
					{
						for (int j = 0; j < width; j += 4)
						{
							graphics3.FillEllipse(Brushes.White, j, i, 3, 3);
							graphics3.FillEllipse(Brushes.White, j + 2, i + 4, 3, 3);
						}
					}
				}
				bitmap2 = Effects.ApplyMask(bitmap2, bitmap3);
				using Graphics graphics4 = Graphics.FromImage(destination);
				graphics4.Clear(Color.Transparent);
				graphics4.DrawImage(bitmap, 0, 0);
				graphics4.DrawImage(bitmap2, 0, 0);
				graphics4.DrawImage(bitmap4, 0, 0);
				using (Pen pen = new Pen(Color.Black))
				{
					pen.Width = 4f;
					graphics4.DrawRectangle(pen, rectangle);
				}
				bitmap.Dispose();
				bitmap2.Dispose();
				bitmap3.Dispose();
				bitmap4.Dispose();
			}
			source.Dispose();
			source = destination;
		}
		catch
		{
			destination?.Dispose();
			bitmap?.Dispose();
			bitmap2?.Dispose();
			bitmap3?.Dispose();
			bitmap4?.Dispose();
		}
		return (Bitmap)source;
	}
}
