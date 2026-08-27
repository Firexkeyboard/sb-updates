using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ImageProcessor.Common.Extensions;
using ImageProcessor.Imaging.Quantizers;
using ImageProcessor.Imaging.Quantizers.WuQuantizer;

namespace ImageProcessor.Imaging.Formats;

public class PngFormat : FormatBase, IQuantizableImageFormat
{
	public override byte[][] FileHeaders => new byte[1][] { new byte[4] { 137, 80, 78, 71 } };

	public override string[] FileExtensions => new string[1] { "png" };

	public override string MimeType => "image/png";

	public override ImageFormat ImageFormat => ImageFormat.Png;

	public IQuantizer Quantizer { get; set; } = new WuQuantizer();

	public override Image Save(Stream stream, Image image, long bitDepth)
	{
		if (base.IsIndexed)
		{
			image = Quantizer.Quantize(image);
			return base.Save(stream, image, bitDepth);
		}
		PixelFormat format = PixelFormat.Format32bppPArgb;
		if (bitDepth == 24)
		{
			format = PixelFormat.Format24bppRgb;
		}
		using Image image2 = image.Copy(format);
		image2.Save(stream, ImageFormat);
		return image;
	}

	public override Image Save(string path, Image image, long bitDepth)
	{
		if (base.IsIndexed)
		{
			image = Quantizer.Quantize(image);
			return base.Save(path, image, bitDepth);
		}
		PixelFormat format = PixelFormat.Format32bppPArgb;
		if (bitDepth == 24)
		{
			format = PixelFormat.Format24bppRgb;
		}
		using Image image2 = image.Copy(format);
		image2.Save(path, ImageFormat);
		return image;
	}
}
