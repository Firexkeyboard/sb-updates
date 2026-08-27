using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ImageProcessor.Imaging.Formats;

public abstract class FormatBase : ISupportedImageFormat, IEquatable<ISupportedImageFormat>
{
	public abstract byte[][] FileHeaders { get; }

	public abstract string[] FileExtensions { get; }

	public abstract string MimeType { get; }

	public string DefaultExtension => MimeType.Replace("image/", string.Empty);

	public abstract ImageFormat ImageFormat { get; }

	public bool IsIndexed { get; set; }

	public int Quality { get; set; }

	protected FormatBase()
	{
		Quality = 90;
	}

	public virtual void ApplyProcessor(Func<ImageFactory, Image> processor, ImageFactory factory)
	{
		factory.Image = processor(factory);
	}

	public virtual Image Load(Stream stream)
	{
		return Image.FromStream(stream, useEmbeddedColorManagement: true, validateImageData: false);
	}

	public virtual Image Save(Stream stream, Image image, long bitDepth)
	{
		image.Save(stream, ImageFormat);
		return image;
	}

	public virtual Image Save(string path, Image image, long bitDepth)
	{
		image.Save(path, ImageFormat);
		return image;
	}

	public override bool Equals(object obj)
	{
		if (obj is ISupportedImageFormat other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(ISupportedImageFormat other)
	{
		if (other != null && MimeType == other.MimeType)
		{
			return IsIndexed == other.IsIndexed;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (MimeType, IsIndexed).GetHashCode();
	}
}
