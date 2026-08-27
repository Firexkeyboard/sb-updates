using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ImageProcessor.Imaging.Colors;

namespace ImageProcessor.Imaging;

public class FastBitmap : IDisposable, IEquatable<FastBitmap>
{
	private const int Format8bppIndexed = 198659;

	private const int Format24bppRgb = 137224;

	private const int Format32bppArgb = 2498570;

	private const int Format32bppPArgb = 925707;

	private readonly Bitmap bitmap;

	private readonly int channel;

	private readonly bool computeIntegrals;

	private readonly bool computeTilted;

	private long[,] normalSumImage;

	private long[,] squaredSumImage;

	private long[,] tiltedSumImage;

	private int normalWidth;

	private int tiltedWidth;

	private int bytesInARow;

	private unsafe long* normalSum;

	private unsafe long* squaredSum;

	private unsafe long* tiltedSum;

	private GCHandle normalSumHandle;

	private GCHandle squaredSumHandle;

	private GCHandle tiltedSumHandle;

	private int pixelSize;

	private BitmapData bitmapData;

	private unsafe byte* pixelBase;

	private bool isDisposed;

	public int Width { get; }

	public int Height { get; }

	public long[,] NormalImage => normalSumImage;

	public long[,] SquaredImage => squaredSumImage;

	public long[,] TiltedImage => tiltedSumImage;

	private unsafe Color32* this[int x, int y] => (Color32*)(pixelBase + y * bytesInARow + x * 4);

	public FastBitmap(Image bitmap)
		: this(bitmap, computeIntegrals: false)
	{
	}

	public FastBitmap(Image bitmap, bool computeIntegrals)
		: this(bitmap, computeIntegrals, computeTilted: false)
	{
	}

	public FastBitmap(Image bitmap, bool computeIntegrals, bool computeTilted)
	{
		int pixelFormat = (int)bitmap.PixelFormat;
		if (pixelFormat != 198659 && pixelFormat != 137224 && pixelFormat != 2498570 && pixelFormat != 925707)
		{
			throw new ArgumentException("Only 8bpp, 24bpp and 32bpp images are supported.");
		}
		this.bitmap = (Bitmap)bitmap;
		Width = this.bitmap.Width;
		Height = this.bitmap.Height;
		channel = ((pixelFormat != 198659) ? 2 : 0);
		this.computeIntegrals = computeIntegrals;
		this.computeTilted = computeTilted;
		LockBitmap();
	}

	public static implicit operator Image(FastBitmap fastBitmap)
	{
		return fastBitmap.bitmap;
	}

	public static implicit operator Bitmap(FastBitmap fastBitmap)
	{
		return fastBitmap.bitmap;
	}

	public unsafe Color GetPixel(int x, int y)
	{
		Color32* ptr = this[x, y];
		return Color.FromArgb(ptr->A, ptr->R, ptr->G, ptr->B);
	}

	public unsafe void SetPixel(int x, int y, Color color)
	{
		this[x, y]->Argb = color.ToArgb();
	}

	public unsafe long GetSum(int x, int y, int rectangleWidth, int rectangleHeight)
	{
		int num = normalWidth * y + x;
		int num2 = normalWidth * (y + rectangleHeight) + (x + rectangleWidth);
		int num3 = normalWidth * (y + rectangleHeight) + x;
		int num4 = normalWidth * y + (x + rectangleWidth);
		return normalSum[num] + normalSum[num2] - normalSum[num3] - normalSum[num4];
	}

	public unsafe long GetSum2(int x, int y, int rectangleWidth, int rectangleHeight)
	{
		int num = normalWidth * y + x;
		int num2 = normalWidth * (y + rectangleHeight) + (x + rectangleWidth);
		int num3 = normalWidth * (y + rectangleHeight) + x;
		int num4 = normalWidth * y + (x + rectangleWidth);
		return squaredSum[num] + squaredSum[num2] - squaredSum[num3] - squaredSum[num4];
	}

	public unsafe long GetSumT(int x, int y, int rectangleWidth, int rectangleHeight)
	{
		int num = tiltedWidth * (y + rectangleWidth) + (x + rectangleWidth + 1);
		int num2 = tiltedWidth * (y + rectangleHeight) + (x - rectangleHeight + 1);
		int num3 = tiltedWidth * y + (x + 1);
		int num4 = tiltedWidth * (y + rectangleWidth + rectangleHeight) + (x + rectangleWidth - rectangleHeight + 1);
		return tiltedSum[num] + tiltedSum[num2] - tiltedSum[num3] - tiltedSum[num4];
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public override bool Equals(object obj)
	{
		if (obj is FastBitmap other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(FastBitmap other)
	{
		if (other != null)
		{
			return bitmap == other.bitmap;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return bitmap.GetHashCode();
	}

	protected unsafe virtual void Dispose(bool disposing)
	{
		if (!isDisposed)
		{
			if (disposing)
			{
				UnlockBitmap();
			}
			if (normalSumHandle.IsAllocated)
			{
				normalSumHandle.Free();
				normalSum = null;
			}
			if (squaredSumHandle.IsAllocated)
			{
				squaredSumHandle.Free();
				squaredSum = null;
			}
			if (tiltedSumHandle.IsAllocated)
			{
				tiltedSumHandle.Free();
				tiltedSum = null;
			}
			isDisposed = true;
		}
	}

	private unsafe void LockBitmap()
	{
		Rectangle rect = new Rectangle(Point.Empty, bitmap.Size);
		pixelSize = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
		bytesInARow = rect.Width * pixelSize;
		if (bytesInARow % 4 != 0)
		{
			bytesInARow = 4 * (bytesInARow / 4 + 1);
		}
		bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
		pixelBase = (byte*)bitmapData.Scan0.ToPointer();
		if (computeIntegrals)
		{
			normalWidth = Width + 1;
			int num = Height + 1;
			tiltedWidth = Width + 2;
			int num2 = Height + 2;
			normalSumImage = new long[num, normalWidth];
			normalSumHandle = GCHandle.Alloc(normalSumImage, GCHandleType.Pinned);
			normalSum = (long*)normalSumHandle.AddrOfPinnedObject().ToPointer();
			squaredSumImage = new long[num, normalWidth];
			squaredSumHandle = GCHandle.Alloc(squaredSumImage, GCHandleType.Pinned);
			squaredSum = (long*)squaredSumHandle.AddrOfPinnedObject().ToPointer();
			if (computeTilted)
			{
				tiltedSumImage = new long[num2, tiltedWidth];
				tiltedSumHandle = GCHandle.Alloc(tiltedSumImage, GCHandleType.Pinned);
				tiltedSum = (long*)tiltedSumHandle.AddrOfPinnedObject().ToPointer();
			}
			CalculateIntegrals();
		}
	}

	private unsafe void CalculateIntegrals()
	{
		int num = bitmapData.Stride - bytesInARow;
		byte* ptr = pixelBase + channel;
		byte* ptr2 = ptr;
		for (int i = 1; i <= Height; i++)
		{
			int num2 = normalWidth * i;
			int num3 = normalWidth * (i - 1);
			int num4 = 1;
			while (num4 <= Width)
			{
				int num5 = *ptr2;
				int num6 = num5 * num5;
				int num7 = num2 + num4;
				int num8 = num2 + (num4 - 1);
				int num9 = num3 + num4;
				int num10 = num3 + (num4 - 1);
				normalSum[num7] = num5 + normalSum[num8] + normalSum[num9] - normalSum[num10];
				squaredSum[num7] = num6 + squaredSum[num8] + squaredSum[num9] - squaredSum[num10];
				num4++;
				ptr2 += pixelSize;
			}
			ptr2 += num;
		}
		if (!computeTilted)
		{
			return;
		}
		ptr2 = ptr;
		int num11 = 1;
		while (num11 <= Height)
		{
			int num12 = tiltedWidth * num11;
			int num13 = tiltedWidth * (num11 - 1);
			int num14 = 2;
			while (num14 < Width + 2)
			{
				int num15 = num13 + (num14 - 1);
				int num16 = num12 + (num14 - 1);
				int num17 = num13 + (num14 - 2);
				int num18 = num12 + num14;
				tiltedSum[num18] = *ptr2 + tiltedSum[num15] + tiltedSum[num16] - tiltedSum[num17];
				num14++;
				ptr2 += pixelSize;
			}
			num11++;
			ptr2 += num;
		}
		int num19 = tiltedWidth * Height;
		int num20 = tiltedWidth * (Height + 1);
		int num21 = 2;
		while (num21 < Width + 2)
		{
			int num22 = num19 + (num21 - 1);
			int num23 = num19 + (num21 - 2);
			int num24 = num20 + (num21 - 1);
			int num25 = num20 + num21;
			tiltedSum[num25] = tiltedSum[num22] + tiltedSum[num24] - tiltedSum[num23];
			num21++;
			ptr2 += pixelSize;
		}
		for (int num26 = Height; num26 >= 0; num26--)
		{
			int num27 = tiltedWidth * num26;
			int num28 = tiltedWidth * (num26 + 1);
			for (int num29 = Width + 1; num29 >= 1; num29--)
			{
				int num30 = num27 + num29;
				int num31 = num28 + (num29 - 1);
				tiltedSum[num30] += tiltedSum[num31];
			}
		}
		for (int num32 = Height + 1; num32 >= 0; num32--)
		{
			int num33 = tiltedWidth * num32;
			for (int num34 = Width + 1; num34 >= 2; num34--)
			{
				int num35 = num33 + num34;
				int num36 = num33 + (num34 - 2);
				tiltedSum[num35] -= tiltedSum[num36];
			}
		}
	}

	private unsafe void UnlockBitmap()
	{
		bitmap.UnlockBits(bitmapData);
		bitmapData = null;
		pixelBase = null;
	}
}
