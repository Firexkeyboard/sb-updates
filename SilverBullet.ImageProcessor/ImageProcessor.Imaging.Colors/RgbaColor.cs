using System;
using System.Drawing;

namespace ImageProcessor.Imaging.Colors;

public readonly struct RgbaColor : IEquatable<RgbaColor>
{
	public static readonly RgbaColor Empty;

	public byte R { get; }

	public byte G { get; }

	public byte B { get; }

	public byte A { get; }

	private RgbaColor(byte red, byte green, byte blue, byte alpha)
	{
		R = red;
		G = green;
		B = blue;
		A = alpha;
	}

	private RgbaColor(Color color)
	{
		R = color.R;
		G = color.G;
		B = color.B;
		A = color.A;
	}

	public static RgbaColor FromRgba(byte red, byte green, byte blue)
	{
		return new RgbaColor(red, green, blue, byte.MaxValue);
	}

	public static RgbaColor FromRgba(byte red, byte green, byte blue, byte alpha)
	{
		return new RgbaColor(red, green, blue, alpha);
	}

	public static RgbaColor FromColor(Color color)
	{
		return new RgbaColor(color);
	}

	public static implicit operator RgbaColor(Color color)
	{
		return FromColor(color);
	}

	public static implicit operator RgbaColor(HslaColor hslaColor)
	{
		return FromColor(hslaColor);
	}

	public static implicit operator RgbaColor(YCbCrColor ycbcrColor)
	{
		return FromColor(ycbcrColor);
	}

	public static implicit operator Color(RgbaColor rgbaColor)
	{
		return Color.FromArgb(rgbaColor.A, rgbaColor.R, rgbaColor.G, rgbaColor.B);
	}

	public static implicit operator YCbCrColor(RgbaColor rgbaColor)
	{
		return YCbCrColor.FromColor(rgbaColor);
	}

	public override string ToString()
	{
		if (R == 0 && G == 0 && B == 0 && A == 0)
		{
			return "RGBA [ Empty ]";
		}
		return $"RGBA [R={R}, G={G}, B={B}, A={A}]";
	}

	public override bool Equals(object obj)
	{
		if (obj is RgbaColor other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(RgbaColor other)
	{
		if (R == other.R && G == other.G && B == other.B)
		{
			return A == other.A;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (R, G, B, A).GetHashCode();
	}
}
