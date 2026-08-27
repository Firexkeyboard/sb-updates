using System;
using System.Drawing;
using System.Drawing.Imaging;
using ImageProcessor.Imaging.Colors;

namespace ImageProcessor.Imaging.Helpers;

public static class ImageMaths
{
	public static RectangleF CenteredRectangle(Rectangle parent, Rectangle child)
	{
		float x = (float)(parent.Width - child.Width) / 2f;
		float y = (float)(parent.Height - child.Height) / 2f;
		int width = child.Width;
		int height = child.Height;
		return new RectangleF(x, y, width, height);
	}

	public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
	{
		if (value.CompareTo(min) < 0)
		{
			return min;
		}
		if (value.CompareTo(max) > 0)
		{
			return max;
		}
		return value;
	}

	public static bool InRange<T>(T value, T min, T max, bool include = true) where T : IComparable<T>
	{
		if (include)
		{
			if (value.CompareTo(min) >= 0)
			{
				return value.CompareTo(max) <= 0;
			}
			return false;
		}
		if (value.CompareTo(min) > 0)
		{
			return value.CompareTo(max) < 0;
		}
		return false;
	}

	public static double DegreesToRadians(double angleInDegrees)
	{
		return angleInDegrees * (Math.PI / 180.0);
	}

	public static Rectangle GetBoundingRectangle(Point topLeft, Point bottomRight)
	{
		return new Rectangle(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
	}

	public static Rectangle GetBoundingRotatedRectangle(int width, int height, float angleInDegrees)
	{
		double num = DegreesToRadians(angleInDegrees);
		double num2 = Math.Sin(num);
		double num3 = Math.Cos(num);
		double value = (double)height * num2 + (double)width * num3;
		double value2 = (double)width * num2 + (double)height * num3;
		num2 = Math.Sin(0.0 - num);
		num3 = Math.Cos(0.0 - num);
		double value3 = (double)height * num2 + (double)width * num3;
		double value4 = (double)width * num2 + (double)height * num3;
		return new Rectangle(0, 0, Convert.ToInt32(Math.Max(Math.Abs(value), Math.Abs(value3))), Convert.ToInt32(Math.Max(Math.Abs(value2), Math.Abs(value4))));
	}

	public static Rectangle GetFilteredBoundingRectangle(Image bitmap, byte componentValue, RgbaComponent channel = RgbaComponent.B)
	{
		int width = bitmap.Width;
		int height = bitmap.Height;
		Point topLeft = default(Point);
		Point bottomRight = default(Point);
		Func<FastBitmap, int, int, byte, bool> delegateFunc;
		switch (channel)
		{
		case RgbaComponent.R:
			delegateFunc = (FastBitmap fastBitmap, int x, int y, byte b) => fastBitmap.GetPixel(x, y).R != b;
			break;
		case RgbaComponent.G:
			delegateFunc = (FastBitmap fastBitmap, int x, int y, byte b) => fastBitmap.GetPixel(x, y).G != b;
			break;
		case RgbaComponent.A:
			delegateFunc = (FastBitmap fastBitmap, int x, int y, byte b) => fastBitmap.GetPixel(x, y).A != b;
			break;
		default:
			delegateFunc = (FastBitmap fastBitmap, int x, int y, byte b) => fastBitmap.GetPixel(x, y).B != b;
			break;
		}
		using (FastBitmap fastBitmap2 = new FastBitmap(bitmap))
		{
			topLeft.Y = getMinY(fastBitmap2);
			topLeft.X = getMinX(fastBitmap2);
			bottomRight.Y = getMaxY(fastBitmap2) + 1;
			bottomRight.X = getMaxX(fastBitmap2) + 1;
		}
		return GetBoundingRectangle(topLeft, bottomRight);
		int getMaxX(FastBitmap fastBitmap)
		{
			for (int num2 = width - 1; num2 > -1; num2--)
			{
				for (int n = 0; n < height; n++)
				{
					if (delegateFunc(fastBitmap, num2, n, componentValue))
					{
						return num2;
					}
				}
			}
			return height;
		}
		int getMaxY(FastBitmap fastBitmap)
		{
			for (int num = height - 1; num > -1; num--)
			{
				for (int m = 0; m < width; m++)
				{
					if (delegateFunc(fastBitmap, m, num, componentValue))
					{
						return num;
					}
				}
			}
			return height;
		}
		int getMinX(FastBitmap fastBitmap)
		{
			for (int k = 0; k < width; k++)
			{
				for (int l = 0; l < height; l++)
				{
					if (delegateFunc(fastBitmap, k, l, componentValue))
					{
						return k;
					}
				}
			}
			return 0;
		}
		int getMinY(FastBitmap fastBitmap)
		{
			for (int i = 0; i < height; i++)
			{
				for (int j = 0; j < width; j++)
				{
					if (delegateFunc(fastBitmap, j, i, componentValue))
					{
						return i;
					}
				}
			}
			return 0;
		}
	}

	public static bool[] GetRoundPixel(Bitmap bitmap, int x, int y)
	{
		bool[] array = new bool[8];
		int num = 0;
		for (int i = -1; i < 2; i++)
		{
			for (int j = -1; j < 2; j++)
			{
				Color pixel = bitmap.GetPixel(x + i, y + j);
				if (i != 0 || j != 0)
				{
					if (byte.MaxValue == pixel.G)
					{
						array[num] = false;
						num++;
					}
					else if (pixel.G == 0)
					{
						array[num] = true;
						num++;
					}
				}
			}
		}
		return array;
	}

	public static Point RotatePoint(Point pointToRotate, double angleInDegrees, Point? centerPoint = null)
	{
		Point point = centerPoint ?? Point.Empty;
		double num = DegreesToRadians(angleInDegrees);
		double num2 = Math.Cos(num);
		double num3 = Math.Sin(num);
		Point result = default(Point);
		result.X = (int)(num2 * (double)(pointToRotate.X - point.X) - num3 * (double)(pointToRotate.Y - point.Y) + (double)point.X);
		result.Y = (int)(num3 * (double)(pointToRotate.X - point.X) + num2 * (double)(pointToRotate.Y - point.Y) + (double)point.Y);
		return result;
	}

	public static Point[] ToPoints(Rectangle rectangle)
	{
		return new Point[4]
		{
			new Point(rectangle.Left, rectangle.Top),
			new Point(rectangle.Right, rectangle.Top),
			new Point(rectangle.Right, rectangle.Bottom),
			new Point(rectangle.Left, rectangle.Bottom)
		};
	}

	public static float ZoomAfterRotation(int imageWidth, int imageHeight, float angleInDegrees)
	{
		Rectangle boundingRotatedRectangle = GetBoundingRotatedRectangle(imageWidth, imageHeight, angleInDegrees);
		return Math.Max((float)boundingRotatedRectangle.Width / (float)imageWidth, (float)boundingRotatedRectangle.Height / (float)imageHeight);
	}

	public static Bitmap GetArgbCopy(Image sourceImage)
	{
		Bitmap bitmap = new Bitmap(sourceImage.Width, sourceImage.Height, PixelFormat.Format32bppArgb);
		using Graphics graphics = Graphics.FromImage(bitmap);
		graphics.DrawImage(sourceImage, new Rectangle(0, 0, bitmap.Width, bitmap.Height), new Rectangle(0, 0, bitmap.Width, bitmap.Height), GraphicsUnit.Pixel);
		graphics.Flush();
		return bitmap;
	}
}
