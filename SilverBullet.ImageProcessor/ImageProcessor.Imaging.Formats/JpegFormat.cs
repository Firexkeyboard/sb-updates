using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace ImageProcessor.Imaging.Formats;

public sealed class JpegFormat : FormatBase
{
	public override byte[][] FileHeaders => new byte[3][]
	{
		new byte[3] { 255, 216, 255 },
		Encoding.ASCII.GetBytes("ÿØÿà..JFIF"),
		Encoding.ASCII.GetBytes("ÿØÿà..EXIF")
	};

	public override string[] FileExtensions => new string[2] { "jpeg", "jpg" };

	public override string MimeType => "image/jpeg";

	public override ImageFormat ImageFormat => ImageFormat.Jpeg;

	public override Image Save(Stream stream, Image image, long bitDepth)
	{
		using (EncoderParameters encoderParams = FormatUtilities.GetEncodingParameters(base.Quality))
		{
			ImageCodecInfo imageCodecInfo = Array.Find(ImageCodecInfo.GetImageEncoders(), (ImageCodecInfo ici) => ici.MimeType.Equals(MimeType, StringComparison.OrdinalIgnoreCase));
			if (imageCodecInfo != null)
			{
				image.Save(stream, imageCodecInfo, encoderParams);
			}
		}
		return image;
	}

	public override Image Save(string path, Image image, long bitDepth)
	{
		using (EncoderParameters encoderParams = FormatUtilities.GetEncodingParameters(base.Quality))
		{
			ImageCodecInfo imageCodecInfo = Array.Find(ImageCodecInfo.GetImageEncoders(), (ImageCodecInfo ici) => ici.MimeType.Equals(MimeType, StringComparison.OrdinalIgnoreCase));
			if (imageCodecInfo != null)
			{
				image.Save(path, imageCodecInfo, encoderParams);
			}
		}
		return image;
	}
}
