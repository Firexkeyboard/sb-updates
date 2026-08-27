using System;
using System.Drawing;
using ImageProcessor.Common.Extensions;
using ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Imaging.Colors;

public readonly struct CmykColor : IEquatable<CmykColor>
{
	public static readonly CmykColor Empty;

	public float C { get; }

	public float M { get; }

	public float Y { get; }

	public float K { get; }

	private CmykColor(float cyan, float magenta, float yellow, float keyline)
	{
		C = Clamp(cyan);
		M = Clamp(magenta);
		Y = Clamp(yellow);
		K = Clamp(keyline);
	}

	private CmykColor(Color color)
	{
		CmykColor cmykColor = color;
		C = cmykColor.C;
		M = cmykColor.M;
		Y = cmykColor.Y;
		K = cmykColor.K;
	}

	public static CmykColor FromCmykColor(float cyan, float magenta, float yellow, float keyline)
	{
		return new CmykColor(cyan, magenta, yellow, keyline);
	}

	public static CmykColor FromColor(Color color)
	{
		return new CmykColor(color);
	}

	public static implicit operator CmykColor(Color color)
	{
		float num = (255f - (float)(int)color.R) / 255f;
		float num2 = (255f - (float)(int)color.G) / 255f;
		float num3 = (255f - (float)(int)color.B) / 255f;
		float num4 = Math.Min(num, Math.Min(num2, num3));
		if (Math.Abs((double)num4 - 1.0) <= 9.999999747378752E-05)
		{
			return new CmykColor(0f, 0f, 0f, 100f);
		}
		num = (num - num4) / (1f - num4) * 100f;
		num2 = (num2 - num4) / (1f - num4) * 100f;
		num3 = (num3 - num4) / (1f - num4) * 100f;
		return new CmykColor(num, num2, num3, num4 * 100f);
	}

	public static implicit operator CmykColor(RgbaColor rgbaColor)
	{
		return FromColor(rgbaColor);
	}

	public static implicit operator CmykColor(YCbCrColor ycbcrColor)
	{
		return FromColor(ycbcrColor);
	}

	public static implicit operator Color(CmykColor cmykColor)
	{
		int value = Convert.ToInt32((double)((1f - cmykColor.C / 100f) * (1f - cmykColor.K / 100f)) * 255.0);
		int value2 = Convert.ToInt32((double)((1f - cmykColor.M / 100f) * (1f - cmykColor.K / 100f)) * 255.0);
		return Color.FromArgb(blue: Convert.ToInt32((double)((1f - cmykColor.Y / 100f) * (1f - cmykColor.K / 100f)) * 255.0).ToByte(), red: value.ToByte(), green: value2.ToByte());
	}

	public static implicit operator RgbaColor(CmykColor cmykColor)
	{
		int value = Convert.ToInt32((double)((1f - cmykColor.C / 100f) * (1f - cmykColor.K / 100f)) * 255.0);
		int value2 = Convert.ToInt32((double)((1f - cmykColor.M / 100f) * (1f - cmykColor.K / 100f)) * 255.0);
		return RgbaColor.FromRgba(blue: Convert.ToInt32((double)((1f - cmykColor.Y / 100f) * (1f - cmykColor.K / 100f)) * 255.0).ToByte(), red: value.ToByte(), green: value2.ToByte());
	}

	public static implicit operator YCbCrColor(CmykColor cmykColor)
	{
		return YCbCrColor.FromColor(cmykColor);
	}

	public static implicit operator HslaColor(CmykColor cmykColor)
	{
		return HslaColor.FromColor(cmykColor);
	}

	public override string ToString()
	{
		if (IsEmpty())
		{
			return "CmykColor [ Empty ]";
		}
		return $"CmykColor [ C={C:#0.##}, M={M:#0.##}, Y={Y:#0.##}, K={K:#0.##}]";
	}

	public override bool Equals(object obj)
	{
		if (obj is CmykColor other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(CmykColor other)
	{
		if (C == other.C && M == other.M && Y == other.Y)
		{
			return K == other.K;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (C, M, Y, K).GetHashCode();
	}

	private static float Clamp(float value)
	{
		return ImageMaths.Clamp(value, 0f, 100f);
	}

	private bool IsEmpty()
	{
		if (Math.Abs(C - 0f) <= 0.0001f && Math.Abs(M - 0f) <= 0.0001f && Math.Abs(Y - 0f) <= 0.0001f)
		{
			return Math.Abs(K - 0f) <= 0.0001f;
		}
		return false;
	}
}
