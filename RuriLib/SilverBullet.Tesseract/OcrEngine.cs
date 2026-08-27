using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Media;
using AngleSharp.Text;
using RuriLib;
using RuriLib.ViewModels;
using Tesseract;

namespace SilverBullet.Tesseract;

public class OcrEngine
{
	private List<Engine> Engines = new List<Engine>();

	private readonly object createEngineLock = new object();

	private readonly object createEngineDisposedLock = new object();

	private readonly object removeEngineLock = new object();

	public TesseractEngine GetOrCreateEngine(BotData data, string lang, EngineMode engineMode)
	{
		if (data.BotsAmount > Engines.Count)
		{
			lock (createEngineLock)
			{
				for (int i = 0; i <= data.BotsAmount - Engines.Count; i++)
				{
					Engines.Add(CreateOcrEngine(data, lang, engineMode));
				}
			}
		}
		lock (removeEngineLock)
		{
			while (Engines.Count > data.BotsAmount)
			{
				int index = Engines.Count - 1;
				try
				{
					Engines[index].Tesseract.Dispose();
				}
				catch
				{
				}
				try
				{
					Engines.RemoveAt(index);
				}
				catch
				{
				}
			}
		}
		if (Engines.Any((Engine e) => e.Tesseract.IsDisposed))
		{
			lock (createEngineDisposedLock)
			{
				try
				{
					List<Engine> list = Engines.Where((Engine e) => e.Tesseract.IsDisposed).ToList();
					if (list != null && list.Count > 0)
					{
						for (int j = 0; j < list.Count; j++)
						{
							int index2 = Engines.IndexOf(list[j]);
							Engines.Remove(list[j]);
							Engines.Insert(index2, CreateOcrEngine(data, lang, engineMode));
						}
					}
				}
				catch
				{
				}
			}
		}
		return Engines[data.BotNumber - 1].Tesseract;
	}

	private Engine CreateOcrEngine(BotData data, string lang, EngineMode engineMode)
	{
		if (!Directory.Exists(".\\tessdata"))
		{
			if (data != null)
			{
				data.Status = BotStatus.ERROR;
				data.Log(new LogEntry("tessdata not found!", Colors.Red));
			}
			throw new DirectoryNotFoundException("tessdata not found!");
		}
		if (!File.Exists(".\\tessdata\\" + lang + ".traineddata"))
		{
			if (data != null)
			{
				data.Status = BotStatus.ERROR;
				data.Log(new LogEntry("Language '" + lang + "' not found!", Colors.Red));
			}
			throw new FileNotFoundException("Language '" + lang + "' not found!");
		}
		Engine engine = new Engine(new TesseractEngine(".\\tessdata", lang, engineMode), lang, engineMode);
		SetVariable(data, engine.Tesseract);
		return engine;
	}

	private bool Dispose(Engine engine)
	{
		try
		{
			engine.Tesseract?.Dispose();
			if (engine.Tesseract != null)
			{
				return engine.Tesseract.IsDisposed;
			}
			return false;
		}
		catch
		{
			if (engine.Tesseract != null)
			{
				return engine.Tesseract.IsDisposed;
			}
			return false;
		}
	}

	public void StopEngines()
	{
		try
		{
			Engines.ForEach(delegate(Engine e)
			{
				try
				{
					e.Tesseract.Dispose();
				}
				catch
				{
				}
			});
		}
		catch
		{
		}
	}

	public void DisposeEngines()
	{
		try
		{
			int num = 0;
			int num2 = 0;
			while (Engines.Count > 0)
			{
				Engine engine = Engines[num];
				if (engine.Tesseract.IsDisposed && num2 < 5)
				{
					num2++;
					Thread.Sleep(50);
					continue;
				}
				try
				{
					Dispose(engine);
				}
				catch
				{
				}
				try
				{
					Engines.RemoveAt(0);
				}
				catch
				{
				}
				num++;
			}
		}
		catch
		{
		}
	}

	private static void SetVariable(BotData data, TesseractEngine tesseract)
	{
		int count = data.GlobalSettings.Ocr.VariableList.Count;
		if (count <= 0)
		{
			return;
		}
		for (int i = 0; i < count; i++)
		{
			try
			{
				TesseractVariable tesseractVariable = data.GlobalSettings.Ocr.VariableList[i];
				string name = tesseractVariable.Name;
				string value = tesseractVariable.Value;
				switch (tesseractVariable.ValueType)
				{
				case VariableValueType.String:
					tesseract.SetVariable(name, value);
					break;
				case VariableValueType.Integer:
					tesseract.SetVariable(name, int.Parse(value));
					break;
				case VariableValueType.Double:
					tesseract.SetVariable(name, double.Parse(value));
					break;
				case VariableValueType.Boolean:
					tesseract.SetVariable(name, value.ToBoolean());
					break;
				}
			}
			catch
			{
			}
		}
	}

	private static bool Equals<T>(List<T> a, List<T> b)
	{
		if (a == null)
		{
			return b == null;
		}
		if (b == null || a.Count != b.Count)
		{
			return false;
		}
		for (int i = 0; i < a.Count; i++)
		{
			if (!object.Equals(a[i], b[i]))
			{
				return false;
			}
		}
		return true;
	}

	~OcrEngine()
	{
		DisposeEngines();
	}
}
