using System.Collections.Generic;
using System.Linq;
using PluginFramework;
using RuriLib;

namespace OpenBullet;

public static class BlocksExtensions
{
	public static IEnumerable<BlockBase> OnlyPlugins(this IEnumerable<BlockBase> blocks)
	{
		return blocks.Where((BlockBase b) => b.IsPlugin());
	}

	public static bool IsPlugin(this BlockBase block)
	{
		return ((object)block).GetType().GetInterface("IBlockPlugin") == typeof(IBlockPlugin);
	}
}
