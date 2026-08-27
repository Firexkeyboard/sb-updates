using System.ComponentModel;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace OpenBullet;

public class SortAdorner : Adorner
{
	private static Geometry ascGeometry = Geometry.Parse("M 0 4 L 3.5 0 L 7 4 Z");

	private static Geometry descGeometry = Geometry.Parse("M 0 0 L 3.5 4 L 7 0 Z");

	public ListSortDirection Direction { get; private set; }

	public SortAdorner(UIElement element, ListSortDirection dir)
		: base(element)
	{
		Direction = dir;
	}

	protected override void OnRender(DrawingContext drawingContext)
	{
		base.OnRender(drawingContext);
		if (!(base.AdornedElement.RenderSize.Width < 20.0))
		{
			TranslateTransform transform = new TranslateTransform(base.AdornedElement.RenderSize.Width - 15.0, (base.AdornedElement.RenderSize.Height - 5.0) / 2.0);
			drawingContext.PushTransform(transform);
			Geometry geometry = ascGeometry;
			if (Direction == ListSortDirection.Descending)
			{
				geometry = descGeometry;
			}
			drawingContext.DrawGeometry(Brushes.Black, null, geometry);
			drawingContext.Pop();
		}
	}
}
