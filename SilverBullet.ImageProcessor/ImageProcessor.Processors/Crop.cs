using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Helpers;
using ImageProcessor.Imaging.MetaData;

namespace ImageProcessor.Processors;

public class Crop : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Crop()
	{
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		Bitmap bitmap = null;
		Image image = factory.Image;
		try
		{
			int width = image.Width;
			int height = image.Height;
			CropLayer cropLayer = DynamicParameter;
			RectangleF value;
			if (cropLayer.CropMode == CropMode.Percentage)
			{
				float num = ((cropLayer.Left > 1f) ? (cropLayer.Left / 100f) : cropLayer.Left);
				float num2 = ((cropLayer.Right > 1f) ? (cropLayer.Right / 100f) : cropLayer.Right);
				float num3 = ((cropLayer.Top > 1f) ? (cropLayer.Top / 100f) : cropLayer.Top);
				float num4 = ((cropLayer.Bottom > 1f) ? (cropLayer.Bottom / 100f) : cropLayer.Bottom);
				float x = num * (float)width;
				float y = num3 * (float)height;
				float width2 = ((num2 < 1f) ? ((1f - num - num2) * (float)width) : ((float)width));
				float height2 = ((num4 < 1f) ? ((1f - num3 - num4) * (float)height) : ((float)height));
				value = new RectangleF(x, y, width2, height2);
			}
			else
			{
				value = new RectangleF(cropLayer.Left, cropLayer.Top, cropLayer.Right, cropLayer.Bottom);
			}
			Rectangle rectangle = Rectangle.Round(value);
			if (rectangle.X < width && rectangle.Y < height)
			{
				if (rectangle.Width > width - rectangle.X)
				{
					rectangle.Width = width - rectangle.X;
				}
				if (rectangle.Height > height - rectangle.Y)
				{
					rectangle.Height = height - rectangle.Y;
				}
				bitmap = new Bitmap(rectangle.Width, rectangle.Height, PixelFormat.Format32bppPArgb);
				bitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);
				int orientation = 0;
				bool flag = factory.PreserveExifData && factory.ExifPropertyItems.ContainsKey(274);
				if (flag)
				{
					orientation = factory.ExifPropertyItems[274].Value[0];
					ForwardRotateFlip(orientation, ref image);
				}
				using (Graphics graphics = Graphics.FromImage(bitmap))
				{
					GraphicsHelper.SetGraphicsOptions(graphics);
					using ImageAttributes imageAttributes = new ImageAttributes();
					imageAttributes.SetWrapMode(WrapMode.TileFlipXY);
					graphics.DrawImage(image, new Rectangle(0, 0, rectangle.Width, rectangle.Height), rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, GraphicsUnit.Pixel, imageAttributes);
				}
				image.Dispose();
				image = bitmap;
				if (flag)
				{
					ReverseRotateFlip(orientation, ref image);
				}
				if (factory.PreserveExifData && factory.ExifPropertyItems.Count > 0)
				{
					factory.SetPropertyItem(ExifPropertyTag.ImageWidth, (ushort)image.Width);
					factory.SetPropertyItem(ExifPropertyTag.ImageHeight, (ushort)image.Height);
				}
			}
		}
		catch (Exception innerException)
		{
			bitmap?.Dispose();
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
		return image;
	}

	private void ForwardRotateFlip(int orientation, ref Image image)
	{
		switch (orientation)
		{
		case 8:
			image.RotateFlip(RotateFlipType.Rotate270FlipNone);
			break;
		case 7:
			image.RotateFlip(RotateFlipType.Rotate270FlipX);
			break;
		case 6:
			image.RotateFlip(RotateFlipType.Rotate90FlipNone);
			break;
		case 5:
			image.RotateFlip(RotateFlipType.Rotate90FlipX);
			break;
		case 3:
			image.RotateFlip(RotateFlipType.Rotate180FlipNone);
			break;
		case 2:
			image.RotateFlip(RotateFlipType.RotateNoneFlipX);
			break;
		case 4:
			break;
		}
	}

	private void ReverseRotateFlip(int orientation, ref Image image)
	{
		switch (orientation)
		{
		case 8:
			image.RotateFlip(RotateFlipType.Rotate90FlipNone);
			break;
		case 7:
			image.RotateFlip(RotateFlipType.Rotate90FlipX);
			break;
		case 6:
			image.RotateFlip(RotateFlipType.Rotate270FlipNone);
			break;
		case 5:
			image.RotateFlip(RotateFlipType.Rotate270FlipX);
			break;
		case 3:
			image.RotateFlip(RotateFlipType.Rotate180FlipNone);
			break;
		case 2:
			image.RotateFlip(RotateFlipType.RotateNoneFlipX);
			break;
		case 4:
			break;
		}
	}
}
