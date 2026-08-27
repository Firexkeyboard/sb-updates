using Tesseract;

namespace SilverBullet.Tesseract;

public class Engine
{
	public TesseractEngine Tesseract { get; private set; }

	public EngineMode EngineMode { get; private set; }

	public string Lang { get; private set; }

	public bool Completed { get; set; }

	public Engine(TesseractEngine tesseract, string lang, EngineMode engineMode)
	{
		Tesseract = tesseract;
		EngineMode = engineMode;
		Lang = lang;
	}
}
