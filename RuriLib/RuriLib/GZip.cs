using System.IO;
using System.IO.Compression;
using System.Text;

namespace RuriLib;

public static class GZip
{
	public static byte[] Zip(string str)
	{
		using MemoryStream memoryStream2 = new MemoryStream(Encoding.UTF8.GetBytes(str));
		using MemoryStream memoryStream = new MemoryStream();
		using (GZipStream destination = new GZipStream(memoryStream, CompressionMode.Compress))
		{
			memoryStream2.CopyTo(destination);
		}
		return memoryStream.ToArray();
	}

	public static string Unzip(byte[] bytes)
	{
		using MemoryStream stream = new MemoryStream(bytes);
		using MemoryStream memoryStream = new MemoryStream();
		using (GZipStream gZipStream = new GZipStream(stream, CompressionMode.Decompress))
		{
			gZipStream.CopyTo(memoryStream);
		}
		return Encoding.UTF8.GetString(memoryStream.ToArray());
	}
}
