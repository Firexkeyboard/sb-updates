using System;

namespace ImageProcessor.Imaging;

public class GaussianLayer : IEquatable<GaussianLayer>
{
	private int size;

	private double sigma;

	private int threshold;

	public int Size
	{
		get
		{
			return size;
		}
		set
		{
			if (value < 0)
			{
				value = 0;
			}
			size = value;
		}
	}

	public double Sigma
	{
		get
		{
			return sigma;
		}
		set
		{
			if (value < 0.0)
			{
				value = 0.0;
			}
			sigma = value;
		}
	}

	public int Threshold
	{
		get
		{
			return threshold;
		}
		set
		{
			if (value < 0)
			{
				value = 0;
			}
			threshold = value;
		}
	}

	public GaussianLayer()
	{
		Size = 3;
		Sigma = 1.4;
		Threshold = 0;
	}

	public GaussianLayer(int size, double sigma = 1.4, int threshold = 0)
	{
		Size = size;
		Sigma = sigma;
		Threshold = threshold;
	}

	public override bool Equals(object obj)
	{
		if (obj is GaussianLayer other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(GaussianLayer other)
	{
		if (other != null && Size == other.Size && Sigma == other.Sigma)
		{
			return Threshold == other.Threshold;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (Size, Sigma, Threshold).GetHashCode();
	}
}
