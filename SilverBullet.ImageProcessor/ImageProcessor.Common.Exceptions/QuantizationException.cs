using System;
using System.Runtime.Serialization;

namespace ImageProcessor.Common.Exceptions;

[Serializable]
public class QuantizationException : Exception
{
	public QuantizationException(string message)
		: base(message)
	{
	}

	public QuantizationException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	public QuantizationException()
	{
	}

	private QuantizationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
