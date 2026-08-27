using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace OpenBullet;

public static class AnimationExtensions
{
	public static void BlurApply(this UIElement element, double from, double to, TimeSpan duration, bool autoReverse = false)
	{
		Storyboard storyboard = new Storyboard();
		DoubleAnimation value = new DoubleAnimation
		{
			From = from,
			To = to,
			Duration = duration,
			AutoReverse = autoReverse
		};
		BlurEffect effect = new BlurEffect();
		element.Effect = effect;
		storyboard.Children.Add(value);
		Storyboard.SetTarget(storyboard, element.Effect);
		Storyboard.SetTargetProperty(storyboard, new PropertyPath("Radius"));
		storyboard.Begin();
	}

	public static void BlurDisable(this UIElement element, TimeSpan duration)
	{
		if (element.Effect is BlurEffect { Radius: not 0.0 } blurEffect)
		{
			DoubleAnimation animation = new DoubleAnimation(blurEffect.Radius, 0.0, duration);
			blurEffect.BeginAnimation(BlurEffect.RadiusProperty, animation);
		}
	}
}
