using System.Collections.Generic;

namespace RuriLib.SB.JS;

public class JsEngine
{
	private List<Engine> Engines = new List<Engine>();

	private readonly object createEngineLock = new object();

	private readonly object createEngineDisposedLock = new object();

	private readonly object removeEngineLock = new object();

	public void DisposeEngines()
	{
	}
}
