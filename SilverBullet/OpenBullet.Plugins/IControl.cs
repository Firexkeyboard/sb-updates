namespace OpenBullet.Plugins;

public interface IControl
{
	dynamic GetValue();

	void SetValue(dynamic value);
}
