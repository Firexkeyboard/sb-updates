using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace ImageProcessor.Imaging.Formats;

public class GifEncoder
{
	private const byte ApplicationBlockSize = 11;

	private const int ApplicationExtensionBlockIdentifier = 65313;

	private const string ApplicationIdentification = "NETSCAPE2.0";

	private const byte FileTrailer = 59;

	private const string FileType = "GIF";

	private const string FileVersion = "89a";

	private const int GraphicControlExtensionBlockIdentifier = 63777;

	private const byte GraphicControlExtensionBlockSize = 4;

	private const long SourceColorBlockLength = 768L;

	private const long SourceColorBlockPosition = 13L;

	private const long SourceGlobalColorInfoPosition = 10L;

	private const long SourceGraphicControlExtensionLength = 8L;

	private const long SourceGraphicControlExtensionPosition = 781L;

	private const long SourceImageBlockHeaderLength = 11L;

	private const long SourceImageBlockPosition = 789L;

	private static readonly ImageConverter Converter = new ImageConverter();

	private readonly MemoryStream imageStream;

	private int? height;

	private bool isFirstImage = true;

	private int? repeatCount;

	private int? width;

	private bool terminated;

	public byte[] ImageBytes { get; set; }

	public GifEncoder(int? width = null, int? height = null, int? repeatCount = null)
	{
		imageStream = new MemoryStream();
		this.width = width;
		this.height = height;
		this.repeatCount = repeatCount;
	}

	public void AddFrame(GifFrame frame)
	{
		using (MemoryStream memoryStream = new MemoryStream())
		{
			frame.Image.Save(memoryStream, ImageFormat.Gif);
			if (isFirstImage)
			{
				WriteHeaderBlock(memoryStream, frame.Image.Width, frame.Image.Height);
			}
			WriteGraphicControlBlock(memoryStream, Convert.ToInt32(frame.Delay.TotalMilliseconds / 10.0));
			WriteImageBlock(memoryStream, !isFirstImage, frame.X, frame.Y, frame.Image.Width, frame.Image.Height);
		}
		isFirstImage = false;
	}

	public Image Save()
	{
		if (!terminated)
		{
			WriteByte(59);
			terminated = true;
		}
		imageStream.Flush();
		imageStream.Position = 0L;
		ImageBytes = imageStream.ToArray();
		imageStream.Dispose();
		return (Image)Converter.ConvertFrom(ImageBytes);
	}

	public void Save(Stream stream)
	{
		if (!terminated)
		{
			WriteByte(59);
			terminated = true;
		}
		if (stream.CanSeek)
		{
			stream.Position = 0L;
		}
		imageStream.Flush();
		imageStream.Position = 0L;
		imageStream.CopyTo(stream);
		imageStream.Position = 0L;
	}

	private void WriteHeaderBlock(Stream sourceGif, int w, int h)
	{
		WriteString("GIF");
		WriteString("89a");
		WriteShort(width ?? w);
		WriteShort(height ?? h);
		sourceGif.Position = 10L;
		WriteByte(sourceGif.ReadByte());
		WriteByte(255);
		WriteByte(0);
		WriteColorTable(sourceGif);
		int num = repeatCount ?? 0;
		if (num != 1)
		{
			num = Math.Max(0, num - 1);
			WriteShort(65313);
			WriteByte(11);
			WriteString("NETSCAPE2.0");
			WriteByte(3);
			WriteByte(1);
			WriteShort(num);
			WriteByte(0);
		}
	}

	private void WriteByte(int value)
	{
		imageStream.WriteByte(Convert.ToByte(value));
	}

	private void WriteColorTable(Stream sourceGif)
	{
		sourceGif.Position = 13L;
		byte[] array = new byte[768];
		sourceGif.Read(array, 0, array.Length);
		imageStream.Write(array, 0, array.Length);
	}

	private void WriteGraphicControlBlock(Stream sourceGif, int frameDelay)
	{
		sourceGif.Position = 781L;
		byte[] array = new byte[8];
		sourceGif.Read(array, 0, array.Length);
		WriteShort(63777);
		WriteByte(4);
		WriteByte((array[3] & 0xF7) | 8);
		WriteShort(frameDelay);
		WriteByte(255);
		WriteByte(0);
	}

	private void WriteImageBlock(Stream sourceGif, bool includeColorTable, int x, int y, int h, int w)
	{
		sourceGif.Position = 789L;
		byte[] array = new byte[11];
		sourceGif.Read(array, 0, array.Length);
		WriteByte(array[0]);
		WriteShort(x);
		WriteShort(y);
		WriteShort(h);
		WriteShort(w);
		if (includeColorTable)
		{
			sourceGif.Position = 10L;
			WriteByte((sourceGif.ReadByte() & 0x3F) | 0x80);
			WriteColorTable(sourceGif);
		}
		else
		{
			WriteByte((array[9] & 7) | 7);
		}
		WriteByte(array[10]);
		sourceGif.Position = 800L;
		for (int num = sourceGif.ReadByte(); num > 0; num = sourceGif.ReadByte())
		{
			byte[] buffer = new byte[num];
			sourceGif.Read(buffer, 0, num);
			imageStream.WriteByte(Convert.ToByte(num));
			imageStream.Write(buffer, 0, num);
		}
		imageStream.WriteByte(0);
	}

	private void WriteShort(int value)
	{
		imageStream.WriteByte(Convert.ToByte(value & 0xFF));
		imageStream.WriteByte(Convert.ToByte((value >> 8) & 0xFF));
	}

	private void WriteString(string value)
	{
		imageStream.Write((from c in value.ToArray()
			select (byte)c).ToArray(), 0, value.Length);
	}
}
