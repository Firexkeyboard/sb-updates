namespace ImageProcessor.Imaging.Helpers;

internal sealed class BigEndianBitConverter : EndianBitConverter
{
	public override Endianness Endianness => Endianness.BigEndian;

	public override bool IsLittleEndian()
	{
		return false;
	}

	protected internal override void CopyBytesImpl(long value, int bytes, byte[] buffer, int index)
	{
		int num = index + bytes - 1;
		for (int i = 0; i < bytes; i++)
		{
			buffer[num - i] = (byte)(value & 0xFF);
			value >>= 8;
		}
	}

	protected internal override long FromBytes(byte[] value, int startIndex, int bytesToConvert)
	{
		long num = 0L;
		for (int i = 0; i < bytesToConvert; i++)
		{
			num = (num << 8) | value[startIndex + i];
		}
		return num;
	}
}
