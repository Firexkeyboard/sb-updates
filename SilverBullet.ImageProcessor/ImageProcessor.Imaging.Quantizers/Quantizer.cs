using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessor.Imaging.Colors;

namespace ImageProcessor.Imaging.Quantizers;

public abstract class Quantizer : IQuantizer
{
	private readonly bool singlePass;

	protected Quantizer(bool singlePass)
	{
		this.singlePass = singlePass;
	}

	public Bitmap Quantize(Image source)
	{
		int height = source.Height;
		int width = source.Width;
		Rectangle rectangle = new Rectangle(0, 0, width, height);
		Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
		bitmap.SetResolution(source.HorizontalResolution, source.VerticalResolution);
		Bitmap bitmap2 = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
		bitmap2.SetResolution(source.HorizontalResolution, source.VerticalResolution);
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			graphics.PageUnit = GraphicsUnit.Pixel;
			graphics.DrawImageUnscaled(source, rectangle);
		}
		BitmapData bitmapData = null;
		try
		{
			bitmapData = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
			if (!singlePass)
			{
				FirstPass(bitmapData, width, height);
			}
			bitmap2.Palette = GetPalette(bitmap2.Palette);
			SecondPass(bitmapData, bitmap2, width, height, rectangle);
			return bitmap2;
		}
		finally
		{
			bitmap.UnlockBits(bitmapData);
		}
	}

	protected unsafe virtual void FirstPass(BitmapData sourceData, int width, int height)
	{
		byte* ptr = (byte*)sourceData.Scan0.ToPointer();
		for (int i = 0; i < height; i++)
		{
			int* ptr2 = (int*)ptr;
			int num = 0;
			while (num < width)
			{
				InitialQuantizePixel((Color32*)ptr2);
				num++;
				ptr2++;
			}
			ptr += sourceData.Stride;
		}
	}

	protected unsafe virtual void SecondPass(BitmapData sourceData, Bitmap output, int width, int height, Rectangle bounds)
	{
		BitmapData bitmapData = null;
		try
		{
			bitmapData = output.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
			byte* ptr = (byte*)sourceData.Scan0.ToPointer();
			int* ptr2 = (int*)ptr;
			int* ptr3 = ptr2;
			byte* ptr4 = (byte*)bitmapData.Scan0.ToPointer();
			byte* ptr5 = ptr4;
			byte b = (*ptr5 = QuantizePixel((Color32*)ptr2));
			for (int i = 0; i < height; i++)
			{
				ptr2 = (int*)ptr;
				ptr5 = ptr4;
				int num = 0;
				while (num < width)
				{
					if (*ptr3 != *ptr2)
					{
						b = QuantizePixel((Color32*)ptr2);
						ptr3 = ptr2;
					}
					*ptr5 = b;
					num++;
					ptr2++;
					ptr5++;
				}
				ptr += sourceData.Stride;
				ptr4 += bitmapData.Stride;
			}
		}
		finally
		{
			output.UnlockBits(bitmapData);
		}
	}

	protected unsafe virtual void InitialQuantizePixel(Color32* pixel)
	{
	}

	protected unsafe abstract byte QuantizePixel(Color32* pixel);

	protected abstract ColorPalette GetPalette(ColorPalette original);
}
