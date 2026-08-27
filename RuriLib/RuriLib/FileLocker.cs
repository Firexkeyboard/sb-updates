using System.Collections;

namespace RuriLib;

public static class FileLocker
{
	public static Hashtable Hashtable = new Hashtable();

	public static object GetLock(string fileName)
	{
		if (!Hashtable.ContainsKey(fileName))
		{
			Hashtable.Add(fileName, new object());
		}
		return Hashtable[fileName];
	}
}
