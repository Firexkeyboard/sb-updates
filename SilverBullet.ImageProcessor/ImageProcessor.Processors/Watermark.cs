using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Imaging;

namespace ImageProcessor.Processors;

public class Watermark : IGraphicsProcessor
{
	public dynamic DynamicParameter { get; set; }

	public Dictionary<string, string> Settings { get; set; }

	public Watermark()
	{
		Settings = new Dictionary<string, string>();
	}

	public Image ProcessImage(ImageFactory factory)
	{
		Image image = factory.Image;
		try
		{
			TextLayer textLayer = DynamicParameter;
			string text = textLayer.Text;
			int num = Math.Min((int)Math.Ceiling((float)textLayer.Opacity / 100f * 255f), 255);
			int fontSize = textLayer.FontSize;
			FontStyle style = textLayer.Style;
			bool flag = false;
			RotateFlipType? rotateFlipType = GetRotateFlipType(factory);
			if (rotateFlipType.HasValue)
			{
				image.RotateFlip(rotateFlipType.Value);
			}
			using Graphics graphics = Graphics.FromImage(image);
			using (Font font = GetFont(textLayer.FontFamily, fontSize, style))
			{
				using StringFormat stringFormat = new StringFormat(StringFormat.GenericTypographic);
				StringFormatFlags? flags = GetFlags(textLayer);
				if (flags.HasValue)
				{
					stringFormat.FormatFlags = flags.Value;
				}
				using Brush brush2 = new SolidBrush(Color.FromArgb(num, textLayer.FontColor));
				Point? point = textLayer.Position;
				SizeF sizeF = graphics.MeasureString(text, font, new SizeF(image.Width, image.Height), stringFormat);
				if (!point.HasValue)
				{
					int x = ((!textLayer.RightToLeft) ? ((int)((float)image.Width - sizeF.Width) / 2) : 0);
					int y = (int)((float)image.Height - sizeF.Height) / 2;
					point = new Point(x, y);
					flag = true;
				}
				graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
				RectangleF layoutRectangle;
				if (textLayer.DropShadow)
				{
					int num2 = num - (int)Math.Ceiling(76.5);
					using Brush brush = new SolidBrush(Color.FromArgb((num2 > 0) ? num2 : 0, Color.Black));
					int num3 = (int)Math.Ceiling((float)fontSize / 24f);
					Point point2 = new Point(point.Value.X + num3, point.Value.Y + num3);
					layoutRectangle = ((!(textLayer.RightToLeft && flag)) ? new RectangleF(point2, new SizeF(image.Width - point2.X, image.Height - point2.Y)) : new RectangleF(point2, new SizeF(image.Width - (int)((float)image.Width - sizeF.Width) / 2 - point2.X, image.Height - point2.Y)));
					graphics.DrawString(text, font, brush, layoutRectangle, stringFormat);
				}
				layoutRectangle = ((!(textLayer.RightToLeft && flag)) ? new RectangleF(point.Value, new SizeF(image.Width - point.Value.X, image.Height - point.Value.Y)) : new RectangleF(point.Value, new SizeF(image.Width - (int)((float)image.Width - sizeF.Width) / 2, image.Height - point.Value.Y)));
				graphics.DrawString(text, font, brush2, layoutRectangle, stringFormat);
			}
			if (rotateFlipType.HasValue)
			{
				RotateFlipType value = rotateFlipType.Value;
				switch (value)
				{
				case RotateFlipType.Rotate270FlipNone:
					image.RotateFlip(RotateFlipType.Rotate90FlipNone);
					break;
				case RotateFlipType.Rotate90FlipNone:
					image.RotateFlip(RotateFlipType.Rotate270FlipNone);
					break;
				default:
					image.RotateFlip(value);
					break;
				}
			}
			return image;
		}
		catch (Exception innerException)
		{
			throw new ImageProcessingException("Error processing image with " + GetType().Name, innerException);
		}
	}

	private Font GetFont(FontFamily fontFamily, int fontSize, FontStyle fontStyle)
	{
		try
		{
			using FontFamily family = new FontFamily(fontFamily.Name);
			return new Font(family, fontSize, fontStyle, GraphicsUnit.Pixel);
		}
		catch
		{
			using FontFamily family2 = FontFamily.GenericSansSerif;
			return new Font(family2, fontSize, fontStyle, GraphicsUnit.Pixel);
		}
	}

	private StringFormatFlags? GetFlags(TextLayer textLayer)
	{
		if (textLayer.Vertical && textLayer.RightToLeft)
		{
			return StringFormatFlags.DirectionRightToLeft | StringFormatFlags.DirectionVertical;
		}
		if (textLayer.Vertical)
		{
			return StringFormatFlags.DirectionVertical;
		}
		if (textLayer.RightToLeft)
		{
			return StringFormatFlags.DirectionRightToLeft;
		}
		return null;
	}

	private RotateFlipType? GetRotateFlipType(ImageFactory factory)
	{
		if (factory.PreserveExifData && factory.ExifPropertyItems.ContainsKey(274))
		{
			switch (factory.ExifPropertyItems[274].Value[0])
			{
			case 8:
				return RotateFlipType.Rotate270FlipNone;
			case 3:
				return RotateFlipType.Rotate180FlipNone;
			case 6:
				return RotateFlipType.Rotate90FlipNone;
			}
		}
		return null;
	}
}
