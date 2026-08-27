using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using ImageProcessor.Common.Extensions;

namespace ImageProcessor.Imaging.Formats;

public class BitmapFormat : FormatBase
{
	public override byte[][] FileHeaders => new byte[1][] { Encoding.ASCII.GetBytes("BM") };

	public override string[] FileExtensions => new string[1] { "bmp" };

	public override string MimeType => "image/bmp";

	public override ImageFormat ImageFormat => ImageFormat.Bmp;

	public override Image Save(Stream stream, Image image, long bitDepth)
	{
		PixelFormat format = PixelFormat.Format32bppPArgb;
		switch (bitDepth)
		{
		case 24L:
			format = PixelFormat.Format24bppRgb;
			break;
		case 8L:
			format = PixelFormat.Format8bppIndexed;
			break;
		}
		using Image image2 = image.Copy(format);
		image2.Save(stream, ImageFormat);
		return image;
	}

	public override Image Save(string path, Image image, long bitDepth)
	{
		PixelFormat format = PixelFormat.Format32bppPArgb;
		switch (bitDepth)
		{
		case 24L:
			format = PixelFormat.Format24bppRgb;
			break;
		case 8L:
			format = PixelFormat.Format8bppIndexed;
			break;
		}
		using Image image2 = image.Copy(format);
		image2.Save(path, ImageFormat);
		return image;
	}
}
