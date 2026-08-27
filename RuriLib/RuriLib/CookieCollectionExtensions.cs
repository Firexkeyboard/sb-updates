using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace RuriLib;

public static class CookieCollectionExtensions
{
	public static IEnumerable<Cookie> GetAllCookies(this CookieContainer c)
	{
		Hashtable hashtable = (Hashtable)c.GetType().GetField("m_domainTable", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(c);
		foreach (DictionaryEntry item in hashtable)
		{
			SortedList sortedList = (SortedList)item.Value.GetType().GetField("m_list", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(item.Value);
			foreach (DictionaryEntry item2 in sortedList)
			{
				CookieCollection cookieCollection = (CookieCollection)item2.Value;
				foreach (Cookie item3 in cookieCollection)
				{
					yield return item3;
				}
			}
		}
	}
}
