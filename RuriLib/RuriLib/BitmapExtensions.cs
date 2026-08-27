using System.Drawing;
using System.Drawing.Imaging;

namespace RuriLib;

public static class BitmapExtensions
{
	public static ImageFormat GetImageFormat(this Image img)
	{
		if (img.RawFormat.Equals(ImageFormat.Jpeg))
		{
			return ImageFormat.Jpeg;
		}
		if (img.RawFormat.Equals(ImageFormat.Bmp))
		{
			return ImageFormat.Bmp;
		}
		if (img.RawFormat.Equals(ImageFormat.Png))
		{
			return ImageFormat.Png;
		}
		if (img.RawFormat.Equals(ImageFormat.Emf))
		{
			return ImageFormat.Emf;
		}
		if (img.RawFormat.Equals(ImageFormat.Exif))
		{
			return ImageFormat.Exif;
		}
		if (img.RawFormat.Equals(ImageFormat.Gif))
		{
			return ImageFormat.Gif;
		}
		if (img.RawFormat.Equals(ImageFormat.Icon))
		{
			return ImageFormat.Icon;
		}
		if (img.RawFormat.Equals(ImageFormat.MemoryBmp))
		{
			return ImageFormat.MemoryBmp;
		}
		if (img.RawFormat.Equals(ImageFormat.Tiff))
		{
			return ImageFormat.Tiff;
		}
		return ImageFormat.Wmf;
	}

	public static Bitmap ConvertPixelFormat(this Image image, PixelFormat pixelFormat)
	{
		Bitmap bitmap = new Bitmap(image);
		Bitmap bitmap2 = new Bitmap(bitmap.Width, bitmap.Height, pixelFormat);
		using Graphics graphics = Graphics.FromImage(bitmap2);
		graphics.DrawImage(bitmap, new Rectangle(0, 0, bitmap2.Width, bitmap2.Height));
		return bitmap2;
	}
}
