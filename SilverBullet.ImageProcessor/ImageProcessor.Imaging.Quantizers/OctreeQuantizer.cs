using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessor.Common.Extensions;
using ImageProcessor.Imaging.Colors;

namespace ImageProcessor.Imaging.Quantizers;

public class OctreeQuantizer : Quantizer
{
	private class Octree
	{
		protected class OctreeNode
		{
			private readonly OctreeNode[] children;

			private bool leaf;

			private int pixelCount;

			private int red;

			private int green;

			private int blue;

			private int paletteIndex;

			public OctreeNode NextReducible { get; }

			public OctreeNode(int level, int colorBits, Octree octree)
			{
				leaf = level == colorBits;
				red = (green = (blue = 0));
				pixelCount = 0;
				if (leaf)
				{
					octree.Leaves++;
					NextReducible = null;
					children = null;
				}
				else
				{
					NextReducible = octree.ReducibleNodes[level];
					octree.ReducibleNodes[level] = this;
					children = new OctreeNode[8];
				}
			}

			public unsafe void AddColor(Color32* pixel, int colorBits, int level, Octree octree)
			{
				if (leaf)
				{
					Increment(pixel);
					octree.TrackPrevious(this);
					return;
				}
				int num = 7 - level;
				int num2 = ((pixel->R & Mask[level]) >> num - 2) | ((pixel->G & Mask[level]) >> num - 1) | ((pixel->B & Mask[level]) >> num);
				OctreeNode octreeNode = children[num2];
				if (octreeNode == null)
				{
					octreeNode = new OctreeNode(level + 1, colorBits, octree);
					children[num2] = octreeNode;
				}
				octreeNode.AddColor(pixel, colorBits, level + 1, octree);
			}

			public int Reduce()
			{
				red = (green = (blue = 0));
				int num = 0;
				for (int i = 0; i < 8; i++)
				{
					if (children[i] != null)
					{
						red += children[i].red;
						green += children[i].green;
						blue += children[i].blue;
						pixelCount += children[i].pixelCount;
						num++;
						children[i] = null;
					}
				}
				leaf = true;
				return num - 1;
			}

			public void ConstructPalette(List<Color> palette, ref int index)
			{
				if (leaf)
				{
					paletteIndex = index++;
					byte b = (red / pixelCount).ToByte();
					byte b2 = (green / pixelCount).ToByte();
					byte b3 = (blue / pixelCount).ToByte();
					palette.Add(Color.FromArgb(b, b2, b3));
				}
				else
				{
					for (int i = 0; i < 8; i++)
					{
						children[i]?.ConstructPalette(palette, ref index);
					}
				}
			}

			public unsafe int GetPaletteIndex(Color32* pixel, int level)
			{
				int result = paletteIndex;
				if (!leaf)
				{
					int num = 7 - level;
					int num2 = ((pixel->R & Mask[level]) >> num - 2) | ((pixel->G & Mask[level]) >> num - 1) | ((pixel->B & Mask[level]) >> num);
					if (children[num2] == null)
					{
						throw new Exception("Didn't expect this!");
					}
					result = children[num2].GetPaletteIndex(pixel, level + 1);
				}
				return result;
			}

			public unsafe void Increment(Color32* pixel)
			{
				pixelCount++;
				red += pixel->R;
				green += pixel->G;
				blue += pixel->B;
			}
		}

		private static readonly int[] Mask = new int[8] { 128, 64, 32, 16, 8, 4, 2, 1 };

		private readonly OctreeNode root;

		private readonly int maxColorBits;

		private OctreeNode previousNode;

		private int previousColor;

		private int Leaves { get; set; }

		private OctreeNode[] ReducibleNodes { get; }

		public Octree(int maxColorBits)
		{
			this.maxColorBits = maxColorBits;
			Leaves = 0;
			ReducibleNodes = new OctreeNode[9];
			root = new OctreeNode(0, this.maxColorBits, this);
			previousColor = 0;
			previousNode = null;
		}

		public unsafe void AddColor(Color32* pixel)
		{
			if (previousColor == pixel->Argb)
			{
				if (previousNode == null)
				{
					previousColor = pixel->Argb;
					root.AddColor(pixel, maxColorBits, 0, this);
				}
				else
				{
					previousNode.Increment(pixel);
				}
			}
			else
			{
				previousColor = pixel->Argb;
				root.AddColor(pixel, maxColorBits, 0, this);
			}
		}

		public List<Color> Palletize(int colorCount)
		{
			while (Leaves > colorCount)
			{
				Reduce();
			}
			List<Color> list = new List<Color>(Leaves);
			int index = 0;
			root.ConstructPalette(list, ref index);
			return list;
		}

		public unsafe int GetPaletteIndex(Color32* pixel)
		{
			return root.GetPaletteIndex(pixel, 0);
		}

		protected void TrackPrevious(OctreeNode node)
		{
			previousNode = node;
		}

		private void Reduce()
		{
			int num = maxColorBits - 1;
			while (num > 0 && ReducibleNodes[num] == null)
			{
				num--;
			}
			OctreeNode octreeNode = ReducibleNodes[num];
			ReducibleNodes[num] = octreeNode.NextReducible;
			Leaves -= octreeNode.Reduce();
			previousNode = null;
		}
	}

	private readonly int maxColors;

	private readonly int maxColorBits;

	private Octree octree;

	public byte Threshold { get; set; } = 64;

	public OctreeQuantizer()
		: this(255, 8)
	{
	}

	public OctreeQuantizer(int maxColors, int maxColorBits)
		: base(singlePass: false)
	{
		if (maxColors > 255)
		{
			throw new ArgumentOutOfRangeException("maxColors", maxColors, "The number of colors should be less than 256");
		}
		if (maxColorBits < 1 || maxColorBits > 8)
		{
			throw new ArgumentOutOfRangeException("maxColorBits", maxColorBits, "This should be between 1 and 8");
		}
		this.maxColors = maxColors;
		this.maxColorBits = maxColorBits;
	}

	protected override void FirstPass(BitmapData sourceData, int width, int height)
	{
		octree = new Octree(maxColorBits);
		base.FirstPass(sourceData, width, height);
	}

	protected unsafe override void InitialQuantizePixel(Color32* pixel)
	{
		octree.AddColor(pixel);
	}

	protected unsafe override byte QuantizePixel(Color32* pixel)
	{
		byte result = (byte)maxColors;
		if (pixel->A > Threshold)
		{
			result = (byte)octree.GetPaletteIndex(pixel);
		}
		return result;
	}

	protected override ColorPalette GetPalette(ColorPalette original)
	{
		for (int i = 0; i < original.Entries.Length; i++)
		{
			original.Entries[i] = Color.FromArgb(0, 0, 0, 0);
		}
		List<Color> list = octree.Palletize(Math.Max(maxColors - 1, 1));
		for (int j = 0; j < list.Count; j++)
		{
			original.Entries[j] = list[j];
		}
		original.Entries[maxColors] = Color.FromArgb(0, 0, 0, 0);
		return original;
	}
}
