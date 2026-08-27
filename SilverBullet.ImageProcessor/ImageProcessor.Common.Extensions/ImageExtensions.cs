using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Formats;
using ImageProcessor.Imaging.Quantizers;
using OpenCvSharp;

namespace ImageProcessor.Common.Extensions;

internal static class ImageExtensions
{
	public static Image Copy(this Image source, AnimationProcessMode animationProcessMode, PixelFormat format = PixelFormat.Format32bppPArgb, bool preserveExifData = false)
	{
		if (source.RawFormat.Equals(ImageFormat.Gif))
		{
			source.SelectActiveFrame(FrameDimension.Time, 0);
			GifDecoder gifDecoder = new GifDecoder(source, animationProcessMode);
			GifEncoder gifEncoder = new GifEncoder(null, null, gifDecoder.LoopCount);
			OctreeQuantizer octreeQuantizer = new OctreeQuantizer();
			for (int i = 0; i < gifDecoder.FrameCount; i++)
			{
				GifFrame frame = gifDecoder.GetFrame(source, i);
				frame.Image = octreeQuantizer.Quantize(((Bitmap)frame.Image).Clone(new Rectangle(0, 0, frame.Image.Width, frame.Image.Height), format));
				((Bitmap)frame.Image).SetResolution(source.HorizontalResolution, source.VerticalResolution);
				gifEncoder.AddFrame(frame);
			}
			return gifEncoder.Save();
		}
		Bitmap bitmap = new Bitmap(source.Width, source.Height, format);
		bitmap.SetResolution(source.HorizontalResolution, source.VerticalResolution);
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			graphics.DrawImageUnscaled(source, 0, 0);
		}
		if (preserveExifData)
		{
			PropertyItem[] propertyItems = source.PropertyItems;
			foreach (PropertyItem propertyItem in propertyItems)
			{
				bitmap.SetPropertyItem(propertyItem);
			}
		}
		return bitmap;
	}

	public static Image Copy(this Image source, PixelFormat format = PixelFormat.Format32bppPArgb)
	{
		return source.Copy(AnimationProcessMode.All, format);
	}

	public static Bitmap ToBitmap(this Mat mat)
	{
		using MemoryStream stream = mat.ToMemoryStream(".png");
		return (Bitmap)Image.FromStream(stream);
	}
}
