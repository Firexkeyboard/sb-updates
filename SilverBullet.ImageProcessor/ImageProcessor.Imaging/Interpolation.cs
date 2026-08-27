using System;

namespace ImageProcessor.Imaging;

internal static class Interpolation
{
	public static double BiCubicKernel(double x)
	{
		if (x < 0.0)
		{
			x = 0.0 - x;
		}
		if (x <= 1.0)
		{
			return (1.5 * x - 2.5) * x * x + 1.0;
		}
		if (x < 2.0)
		{
			return ((-0.5 * x + 2.5) * x - 4.0) * x + 2.0;
		}
		return 0.0;
	}

	public static double BiCubicBSplineKernel(double x)
	{
		double num = 0.0;
		double num2 = x + 2.0;
		double num3 = x + 1.0;
		double num4 = x - 1.0;
		if (num2 > 0.0)
		{
			num += num2 * num2 * num2;
		}
		if (num3 > 0.0)
		{
			num -= 4.0 * num3 * num3 * num3;
		}
		if (x > 0.0)
		{
			num += 6.0 * x * x * x;
		}
		if (num4 > 0.0)
		{
			num -= 4.0 * num4 * num4 * num4;
		}
		return num / 6.0;
	}

	internal static double LanczosKernel3(double x)
	{
		if (x < 0.0)
		{
			x = 0.0 - x;
		}
		if (x < 3.0)
		{
			return SinC(x) * SinC(x / 3.0);
		}
		return 0.0;
	}

	private static double SinC(double x)
	{
		if (Math.Abs(x) > 0.0001)
		{
			x *= Math.PI;
			return Clean(Math.Sin(x) / x);
		}
		return 1.0;
	}

	private static double Clean(double x)
	{
		if (Math.Abs(x) < 0.0001)
		{
			return 0.0;
		}
		return x;
	}
}
