using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading;
using ImageProcessor.Common.Extensions;

namespace ImageProcessor.Common.Helpers;

internal static class TypeFinder
{
	private static readonly HashSet<Assembly> LocalFilteredAssemblyCache = new HashSet<Assembly>();

	private static readonly ReaderWriterLockSlim LocalFilteredAssemblyCacheLocker = new ReaderWriterLockSlim();

	private static readonly ReaderWriterLockSlim Locker = new ReaderWriterLockSlim();

	private static readonly string[] KnownAssemblyExclusionFilter = new string[42]
	{
		"mscorlib,", "System.", "Antlr3.", "Autofac.", "Autofac,", "Castle.", "ClientDependency.", "DataAnnotationsExtensions.", "Dynamic,", "HtmlDiff,",
		"Iesi.Collections,", "log4net,", "Microsoft.", "Newtonsoft.", "NHibernate.", "NHibernate,", "NuGet.", "RouteDebugger,", "SqlCE4Umbraco,", "Lucene.",
		"Examine,", "AutoMapper.", "Examine.", "ServiceStack.", "MySql.", "HtmlAgilityPack.", "TidyNet.", "ICSharpCode.", "CookComputing.", "AzureDirectory,",
		"itextsharp,", "UrlRewritingNet.", "HtmlAgilityPack,", "MiniProfiler,", "Moq,", "nunit.", "TidyNet,", "WebDriver,", "DevExpress.", "Telerik.",
		"Google.", "ImageProcessor,"
	};

	private static HashSet<Assembly> allAssemblies;

	private static HashSet<Assembly> binFolderAssemblies;

	internal static HashSet<Assembly> GetAllAssemblies()
	{
		using UpgradeableReadLock upgradeableReadLock = new UpgradeableReadLock(Locker);
		if (allAssemblies == null)
		{
			upgradeableReadLock.UpgradeToWriteLock();
			try
			{
				List<string> list = Directory.GetFiles(IOHelper.GetRootDirectoryBinFolder(), "*.dll", SearchOption.TopDirectoryOnly).ToList();
				HashSet<Assembly> hashSet = new HashSet<Assembly>();
				foreach (string item3 in list)
				{
					try
					{
						AssemblyName assemblyName = AssemblyName.GetAssemblyName(item3);
						hashSet.Add(Assembly.Load(assemblyName));
					}
					catch (Exception ex)
					{
						if (!(ex is SecurityException) && !(ex is BadImageFormatException) && !(ex is IOException))
						{
							throw;
						}
					}
				}
				if (hashSet.Count == 0)
				{
					Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
					foreach (Assembly item in assemblies)
					{
						hashSet.Add(item);
					}
				}
				string[] source = new string[2] { ".cs", ".vb" };
				DirectoryInfo appCodeFolder = new DirectoryInfo(IOHelper.MapPath("~/App_code"));
				if (appCodeFolder.Exists && source.Any((string x) => appCodeFolder.GetFiles("*" + x).Length != 0))
				{
					Assembly item2 = Assembly.Load("App_Code");
					if (!hashSet.Contains(item2))
					{
						hashSet.Add(item2);
					}
				}
				allAssemblies = new HashSet<Assembly>(hashSet);
			}
			catch (InvalidOperationException ex2)
			{
				if (!(ex2.InnerException is SecurityException))
				{
					throw;
				}
				binFolderAssemblies = allAssemblies;
			}
		}
		return allAssemblies;
	}

	internal static HashSet<Assembly> GetBinAssemblies()
	{
		if (binFolderAssemblies == null)
		{
			using (new WriteLock(Locker))
			{
				Assembly[] array = GetAssembliesWithKnownExclusions().ToArray();
				IEnumerable<AssemblyName> enumerable = Directory.GetFiles(Assembly.GetExecutingAssembly().GetAssemblyFile().Directory.FullName, "*.dll", SearchOption.TopDirectoryOnly).ToList().Select(AssemblyName.GetAssemblyName);
				HashSet<Assembly> hashSet = new HashSet<Assembly>();
				HashSet<Assembly> hashSet2 = new HashSet<Assembly>();
				Assembly[] array2 = array;
				foreach (Assembly item in array2)
				{
					hashSet.Add(item);
				}
				foreach (AssemblyName assemblyName in enumerable)
				{
					Assembly assembly = hashSet.FirstOrDefault((Assembly a) => a.GetAssemblyFile() == assemblyName.GetAssemblyFile());
					if (assembly != null)
					{
						hashSet2.Add(assembly);
					}
				}
				binFolderAssemblies = new HashSet<Assembly>(hashSet2);
			}
		}
		return binFolderAssemblies;
	}

	internal static HashSet<Assembly> GetAssembliesWithKnownExclusions(IEnumerable<Assembly> excludeFromResults = null)
	{
		using UpgradeableReadLock upgradeableReadLock = new UpgradeableReadLock(LocalFilteredAssemblyCacheLocker);
		if (LocalFilteredAssemblyCache.Count > 0)
		{
			return LocalFilteredAssemblyCache;
		}
		upgradeableReadLock.UpgradeToWriteLock();
		foreach (Assembly filteredAssembly in GetFilteredAssemblies(excludeFromResults, KnownAssemblyExclusionFilter))
		{
			LocalFilteredAssemblyCache.Add(filteredAssembly);
		}
		return LocalFilteredAssemblyCache;
	}

	private static IEnumerable<Assembly> GetFilteredAssemblies(IEnumerable<Assembly> excludeFromResults = null, string[] exclusionFilter = null)
	{
		if (excludeFromResults == null)
		{
			excludeFromResults = new HashSet<Assembly>();
		}
		if (exclusionFilter == null)
		{
			exclusionFilter = new string[0];
		}
		return from x in GetAllAssemblies()
			where !excludeFromResults.Contains(x) && !x.GlobalAssemblyCache && !exclusionFilter.Any((string f) => x.FullName.StartsWith(f, StringComparison.OrdinalIgnoreCase))
			select x;
	}
}
