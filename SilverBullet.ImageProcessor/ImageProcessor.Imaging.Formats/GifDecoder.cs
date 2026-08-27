using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace ImageProcessor.Imaging.Formats;

public class GifDecoder
{
	public int Width { get; set; }

	public int Height { get; set; }

	public bool IsAnimated { get; set; }

	public int LoopCount { get; set; }

	public int FrameCount { get; set; }

	public GifDecoder(Image image)
		: this(image, AnimationProcessMode.All)
	{
	}

	public GifDecoder(Image image, AnimationProcessMode animationProcessMode)
	{
		Height = image.Height;
		Width = image.Width;
		if (FormatUtilities.IsAnimated(image) && animationProcessMode == AnimationProcessMode.All)
		{
			IsAnimated = true;
			FrameCount = image.GetFrameCount(FrameDimension.Time);
			LoopCount = (image.PropertyIdList.Contains(20737) ? BitConverter.ToInt16(image.GetPropertyItem(20737).Value, 0) : 0);
		}
		else
		{
			FrameCount = 1;
		}
	}

	public GifFrame GetFrame(Image image, int index)
	{
		image.SelectActiveFrame(FrameDimension.Time, index);
		Bitmap bitmap = new Bitmap(image);
		bitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);
		image.SelectActiveFrame(FrameDimension.Time, 0);
		byte[] array = (image.PropertyIdList.Contains(20736) ? image.GetPropertyItem(20736).Value : new byte[4]);
		TimeSpan delay = TimeSpan.FromMilliseconds(BitConverter.ToInt32(array, 4 * index % array.Length) * 10);
		return new GifFrame
		{
			Delay = delay,
			Image = bitmap
		};
	}
}
