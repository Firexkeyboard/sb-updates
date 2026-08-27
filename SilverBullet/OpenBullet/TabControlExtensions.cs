using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace OpenBullet;

public static class TabControlExtensions
{
	public static TabItem GetItemByItemName(this IEnumerable<TabItem> tabItems, string name)
	{
		return tabItems.FirstOrDefault((TabItem i) => i.Header?.ToString() == name);
	}

	public static int GetIndexByItemName(this TabControl tabControl, string name)
	{
		return tabControl.Items.IndexOf(tabControl.Items.OfType<TabItem>().GetItemByItemName(name));
	}

	public static int SelectIndexByHeaderName(this TabControl tabControl, string headerName)
	{
		return tabControl.SelectedIndex = tabControl.GetIndexByItemName(headerName);
	}
}
