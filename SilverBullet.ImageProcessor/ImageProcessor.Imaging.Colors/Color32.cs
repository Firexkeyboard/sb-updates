using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ImageProcessor.Imaging.Colors;

[StructLayout(LayoutKind.Explicit)]
public struct Color32 : IEquatable<Color32>
{
	[FieldOffset(0)]
	public byte B;

	[FieldOffset(1)]
	public byte G;

	[FieldOffset(2)]
	public byte R;

	[FieldOffset(3)]
	public byte A;

	[FieldOffset(0)]
	public int Argb;

	public Color Color => Color.FromArgb(A, R, G, B);

	public Color32(byte alpha, byte red, byte green, byte blue)
	{
		this = default(Color32);
		A = alpha;
		R = red;
		G = green;
		B = blue;
	}

	public Color32(int argb)
	{
		this = default(Color32);
		Argb = argb;
	}

	public override bool Equals(object obj)
	{
		if (obj is Color32 other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(Color32 other)
	{
		return Argb == other.Argb;
	}

	public override int GetHashCode()
	{
		return Argb.GetHashCode();
	}
}
