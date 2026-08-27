using System.Drawing.Imaging;
using ImageProcessor.Imaging.Formats;

namespace ImageProcessor.Imaging.MetaData;

internal static class ImageFactoryMetaExtensions
{
	private static readonly ExifBitConverter BitConverter = new ExifBitConverter(new ComputerArchitectureInfo());

	public static ImageFactory SetPropertyItem(this ImageFactory imageFactory, ExifPropertyTag id, byte value)
	{
		byte[] array = new byte[1] { value };
		return imageFactory.SetPropertyItem(id, ExifPropertyTagType.Byte, array.Length, array);
	}

	public static ImageFactory SetPropertyItem(this ImageFactory imageFactory, ExifPropertyTag id, string value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		return imageFactory.SetPropertyItem(id, ExifPropertyTagType.ASCII, bytes.Length, bytes);
	}

	public static ImageFactory SetPropertyItem(this ImageFactory imageFactory, ExifPropertyTag id, ushort value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		return imageFactory.SetPropertyItem(id, ExifPropertyTagType.UShort, bytes.Length, bytes);
	}

	public static ImageFactory SetPropertyItem(this ImageFactory imageFactory, ExifPropertyTag id, uint value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		return imageFactory.SetPropertyItem(id, ExifPropertyTagType.ULong, bytes.Length, bytes);
	}

	public static ImageFactory SetPropertyItem(this ImageFactory imageFactory, ExifPropertyTag id, Rational<uint> value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		return imageFactory.SetPropertyItem(id, ExifPropertyTagType.Rational, bytes.Length, bytes);
	}

	public static ImageFactory SetPropertyItem(this ImageFactory imageFactory, ExifPropertyTag id, byte[] value)
	{
		return imageFactory.SetPropertyItem(id, ExifPropertyTagType.Undefined, value.Length, value);
	}

	public static ImageFactory SetPropertyItem(this ImageFactory imageFactory, ExifPropertyTag id, int value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		return imageFactory.SetPropertyItem(id, ExifPropertyTagType.SLong, bytes.Length, bytes);
	}

	public static ImageFactory SetPropertyItem(this ImageFactory imageFactory, ExifPropertyTag id, Rational<int> value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		return imageFactory.SetPropertyItem(id, ExifPropertyTagType.SRational, bytes.Length, bytes);
	}

	private static ImageFactory SetPropertyItem(this ImageFactory imageFactory, ExifPropertyTag id, ExifPropertyTagType type, int length, byte[] value)
	{
		PropertyItem propertyItem = FormatUtilities.CreatePropertyItem();
		propertyItem.Id = (int)id;
		propertyItem.Type = (short)type;
		propertyItem.Len = length;
		propertyItem.Value = value;
		imageFactory.ExifPropertyItems[propertyItem.Id] = propertyItem;
		return imageFactory;
	}
}
