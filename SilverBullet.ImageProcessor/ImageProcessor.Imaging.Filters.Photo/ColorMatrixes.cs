using System.Drawing.Imaging;

namespace ImageProcessor.Imaging.Filters.Photo;

internal static class ColorMatrixes
{
	private static ColorMatrix blackWhite;

	private static ColorMatrix comicHigh;

	private static ColorMatrix comicLow;

	private static ColorMatrix greyScale;

	private static ColorMatrix hiSatch;

	private static ColorMatrix invert;

	private static ColorMatrix lomograph;

	private static ColorMatrix loSatch;

	private static ColorMatrix polaroid;

	private static ColorMatrix sepia;

	internal static ColorMatrix BlackWhite => blackWhite ?? (blackWhite = new ColorMatrix(new float[5][]
	{
		new float[5] { 1.5f, 1.5f, 1.5f, 0f, 0f },
		new float[5] { 1.5f, 1.5f, 1.5f, 0f, 0f },
		new float[5] { 1.5f, 1.5f, 1.5f, 0f, 0f },
		new float[5] { 0f, 0f, 0f, 1f, 0f },
		new float[5] { -1f, -1f, -1f, 0f, 1f }
	}));

	internal static ColorMatrix ComicHigh => comicHigh ?? (comicHigh = new ColorMatrix(new float[5][]
	{
		new float[5] { 2f, -0.5f, -0.5f, 0f, 0f },
		new float[5] { -0.5f, 2f, -0.5f, 0f, 0f },
		new float[5] { -0.5f, -0.5f, 2f, 0f, 0f },
		new float[5] { 0f, 0f, 0f, 1f, 0f },
		new float[5] { 0f, 0f, 0f, 0f, 1f }
	}));

	internal static ColorMatrix ComicLow => comicLow ?? (comicLow = new ColorMatrix(new float[5][]
	{
		new float[5] { 1f, 0f, 0f, 0f, 0f },
		new float[5] { 0f, 1f, 0f, 0f, 0f },
		new float[5] { 0f, 0f, 1f, 0f, 0f },
		new float[5] { 0f, 0f, 0f, 1f, 0f },
		new float[5] { 0.075f, 0.075f, 0.075f, 0f, 1f }
	}));

	internal static ColorMatrix GreyScale => greyScale ?? (greyScale = new ColorMatrix(new float[5][]
	{
		new float[5] { 0.33f, 0.33f, 0.33f, 0f, 0f },
		new float[5] { 0.59f, 0.59f, 0.59f, 0f, 0f },
		new float[5] { 0.11f, 0.11f, 0.11f, 0f, 0f },
		new float[5] { 0f, 0f, 0f, 1f, 0f },
		new float[5] { 0f, 0f, 0f, 0f, 1f }
	}));

	internal static ColorMatrix HiSatch => hiSatch ?? (hiSatch = new ColorMatrix(new float[5][]
	{
		new float[5] { 3f, -1f, -1f, 0f, 0f },
		new float[5] { -1f, 3f, -1f, 0f, 0f },
		new float[5] { -1f, -1f, 3f, 0f, 0f },
		new float[5] { 0f, 0f, 0f, 1f, 0f },
		new float[5] { 0f, 0f, 0f, 0f, 1f }
	}));

	internal static ColorMatrix Invert => invert ?? (invert = new ColorMatrix(new float[5][]
	{
		new float[5] { -1f, 0f, 0f, 0f, 0f },
		new float[5] { 0f, -1f, 0f, 0f, 0f },
		new float[5] { 0f, 0f, -1f, 0f, 0f },
		new float[5] { 0f, 0f, 0f, 1f, 0f },
		new float[5] { 1f, 1f, 1f, 0f, 1f }
	}));

	internal static ColorMatrix Lomograph => lomograph ?? (lomograph = new ColorMatrix(new float[5][]
	{
		new float[5] { 1.5f, 0f, 0f, 0f, 0f },
		new float[5] { 0f, 1.45f, 0f, 0f, 0f },
		new float[5] { 0f, 0f, 1.09f, 0f, 0f },
		new float[5] { 0f, 0f, 0f, 1f, 0f },
		new float[5] { -0.1f, 0.05f, -0.08f, 0f, 1f }
	}));

	internal static ColorMatrix LoSatch => loSatch ?? (loSatch = new ColorMatrix(new float[5][]
	{
		new float[5] { 1f, 0f, 0f, 0f, 0f },
		new float[5] { 0f, 1f, 0f, 0f, 0f },
		new float[5] { 0f, 0f, 1f, 0f, 0f },
		new float[5] { 0f, 0f, 0f, 1f, 0f },
		new float[5] { 0.1f, 0.1f, 0.1f, 0f, 1f }
	}));

	internal static ColorMatrix Polaroid => polaroid ?? (polaroid = new ColorMatrix(new float[5][]
	{
		new float[5] { 1.638f, -0.062f, -0.262f, 0f, 0f },
		new float[5] { -0.122f, 1.378f, -0.122f, 0f, 0f },
		new float[5] { 1.016f, -0.016f, 1.383f, 0f, 0f },
		new float[5] { 0f, 0f, 0f, 1f, 0f },
		new float[5] { 0.06f, -0.05f, -0.05f, 0f, 1f }
	}));

	internal static ColorMatrix Sepia => sepia ?? (sepia = new ColorMatrix(new float[5][]
	{
		new float[5] { 0.393f, 0.349f, 0.272f, 0f, 0f },
		new float[5] { 0.769f, 0.686f, 0.534f, 0f, 0f },
		new float[5] { 0.189f, 0.168f, 0.131f, 0f, 0f },
		new float[5] { 0f, 0f, 0f, 1f, 0f },
		new float[5] { 0f, 0f, 0f, 0f, 1f }
	}));
}
