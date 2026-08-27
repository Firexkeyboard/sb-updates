using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using CryptSharp.Utility;
using HashLib;
using Org.BouncyCastle.Crypto.Digests;
using Scrypt;

namespace RuriLib.Functions.Crypto;

public static class Crypto
{
	private static readonly RandomNumberGenerator _saltGenerator = RandomNumberGenerator.Create();

	public static byte[] MD2(byte[] input)
	{
		return HashFactory.Crypto.CreateMD2().ComputeBytes(input).GetBytes();
	}

	public static byte[] MD4(byte[] input)
	{
		return HashFactory.Crypto.CreateMD4().ComputeBytes(input).GetBytes();
	}

	public static byte[] MD5(byte[] input)
	{
		using MD5 mD = System.Security.Cryptography.MD5.Create();
		return mD.ComputeHash(input);
	}

	public static byte[] HMACMD5(byte[] input, byte[] key)
	{
		using HMACMD5 hMACMD = new HMACMD5(key);
		return hMACMD.ComputeHash(input);
	}

	public static byte[] SHA1(byte[] input)
	{
		using var sha = System.Security.Cryptography.SHA1.Create();
		return sha.ComputeHash(input);
	}

	public static byte[] HMACSHA1(byte[] input, byte[] key)
	{
		using HMACSHA1 hMACSHA = new HMACSHA1(key);
		return hMACSHA.ComputeHash(input);
	}

	public static byte[] SHA256(byte[] input)
	{
		using var sha = System.Security.Cryptography.SHA256.Create();
		return sha.ComputeHash(input);
	}

	public static byte[] HMACSHA256(byte[] input, byte[] key)
	{
		using HMACSHA256 hMACSHA = new HMACSHA256(key);
		return hMACSHA.ComputeHash(input);
	}

	public static byte[] SHA384(byte[] input)
	{
		using var sha = System.Security.Cryptography.SHA384.Create();
		return sha.ComputeHash(input);
	}

	public static byte[] HMACSHA384(byte[] input, byte[] key)
	{
		using HMACSHA384 hMACSHA = new HMACSHA384(key);
		return hMACSHA.ComputeHash(input);
	}

	public static byte[] SHA512(byte[] input)
	{
		using var sha = System.Security.Cryptography.SHA512.Create();
		return sha.ComputeHash(input);
	}

	public static byte[] HMACSHA512(byte[] input, byte[] key)
	{
		using HMACSHA512 hMACSHA = new HMACSHA512(key);
		return hMACSHA.ComputeHash(input);
	}

	public static byte[] SHA3_224(byte[] input)
	{
		var d = new Sha3Digest(224);
		d.BlockUpdate(input, 0, input.Length);
		byte[] r = new byte[d.GetDigestSize()];
		d.DoFinal(r, 0);
		return r;
	}

	public static byte[] SHA3_256(byte[] input)
	{
		var d = new Sha3Digest(256);
		d.BlockUpdate(input, 0, input.Length);
		byte[] r = new byte[d.GetDigestSize()];
		d.DoFinal(r, 0);
		return r;
	}

	public static byte[] SHA3_384(byte[] input)
	{
		var d = new Sha3Digest(384);
		d.BlockUpdate(input, 0, input.Length);
		byte[] r = new byte[d.GetDigestSize()];
		d.DoFinal(r, 0);
		return r;
	}

	public static byte[] SHA3_512(byte[] input)
	{
		var d = new Sha3Digest(512);
		d.BlockUpdate(input, 0, input.Length);
		byte[] r = new byte[d.GetDigestSize()];
		d.DoFinal(r, 0);
		return r;
	}

	public static string ToHex(this byte[] bytes)
	{
		StringBuilder stringBuilder = new StringBuilder(bytes.Length * 2);
		foreach (byte b in bytes)
		{
			stringBuilder.Append(b.ToString("X2"));
		}
		return stringBuilder.ToString();
	}

	public static byte[] FromHex(this string input)
	{
		byte[] array = new byte[input.Length / 2];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = Convert.ToByte(input.Substring(i * 2, 2), 16);
		}
		return array;
	}

	public static HashAlgorithmName ToHashAlgorithmName(this Hash type)
	{
		return type switch
		{
			Hash.MD5 => HashAlgorithmName.MD5, 
			Hash.SHA1 => HashAlgorithmName.SHA1, 
			Hash.SHA256 => HashAlgorithmName.SHA256, 
			Hash.SHA384 => HashAlgorithmName.SHA384, 
			Hash.SHA512 => HashAlgorithmName.SHA512, 
			_ => throw new NotSupportedException("No such algorithm name"), 
		};
	}

	private static string RSAEncrypt(string dataToEncrypt, RSAParameters RSAKeyInfo, bool doOAEPPadding)
	{
		RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
		rSACryptoServiceProvider.ImportParameters(RSAKeyInfo);
		return Convert.ToBase64String(rSACryptoServiceProvider.Encrypt(Convert.FromBase64String(dataToEncrypt), doOAEPPadding));
	}

	private static string RSADecrypt(string dataToDecrypt, RSAParameters RSAKeyInfo, bool doOAEPPadding)
	{
		RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
		rSACryptoServiceProvider.ImportParameters(RSAKeyInfo);
		return Convert.ToBase64String(rSACryptoServiceProvider.Decrypt(Convert.FromBase64String(dataToDecrypt), doOAEPPadding));
	}

	public static string RSAEncrypt(string data, string n, string e, bool oaep)
	{
		return RSAEncrypt(data, new RSAParameters
		{
			Modulus = Convert.FromBase64String(n),
			Exponent = Convert.FromBase64String(e)
		}, oaep);
	}

	public static string RSADecrypt(string data, string n, string d, bool oaep)
	{
		return RSADecrypt(data, new RSAParameters
		{
			D = Convert.FromBase64String(d),
			Modulus = Convert.FromBase64String(n)
		}, oaep);
	}

	public static string RSAPkcs1Pad2(string message, string modulus, string exponent)
	{
		BigInteger modulus2 = HexToBigInteger(modulus);
		BigInteger exponent2 = HexToBigInteger(exponent);
		int keyByteLen = modulus.Length / 2; // derive key size from hex modulus length
		BigInteger value = Pkcs1Pad2(message, keyByteLen);
		byte[] raw = BigInteger.ModPow(value, exponent2, modulus2)
			.ToByteArray(isUnsigned: true, isBigEndian: true);
		if (raw.Length < keyByteLen)
		{
			byte[] padded = new byte[keyByteLen];
			Array.Copy(raw, 0, padded, keyByteLen - raw.Length, raw.Length);
			return Convert.ToBase64String(padded);
		}
		return Convert.ToBase64String(raw);
	}

	private static BigInteger HexToBigInteger(string hex)
	{
		return BigInteger.Parse("00" + hex, NumberStyles.AllowHexSpecifier);
	}

	private static BigInteger Pkcs1Pad2(string data, int keySize)
	{
		if (keySize < data.Length + 11)
		{
			return default(BigInteger);
		}
		byte[] array = new byte[keySize];
		int num = data.Length - 1;
		while (num >= 0 && keySize > 0)
		{
			array[--keySize] = (byte)data[num--];
		}
		array[--keySize] = 0;
		var _rngBuf = new byte[1];
		while (keySize > 2)
		{
			do { _saltGenerator.GetBytes(_rngBuf); } while (_rngBuf[0] == 0);
			array[--keySize] = _rngBuf[0];
		}
		array[--keySize] = 2;
		array[--keySize] = 0;
		Array.Reverse(array);
		return new BigInteger(array);
	}

	public static string PBKDF2PKCS5(string password, string salt, int saltSize = 8, int iterations = 1, int keyLength = 16, Hash type = Hash.SHA1)
	{
		if (salt != string.Empty)
		{
			using (Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, Convert.FromBase64String(salt), iterations, type.ToHashAlgorithmName()))
			{
				return Convert.ToBase64String(rfc2898DeriveBytes.GetBytes(keyLength));
			}
		}
		using Rfc2898DeriveBytes rfc2898DeriveBytes2 = new Rfc2898DeriveBytes(password, saltSize, iterations, type.ToHashAlgorithmName());
		return Convert.ToBase64String(rfc2898DeriveBytes2.GetBytes(keyLength));
	}

	public static string AESEncrypt(string data, string key, string iv = "", CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.None, bool hexKeys = false)
	{
		byte[][] array = ConvertKeys(key, iv, hexKeys);
		return EncryptStringToBytes_Aes(data, array[0], array[1], mode, padding);
	}

	public static string AESDecrypt(string data, string key, string iv = "", CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.None, bool hexKeys = false)
	{
		byte[][] array = ConvertKeys(key, iv, hexKeys);
		return DecryptStringFromBytes_Aes(data, array[0], array[1], mode, padding);
	}

	private static byte[][] ConvertKeys(string key, string iv, bool hexKeys)
	{
		byte[][] array = new byte[2][];
		if (hexKeys)
		{
			array[0] = key.FromHex();
			if (string.IsNullOrEmpty(iv))
			{
				array[1] = key.FromHex();
				Array.Resize(ref array[1], 16);
			}
			else
			{
				array[1] = iv.FromHex();
			}
			return array;
		}
		array[0] = Convert.FromBase64String(key);
		if (string.IsNullOrEmpty(iv))
		{
			array[1] = Convert.FromBase64String(key);
			Array.Resize(ref array[1], 16);
		}
		else
		{
			array[1] = Convert.FromBase64String(iv);
		}
		return array;
	}

	private static string EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV, CipherMode mode, PaddingMode padding)
	{
		if (plainText == null || plainText.Length <= 0)
		{
			throw new ArgumentNullException("plainText");
		}
		if (Key == null || Key.Length == 0)
		{
			throw new ArgumentNullException("Key");
		}
		if (IV == null || IV.Length == 0)
		{
			throw new ArgumentNullException("IV");
		}
		byte[] inArray;
		using (Aes aesEnc = Aes.Create())
		{
			aesEnc.KeySize = 256;
			aesEnc.BlockSize = 128;
			aesEnc.Key = Key;
			aesEnc.IV = IV;
			aesEnc.Mode = mode;
			aesEnc.Padding = padding;
			ICryptoTransform transform = aesEnc.CreateEncryptor(aesEnc.Key, aesEnc.IV);
			using MemoryStream memoryStream = new MemoryStream();
			using CryptoStream stream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write);
			using (StreamWriter streamWriter = new StreamWriter(stream))
			{
				streamWriter.Write(plainText);
			}
			inArray = memoryStream.ToArray();
		}
		return Convert.ToBase64String(inArray);
	}

	private static string DecryptStringFromBytes_Aes(string cipherTextString, byte[] Key, byte[] IV, CipherMode mode, PaddingMode padding)
	{
		byte[] array = Convert.FromBase64String(cipherTextString);
		if (array == null || array.Length == 0)
		{
			throw new ArgumentNullException("cipherText");
		}
		if (Key == null || Key.Length == 0)
		{
			throw new ArgumentNullException("Key");
		}
		if (IV == null || IV.Length == 0)
		{
			throw new ArgumentNullException("IV");
		}
		using Aes aes = Aes.Create();
		aes.KeySize = 256;
		aes.BlockSize = 128;
		aes.Key = Key;
		aes.IV = IV;
		aes.Mode = mode;
		aes.Padding = padding;
		ICryptoTransform transform = aes.CreateDecryptor(aes.Key, aes.IV);
		using MemoryStream stream = new MemoryStream(array);
		using CryptoStream stream2 = new CryptoStream(stream, transform, CryptoStreamMode.Read);
		using StreamReader streamReader = new StreamReader(stream2);
		return streamReader.ReadToEnd();
	}

	public static string ScryptEncoder(string input, string salt = "", int cost = 1024, int blockSize = 1, int parallel = 1, int outputLength = 16, bool base64Output = false)
	{
		byte[] array = null;
		if (string.IsNullOrWhiteSpace(salt))
		{
			byte[] array2 = new byte[32];
			_saltGenerator.GetBytes(array2);
			array = array2;
		}
		byte[] bytes = Encoding.UTF8.GetBytes(input);
		if (array == null)
		{
			array = Encoding.UTF8.GetBytes(salt);
		}
		byte[] array3 = SCrypt.ComputeDerivedKey(bytes, array, cost, blockSize, parallel, null, outputLength);
		if (!base64Output)
		{
			return BitConverter.ToString(array3).Replace("-", "").ToLower();
		}
		return Convert.ToBase64String(array3);
	}

	public static bool ScryptCompare(string input, string hashedPassword)
	{
		try
		{
			return new ScryptEncoder().Compare(input, hashedPassword);
		}
		catch
		{
			return false;
		}
	}

	public static bool ScryptIsValid(string hashedPassword)
	{
		return new ScryptEncoder().IsValid(hashedPassword);
	}

	public static string BcryptEncoder(string input, int? workFactor, string salt = "")
	{
		if (workFactor.HasValue && string.IsNullOrEmpty(salt))
		{
			return BCrypt.Net.BCrypt.HashPassword(input, workFactor.Value);
		}
		if (!workFactor.HasValue && !string.IsNullOrEmpty(salt))
		{
			return BCrypt.Net.BCrypt.HashPassword(input, salt);
		}
		return BCrypt.Net.BCrypt.HashPassword(input);
	}

	public static bool BcryptVerify(string input, string hash)
	{
		return BCrypt.Net.BCrypt.Verify(input, hash);
	}

	public static string BcryptGenerateSalt(int? workFactor)
	{
		if (!workFactor.HasValue)
		{
			return BCrypt.Net.BCrypt.GenerateSalt();
		}
		return BCrypt.Net.BCrypt.GenerateSalt(workFactor.Value);
	}
}
