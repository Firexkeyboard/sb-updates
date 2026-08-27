using System;
using System.Drawing;

namespace ImageProcessor.Imaging;

public class ImageLayer : IDisposable, IEquatable<ImageLayer>
{
	private bool isDisposed;

	public Image Image { get; set; }

	public Size Size { get; set; }

	public int Opacity { get; set; } = 100;

	public Point? Position { get; set; }

	public override bool Equals(object obj)
	{
		if (obj is ImageLayer other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(ImageLayer other)
	{
		if (other != null && Image == other.Image && Size == other.Size && Opacity == other.Opacity)
		{
			Point? position = Position;
			Point? position2 = other.Position;
			if (position.HasValue != position2.HasValue)
			{
				return false;
			}
			if (!position.HasValue)
			{
				return true;
			}
			return position.GetValueOrDefault() == position2.GetValueOrDefault();
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (Image, Size, Opacity, Position).GetHashCode();
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
				Image?.Dispose();
			}
			isDisposed = true;
		}
	}
}
