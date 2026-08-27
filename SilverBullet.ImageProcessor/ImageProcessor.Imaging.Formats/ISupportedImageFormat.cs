using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ImageProcessor.Imaging.Formats;

public interface ISupportedImageFormat
{
	byte[][] FileHeaders { get; }

	string[] FileExtensions { get; }

	string MimeType { get; }

	string DefaultExtension { get; }

	ImageFormat ImageFormat { get; }

	bool IsIndexed { get; set; }

	int Quality { get; set; }

	void ApplyProcessor(Func<ImageFactory, Image> processor, ImageFactory factory);

	Image Load(Stream stream);

	Image Save(Stream stream, Image image, long bitDepth);

	Image Save(string path, Image image, long bitDepth);
}
