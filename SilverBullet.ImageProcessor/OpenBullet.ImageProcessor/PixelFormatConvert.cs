using System.Drawing;
using System.Drawing.Imaging;

namespace OpenBullet.ImageProcessor;

public abstract class PixelFormatConvert
{
	public static Bitmap To(Bitmap orig, PixelFormat pixelFormat)
	{
		Bitmap bitmap = new Bitmap(orig.Width, orig.Height, pixelFormat);
		using Graphics graphics = Graphics.FromImage(bitmap);
		graphics.DrawImage(orig, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
		return bitmap;
	}
}
