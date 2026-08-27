namespace ImageProcessor.Imaging.Helpers;

internal sealed class LittleEndianBitConverter : EndianBitConverter
{
	public override Endianness Endianness => Endianness.LittleEndian;

	public override bool IsLittleEndian()
	{
		return true;
	}

	protected internal override void CopyBytesImpl(long value, int bytes, byte[] buffer, int index)
	{
		for (int i = 0; i < bytes; i++)
		{
			buffer[i + index] = (byte)(value & 0xFF);
			value >>= 8;
		}
	}

	protected internal override long FromBytes(byte[] value, int startIndex, int bytesToConvert)
	{
		long num = 0L;
		for (int i = 0; i < bytesToConvert; i++)
		{
			num = (num << 8) | value[startIndex + bytesToConvert - 1 - i];
		}
		return num;
	}
}
