using System;
using System.Drawing;
using ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Imaging.Colors;

public readonly struct YCbCrColor : IEquatable<YCbCrColor>
{
	public static readonly YCbCrColor Empty;

	public float Y { get; }

	public float Cb { get; }

	public float Cr { get; }

	private YCbCrColor(float y, float cb, float cr)
	{
		Y = ImageMaths.Clamp(y, 0f, 255f);
		Cb = ImageMaths.Clamp(cb, 0f, 255f);
		Cr = ImageMaths.Clamp(cr, 0f, 255f);
	}

	public static YCbCrColor FromYCbCr(float y, float cb, float cr)
	{
		return new YCbCrColor(y, cb, cr);
	}

	public static YCbCrColor FromColor(Color color)
	{
		byte r = color.R;
		byte g = color.G;
		byte b = color.B;
		float y = (float)(0.299 * (double)(int)r + 0.587 * (double)(int)g + 0.114 * (double)(int)b);
		float cb = 128f + (float)(-0.168736 * (double)(int)r - 0.331264 * (double)(int)g + 0.5 * (double)(int)b);
		float cr = 128f + (float)(0.5 * (double)(int)r - 0.418688 * (double)(int)g - 0.081312 * (double)(int)b);
		return new YCbCrColor(y, cb, cr);
	}

	public static implicit operator YCbCrColor(Color color)
	{
		return FromColor(color);
	}

	public static implicit operator YCbCrColor(RgbaColor rgbaColor)
	{
		return FromColor(rgbaColor);
	}

	public static implicit operator YCbCrColor(HslaColor hslaColor)
	{
		return FromColor(hslaColor);
	}

	public static implicit operator Color(YCbCrColor ycbcrColor)
	{
		float y = ycbcrColor.Y;
		float num = ycbcrColor.Cb - 128f;
		float num2 = ycbcrColor.Cr - 128f;
		byte red = Convert.ToByte(ImageMaths.Clamp((double)y + 1.402 * (double)num2, 0.0, 255.0));
		byte green = Convert.ToByte(ImageMaths.Clamp((double)y - 0.34414 * (double)num - 0.71414 * (double)num2, 0.0, 255.0));
		byte blue = Convert.ToByte(ImageMaths.Clamp((double)y + 1.772 * (double)num, 0.0, 255.0));
		return Color.FromArgb(255, red, green, blue);
	}

	public override string ToString()
	{
		if (IsEmpty())
		{
			return "YCbCrColor [ Empty ]";
		}
		return $"YCbCrColor [ Y={Y:#0.##}, Cb={Cb:#0.##}, Cr={Cr:#0.##}]";
	}

	public override bool Equals(object obj)
	{
		if (obj is YCbCrColor other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(YCbCrColor other)
	{
		if (Y == other.Y && Cb == other.Cb)
		{
			return Cr == other.Cr;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (Y, Cb, Cr).GetHashCode();
	}

	private bool IsEmpty()
	{
		if (Math.Abs(Y - 0f) <= 0.0001f && Math.Abs(Cb - 0f) <= 0.0001f)
		{
			return Math.Abs(Cr - 0f) <= 0.0001f;
		}
		return false;
	}
}
