using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging.Colors;

namespace ImageProcessor.Imaging.Quantizers.WuQuantizer;

public abstract class WuQuantizerBase : IWuQuantizer, IQuantizer
{
	protected const byte AlphaMax = byte.MaxValue;

	protected const byte AlphaMin = 0;

	protected const int Alpha = 3;

	protected const int Red = 2;

	protected const int Green = 1;

	protected const int Blue = 0;

	private const int SideSize = 33;

	private const int MaxSideIndex = 32;

	public byte Threshold { get; set; }

	public byte Fade { get; set; } = 1;

	public Bitmap Quantize(Image source)
	{
		return Quantize(source, Threshold, Fade);
	}

	public Bitmap Quantize(Image image, int alphaThreshold, int alphaFader)
	{
		return Quantize(image, alphaThreshold, alphaFader, null, 256);
	}

	public Bitmap Quantize(Image source, int alphaThreshold, int alphaFader, Histogram histogram, int maxColors)
	{
		try
		{
			ImageBuffer imageBuffer;
			if (Image.GetPixelFormatSize(source.PixelFormat) != 32)
			{
				Bitmap bitmap = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppPArgb);
				bitmap.SetResolution(source.HorizontalResolution, source.VerticalResolution);
				using (Graphics graphics = Graphics.FromImage(bitmap))
				{
					graphics.PageUnit = GraphicsUnit.Pixel;
					graphics.Clear(Color.Transparent);
					graphics.DrawImageUnscaled(source, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
				}
				source.Dispose();
				imageBuffer = new ImageBuffer(bitmap);
			}
			else
			{
				imageBuffer = new ImageBuffer((Bitmap)source);
			}
			if (histogram == null)
			{
				histogram = new Histogram();
			}
			else
			{
				histogram.Clear();
			}
			BuildHistogram(histogram, imageBuffer, alphaThreshold, alphaFader);
			CalculateMoments(histogram.Moments);
			Color32[] lookups = BuildLookups(SplitData(ref maxColors, histogram.Moments), histogram.Moments);
			return GetQuantizedImage(imageBuffer, maxColors, lookups, alphaThreshold);
		}
		catch (Exception ex)
		{
			throw new QuantizationException(ex.Message, ex);
		}
	}

	internal abstract Bitmap GetQuantizedImage(ImageBuffer imageBuffer, int colorCount, Color32[] lookups, int alphaThreshold);

	private static void BuildHistogram(Histogram histogram, ImageBuffer imageBuffer, int alphaThreshold, int alphaFader)
	{
		ColorMoment[,,,] moments = histogram.Moments;
		foreach (Color32[] pixelLine in imageBuffer.PixelLines)
		{
			for (int i = 0; i < pixelLine.Length; i++)
			{
				Color32 pixel = pixelLine[i];
				byte b = pixel.A;
				if (b > alphaThreshold)
				{
					if (b < byte.MaxValue)
					{
						int num = pixel.A + pixel.A % alphaFader;
						b = (byte)((num > 255) ? 255u : ((uint)num));
					}
					byte r = pixel.R;
					byte g = pixel.G;
					byte b2 = pixel.B;
					b = (byte)((b >> 3) + 1);
					r = (byte)((r >> 3) + 1);
					g = (byte)((g >> 3) + 1);
					b2 = (byte)((b2 >> 3) + 1);
					moments[b, r, g, b2].Add(pixel);
				}
			}
		}
		moments[0, 0, 0, 0].Add(new Color32(0, 0, 0, 0));
	}

	private static void CalculateMoments(ColorMoment[,,,] moments)
	{
		ColorMoment[,] array = new ColorMoment[33, 33];
		ColorMoment[] array2 = new ColorMoment[33];
		for (int i = 1; i < 33; i++)
		{
			for (int j = 1; j < 33; j++)
			{
				Array.Clear(array2, 0, array2.Length);
				for (int k = 1; k < 33; k++)
				{
					ColorMoment moment = default(ColorMoment);
					for (int l = 1; l < 33; l++)
					{
						moment.AddFast(ref moments[i, j, k, l]);
						array2[l].AddFast(ref moment);
						array[k, l].AddFast(ref array2[l]);
						ColorMoment colorMoment = moments[i - 1, j, k, l];
						colorMoment.AddFast(ref array[k, l]);
						moments[i, j, k, l] = colorMoment;
					}
				}
			}
		}
	}

	private static ColorMoment Top(Box cube, int direction, int position, ColorMoment[,,,] moment)
	{
		return direction switch
		{
			3 => moment[position, cube.RedMaximum, cube.GreenMaximum, cube.BlueMaximum] - moment[position, cube.RedMaximum, cube.GreenMinimum, cube.BlueMaximum] - moment[position, cube.RedMinimum, cube.GreenMaximum, cube.BlueMaximum] + moment[position, cube.RedMinimum, cube.GreenMinimum, cube.BlueMaximum] - (moment[position, cube.RedMaximum, cube.GreenMaximum, cube.BlueMinimum] - moment[position, cube.RedMaximum, cube.GreenMinimum, cube.BlueMinimum] - moment[position, cube.RedMinimum, cube.GreenMaximum, cube.BlueMinimum] + moment[position, cube.RedMinimum, cube.GreenMinimum, cube.BlueMinimum]), 
			2 => moment[cube.AlphaMaximum, position, cube.GreenMaximum, cube.BlueMaximum] - moment[cube.AlphaMaximum, position, cube.GreenMinimum, cube.BlueMaximum] - moment[cube.AlphaMinimum, position, cube.GreenMaximum, cube.BlueMaximum] + moment[cube.AlphaMinimum, position, cube.GreenMinimum, cube.BlueMaximum] - (moment[cube.AlphaMaximum, position, cube.GreenMaximum, cube.BlueMinimum] - moment[cube.AlphaMaximum, position, cube.GreenMinimum, cube.BlueMinimum] - moment[cube.AlphaMinimum, position, cube.GreenMaximum, cube.BlueMinimum] + moment[cube.AlphaMinimum, position, cube.GreenMinimum, cube.BlueMinimum]), 
			1 => moment[cube.AlphaMaximum, cube.RedMaximum, position, cube.BlueMaximum] - moment[cube.AlphaMaximum, cube.RedMinimum, position, cube.BlueMaximum] - moment[cube.AlphaMinimum, cube.RedMaximum, position, cube.BlueMaximum] + moment[cube.AlphaMinimum, cube.RedMinimum, position, cube.BlueMaximum] - (moment[cube.AlphaMaximum, cube.RedMaximum, position, cube.BlueMinimum] - moment[cube.AlphaMaximum, cube.RedMinimum, position, cube.BlueMinimum] - moment[cube.AlphaMinimum, cube.RedMaximum, position, cube.BlueMinimum] + moment[cube.AlphaMinimum, cube.RedMinimum, position, cube.BlueMinimum]), 
			0 => moment[cube.AlphaMaximum, cube.RedMaximum, cube.GreenMaximum, position] - moment[cube.AlphaMaximum, cube.RedMaximum, cube.GreenMinimum, position] - moment[cube.AlphaMaximum, cube.RedMinimum, cube.GreenMaximum, position] + moment[cube.AlphaMaximum, cube.RedMinimum, cube.GreenMinimum, position] - (moment[cube.AlphaMinimum, cube.RedMaximum, cube.GreenMaximum, position] - moment[cube.AlphaMinimum, cube.RedMaximum, cube.GreenMinimum, position] - moment[cube.AlphaMinimum, cube.RedMinimum, cube.GreenMaximum, position] + moment[cube.AlphaMinimum, cube.RedMinimum, cube.GreenMinimum, position]), 
			_ => default(ColorMoment), 
		};
	}

	private static ColorMoment Bottom(Box cube, int direction, ColorMoment[,,,] moment)
	{
		return direction switch
		{
			3 => -moment[cube.AlphaMinimum, cube.RedMaximum, cube.GreenMaximum, cube.BlueMaximum] + moment[cube.AlphaMinimum, cube.RedMaximum, cube.GreenMinimum, cube.BlueMaximum] + moment[cube.AlphaMinimum, cube.RedMinimum, cube.GreenMaximum, cube.BlueMaximum] - moment[cube.AlphaMinimum, cube.RedMinimum, cube.GreenMinimum, cube.BlueMaximum] - (-moment[cube.AlphaMinimum, cube.RedMaximum, cube.GreenMaximum, cube.BlueMinimum] + moment[cube.AlphaMinimum, cube.RedMaximum, cube.GreenMinimum, cube.BlueMinimum] + moment[cube.AlphaMinimum, cube.RedMinimum, cube.GreenMaximum, cube.BlueMinimum] - moment[cube.AlphaMinimum, cube.RedMinimum, cube.GreenMinimum, cube.BlueMinimum]), 
			2 => -moment[cube.AlphaMaximum, cube.RedMinimum, cube.GreenMaximum, cube.BlueMaximum] + moment[cube.AlphaMaximum, cube.RedMinimum, cube.GreenMinimum, cube.BlueMaximum] + moment[cube.AlphaMinimum, cube.RedMinimum, cube.GreenMaximum, cube.BlueMaximum] - moment[cube.AlphaMinimum, cube.RedMinimum, cube.GreenMinimum, cube.BlueMaximum] - (-moment[cube.AlphaMaximum, cube.RedMinimum, cube.GreenMaximum, cube.BlueMinimum] + moment[cube.AlphaMaximum, cube.RedMinimum, cube.GreenMinimum, cube.BlueMinimum] + moment[cube.AlphaMinimum, cube.RedMinimum, cube.GreenMaximum, cube.BlueMinimum] - moment[cube.AlphaMinimum, cube.RedMinimum, cube.GreenMinimum, cube.BlueMinimum]), 
			1 => -moment[cube.AlphaMaximum, cube.RedMaximum, cube.GreenMinimum, cube.BlueMaximum] + moment[cube.AlphaMaximum, cube.RedMinimum, cube.GreenMinimum, cube.BlueMaximum] + moment[cube.AlphaMinimum, cube.RedMaximum, cube.GreenMinimum, cube.BlueMaximum] - moment[cube.AlphaMinimum, cube.RedMinimum, cube.GreenMinimum, cube.BlueMaximum] - (-moment[cube.AlphaMaximum, cube.RedMaximum, cube.GreenMinimum, cube.BlueMinimum] + moment[cube.AlphaMaximum, cube.RedMinimum, cube.GreenMinimum, cube.BlueMinimum] + moment[cube.AlphaMinimum, cube.RedMaximum, cube.GreenMinimum, cube.BlueMinimum] - moment[cube.AlphaMinimum, cube.RedMinimum, cube.GreenMinimum, cube.BlueMinimum]), 
			0 => -moment[cube.AlphaMaximum, cube.RedMaximum, cube.GreenMaximum, cube.BlueMinimum] + moment[cube.AlphaMaximum, cube.RedMaximum, cube.GreenMinimum, cube.BlueMinimum] + moment[cube.AlphaMaximum, cube.RedMinimum, cube.GreenMaximum, cube.BlueMinimum] - moment[cube.AlphaMaximum, cube.RedMinimum, cube.GreenMinimum, cube.BlueMinimum] - (-moment[cube.AlphaMinimum, cube.RedMaximum, cube.GreenMaximum, cube.BlueMinimum] + moment[cube.AlphaMinimum, cube.RedMaximum, cube.GreenMinimum, cube.BlueMinimum] + moment[cube.AlphaMinimum, cube.RedMinimum, cube.GreenMaximum, cube.BlueMinimum] - moment[cube.AlphaMinimum, cube.RedMinimum, cube.GreenMinimum, cube.BlueMinimum]), 
			_ => default(ColorMoment), 
		};
	}

	private static CubeCut Maximize(ColorMoment[,,,] moments, Box cube, int direction, byte first, byte last, ColorMoment whole)
	{
		ColorMoment colorMoment = Bottom(cube, direction, moments);
		float num = 0f;
		byte? cutPoint = null;
		for (byte b = first; b < last; b++)
		{
			ColorMoment colorMoment2 = colorMoment + Top(cube, direction, b, moments);
			if (colorMoment2.Weight != 0)
			{
				long num2 = colorMoment2.WeightedDistance();
				colorMoment2 = whole - colorMoment2;
				if (colorMoment2.Weight != 0)
				{
					num2 += colorMoment2.WeightedDistance();
					if ((float)num2 > num)
					{
						num = num2;
						cutPoint = b;
					}
				}
			}
		}
		return new CubeCut(cutPoint, num);
	}

	private static bool Cut(ColorMoment[,,,] moments, ref Box first, ref Box second)
	{
		ColorMoment whole = Volume(moments, first);
		CubeCut cubeCut = Maximize(moments, first, 3, (byte)(first.AlphaMinimum + 1), first.AlphaMaximum, whole);
		CubeCut cubeCut2 = Maximize(moments, first, 2, (byte)(first.RedMinimum + 1), first.RedMaximum, whole);
		CubeCut cubeCut3 = Maximize(moments, first, 1, (byte)(first.GreenMinimum + 1), first.GreenMaximum, whole);
		CubeCut cubeCut4 = Maximize(moments, first, 0, (byte)(first.BlueMinimum + 1), first.BlueMaximum, whole);
		int num;
		if (!(cubeCut.Value >= cubeCut2.Value) || !(cubeCut.Value >= cubeCut3.Value) || !(cubeCut.Value >= cubeCut4.Value))
		{
			num = ((cubeCut2.Value >= cubeCut.Value && cubeCut2.Value >= cubeCut3.Value && cubeCut2.Value >= cubeCut4.Value) ? 2 : ((cubeCut3.Value >= cubeCut.Value && cubeCut3.Value >= cubeCut2.Value && cubeCut3.Value >= cubeCut4.Value) ? 1 : 0));
		}
		else
		{
			num = 3;
			if (!cubeCut.Position.HasValue)
			{
				return false;
			}
		}
		second.AlphaMaximum = first.AlphaMaximum;
		second.RedMaximum = first.RedMaximum;
		second.GreenMaximum = first.GreenMaximum;
		second.BlueMaximum = first.BlueMaximum;
		switch (num)
		{
		case 3:
			if (!cubeCut.Position.HasValue)
			{
				return false;
			}
			second.AlphaMinimum = (first.AlphaMaximum = cubeCut.Position.Value);
			second.RedMinimum = first.RedMinimum;
			second.GreenMinimum = first.GreenMinimum;
			second.BlueMinimum = first.BlueMinimum;
			break;
		case 2:
			if (!cubeCut2.Position.HasValue)
			{
				return false;
			}
			second.RedMinimum = (first.RedMaximum = cubeCut2.Position.Value);
			second.AlphaMinimum = first.AlphaMinimum;
			second.GreenMinimum = first.GreenMinimum;
			second.BlueMinimum = first.BlueMinimum;
			break;
		case 1:
			if (!cubeCut3.Position.HasValue)
			{
				return false;
			}
			second.GreenMinimum = (first.GreenMaximum = cubeCut3.Position.Value);
			second.AlphaMinimum = first.AlphaMinimum;
			second.RedMinimum = first.RedMinimum;
			second.BlueMinimum = first.BlueMinimum;
			break;
		case 0:
			if (!cubeCut4.Position.HasValue)
			{
				return false;
			}
			second.BlueMinimum = (first.BlueMaximum = cubeCut4.Position.Value);
			second.AlphaMinimum = first.AlphaMinimum;
			second.RedMinimum = first.RedMinimum;
			second.GreenMinimum = first.GreenMinimum;
			break;
		}
		first.Size = (first.AlphaMaximum - first.AlphaMinimum) * (first.RedMaximum - first.RedMinimum) * (first.GreenMaximum - first.GreenMinimum) * (first.BlueMaximum - first.BlueMinimum);
		second.Size = (second.AlphaMaximum - second.AlphaMinimum) * (second.RedMaximum - second.RedMinimum) * (second.GreenMaximum - second.GreenMinimum) * (second.BlueMaximum - second.BlueMinimum);
		return true;
	}

	private static float CalculateVariance(ColorMoment[,,,] moments, Box cube)
	{
		return Volume(moments, cube).Variance();
	}

	private static ColorMoment Volume(ColorMoment[,,,] moments, Box cube)
	{
		return moments[cube.AlphaMaximum, cube.RedMaximum, cube.GreenMaximum, cube.BlueMaximum] - moments[cube.AlphaMaximum, cube.RedMaximum, cube.GreenMinimum, cube.BlueMaximum] - moments[cube.AlphaMaximum, cube.RedMinimum, cube.GreenMaximum, cube.BlueMaximum] + moments[cube.AlphaMaximum, cube.RedMinimum, cube.GreenMinimum, cube.BlueMaximum] - moments[cube.AlphaMinimum, cube.RedMaximum, cube.GreenMaximum, cube.BlueMaximum] + moments[cube.AlphaMinimum, cube.RedMaximum, cube.GreenMinimum, cube.BlueMaximum] + moments[cube.AlphaMinimum, cube.RedMinimum, cube.GreenMaximum, cube.BlueMaximum] - moments[cube.AlphaMinimum, cube.RedMinimum, cube.GreenMinimum, cube.BlueMaximum] - (moments[cube.AlphaMaximum, cube.RedMaximum, cube.GreenMaximum, cube.BlueMinimum] - moments[cube.AlphaMinimum, cube.RedMaximum, cube.GreenMaximum, cube.BlueMinimum] - moments[cube.AlphaMaximum, cube.RedMaximum, cube.GreenMinimum, cube.BlueMinimum] + moments[cube.AlphaMinimum, cube.RedMaximum, cube.GreenMinimum, cube.BlueMinimum] - moments[cube.AlphaMaximum, cube.RedMinimum, cube.GreenMaximum, cube.BlueMinimum] + moments[cube.AlphaMinimum, cube.RedMinimum, cube.GreenMaximum, cube.BlueMinimum] + moments[cube.AlphaMaximum, cube.RedMinimum, cube.GreenMinimum, cube.BlueMinimum] - moments[cube.AlphaMinimum, cube.RedMinimum, cube.GreenMinimum, cube.BlueMinimum]);
	}

	private static Box[] SplitData(ref int colorCount, ColorMoment[,,,] moments)
	{
		colorCount--;
		int num = 0;
		float[] array = new float[colorCount];
		Box[] array2 = new Box[colorCount];
		array2[0].AlphaMaximum = 32;
		array2[0].RedMaximum = 32;
		array2[0].GreenMaximum = 32;
		array2[0].BlueMaximum = 32;
		for (int i = 1; i < colorCount; i++)
		{
			if (Cut(moments, ref array2[num], ref array2[i]))
			{
				array[num] = ((array2[num].Size > 1) ? CalculateVariance(moments, array2[num]) : 0f);
				array[i] = ((array2[i].Size > 1) ? CalculateVariance(moments, array2[i]) : 0f);
			}
			else
			{
				array[num] = 0f;
				i--;
			}
			num = 0;
			float num2 = array[0];
			for (int j = 1; j <= i; j++)
			{
				if (!(array[j] <= num2))
				{
					num2 = array[j];
					num = j;
				}
			}
			if (!((double)num2 > 0.0))
			{
				colorCount = i + 1;
				break;
			}
		}
		return array2.Take(colorCount).ToArray();
	}

	private static Color32[] BuildLookups(Box[] cubes, ColorMoment[,,,] moments)
	{
		Color32[] array = new Color32[cubes.Length];
		for (int i = 0; i < cubes.Length; i++)
		{
			ColorMoment colorMoment = Volume(moments, cubes[i]);
			if (colorMoment.Weight > 0)
			{
				array[i] = new Color32
				{
					A = (byte)(colorMoment.Alpha / colorMoment.Weight),
					R = (byte)(colorMoment.Red / colorMoment.Weight),
					G = (byte)(colorMoment.Green / colorMoment.Weight),
					B = (byte)(colorMoment.Blue / colorMoment.Weight)
				};
			}
		}
		return array;
	}
}
