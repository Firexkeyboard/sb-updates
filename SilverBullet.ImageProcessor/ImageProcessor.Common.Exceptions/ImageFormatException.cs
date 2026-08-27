using System;
using System.Runtime.Serialization;

namespace ImageProcessor.Common.Exceptions;

[Serializable]
public sealed class ImageFormatException : Exception
{
	public ImageFormatException(string message)
		: base(message)
	{
	}

	public ImageFormatException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	public ImageFormatException()
	{
	}

	private ImageFormatException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
