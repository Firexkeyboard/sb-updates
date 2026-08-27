using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using ImageProcessor.Imaging.Quantizers;

namespace ImageProcessor.Imaging.Formats;

public class GifFormat : FormatBase, IQuantizableImageFormat, IAnimatedImageFormat
{
	public AnimationProcessMode AnimationProcessMode { get; set; }

	public override byte[][] FileHeaders => new byte[1][] { Encoding.ASCII.GetBytes("GIF") };

	public override string[] FileExtensions => new string[1] { "gif" };

	public override string MimeType => "image/gif";

	public override ImageFormat ImageFormat => ImageFormat.Gif;

	public IQuantizer Quantizer { get; set; } = new OctreeQuantizer(255, 8);

	public override void ApplyProcessor(Func<ImageFactory, Image> processor, ImageFactory factory)
	{
		GifDecoder gifDecoder = new GifDecoder(factory.Image, factory.AnimationProcessMode);
		Image image = factory.Image;
		GifEncoder gifEncoder = new GifEncoder(null, null, gifDecoder.LoopCount);
		for (int i = 0; i < gifDecoder.FrameCount; i++)
		{
			GifFrame frame = gifDecoder.GetFrame(image, i);
			factory.Image = frame.Image;
			frame.Image = Quantizer.Quantize(processor(factory));
			gifEncoder.AddFrame(frame);
		}
		image.Dispose();
		factory.Image = gifEncoder.Save();
	}

	public override Image Save(Stream stream, Image image, long bitDepth)
	{
		GifDecoder gifDecoder = new GifDecoder(image, AnimationProcessMode.All);
		GifEncoder gifEncoder = new GifEncoder(null, null, gifDecoder.LoopCount);
		for (int i = 0; i < gifDecoder.FrameCount; i++)
		{
			GifFrame frame = gifDecoder.GetFrame(image, i);
			frame.Image = Quantizer.Quantize(frame.Image);
			gifEncoder.AddFrame(frame);
		}
		gifEncoder.Save(stream);
		return gifEncoder.Save();
	}

	public override Image Save(string path, Image image, long bitDepth)
	{
		using FileStream stream = File.OpenWrite(path);
		GifDecoder gifDecoder = new GifDecoder(image, AnimationProcessMode.All);
		GifEncoder gifEncoder = new GifEncoder(null, null, gifDecoder.LoopCount);
		for (int i = 0; i < gifDecoder.FrameCount; i++)
		{
			GifFrame frame = gifDecoder.GetFrame(image, i);
			frame.Image = Quantizer.Quantize(frame.Image);
			gifEncoder.AddFrame(frame);
		}
		gifEncoder.Save(stream);
		return gifEncoder.Save();
	}
}
