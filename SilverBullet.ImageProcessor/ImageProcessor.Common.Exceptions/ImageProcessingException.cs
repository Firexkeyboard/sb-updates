using System;
using System.Runtime.Serialization;

namespace ImageProcessor.Common.Exceptions;

[Serializable]
public sealed class ImageProcessingException : Exception
{
	public ImageProcessingException(string message)
		: base(message)
	{
	}

	public ImageProcessingException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	public ImageProcessingException()
	{
	}

	private ImageProcessingException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
