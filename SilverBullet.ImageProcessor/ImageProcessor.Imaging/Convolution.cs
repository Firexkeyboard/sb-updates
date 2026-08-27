using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using ImageProcessor.Common.Extensions;
using ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Imaging;

public class Convolution
{
	private readonly double standardDeviation = 1.4;

	public int Threshold { get; set; }

	public double Divider { get; set; }

	public bool UseDynamicDividerForEdges { get; set; } = true;

	public Convolution()
	{
	}

	public Convolution(double standardDeviation)
	{
		this.standardDeviation = standardDeviation;
	}

	public double[,] CreateGaussianKernel(int kernelSize)
	{
		double[,] array = new double[kernelSize, 1];
		double num = 0.0;
		int num2 = kernelSize / 2;
		for (int i = 0; i < kernelSize; i++)
		{
			int num3 = i - num2;
			double num4 = Gaussian(num3);
			num += num4;
			array[i, 0] = num4;
		}
		for (int j = 0; j < kernelSize; j++)
		{
			array[j, 0] /= num;
		}
		return array;
	}

	public double[,] CreateGaussianKernel2D(int kernelSize)
	{
		double[,] array = new double[kernelSize, kernelSize];
		int num = kernelSize / 2;
		for (int i = 0; i < kernelSize; i++)
		{
			int num2 = i - num;
			for (int j = 0; j < kernelSize; j++)
			{
				int num3 = j - num;
				double num4 = Gaussian2D(num2, num3);
				array[i, j] = num4;
			}
		}
		return array;
	}

	public double[,] CreateGuassianBlurFilter(int kernelSize)
	{
		double[,] array = CreateGaussianKernel2D(kernelSize);
		double num = array[0, 0];
		double[,] array2 = new double[kernelSize, kernelSize];
		int num2 = 0;
		for (int i = 0; i < kernelSize; i++)
		{
			for (int j = 0; j < kernelSize; j++)
			{
				double num3 = array[i, j] / num;
				if (num3 > 65535.0)
				{
					num3 = 65535.0;
				}
				array2[i, j] = (int)num3;
				num2 += (int)array2[i, j];
			}
		}
		Divider = num2;
		return array2;
	}

	public double[,] CreateGuassianSharpenFilter(int kernelSize)
	{
		double[,] array = CreateGaussianKernel2D(kernelSize);
		double num = array[0, 0];
		double[,] array2 = new double[kernelSize, kernelSize];
		int num2 = 0;
		int num3 = 0;
		for (int i = 0; i < kernelSize; i++)
		{
			for (int j = 0; j < kernelSize; j++)
			{
				double num4 = array[i, j] / num;
				if (num4 > 65535.0)
				{
					num4 = 65535.0;
				}
				array2[i, j] = (int)num4;
				num2 += (int)array2[i, j];
			}
		}
		int num5 = kernelSize >> 1;
		for (int k = 0; k < kernelSize; k++)
		{
			for (int l = 0; l < kernelSize; l++)
			{
				if (k == num5 && l == num5)
				{
					array2[k, l] = (double)(2 * num2) - array2[k, l];
				}
				else
				{
					array2[k, l] = 0.0 - array2[k, l];
				}
				num3 += (int)array2[k, l];
			}
		}
		Divider = num3;
		return array2;
	}

	public Bitmap ProcessKernel(Bitmap source, double[,] kernel, bool fixGamma)
	{
		int width = source.Width;
		int height = source.Height;
		Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
		bitmap.SetResolution(source.HorizontalResolution, source.VerticalResolution);
		FastBitmap sourceBitmap = new FastBitmap(source);
		try
		{
			FastBitmap destinationBitmap = new FastBitmap(bitmap);
			try
			{
				int kernelLength = kernel.GetLength(0);
				int radius = kernelLength >> 1;
				int kernelSize = kernelLength * kernelLength;
				int threshold = Threshold;
				Parallel.For(0, height, delegate(int y)
				{
					for (int i = 0; i < width; i++)
					{
						double num2;
						int num;
						double num5;
						double num4;
						double num3;
						double num6 = (num5 = (num4 = (num3 = (num2 = (num = 0)))));
						for (int j = 0; j < kernelLength; j++)
						{
							int num7 = j - radius;
							int num8 = y + num7;
							if (num8 >= 0)
							{
								if (num8 >= height)
								{
									break;
								}
								for (int k = 0; k < kernelLength; k++)
								{
									int num9 = k - radius;
									int num10 = i + num9;
									if (num10 >= 0 && num10 < width)
									{
										Color composite = sourceBitmap.GetPixel(num10, num8);
										if (fixGamma)
										{
											composite = PixelOperations.ToLinear(composite);
										}
										double num11 = kernel[j, k];
										num2 += num11;
										num6 += num11 * (double)(int)composite.R;
										num5 += num11 * (double)(int)composite.G;
										num4 += num11 * (double)(int)composite.B;
										num3 += num11 * (double)(int)composite.A;
										num++;
									}
								}
							}
						}
						if (num == kernelSize)
						{
							num2 = Divider;
						}
						else if (!UseDynamicDividerForEdges)
						{
							num2 = Divider;
						}
						if ((long)num2 != 0L)
						{
							num6 /= num2;
							num5 /= num2;
							num4 /= num2;
							num3 /= num2;
						}
						num6 += (double)threshold;
						num5 += (double)threshold;
						num4 += (double)threshold;
						num3 += (double)threshold;
						Color color = Color.FromArgb(num3.ToByte(), num6.ToByte(), num5.ToByte(), num4.ToByte());
						if (fixGamma)
						{
							color = PixelOperations.ToSRGB(color);
						}
						destinationBitmap.SetPixel(i, y, color);
					}
				});
			}
			finally
			{
				if (destinationBitmap != null)
				{
					((IDisposable)destinationBitmap).Dispose();
				}
			}
		}
		finally
		{
			if (sourceBitmap != null)
			{
				((IDisposable)sourceBitmap).Dispose();
			}
		}
		source.Dispose();
		return bitmap;
	}

	public Color[,] ProcessKernel(Color[,] source, double[,] kernel, bool fixGamma)
	{
		int width = source.GetLength(0);
		int height = source.GetLength(1);
		int kernelLength = kernel.GetLength(0);
		int radius = kernelLength >> 1;
		int kernelSize = kernelLength * kernelLength;
		int threshold = Threshold;
		Color[,] destination = new Color[width, height];
		Parallel.For(0, height, delegate(int y)
		{
			for (int i = 0; i < width; i++)
			{
				double num2;
				int num;
				double num5;
				double num4;
				double num3;
				double num6 = (num5 = (num4 = (num3 = (num2 = (num = 0)))));
				for (int j = 0; j < kernelLength; j++)
				{
					int num7 = j - radius;
					int num8 = y + num7;
					if (num8 >= 0)
					{
						if (num8 >= height)
						{
							break;
						}
						for (int k = 0; k < kernelLength; k++)
						{
							int num9 = k - radius;
							int num10 = i + num9;
							if (num10 >= 0 && num10 < width)
							{
								Color composite = source[num10, num8];
								if (fixGamma)
								{
									composite = PixelOperations.ToLinear(composite);
								}
								double num11 = kernel[j, k];
								num2 += num11;
								num6 += num11 * (double)(int)composite.R;
								num5 += num11 * (double)(int)composite.G;
								num4 += num11 * (double)(int)composite.B;
								num3 += num11 * (double)(int)composite.A;
								num++;
							}
						}
					}
				}
				if (num == kernelSize)
				{
					num2 = Divider;
				}
				else if (!UseDynamicDividerForEdges)
				{
					num2 = Divider;
				}
				if ((long)num2 != 0L)
				{
					num6 /= num2;
					num5 /= num2;
					num4 /= num2;
					num3 /= num2;
				}
				num6 += (double)threshold;
				num5 += (double)threshold;
				num4 += (double)threshold;
				num3 += (double)threshold;
				Color color = Color.FromArgb(num3.ToByte(), num6.ToByte(), num5.ToByte(), num4.ToByte());
				if (fixGamma)
				{
					color = PixelOperations.ToSRGB(color);
				}
				destination[i, y] = color;
			}
		});
		return destination;
	}

	private double Gaussian(double x)
	{
		double num = Math.Sqrt(Math.PI * 2.0) * standardDeviation;
		double num2 = (0.0 - x) * x;
		double num3 = 2.0 * Math.Pow(standardDeviation, 2.0);
		double num4 = 1.0 / num;
		double num5 = Math.Exp(num2 / num3);
		return num4 * num5;
	}

	private double Gaussian2D(double x, double y)
	{
		double num = Math.PI * 2.0 * Math.Pow(standardDeviation, 2.0);
		double num2 = (0.0 - x) * x + (0.0 - y) * y;
		double num3 = 2.0 * Math.Pow(standardDeviation, 2.0);
		double num4 = 1.0 / num;
		double num5 = Math.Exp(num2 / num3);
		return num4 * num5;
	}
}
