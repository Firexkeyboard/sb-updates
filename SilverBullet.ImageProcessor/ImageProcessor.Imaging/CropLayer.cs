using System;

namespace ImageProcessor.Imaging;

public class CropLayer : IEquatable<CropLayer>
{
	public float Left { get; set; }

	public float Top { get; set; }

	public float Right { get; set; }

	public float Bottom { get; set; }

	public CropMode CropMode { get; set; }

	public CropLayer(float left, float top, float right, float bottom, CropMode cropMode = CropMode.Percentage)
	{
		if (left < 0f)
		{
			throw new ArgumentOutOfRangeException("left");
		}
		if (top < 0f)
		{
			throw new ArgumentOutOfRangeException("top");
		}
		if (right < 0f)
		{
			throw new ArgumentOutOfRangeException("right");
		}
		if (bottom < 0f)
		{
			throw new ArgumentOutOfRangeException("bottom");
		}
		Left = left;
		Top = top;
		Right = right;
		Bottom = bottom;
		CropMode = cropMode;
	}

	public override bool Equals(object obj)
	{
		if (obj is CropLayer other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(CropLayer other)
	{
		if (other != null && Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom)
		{
			return CropMode == other.CropMode;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (Left, Top, Right, Bottom, CropMode).GetHashCode();
	}
}
