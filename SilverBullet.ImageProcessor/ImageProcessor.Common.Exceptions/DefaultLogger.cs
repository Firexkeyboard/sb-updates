#define TRACE
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ImageProcessor.Common.Exceptions;

public class DefaultLogger : ILogger
{
	public void Log<T>(string text, [CallerMemberName] string callerName = null, [CallerLineNumber] int lineNumber = 0)
	{
		LogInternal(typeof(T), text, callerName, lineNumber);
	}

	public void Log(Type type, string text, [CallerMemberName] string callerName = null, [CallerLineNumber] int lineNumber = 0)
	{
		LogInternal(type, text, callerName, lineNumber);
	}

	[Conditional("TRACE")]
	private void LogInternal(Type type, string text, string callerName = null, int lineNumber = 0)
	{
		Trace.WriteLine(string.Format("{0} - {1}: {2} {3}:{4}", DateTime.UtcNow.ToString("s"), type.Name, callerName, lineNumber, text));
	}
}
