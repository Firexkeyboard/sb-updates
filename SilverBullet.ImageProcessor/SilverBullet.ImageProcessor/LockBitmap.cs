using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SilverBullet.ImageProcessor;

public class LockBitmap : IDisposable
{
	private readonly Bitmap _bitmap;

	private readonly int _width;

	private readonly int _height;

	private readonly Rectangle _rect;

	private readonly PixelFormat _pixelFormat;

	private BitmapData _bitmapData;

	private IntPtr _ptr;

	private int _length;

	private byte[] _values;

	private readonly int _depth;

	private readonly int _colorLength;

	public byte[] Pixels => new byte[_width * _height * _colorLength];

	public int Depth => _depth;

	public int Width => _width;

	public int Height => _height;

	public Bitmap Source { get; private set; }

	public bool IsModify { get; private set; }

	public LockBitmap(Bitmap bitmap)
	{
		Source = bitmap;
		_bitmap = bitmap;
		_width = _bitmap.Width;
		_height = _bitmap.Height;
		_rect = new Rectangle(0, 0, _width, _height);
		_pixelFormat = _bitmap.PixelFormat;
		_depth = Image.GetPixelFormatSize(_bitmap.PixelFormat);
		_colorLength = _depth / 8;
		IsModify = false;
	}

	public void LockBits()
	{
		if (_depth != 8 && _depth != 24 && _depth != 32)
		{
			throw new ArgumentException("Only 8, 24 and 32 bpp images are supported.");
		}
		_bitmapData = _bitmap.LockBits(_rect, ImageLockMode.ReadWrite, _pixelFormat);
		_ptr = _bitmapData.Scan0;
		_length = _bitmapData.Stride * _height;
		_values = new byte[_length];
		Marshal.Copy(_ptr, _values, 0, _length);
	}

	public void UnlockBits()
	{
		if (IsModify)
		{
			Marshal.Copy(_values, 0, _ptr, _length);
		}
		_bitmap.UnlockBits(_bitmapData);
	}

	public Color GetPixel(int x, int y)
	{
		int num = _colorLength * x;
		int num2 = y * _bitmapData.Stride + num;
		Color result = Color.Empty;
		if (num2 > _values.Length - _colorLength)
		{
			throw new IndexOutOfRangeException();
		}
		switch (_depth)
		{
		case 32:
		{
			byte b = _values[num2];
			byte green = _values[num2 + 1];
			byte red = _values[num2 + 2];
			result = Color.FromArgb(_values[num2 + 3], red, green, b);
			break;
		}
		case 24:
		{
			byte b = _values[num2];
			byte green = _values[num2 + 1];
			byte red = _values[num2 + 2];
			result = Color.FromArgb(red, green, b);
			break;
		}
		case 8:
		{
			byte b = _values[num2];
			result = Color.FromArgb(b, b, b);
			break;
		}
		}
		return result;
	}

	public void SetPixel(int x, int y, Color color)
	{
		int num = _colorLength * x;
		int num2 = y * _bitmapData.Stride + num;
		switch (_depth)
		{
		case 32:
			_values[num2] = color.B;
			_values[num2 + 1] = color.G;
			_values[num2 + 2] = color.R;
			_values[num2 + 3] = color.A;
			break;
		case 24:
			_values[num2] = color.B;
			_values[num2 + 1] = color.G;
			_values[num2 + 2] = color.R;
			break;
		case 8:
			_values[num2] = color.B;
			break;
		}
		IsModify = true;
	}

	public void Dispose()
	{
		_bitmap?.Dispose();
		UnlockBits();
	}
}
