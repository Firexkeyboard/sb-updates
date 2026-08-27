using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using Newtonsoft.Json;
using RuriLib.Models;

namespace RuriLib;

public static class IOManager
{
	private static JsonSerializerSettings settings = new JsonSerializerSettings
	{
		TypeNameHandling = TypeNameHandling.All
	};

	public static void SaveSettings<T>(string settingsFile, T settings)
	{
		File.WriteAllText(settingsFile, JsonConvert.SerializeObject(settings, Formatting.Indented));
	}

	public static T LoadSettings<T>(string settingsFile)
	{
		return JsonConvert.DeserializeObject<T>(File.ReadAllText(settingsFile));
	}

	public static T DeserializeObject<T>(string value)
	{
		return JsonConvert.DeserializeObject<T>(value);
	}

	public static string SerializeBlock(BlockBase block)
	{
		return JsonConvert.SerializeObject(block, Formatting.None, settings);
	}

	public static BlockBase DeserializeBlock(string block)
	{
		return JsonConvert.DeserializeObject<BlockBase>(block, settings);
	}

	public static string SerializeBlocks(List<BlockBase> blocks)
	{
		return JsonConvert.SerializeObject(blocks, Formatting.None, settings);
	}

	public static List<BlockBase> DeserializeBlocks(string blocks)
	{
		return JsonConvert.DeserializeObject<List<BlockBase>>(blocks, settings);
	}

	public static string SerializeConfig(Config config)
	{
		StringWriter stringWriter = new StringWriter();
		stringWriter.WriteLine("[SETTINGS]");
		stringWriter.WriteLine(JsonConvert.SerializeObject(config.Settings, Formatting.Indented));
		stringWriter.WriteLine("");
		stringWriter.WriteLine("[SCRIPT]");
		stringWriter.Write(config.Script);
		return stringWriter.ToString();
	}

	public static Config DeserializeConfig(string config)
	{
		string[] array = config.Split(new string[2] { "[SETTINGS]", "[SCRIPT]" }, StringSplitOptions.RemoveEmptyEntries);
		return new Config(JsonConvert.DeserializeObject<ConfigSettings>(array[0]), array[1].TrimStart('\r', '\n'));
	}

	public static string SerializeProxies(List<CProxy> proxies)
	{
		return JsonConvert.SerializeObject(proxies, Formatting.None);
	}

	public static List<CProxy> DeserializeProxies(string proxies)
	{
		return JsonConvert.DeserializeObject<List<CProxy>>(proxies);
	}

	public static Config LoadConfig(string fileName, bool liloX = false)
	{
		if (liloX)
		{
			return LoadConfigX(fileName);
		}
		return DeserializeConfig(File.ReadAllText(fileName));
	}

	private static Config LoadConfigX(string fileName)
	{
		return DeserializeConfigX(File.ReadAllText(fileName));
	}

	private static Config DeserializeConfigX(string config)
	{
		if (!config.Contains("ID") && !config.Contains("Body"))
		{
			try
			{
				byte[] bytes = Convert.FromBase64String(config);
				config = Encoding.UTF8.GetString(bytes);
				config = DecryptX(Regex.Match(config, "0x;(.*?)x;0").Groups[1].Value, "THISISOBmodedByForlax");
			}
			catch
			{
				config = DecryptX(Regex.Match(config, "0x;(.*?)x;0").Groups[1].Value, "0THISISOBmodedByForlaxNIGGAs");
			}
		}
		else
		{
			byte[] bytes2 = Convert.FromBase64String(BellaCiao(Regex.Match(config, "\"Body\": \"(.*?)\"").Groups[1].Value, 2));
			config = DecryptX(Regex.Match(Encoding.UTF8.GetString(bytes2), "x0;(.*?)0;x").Groups[1].Value, "0THISISOBmodedByForlaxNIGGAs");
		}
		string[] array = config.Split(new string[2] { "[SETTINGS]", "[SCRIPT]" }, StringSplitOptions.RemoveEmptyEntries);
		return new Config(JsonConvert.DeserializeObject<ConfigSettings>(array[0]), array[1].TrimStart('\r', '\n'));
	}

	private static string DecryptX(string cipherText, string passPhrase)
	{
		byte[] array = Convert.FromBase64String(cipherText);
		byte[] salt = array.Take(32).ToArray();
		byte[] rgbIV = array.Skip(32).Take(32).ToArray();
		byte[] array2 = array.Skip(64).Take(array.Length - 64).ToArray();
		using Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(passPhrase, salt, 1000, HashAlgorithmName.SHA1);
		byte[] bytes = rfc2898DeriveBytes.GetBytes(32);
#pragma warning disable SYSLIB0022 // RijndaelManaged is obsolete: Rijndael with 256-bit blocks is not AES-compatible
		using RijndaelManaged rijndaelManaged = new RijndaelManaged();
		rijndaelManaged.BlockSize = 256;
		rijndaelManaged.Mode = CipherMode.CBC;
		rijndaelManaged.Padding = PaddingMode.PKCS7;
		using ICryptoTransform transform = rijndaelManaged.CreateDecryptor(bytes, rgbIV);
		using MemoryStream memoryStream = new MemoryStream(array2);
		using CryptoStream cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Read);
		byte[] array3 = new byte[array2.Length];
		int count = cryptoStream.Read(array3, 0, array3.Length);
		memoryStream.Close();
		cryptoStream.Close();
		return Encoding.UTF8.GetString(array3, 0, count);
#pragma warning restore SYSLIB0022
	}

	public static string BellaCiao(string helpme, int op)
	{
		if (op != 1)
		{
			string text = "ay$a5%&jwrtmnh;lasjdf98787OMGFORLAX";
			string text2 = "abc@98797hjkas$&asd(*$%GJMANIGE";
			byte[] array = new byte[0];
			array = Encoding.UTF8.GetBytes(text2.Substring(0, 8));
			byte[] array2 = new byte[0];
			array2 = Encoding.UTF8.GetBytes(text.Substring(0, 8));
			byte[] array3 = new byte[helpme.Length / 2];
			for (int i = 0; i < array3.Length; i++)
			{
				array3[i] = Convert.ToByte(helpme.Substring(i * 2, 2), 16);
			}
			helpme = Encoding.UTF8.GetString(array3);
			byte[] array4 = new byte[helpme.Replace(" ", "+").Length];
			array4 = Convert.FromBase64String(helpme.Replace(" ", "+"));
			using DES dESCryptoServiceProvider = DES.Create();
			MemoryStream memoryStream = new MemoryStream();
			CryptoStream cryptoStream = new CryptoStream(memoryStream, dESCryptoServiceProvider.CreateDecryptor(array2, array), CryptoStreamMode.Write);
			cryptoStream.Write(array4, 0, array4.Length);
			cryptoStream.FlushFinalBlock();
			return Encoding.UTF8.GetString(memoryStream.ToArray());
		}
		string text3 = "";
		string text4 = "ay$a5%&jwrtmnh;lasjdf98787OMGFORLAX";
		string text5 = "abc@98797hjkas$&asd(*$%GJMANIGE";
		byte[] array5 = new byte[0];
		array5 = Encoding.UTF8.GetBytes(text5.Substring(0, 8));
		byte[] array6 = new byte[0];
		array6 = Encoding.UTF8.GetBytes(text4.Substring(0, 8));
		byte[] bytes = Encoding.UTF8.GetBytes(helpme);
		using DES dESCryptoServiceProvider2 = DES.Create();
		CryptoStream cryptoStream2 = new CryptoStream(new MemoryStream(), dESCryptoServiceProvider2.CreateEncryptor(array6, array5), CryptoStreamMode.Write);
		cryptoStream2.Write(bytes, 0, bytes.Length);
		cryptoStream2.FlushFinalBlock();
		text3 = Convert.ToBase64String(new MemoryStream().ToArray());
		StringBuilder stringBuilder = new StringBuilder();
		byte[] bytes2 = Encoding.UTF8.GetBytes(text3);
		for (int j = 0; j < bytes2.Length; j++)
		{
			stringBuilder.Append(bytes2[j].ToString("X2"));
		}
		return stringBuilder.ToString();
	}

	public static void SaveConfig(Config config, string fileName)
	{
		File.WriteAllText(fileName, SerializeConfig(config));
	}

	public static Config CloneConfig(Config config)
	{
		return DeserializeConfig(SerializeConfig(config));
	}

	public static BlockBase CloneBlock(BlockBase block)
	{
		return DeserializeBlock(SerializeBlock(block));
	}

	public static List<CProxy> CloneProxies(List<CProxy> proxies)
	{
		return DeserializeProxies(SerializeProxies(proxies));
	}

	public static EnvironmentSettings ParseEnvironmentSettings(string envFile)
	{
		EnvironmentSettings environmentSettings = new EnvironmentSettings();
		string[] array = (from l in File.ReadAllLines(envFile)
			where !string.IsNullOrEmpty(l)
			select l).ToArray();
		for (int i = 0; i < array.Count(); i++)
		{
			string text = array[i];
			if (text.StartsWith("#") || !text.StartsWith("["))
			{
				continue;
			}
			string text2 = text;
			Type type = text.Trim() switch
			{
				"[WLTYPE]" => typeof(WordlistType), 
				"[CUSTOMKC]" => typeof(CustomKeychain), 
				"[EXPFORMAT]" => typeof(ExportFormat), 
				_ => throw new Exception("Unrecognized ini header"), 
			};
			List<string> list = new List<string>();
			int j;
			for (j = i + 1; j < array.Count(); j++)
			{
				text = array[j];
				if (text.StartsWith("["))
				{
					break;
				}
				if (!(text.Trim() == string.Empty) && !text.StartsWith("#"))
				{
					list.Add(text);
				}
			}
			switch (text2)
			{
			case "[WLTYPE]":
				environmentSettings.WordlistTypes.Add((WordlistType)ParseObjectFromIni(type, list));
				break;
			case "[CUSTOMKC]":
				environmentSettings.CustomKeychains.Add((CustomKeychain)ParseObjectFromIni(type, list));
				break;
			case "[EXPFORMAT]":
				environmentSettings.ExportFormats.Add((ExportFormat)ParseObjectFromIni(type, list));
				break;
			}
			i = j - 1;
		}
		return environmentSettings;
	}

	private static object ParseObjectFromIni(Type type, List<string> parameters)
	{
		object obj = Activator.CreateInstance(type);
		foreach (string[] item in from p in parameters
			where !string.IsNullOrEmpty(p)
			select p.Split(new char[1] { '=' }, 2))
		{
			PropertyInfo property = type.GetProperty(item[0]);
			object value = property.GetValue(obj);
			dynamic val = null;
			if (!(value is string))
			{
				if (value is int)
				{
					_ = (int)value;
					val = int.Parse(item[1]);
				}
				else if (value is bool)
				{
					_ = (bool)value;
					val = bool.Parse(item[1]);
				}
				else if (!(value is List<string>))
				{
					if (value is System.Windows.Media.Color)
					{
						_ = (System.Windows.Media.Color)value;
						val = System.Windows.Media.Color.FromRgb(System.Drawing.Color.FromName(item[1]).R, System.Drawing.Color.FromName(item[1]).G, System.Drawing.Color.FromName(item[1]).B);
					}
				}
				else
				{
					val = item[1].Split(',').ToList();
				}
			}
			else
			{
				val = item[1];
			}
			property.SetValue(obj, val);
		}
		return obj;
	}
}
