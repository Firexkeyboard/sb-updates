using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessor.Imaging.Colors;

namespace ImageProcessor.Imaging.Quantizers.WuQuantizer;

public class WuQuantizer : WuQuantizerBase
{
	internal override Bitmap GetQuantizedImage(ImageBuffer imageBuffer, int colorCount, Color32[] lookups, int alphaThreshold)
	{
		Bitmap bitmap = new Bitmap(imageBuffer.Image.Width, imageBuffer.Image.Height, PixelFormat.Format8bppIndexed);
		bitmap.SetResolution(imageBuffer.Image.HorizontalResolution, imageBuffer.Image.VerticalResolution);
		ImageBuffer imageBuffer2 = new ImageBuffer(bitmap);
		PaletteColorHistory[] array = new PaletteColorHistory[colorCount + 1];
		imageBuffer2.UpdatePixelIndexes(IndexedPixels(imageBuffer, lookups, alphaThreshold, array));
		bitmap.Palette = BuildPalette(bitmap.Palette, array);
		return bitmap;
	}

	private static ColorPalette BuildPalette(ColorPalette palette, PaletteColorHistory[] paletteHistory)
	{
		int num = paletteHistory.Length;
		for (int i = 0; i < num; i++)
		{
			palette.Entries[i] = paletteHistory[i].ToNormalizedColor();
		}
		return palette;
	}

	private static IEnumerable<byte[]> IndexedPixels(ImageBuffer image, Color32[] lookups, int alphaThreshold, PaletteColorHistory[] paletteHistogram)
	{
		byte[] lineIndexes = new byte[image.Image.Width];
		PaletteLookup lookup = new PaletteLookup(lookups);
		byte fallback = (byte)((lookups.Length >= 255) ? byte.MaxValue : 0);
		foreach (Color32[] pixelLine in image.PixelLines)
		{
			int num = pixelLine.Length;
			for (int i = 0; i < num; i++)
			{
				Color32 pixel = pixelLine[i];
				byte b = fallback;
				if (pixel.A >= alphaThreshold)
				{
					b = lookup.GetPaletteIndex(pixel);
					paletteHistogram[b].AddPixel(pixel);
				}
				lineIndexes[i] = b;
			}
			yield return lineIndexes;
		}
	}
}
