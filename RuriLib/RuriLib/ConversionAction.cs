namespace RuriLib;

public enum ConversionAction
{
	Encoding,              // classic From→To encoding pair (existing behavior)
	BigIntegerToByteArray,
	ByteArrayToBigInteger,
	ReadableSize,
	Base64ToBytes,
	Base64ToUtf8,
	BinaryStringToBytes,
	BytesToBase64,
	BytesToBinaryString,
	BytesToHex,
	BytesToString,
	HexToBytes,
	StringToBytes,
	Utf8ToBase64,
}
