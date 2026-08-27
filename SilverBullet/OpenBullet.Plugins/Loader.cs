using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenBullet.Views.UserControls;
using PluginFramework;
using RuriLib;
using RuriLib.ViewModels;

namespace OpenBullet.Plugins;

public static class Loader
{
	public static (IEnumerable<PluginControl>, IEnumerable<IBlockPlugin>, IEnumerable<string>) LoadPlugins(string folder)
	{
		List<PluginControl> list = new List<PluginControl>();
		List<IBlockPlugin> list2 = new List<IBlockPlugin>();
		List<string> list3 = new List<string>();
		string[] files = Directory.GetFiles(folder, "*.dll");
		foreach (string text in files)
		{
			Assembly assembly = Assembly.LoadFrom(text);
			string text2 = Path.Combine(SB.pluginsFolder, Path.GetFileNameWithoutExtension(text));
			if (Directory.Exists(text2))
			{
				Hook(text2);
			}
			LoadDependencies(assembly.GetReferencedAssemblies());
			Type[] types = assembly.GetTypes();
			foreach (Type type in types)
			{
				if (type.GetInterface("IPlugin") == typeof(IPlugin))
				{
					list3.Add(assembly.GetName().Name);
					list.Add(new PluginControl(type, SB.App, type.GetTypeInfo().IsSubclassOf(typeof(ViewModelBase))));
				}
				else if (type.GetInterface("IBlockPlugin") == typeof(IBlockPlugin) && type.GetTypeInfo().IsSubclassOf(typeof(BlockBase)))
				{
					object obj = Activator.CreateInstance(type);
					list2.Add((IBlockPlugin)((obj is IBlockPlugin) ? obj : null));
					list3.Add(assembly.GetName().Name);
				}
			}
		}
		return (list, list2, list3);
	}

	public static void LoadDependencies(IEnumerable<AssemblyName> assemblies)
	{
		foreach (AssemblyName asm in assemblies)
		{
			if (!AppDomain.CurrentDomain.GetAssemblies().Any((Assembly a) => a.GetName().FullName == asm.FullName))
			{
				try
				{
					AppDomain.CurrentDomain.Load(asm);
					LoadDependencies(Assembly.Load(asm).GetReferencedAssemblies());
				}
				catch
				{
				}
			}
		}
	}

	public static void Hook(params string[] folders)
	{
		AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs args)
		{
			Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((Assembly a) => a.FullName == args.Name);
			if (assembly != null)
			{
				return assembly;
			}
			AssemblyName n = new AssemblyName(args.Name);
			if (n.Name.EndsWith(".xmlserializers", StringComparison.OrdinalIgnoreCase))
			{
				return (Assembly)null;
			}
			if (n.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
			{
				return (Assembly)null;
			}
			string text = null;
			string[] array = folders;
			foreach (string dir in array)
			{
				text = new string[2] { "*.dll", "*.exe" }.SelectMany((string g) => Directory.EnumerateFiles(dir, g)).FirstOrDefault(delegate(string f)
				{
					try
					{
						return n.Name.Equals(AssemblyName.GetAssemblyName(f).Name, StringComparison.OrdinalIgnoreCase);
					}
					catch (BadImageFormatException)
					{
						return false;
					}
					catch (Exception innerException)
					{
						throw new ApplicationException("Error loading assembly " + f, innerException);
					}
				});
				if (text != null)
				{
					return Assembly.LoadFrom(text);
				}
			}
			throw new ApplicationException("Assembly " + args.Name + " not found");
		};
	}
}
