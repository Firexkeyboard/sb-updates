using System;
using System.Text;
using ImageProcessor.Imaging.Helpers;

namespace ImageProcessor.Imaging.MetaData;

internal sealed class ExifBitConverter : EndianBitConverter
{
	private readonly IComputerArchitectureInfo computerArchitectureInfo;

	public override Endianness Endianness
	{
		get
		{
			if (!IsLittleEndian())
			{
				return Endianness.BigEndian;
			}
			return Endianness.LittleEndian;
		}
	}

	public ExifBitConverter(IComputerArchitectureInfo computerArchitectureInfo)
	{
		this.computerArchitectureInfo = computerArchitectureInfo;
	}

	public override bool IsLittleEndian()
	{
		return computerArchitectureInfo.IsLittleEndian();
	}

	public byte[] GetBytes(string value, bool addnull)
	{
		if (addnull)
		{
			value += "\0";
		}
		byte[] bytes = Encoding.ASCII.GetBytes(value);
		if (!IsLittleEndian())
		{
			Array.Reverse(bytes);
		}
		return bytes;
	}

	public byte[] GetBytes(string value)
	{
		return GetBytes(value, addnull: false);
	}

	public byte[] GetBytes(Rational<uint> value)
	{
		byte[] bytes = GetBytes(value.Numerator);
		byte[] bytes2 = GetBytes(value.Denominator);
		byte[] array = new byte[8];
		Array.Copy(bytes, 0, array, 0, 4);
		Array.Copy(bytes2, 0, array, 4, 4);
		return array;
	}

	public byte[] GetBytes(Rational<int> value)
	{
		byte[] bytes = GetBytes(value.Numerator);
		byte[] bytes2 = GetBytes(value.Denominator);
		byte[] array = new byte[8];
		Array.Copy(bytes, 0, array, 0, 4);
		Array.Copy(bytes2, 0, array, 4, 4);
		return array;
	}

	protected internal override long FromBytes(byte[] value, int startIndex, int bytesToConvert)
	{
		if (IsLittleEndian())
		{
			return EndianBitConverter.Little.FromBytes(value, startIndex, bytesToConvert);
		}
		return EndianBitConverter.Big.FromBytes(value, startIndex, bytesToConvert);
	}

	protected internal override void CopyBytesImpl(long value, int bytes, byte[] buffer, int index)
	{
		if (IsLittleEndian())
		{
			EndianBitConverter.Little.CopyBytesImpl(value, bytes, buffer, index);
		}
		else
		{
			EndianBitConverter.Big.CopyBytesImpl(value, bytes, buffer, index);
		}
	}
}
