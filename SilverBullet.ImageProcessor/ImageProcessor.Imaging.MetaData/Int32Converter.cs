using System.Runtime.InteropServices;

namespace ImageProcessor.Imaging.MetaData;

[StructLayout(LayoutKind.Explicit)]
internal readonly struct Int32Converter
{
	[FieldOffset(0)]
	public readonly int Value;

	[FieldOffset(0)]
	public readonly byte Byte1;

	[FieldOffset(1)]
	public readonly byte Byte2;

	[FieldOffset(2)]
	public readonly byte Byte3;

	[FieldOffset(3)]
	public readonly byte Byte4;

	public Int32Converter(int value)
	{
		this = default(Int32Converter);
		Value = value;
	}

	public static implicit operator int(Int32Converter value)
	{
		return value.Value;
	}

	public static implicit operator Int32Converter(int value)
	{
		return new Int32Converter(value);
	}
}
