using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Processors;

public class Tint : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Tint()
	{
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		Bitmap bitmap = null;
		Image image = factory.Image;
		try
		{
			Color color = DynamicParameter;
			ColorMatrix newColorMatrix = new ColorMatrix(new float[5][]
			{
				new float[5]
				{
					(float)(int)color.R / 255f,
					0f,
					0f,
					0f,
					0f
				},
				new float[5]
				{
					0f,
					(float)(int)color.G / 255f,
					0f,
					0f,
					0f
				},
				new float[5]
				{
					0f,
					0f,
					(float)(int)color.B / 255f,
					0f,
					0f
				},
				new float[5]
				{
					0f,
					0f,
					0f,
					(float)(int)color.A / 255f,
					0f
				},
				new float[5] { 0f, 0f, 0f, 0f, 1f }
			});
			bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppPArgb);
			bitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);
			using Graphics graphics = Graphics.FromImage(bitmap);
			GraphicsHelper.SetGraphicsOptions(graphics);
			using ImageAttributes imageAttributes = new ImageAttributes();
			imageAttributes.SetColorMatrix(newColorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
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
