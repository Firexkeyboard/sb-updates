using System.Collections.Generic;

namespace RuriLib.Interfaces;

public interface ILogger
{
	IEnumerable<LogEntry> Entries { get; }

	bool Enabled { get; }

	int BufferSize { get; }

	void Log(string message, LogLevel level = LogLevel.Info, bool prompt = false, int timeout = 0);
}
