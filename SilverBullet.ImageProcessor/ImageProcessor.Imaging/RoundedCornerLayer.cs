using System;

namespace ImageProcessor.Imaging;

public class RoundedCornerLayer : IEquatable<RoundedCornerLayer>
{
	public int Radius { get; set; }

	public bool TopLeft { get; set; }

	public bool TopRight { get; set; }

	public bool BottomLeft { get; set; }

	public bool BottomRight { get; set; }

	public RoundedCornerLayer(int radius, bool topLeft = true, bool topRight = true, bool bottomLeft = true, bool bottomRight = true)
	{
		Radius = radius;
		TopLeft = topLeft;
		TopRight = topRight;
		BottomLeft = bottomLeft;
		BottomRight = bottomRight;
	}

	public override bool Equals(object obj)
	{
		if (obj is RoundedCornerLayer other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(RoundedCornerLayer other)
	{
		if (other != null && Radius == other.Radius && TopLeft == other.TopLeft && TopRight == other.TopRight && BottomLeft == other.BottomLeft)
		{
			return BottomRight == other.BottomRight;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (Radius, TopLeft, TopRight, BottomLeft, BottomRight).GetHashCode();
	}
}
