using System;

namespace RuriLib.LS;

public class BlockProcessingException : Exception
{
	public BlockProcessingException()
	{
	}

	public BlockProcessingException(string message)
		: base(message)
	{
	}

	public BlockProcessingException(string message, Exception inner)
		: base(message, inner)
	{
	}
}
