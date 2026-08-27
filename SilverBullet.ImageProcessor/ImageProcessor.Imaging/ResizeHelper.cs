using System;
using System.Drawing;

namespace ImageProcessor.Imaging;

internal static class ResizeHelper
{
	public static (Size, Rectangle) CalculateTargetLocationAndBounds(Size sourceSize, ResizeLayer options, int width, int height)
	{
		if (width <= 0 && height <= 0)
		{
			ThrowInvalid($"Target width {width} and height {height} must be greater than zero.");
		}
		return options.ResizeMode switch
		{
			ResizeMode.Crop => CalculateCropRectangle(sourceSize, options, width, height), 
			ResizeMode.Pad => CalculatePadRectangle(sourceSize, options, width, height), 
			ResizeMode.BoxPad => CalculateBoxPadRectangle(sourceSize, options, width, height), 
			ResizeMode.Max => CalculateMaxRectangle(sourceSize, width, height), 
			ResizeMode.Min => CalculateMinRectangle(sourceSize, width, height), 
			_ => (new Size(width, height), new Rectangle(0, 0, width, height)), 
		};
	}

	private static (Size, Rectangle) CalculateBoxPadRectangle(Size source, ResizeLayer options, int width, int height)
	{
		if (width <= 0 || height <= 0)
		{
			return (new Size(source.Width, source.Height), new Rectangle(0, 0, source.Width, source.Height));
		}
		int width2 = source.Width;
		int height2 = source.Height;
		float num = Math.Abs((float)height / (float)height2);
		float num2 = Math.Abs((float)width / (float)width2);
		int num3 = ((height > 0) ? height : ((int)Math.Round((float)height2 * num2)));
		int num4 = ((width > 0) ? width : ((int)Math.Round((float)width2 * num)));
		if (width2 < num4 && height2 < num3)
		{
			int width3 = width2;
			int height3 = height2;
			width = num4;
			height = num3;
			int y;
			int x;
			switch (options.AnchorPosition)
			{
			case AnchorPosition.Left:
				y = (height - height2) / 2;
				x = 0;
				break;
			case AnchorPosition.Right:
				y = (height - height2) / 2;
				x = width - width2;
				break;
			case AnchorPosition.TopRight:
				y = 0;
				x = width - width2;
				break;
			case AnchorPosition.Top:
				y = 0;
				x = (width - width2) / 2;
				break;
			case AnchorPosition.TopLeft:
				y = 0;
				x = 0;
				break;
			case AnchorPosition.BottomRight:
				y = height - height2;
				x = width - width2;
				break;
			case AnchorPosition.Bottom:
				y = height - height2;
				x = (width - width2) / 2;
				break;
			case AnchorPosition.BottomLeft:
				y = height - height2;
				x = 0;
				break;
			default:
				y = (height - height2) / 2;
				x = (width - width2) / 2;
				break;
			}
			return (new Size(width, height), new Rectangle(x, y, width3, height3));
		}
		return CalculatePadRectangle(source, options, width, height);
	}

	private static (Size, Rectangle) CalculateCropRectangle(Size source, ResizeLayer options, int width, int height)
	{
		int width2 = source.Width;
		int height2 = source.Height;
		int num = 0;
		int num2 = 0;
		int width3 = width;
		int height3 = height;
		float num3 = Math.Abs((float)height / (float)height2);
		float num4 = Math.Abs((float)width / (float)width2);
		if (num3 < num4)
		{
			float num5 = num4;
			PointF? center = options.Center;
			if (center.HasValue)
			{
				num2 = (int)Math.Round((0f - num5 * (float)height2) * center.Value.Y + (float)height / 2f);
				if (num2 > 0)
				{
					num2 = 0;
				}
				if (num2 < (int)Math.Round((float)height - (float)height2 * num5))
				{
					num2 = (int)Math.Round((float)height - (float)height2 * num5);
				}
			}
			else
			{
				switch (options.AnchorPosition)
				{
				case AnchorPosition.Top:
				case AnchorPosition.TopLeft:
				case AnchorPosition.TopRight:
					num2 = 0;
					break;
				case AnchorPosition.Bottom:
				case AnchorPosition.BottomRight:
				case AnchorPosition.BottomLeft:
					num2 = (int)Math.Round((float)height - (float)height2 * num5);
					break;
				default:
					num2 = (int)Math.Round(((float)height - (float)height2 * num5) / 2f);
					break;
				}
			}
			height3 = (int)Math.Ceiling((float)height2 * num4);
		}
		else
		{
			float num5 = num3;
			PointF? center2 = options.Center;
			if (center2.HasValue)
			{
				num = (int)Math.Round((0f - num5 * (float)width2) * center2.Value.X + (float)width / 2f);
				if (num > 0)
				{
					num = 0;
				}
				if (num < (int)Math.Round((float)width - (float)width2 * num5))
				{
					num = (int)Math.Round((float)width - (float)width2 * num5);
				}
			}
			else
			{
				switch (options.AnchorPosition)
				{
				case AnchorPosition.Left:
				case AnchorPosition.TopLeft:
				case AnchorPosition.BottomLeft:
					num = 0;
					break;
				case AnchorPosition.Right:
				case AnchorPosition.TopRight:
				case AnchorPosition.BottomRight:
					num = (int)Math.Round((float)width - (float)width2 * num5);
					break;
				default:
					num = (int)Math.Round(((float)width - (float)width2 * num5) / 2f);
					break;
				}
			}
			width3 = (int)Math.Ceiling((float)width2 * num3);
		}
		return (new Size(width, height), new Rectangle(num, num2, width3, height3));
	}

	private static (Size, Rectangle) CalculateMaxRectangle(Size source, int width, int height)
	{
		int width2 = width;
		int height2 = height;
		float num = Math.Abs((float)height / (float)source.Height);
		float num2 = Math.Abs((float)width / (float)source.Width);
		float num3 = (float)height / (float)width;
		if ((float)source.Height / (float)source.Width < num3)
		{
			height2 = (int)Math.Round((float)source.Height * num2);
		}
		else
		{
			width2 = (int)Math.Round((float)source.Width * num);
		}
		return (new Size(width2, height2), new Rectangle(0, 0, width2, height2));
	}

	private static (Size, Rectangle) CalculateMinRectangle(Size source, int width, int height)
	{
		int width2 = source.Width;
		int height2 = source.Height;
		int width3 = width;
		int height3 = height;
		if (width > width2 || height > height2)
		{
			return (new Size(width2, height2), new Rectangle(0, 0, width2, height2));
		}
		int num = width2 - width;
		int num2 = height2 - height;
		if (num < num2)
		{
			float num3 = (float)height2 / (float)width2;
			height3 = (int)Math.Round((float)width * num3);
		}
		else if (num > num2)
		{
			float num4 = (float)width2 / (float)height2;
			width3 = (int)Math.Round((float)height * num4);
		}
		else if (height > width)
		{
			float num5 = Math.Abs((float)width / (float)width2);
			height3 = (int)Math.Round((float)height2 * num5);
		}
		else
		{
			float num6 = Math.Abs((float)height / (float)height2);
			width3 = (int)Math.Round((float)width2 * num6);
		}
		return (new Size(width3, height3), new Rectangle(0, 0, width3, height3));
	}

	private static (Size, Rectangle) CalculatePadRectangle(Size sourceSize, ResizeLayer options, int width, int height)
	{
		int width2 = sourceSize.Width;
		int height2 = sourceSize.Height;
		int x = 0;
		int y = 0;
		int width3 = width;
		int height3 = height;
		float num = Math.Abs((float)height / (float)height2);
		float num2 = Math.Abs((float)width / (float)width2);
		if (num < num2)
		{
			float num3 = num;
			width3 = (int)Math.Round((float)width2 * num);
			switch (options.AnchorPosition)
			{
			case AnchorPosition.Left:
			case AnchorPosition.TopLeft:
			case AnchorPosition.BottomLeft:
				x = 0;
				break;
			case AnchorPosition.Right:
			case AnchorPosition.TopRight:
			case AnchorPosition.BottomRight:
				x = (int)Math.Round((float)width - (float)width2 * num3);
				break;
			default:
				x = (int)Math.Round(((float)width - (float)width2 * num3) / 2f);
				break;
			}
		}
		else
		{
			float num3 = num2;
			height3 = (int)Math.Round((float)height2 * num2);
			switch (options.AnchorPosition)
			{
			case AnchorPosition.Top:
			case AnchorPosition.TopLeft:
			case AnchorPosition.TopRight:
				y = 0;
				break;
			case AnchorPosition.Bottom:
			case AnchorPosition.BottomRight:
			case AnchorPosition.BottomLeft:
				y = (int)Math.Round((float)height - (float)height2 * num3);
				break;
			default:
				y = (int)Math.Round(((float)height - (float)height2 * num3) / 2f);
				break;
			}
		}
		return (new Size(width, height), new Rectangle(x, y, width3, height3));
	}

	private static void ThrowInvalid(string message)
	{
		throw new InvalidOperationException(message);
	}
}
