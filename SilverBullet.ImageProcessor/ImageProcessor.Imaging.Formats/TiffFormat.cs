using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ImageProcessor.Imaging.Formats;

public class TiffFormat : FormatBase
{
	public override byte[][] FileHeaders => new byte[2][]
	{
		new byte[4] { 73, 73, 42, 0 },
		new byte[4] { 77, 77, 0, 42 }
	};

	public override string[] FileExtensions => new string[2] { "tiff", "tif" };

	public override string MimeType => "image/tiff";

	public override ImageFormat ImageFormat => ImageFormat.Tiff;

	public override void ApplyProcessor(Func<ImageFactory, Image> processor, ImageFactory factory)
	{
		base.ApplyProcessor(processor, factory);
		if (!factory.PreserveExifData)
		{
			return;
		}
		foreach (KeyValuePair<int, PropertyItem> exifPropertyItem in factory.ExifPropertyItems)
		{
			factory.Image.SetPropertyItem(exifPropertyItem.Value);
		}
	}

	public override Image Save(Stream stream, Image image, long bitDepth)
	{
		using (EncoderParameters encoderParameters = new EncoderParameters(2))
		{
			encoderParameters.Param[0] = new EncoderParameter(Encoder.Compression, (bitDepth == 1) ? 4 : 2);
			encoderParameters.Param[1] = new EncoderParameter(Encoder.ColorDepth, Math.Min(32L, bitDepth));
			ImageCodecInfo imageCodecInfo = Array.Find(ImageCodecInfo.GetImageEncoders(), (ImageCodecInfo ici) => ici.MimeType.Equals(MimeType, StringComparison.OrdinalIgnoreCase));
			if (imageCodecInfo != null)
			{
				image.Save(stream, imageCodecInfo, encoderParameters);
			}
		}
		return image;
	}

	public override Image Save(string path, Image image, long bitDepth)
	{
		using (EncoderParameters encoderParameters = new EncoderParameters(2))
		{
			encoderParameters.Param[0] = new EncoderParameter(Encoder.Compression, (bitDepth == 1) ? 4 : 2);
			encoderParameters.Param[1] = new EncoderParameter(Encoder.ColorDepth, Math.Min(32L, bitDepth));
			ImageCodecInfo imageCodecInfo = Array.Find(ImageCodecInfo.GetImageEncoders(), (ImageCodecInfo ici) => ici.MimeType.Equals(MimeType, StringComparison.OrdinalIgnoreCase));
			if (imageCodecInfo != null)
			{
				image.Save(path, imageCodecInfo, encoderParameters);
			}
		}
		return image;
	}
}
