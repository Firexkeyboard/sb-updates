using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessor.Common.Exceptions;

namespace ImageProcessor.Processors;

public class Saturation : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Saturation()
	{
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		Bitmap bitmap = null;
		Image image = factory.Image;
		try
		{
			float num = (float)DynamicParameter / 100f;
			num += 1f;
			float num2 = 1f - num;
			float num3 = 0.3086f * num2;
			float num4 = 0.6094f * num2;
			float num5 = 0.082f * num2;
			bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppPArgb);
			bitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);
			ColorMatrix colorMatrix = new ColorMatrix(new float[5][]
			{
				new float[5]
				{
					num3 + num,
					num3,
					num3,
					0f,
					0f
				},
				new float[5]
				{
					num4,
					num4 + num,
					num4,
					0f,
					0f
				},
				new float[5]
				{
					num5,
					num5,
					num5 + num,
					0f,
					0f
				},
				new float[5] { 0f, 0f, 0f, 1f, 0f },
				new float[5] { 0f, 0f, 0f, 0f, 1f }
			});
			using Graphics graphics = Graphics.FromImage(bitmap);
			using ImageAttributes imageAttributes = new ImageAttributes();
			imageAttributes.SetColorMatrix(colorMatrix);
			graphics.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, imageAttributes);
			image.Dispose();
			return bitmap;
		}
		catch (Exception innerException)
		{
			bitmap?.Dispose();
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
	}
}
