using System;
using System.Collections.Generic;
using System.Linq;
using ImageProcessor.Imaging.Colors;

namespace ImageProcessor.Imaging.Quantizers.WuQuantizer;

internal class PaletteLookup
{
	private struct LookupNode
	{
		public byte PaletteIndex;

		public Color32 Color32;
	}

	private Dictionary<int, LookupNode[]> lookupNodes;

	private int paletteMask;

	private LookupNode[] Palette { get; }

	public PaletteLookup(Color32[] palette)
	{
		Palette = new LookupNode[palette.Length];
		for (int i = 0; i < palette.Length; i++)
		{
			Palette[i] = new LookupNode
			{
				Color32 = palette[i],
				PaletteIndex = (byte)i
			};
		}
		BuildLookup(palette);
	}

	public byte GetPaletteIndex(Color32 pixel)
	{
		int num = pixel.Argb & paletteMask;
		if (!lookupNodes.TryGetValue(num, out var value))
		{
			value = Palette;
		}
		if (value.Length == 1)
		{
			return value[0].PaletteIndex;
		}
		int num2 = int.MaxValue;
		byte b = 0;
		LookupNode[] array = value;
		for (int i = 0; i < array.Length; i++)
		{
			LookupNode lookupNode = array[i];
			Color32 color = lookupNode.Color32;
			int num3 = pixel.A - color.A;
			int num4 = num3 * num3;
			int num5 = pixel.R - color.R;
			num4 += num5 * num5;
			int num6 = pixel.G - color.G;
			num4 += num6 * num6;
			int num7 = pixel.B - color.B;
			num4 += num7 * num7;
			if (num4 < num2)
			{
				num2 = num4;
				b = lookupNode.PaletteIndex;
			}
		}
		if (value == Palette && num != 0)
		{
			lookupNodes[num] = new LookupNode[1] { value[b] };
		}
		return b;
	}

	private static byte ComputeBitMask(byte max, int bits)
	{
		byte b = 0;
		if (bits != 0)
		{
			byte b2 = HighestSetBitIndex(max);
			for (int i = 0; i < bits; i++)
			{
				b <<= 1;
				b++;
			}
			for (int j = 0; j <= b2 - bits; j++)
			{
				b <<= 1;
			}
		}
		return b;
	}

	private static int GetMask(Color32[] palette)
	{
		byte[] source = palette.Select((Color32 p) => p.A).ToArray();
		byte max = source.Max();
		int num = source.Distinct().Count();
		byte[] source2 = palette.Select((Color32 p) => p.R).ToArray();
		byte max2 = source2.Max();
		int num2 = source2.Distinct().Count();
		byte[] source3 = palette.Select((Color32 p) => p.G).ToArray();
		byte max3 = source3.Max();
		int num3 = source3.Distinct().Count();
		byte[] source4 = palette.Select((Color32 p) => p.B).ToArray();
		byte max4 = source4.Max();
		int num4 = source4.Distinct().Count();
		double num5 = num + num2 + num3 + num4;
		double num6 = 1.0 + Math.Log(num * num2 * num3 * num4);
		byte alpha = ComputeBitMask(max, Convert.ToInt32(Math.Round((double)num / num5 * num6)));
		byte red = ComputeBitMask(max2, Convert.ToInt32(Math.Round((double)num2 / num5 * num6)));
		byte green = ComputeBitMask(max3, Convert.ToInt32(Math.Round((double)num3 / num5 * num6)));
		byte blue = ComputeBitMask(max4, Convert.ToInt32(Math.Round((double)num4 / num5 * num6)));
		return new Color32(alpha, red, green, blue).Argb;
	}

	private static byte HighestSetBitIndex(byte value)
	{
		byte result = 0;
		for (int i = 0; i < 8; i++)
		{
			if ((value & 1) != 0)
			{
				result = (byte)i;
			}
			value >>= 1;
		}
		return result;
	}

	private void BuildLookup(Color32[] palette)
	{
		int mask = GetMask(palette);
		Dictionary<int, List<LookupNode>> dictionary = new Dictionary<int, List<LookupNode>>();
		LookupNode[] palette2 = Palette;
		for (int i = 0; i < palette2.Length; i++)
		{
			LookupNode item = palette2[i];
			int key = item.Color32.Argb & mask;
			if (!dictionary.TryGetValue(key, out var value))
			{
				value = (dictionary[key] = new List<LookupNode>());
			}
			value.Add(item);
		}
		lookupNodes = new Dictionary<int, LookupNode[]>(dictionary.Count);
		foreach (int key2 in dictionary.Keys)
		{
			lookupNodes[key2] = dictionary[key2].ToArray();
		}
		paletteMask = mask;
	}
}
