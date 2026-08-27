using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using ImageProcessor.Configuration;

namespace ImageProcessor.Imaging.Formats;

public static class FormatUtilities
{
	public static ISupportedImageFormat GetFormat(Stream stream)
	{
		if (stream.CanSeek)
		{
			stream.Position = 0L;
		}
		IEnumerable<ISupportedImageFormat> supportedImageFormats = ImageProcessorBootstrapper.Instance.SupportedImageFormats;
		byte[] array = new byte[supportedImageFormats.Max((ISupportedImageFormat f) => f.FileHeaders.Max((byte[] h) => h.Length))];
		stream.Read(array, 0, array.Length);
		foreach (ISupportedImageFormat item in supportedImageFormats)
		{
			byte[][] fileHeaders = item.FileHeaders;
			foreach (byte[] array2 in fileHeaders)
			{
				if (array2.SequenceEqual(array.Take(array2.Length)))
				{
					if (stream.CanSeek)
					{
						stream.Position = 0L;
					}
					return Activator.CreateInstance(item.GetType()) as ISupportedImageFormat;
				}
			}
		}
		if (stream.CanSeek)
		{
			stream.Position = 0L;
		}
		return null;
	}

	public static bool IsIndexed(Image image)
	{
		return (image.PixelFormat & PixelFormat.Indexed) != 0;
	}

	public static bool HasAlpha(Image image)
	{
		return (image.Flags & 2) == 2;
	}

	public static bool IsAnimated(Image image)
	{
		return ImageAnimator.CanAnimate(image);
	}

	public static EncoderParameters GetEncodingParameters(int quality)
	{
		EncoderParameters encoderParameters = null;
		try
		{
			EncoderParameters encoderParameters2 = new EncoderParameters(1);
			encoderParameters2.Param[0] = new EncoderParameter(Encoder.Quality, quality);
			encoderParameters = encoderParameters2;
		}
		catch
		{
			encoderParameters?.Dispose();
		}
		return encoderParameters;
	}

	public static PropertyItem CreatePropertyItem()
	{
		return (PropertyItem)typeof(PropertyItem).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[0], null).Invoke(null);
	}
}
