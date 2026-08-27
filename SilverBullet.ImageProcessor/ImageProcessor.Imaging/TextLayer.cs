using System;
using System.Drawing;
using System.Drawing.Text;

namespace ImageProcessor.Imaging;

public class TextLayer : IDisposable, IEquatable<TextLayer>
{
	private bool isDisposed;

	public string Text { get; set; }

	public Color FontColor { get; set; } = Color.Black;

	public FontFamily FontFamily { get; set; } = new FontFamily(GenericFontFamilies.SansSerif);

	public int FontSize { get; set; } = 48;

	public FontStyle Style { get; set; }

	public int Opacity { get; set; } = 100;

	public Point? Position { get; set; }

	public bool DropShadow { get; set; }

	public bool Vertical { get; set; }

	public bool RightToLeft { get; set; }

	public override bool Equals(object obj)
	{
		if (obj is TextLayer other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(TextLayer other)
	{
		if (other != null && Text == other.Text && FontColor == other.FontColor && ((FontFamily == null || other.FontFamily == null) ? (FontFamily == other.FontFamily) : FontFamily.Equals(other.FontFamily)) && FontSize == other.FontSize && Style == other.Style && Opacity == other.Opacity)
		{
			Point? position = Position;
			Point? position2 = other.Position;
			if (position.HasValue == position2.HasValue && (!position.HasValue || position.GetValueOrDefault() == position2.GetValueOrDefault()) && DropShadow == other.DropShadow && Vertical == other.Vertical)
			{
				return RightToLeft == other.RightToLeft;
			}
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (Text, FontColor, FontFamily, FontSize, Style, Opacity, Position, DropShadow, Vertical, RightToLeft).GetHashCode();
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!isDisposed)
		{
			if (disposing)
			{
				FontFamily?.Dispose();
			}
			isDisposed = true;
		}
	}
}
