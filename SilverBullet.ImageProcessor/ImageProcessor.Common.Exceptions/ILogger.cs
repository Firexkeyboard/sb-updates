using System;
using System.Runtime.CompilerServices;

namespace ImageProcessor.Common.Exceptions;

public interface ILogger
{
	void Log<T>(string text, [CallerMemberName] string callerName = null, [CallerLineNumber] int lineNumber = 0);

	void Log(Type type, string text, [CallerMemberName] string callerName = null, [CallerLineNumber] int lineNumber = 0);
}
