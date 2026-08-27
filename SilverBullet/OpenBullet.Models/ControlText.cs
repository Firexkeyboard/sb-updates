namespace OpenBullet.Models;

public class ControlText<T>
{
	public T Control { get; private set; }

	public string Text { get; private set; }

	public ControlText(T cType, string text)
	{
		Control = cType;
		Text = text;
	}
}
