using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Media;
using Humanizer;
using Microsoft.IdentityModel.Tokens;
using RuriLib.Functions.Crypto;
using RuriLib.Functions.EvalString;
using RuriLib.Functions.Formats;
using RuriLib.Functions.NTLM;
using RuriLib.Functions.Time;
using RuriLib.Functions.UserAgent;
using RuriLib.Functions.WordToNum;
using RuriLib.LS;
using RuriLib.Models;

namespace RuriLib;

public enum GuidVersion { V4, V7 }
public enum GuidFormat  { D, N, B, P }

public class BlockFunction : BlockBase
{
	public enum Function
	{
		Constant,
		Base64Encode,
		Base64Decode,
		Hash,
		HMAC,
		Translate,
		CurrentDate,
		CurrentTime,
		DayOfWeek,
		CurrentDay,
		CurrentMonth,
		CurrentYear,
		DateToUnixTime,
		DateToSolar,
		DateToGregorian,
		GetRemainingDay,
		Length,
		ToLowercase,
		ToUppercase,
		ToLetter,
		ToDigit,
		ToLetterOrDigit,
		NumberToWords,
		WordsToNumber,
		Replace,
		RegexMatch,
		URLEncode,
		URLDecode,
		Unescape,
		HTMLEntityEncode,
		HTMLEntityDecode,
		Encoding,
		UnixTimeToDate,
		CurrentUnixTime,
		UnixTimeToISO8601,
		RandomNum,
		RandomString,
		EvaluateMathString,
		Ceil,
		Floor,
		Round,
		Abs,
		Compute,
		CountOccurrences,
		ClearCookies,
		RSAEncrypt,
		RSAPKCS1PAD2,
		Delay,
		CharAt,
		Split,
		Remove,
		Substring,
		ReverseString,
		Trim,
		GetRandomUA,
		AESEncrypt,
		AESDecrypt,
		PBKDF2PKCS5,
		GenerateOAuthVerifier,
		GenerateOAuthChallenge,
		GenerateGUID,
		GenerateBytes,
		Ntlm,
		SCrypt,
		BCrypt,
		MergeByteArrays,
		// String
		RegexReplace,
		// Crypto
		XOR,
		XORStrings,
		RSADecrypt,
		JWTEncode,
		// Math / Float / Int
		MaxFloat,
		MinFloat,
		RandomFloat,
		MaxInt,
		MinInt,
		// Dictionary operations
		AddKeyValuePair,
		GetKey,
		RemoveByKey,
		// List operations
		CreateListOfNumbers,
		IndexOf,
		ListToDict,
		// OB2 standalone crypto
		BCryptVerify,
		ScryptDeriveKey,
		AWS4Signature,
	}

	public enum DateToUnixTimeType
	{
		Seconds,
		Miliseconds
	}

	public enum EncodingMethods
	{
		GetBytes,
		GetString
	}

	public enum ScryptMethods
	{
		Encode,
		Compare,
		IsValid
	}

	public enum BCryptMethods
	{
		Encode,
		GenerateSalt,
		Verify
	}

	private string variableName = "";

	private bool isCapture;

	private string inputString = "";

	private Function functionType;

	private Hash hashType = Hash.SHA512;

	private bool inputBase64;

	private string hmacKey = "";

	private bool hmacBase64;

	private bool keyBase64;

	private bool stopAfterFirstMatch = true;

	private bool useVar;

	private string dateFormat = "yyyy-MM-dd:HH-mm-ss";

	private string replaceWhat = "";

	private string replaceWith = "";

	private bool useRegex;

	private string regexMatch = "";

	private string randomMin = "0";

	private string randomMax = "0";

	private bool randomZeroPad;

	private string stringToFind = "";

	private string rsaN = "";

	private string rsaE = "";

	private string rsaD = "";

	private bool rsaOAEP = true;

	private string charIndex = "0";

	private string separator;

	private int splitIndex = 1;

	private StringSplitOptions stringSplitOption;

	private string removeSIndex;

	private string removeCount;

	private string substringIndex = "0";

	private string substringLength = "1";

	private bool userAgentSpecifyBrowser;

	private UserAgent.Browser userAgentBrowser;

	private string aesKey = "";

	private string aesIV = "";

	private CipherMode aesMode = CipherMode.CBC;

	private PaddingMode aesPadding = PaddingMode.None;

	private bool hexKeys;

	private string kdfSalt = "";

	private int kdfSaltSize = 8;

	private int kdfIterations = 1;

	private int kdfKeySize = 16;

	private Hash kdfAlgorithm = Hash.SHA1;

	private DateToUnixTimeType unixTimeType;

	private object getEncoding;

	private EncodingMethods encFunc;

	private ScryptMethods scryptMeth;

	private string scryptSalt = "";

	private int scryptCost = 1024;

	private int scryptBlockSize = 1;

	private int scryptOutputLength = 16;

	private bool scryptBase64Output;

	private string scryptHashedPassword;

	private BCryptMethods bcryptMeth;

	private bool guidUppercase;
	private GuidVersion guidVersion = GuidVersion.V4;
	private GuidFormat  guidFormat  = GuidFormat.D;

	private string bcryptHashedPassword = "";

	private int bcryptWorkFactor;

	private string bcryptSalt = "";

	private bool useBCryptWorkFactor;

	private string secondInput = "";

	private string thirdInput = "";

	private string jwtAlgorithm = "HS256";

	private string awsRegion = "";

	private string awsService = "";

	private static readonly string _lowercase = "abcdefghijklmnopqrstuvwxyz";

	private static readonly string _uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

	private static readonly string _digits = "0123456789";

	private static readonly string _symbols = "\\!\"£$%&/()=?^'{}[]@#,;.:-_*+";

	private static readonly string _hex = _digits + "abcdef";

	private static readonly string _udChars = _uppercase + _digits;

	private static readonly string _ldChars = _lowercase + _digits;

	private static readonly string _upperlwr = _lowercase + _uppercase;

	private static readonly string _ludChars = _lowercase + _uppercase + _digits;

	private static readonly string _allChars = _lowercase + _uppercase + _digits + _symbols;

	private static readonly NumberStyles _style = NumberStyles.Number | NumberStyles.AllowCurrencySymbol;

	private static readonly IFormatProvider _provider = new CultureInfo("en-US");

	public string VariableName
	{
		get
		{
			return variableName;
		}
		set
		{
			variableName = value;
			OnPropertyChanged("VariableName");
		}
	}

	public bool IsCapture
	{
		get
		{
			return isCapture;
		}
		set
		{
			isCapture = value;
			OnPropertyChanged("IsCapture");
		}
	}

	public string InputString
	{
		get
		{
			return inputString;
		}
		set
		{
			inputString = value;
			OnPropertyChanged("InputString");
		}
	}

	public Function FunctionType
	{
		get
		{
			return functionType;
		}
		set
		{
			functionType = value;
			OnPropertyChanged("FunctionType");
		}
	}

	public Hash HashType
	{
		get
		{
			return hashType;
		}
		set
		{
			hashType = value;
			OnPropertyChanged("HashType");
		}
	}

	public bool InputBase64
	{
		get
		{
			return inputBase64;
		}
		set
		{
			inputBase64 = value;
			OnPropertyChanged("InputBase64");
		}
	}

	public string HmacKey
	{
		get
		{
			return hmacKey;
		}
		set
		{
			hmacKey = value;
			OnPropertyChanged("HmacKey");
		}
	}

	public bool HmacBase64
	{
		get
		{
			return hmacBase64;
		}
		set
		{
			hmacBase64 = value;
			OnPropertyChanged("HmacBase64");
		}
	}

	public bool KeyBase64
	{
		get
		{
			return keyBase64;
		}
		set
		{
			keyBase64 = value;
			OnPropertyChanged("KeyBase64");
		}
	}

	public bool StopAfterFirstMatch
	{
		get
		{
			return stopAfterFirstMatch;
		}
		set
		{
			stopAfterFirstMatch = value;
			OnPropertyChanged("StopAfterFirstMatch");
		}
	}

	public bool UseVar
	{
		get
		{
			return useVar;
		}
		set
		{
			useVar = value;
			OnPropertyChanged("UseVar");
		}
	}

	public Dictionary<string, string> TranslationDictionary { get; set; } = new Dictionary<string, string>();

	public string DateFormat
	{
		get
		{
			return dateFormat;
		}
		set
		{
			dateFormat = value;
			OnPropertyChanged("DateFormat");
		}
	}

	public string ReplaceWhat
	{
		get
		{
			return replaceWhat;
		}
		set
		{
			replaceWhat = value;
			OnPropertyChanged("ReplaceWhat");
		}
	}

	public string ReplaceWith
	{
		get
		{
			return replaceWith;
		}
		set
		{
			replaceWith = value;
			OnPropertyChanged("ReplaceWith");
		}
	}

	public bool UseRegex
	{
		get
		{
			return useRegex;
		}
		set
		{
			useRegex = value;
			OnPropertyChanged("UseRegex");
		}
	}

	public string RegexMatch
	{
		get
		{
			return regexMatch;
		}
		set
		{
			regexMatch = value;
			OnPropertyChanged("RegexMatch");
		}
	}

	public string RandomMin
	{
		get
		{
			return randomMin;
		}
		set
		{
			randomMin = value;
			OnPropertyChanged("RandomMin");
		}
	}

	public string RandomMax
	{
		get
		{
			return randomMax;
		}
		set
		{
			randomMax = value;
			OnPropertyChanged("RandomMax");
		}
	}

	public bool RandomZeroPad
	{
		get
		{
			return randomZeroPad;
		}
		set
		{
			randomZeroPad = value;
			OnPropertyChanged("RandomZeroPad");
		}
	}

	public string StringToFind
	{
		get
		{
			return stringToFind;
		}
		set
		{
			stringToFind = value;
			OnPropertyChanged("StringToFind");
		}
	}

	public string RsaN
	{
		get
		{
			return rsaN;
		}
		set
		{
			rsaN = value;
			OnPropertyChanged("RsaN");
		}
	}

	public string RsaE
	{
		get
		{
			return rsaE;
		}
		set
		{
			rsaE = value;
			OnPropertyChanged("RsaE");
		}
	}

	public string RsaD
	{
		get
		{
			return rsaD;
		}
		set
		{
			rsaD = value;
			OnPropertyChanged("RsaD");
		}
	}

	public bool RsaOAEP
	{
		get
		{
			return rsaOAEP;
		}
		set
		{
			rsaOAEP = value;
			OnPropertyChanged("RsaOAEP");
		}
	}

	public string CharIndex
	{
		get
		{
			return charIndex;
		}
		set
		{
			charIndex = value;
			OnPropertyChanged("CharIndex");
		}
	}

	public string Separator
	{
		get
		{
			return separator;
		}
		set
		{
			separator = value;
			OnPropertyChanged("Separator");
		}
	}

	public int SplitIndex
	{
		get
		{
			return splitIndex;
		}
		set
		{
			splitIndex = value;
			OnPropertyChanged("SplitIndex");
		}
	}

	public StringSplitOptions StringSplitOption
	{
		get
		{
			return stringSplitOption;
		}
		set
		{
			stringSplitOption = value;
			OnPropertyChanged("StringSplitOption");
		}
	}

	public string RemoveSIndex
	{
		get
		{
			return removeSIndex;
		}
		set
		{
			removeSIndex = value;
			OnPropertyChanged("RemoveSIndex");
		}
	}

	public string RemoveCount
	{
		get
		{
			return removeCount;
		}
		set
		{
			removeCount = value;
			OnPropertyChanged("RemoveCount");
		}
	}

	public string SubstringIndex
	{
		get
		{
			return substringIndex;
		}
		set
		{
			substringIndex = value;
			OnPropertyChanged("SubstringIndex");
		}
	}

	public string SubstringLength
	{
		get
		{
			return substringLength;
		}
		set
		{
			substringLength = value;
			OnPropertyChanged("SubstringLength");
		}
	}

	public bool UserAgentSpecifyBrowser
	{
		get
		{
			return userAgentSpecifyBrowser;
		}
		set
		{
			userAgentSpecifyBrowser = value;
			OnPropertyChanged("UserAgentSpecifyBrowser");
		}
	}

	public UserAgent.Browser UserAgentBrowser
	{
		get
		{
			return userAgentBrowser;
		}
		set
		{
			userAgentBrowser = value;
			OnPropertyChanged("UserAgentBrowser");
		}
	}

	public string AesKey
	{
		get
		{
			return aesKey;
		}
		set
		{
			aesKey = value;
			OnPropertyChanged("AesKey");
		}
	}

	public string AesIV
	{
		get
		{
			return aesIV;
		}
		set
		{
			aesIV = value;
			OnPropertyChanged("AesIV");
		}
	}

	public CipherMode AesMode
	{
		get
		{
			return aesMode;
		}
		set
		{
			aesMode = value;
			OnPropertyChanged("AesMode");
		}
	}

	public PaddingMode AesPadding
	{
		get
		{
			return aesPadding;
		}
		set
		{
			aesPadding = value;
			OnPropertyChanged("AesPadding");
		}
	}

	public bool HexKeys
	{
		get
		{
			return hexKeys;
		}
		set
		{
			hexKeys = value;
			OnPropertyChanged("HexKeys");
		}
	}

	public string KdfSalt
	{
		get
		{
			return kdfSalt;
		}
		set
		{
			kdfSalt = value;
			OnPropertyChanged("KdfSalt");
		}
	}

	public int KdfSaltSize
	{
		get
		{
			return kdfSaltSize;
		}
		set
		{
			kdfSaltSize = value;
			OnPropertyChanged("KdfSaltSize");
		}
	}

	public int KdfIterations
	{
		get
		{
			return kdfIterations;
		}
		set
		{
			kdfIterations = value;
			OnPropertyChanged("KdfIterations");
		}
	}

	public int KdfKeySize
	{
		get
		{
			return kdfKeySize;
		}
		set
		{
			kdfKeySize = value;
			OnPropertyChanged("KdfKeySize");
		}
	}

	public Hash KdfAlgorithm
	{
		get
		{
			return kdfAlgorithm;
		}
		set
		{
			kdfAlgorithm = value;
			OnPropertyChanged("KdfAlgorithm");
		}
	}

	public DateToUnixTimeType UnixTimeType
	{
		get
		{
			return unixTimeType;
		}
		set
		{
			unixTimeType = value;
			OnPropertyChanged("UnixTimeType");
		}
	}

	public object GetEncoding
	{
		get
		{
			return getEncoding;
		}
		set
		{
			getEncoding = value;
			OnPropertyChanged("GetEncoding");
		}
	}

	public EncodingMethods EncFunc
	{
		get
		{
			return encFunc;
		}
		set
		{
			encFunc = value;
			OnPropertyChanged("EncFunc");
		}
	}

	public ScryptMethods ScryptMeth
	{
		get
		{
			return scryptMeth;
		}
		set
		{
			scryptMeth = value;
			OnPropertyChanged("ScryptMeth");
		}
	}

	public string ScryptSalt
	{
		get
		{
			return scryptSalt;
		}
		set
		{
			scryptSalt = value;
			OnPropertyChanged("ScryptSalt");
		}
	}

	public int ScryptCost
	{
		get
		{
			return scryptCost;
		}
		set
		{
			scryptCost = value;
			OnPropertyChanged("ScryptCost");
		}
	}

	public int ScryptBlockSize
	{
		get
		{
			return scryptBlockSize;
		}
		set
		{
			scryptBlockSize = value;
			OnPropertyChanged("ScryptBlockSize");
		}
	}

	public int ScryptOutputLength
	{
		get
		{
			return scryptOutputLength;
		}
		set
		{
			scryptOutputLength = value;
			OnPropertyChanged("ScryptOutputLength");
		}
	}

	public bool Base64Output
	{
		get
		{
			return scryptBase64Output;
		}
		set
		{
			scryptBase64Output = value;
			OnPropertyChanged("Base64Output");
		}
	}

	public bool GuidUppercase
	{
		get { return guidUppercase; }
		set { guidUppercase = value; OnPropertyChanged("GuidUppercase"); }
	}

	public GuidVersion GuidVer
	{
		get { return guidVersion; }
		set { guidVersion = value; OnPropertyChanged("GuidVer"); }
	}

	public GuidFormat GuidFmt
	{
		get { return guidFormat; }
		set { guidFormat = value; OnPropertyChanged("GuidFmt"); }
	}

	public string ScryptHashedPassword
	{
		get
		{
			return scryptHashedPassword;
		}
		set
		{
			scryptHashedPassword = value;
			OnPropertyChanged("ScryptHashedPassword");
		}
	}

	public BCryptMethods BCryptMeth
	{
		get
		{
			return bcryptMeth;
		}
		set
		{
			bcryptMeth = value;
			OnPropertyChanged("BCryptMeth");
		}
	}

	public string BCryptHashedPassword
	{
		get
		{
			return bcryptHashedPassword;
		}
		set
		{
			bcryptHashedPassword = value;
			OnPropertyChanged("BCryptHashedPassword");
		}
	}

	public int BCryptWorkFactor
	{
		get
		{
			return bcryptWorkFactor;
		}
		set
		{
			bcryptWorkFactor = value;
			OnPropertyChanged("BCryptWorkFactor");
		}
	}

	public string BCryptSalt
	{
		get
		{
			return bcryptSalt;
		}
		set
		{
			bcryptSalt = value;
			OnPropertyChanged("BCryptSalt");
		}
	}

	public bool UseWorkFactor
	{
		get
		{
			return useBCryptWorkFactor;
		}
		set
		{
			useBCryptWorkFactor = value;
			OnPropertyChanged("UseWorkFactor");
		}
	}

	public string SecondInput
	{
		get => secondInput;
		set { secondInput = value; OnPropertyChanged("SecondInput"); }
	}

	public string ThirdInput
	{
		get => thirdInput;
		set { thirdInput = value; OnPropertyChanged("ThirdInput"); }
	}

	public string JwtAlgorithm
	{
		get => jwtAlgorithm;
		set { jwtAlgorithm = value; OnPropertyChanged("JwtAlgorithm"); }
	}

	public string AwsRegion
	{
		get => awsRegion;
		set { awsRegion = value; OnPropertyChanged("AwsRegion"); }
	}

	public string AwsService
	{
		get => awsService;
		set { awsService = value; OnPropertyChanged("AwsService"); }
	}

	public BlockFunction()
	{
		base.Label = "FUNCTION";
	}

	public override BlockBase FromLS(string line)
	{
		string input = line.Trim();
		if (input.StartsWith("#"))
		{
			base.Label = LineParser.ParseLabel(ref input);
		}
		FunctionType = (Function)LineParser.ParseEnum(ref input, "Function Name", typeof(Function));
		switch (FunctionType)
		{
		case Function.Hash:
			HashType = LineParser.ParseEnum(ref input, "Hash Type", typeof(Hash));
			while (LineParser.Lookahead(ref input) == TokenType.Boolean)
			{
				LineParser.SetBool(ref input, this);
			}
			break;
		case Function.HMAC:
			HashType = LineParser.ParseEnum(ref input, "Hash Type", typeof(Hash));
			HmacKey = LineParser.ParseLiteral(ref input, "HMAC Key");
			while (LineParser.Lookahead(ref input) == TokenType.Boolean)
			{
				LineParser.SetBool(ref input, this);
			}
			break;
		case Function.Translate:
			while (LineParser.Lookahead(ref input) == TokenType.Boolean)
			{
				LineParser.SetBool(ref input, this);
			}
			TranslationDictionary = new Dictionary<string, string>();
			while (input != string.Empty && LineParser.Lookahead(ref input) == TokenType.Parameter)
			{
				LineParser.EnsureIdentifier(ref input, "KEY");
				string key = LineParser.ParseLiteral(ref input, "Key");
				LineParser.EnsureIdentifier(ref input, "VALUE");
				string value = LineParser.ParseLiteral(ref input, "Value");
				TranslationDictionary[key] = value;
			}
			break;
		case Function.DateToUnixTime:
		{
			DateFormat = LineParser.ParseLiteral(ref input, "DATE FORMAT");
			string text2 = input;
			try
			{
				UnixTimeType = LineParser.ParseEnum(ref input, "UnixTimeType", typeof(DateToUnixTimeType));
			}
			catch
			{
				input = text2;
			}
			break;
		}
		case Function.UnixTimeToDate:
		{
			DateFormat = LineParser.ParseLiteral(ref input, "DATE FORMAT");
			if (LineParser.Lookahead(ref input) == TokenType.Literal)
				InputString = LineParser.ParseLiteral(ref input, "INPUT");
			// else: single-arg = only DateFormat given; InputString stays empty (use pipeline value)
			string text = input;
			try
			{
				UnixTimeType = LineParser.ParseEnum(ref input, "UnixTimeType", typeof(DateToUnixTimeType));
			}
			catch
			{
				input = text;
			}
			break;
		}
		case Function.Replace:
			ReplaceWhat = LineParser.ParseLiteral(ref input, "What");
			ReplaceWith = LineParser.ParseLiteral(ref input, "With");
			if (LineParser.Lookahead(ref input) == TokenType.Boolean)
			{
				LineParser.SetBool(ref input, this);
			}
			break;
		case Function.RegexMatch:
			RegexMatch = LineParser.ParseLiteral(ref input, "Pattern");
			break;
		case Function.RandomNum:
			if (LineParser.Lookahead(ref input) == TokenType.Literal)
			{
				RandomMin = LineParser.ParseLiteral(ref input, "Minimum");
				RandomMax = LineParser.ParseLiteral(ref input, "Maximum");
			}
			else
			{
				RandomMin = LineParser.ParseInt(ref input, "Minimum").ToString();
				RandomMax = LineParser.ParseInt(ref input, "Maximum").ToString();
			}
			if (LineParser.Lookahead(ref input) == TokenType.Boolean)
			{
				LineParser.SetBool(ref input, this);
			}
			break;
		case Function.CountOccurrences:
			StringToFind = LineParser.ParseLiteral(ref input, "string to find");
			break;
		case Function.CharAt:
			CharIndex = LineParser.ParseLiteral(ref input, "Index");
			break;
		case Function.Split:
			Separator = LineParser.ParseLiteral(ref input, "Separator");
			SplitIndex = LineParser.ParseInt(ref input, "Split Index");
			if (input.StartsWith("RemoveEmptyEntries \""))
			{
				try
				{
					StringSplitOption = LineParser.ParseEnum(ref input, "String Split Option", typeof(StringSplitOptions));
				}
				catch
				{
				}
			}
			break;
		case Function.Remove:
			RemoveSIndex = LineParser.ParseLiteral(ref input, "SIndex");
			RemoveCount = LineParser.ParseLiteral(ref input, "Count");
			break;
		case Function.Substring:
			SubstringIndex = LineParser.ParseLiteral(ref input, "Index");
			SubstringLength = LineParser.ParseLiteral(ref input, "Length");
			break;
		case Function.RSAEncrypt:
			RsaN = LineParser.ParseLiteral(ref input, "Public Key Modulus");
			RsaE = LineParser.ParseLiteral(ref input, "Public Key Exponent");
			if (LineParser.Lookahead(ref input) == TokenType.Boolean)
			{
				LineParser.SetBool(ref input, this);
			}
			break;
		case Function.RSAPKCS1PAD2:
			RsaN = LineParser.ParseLiteral(ref input, "Public Key Modulus");
			RsaE = LineParser.ParseLiteral(ref input, "Public Key Exponent");
			break;
		case Function.GetRandomUA:
			if (LineParser.ParseToken(ref input, TokenType.Parameter, essential: false, proceed: false) == "BROWSER")
			{
				LineParser.EnsureIdentifier(ref input, "BROWSER");
				UserAgentSpecifyBrowser = true;
				UserAgentBrowser = LineParser.ParseEnum(ref input, "BROWSER", typeof(UserAgent.Browser));
			}
			break;
		case Function.AESEncrypt:
		case Function.AESDecrypt:
			AesKey = LineParser.ParseLiteral(ref input, "Key");
			AesIV = LineParser.ParseLiteral(ref input, "IV");
			AesMode = LineParser.ParseEnum(ref input, "Cipher mode", typeof(CipherMode));
			AesPadding = LineParser.ParseEnum(ref input, "Padding mode", typeof(PaddingMode));
			if (LineParser.Lookahead(ref input) == TokenType.Boolean)
			{
				LineParser.SetBool(ref input, this);
			}
			break;
		case Function.PBKDF2PKCS5:
			if (LineParser.Lookahead(ref input) == TokenType.Literal)
			{
				KdfSalt = LineParser.ParseLiteral(ref input, "Salt");
			}
			else
			{
				KdfSaltSize = LineParser.ParseInt(ref input, "Salt size");
			}
			KdfIterations = LineParser.ParseInt(ref input, "Iterations");
			KdfKeySize = LineParser.ParseInt(ref input, "Key size");
			KdfAlgorithm = LineParser.ParseEnum(ref input, "Algorithm", typeof(Hash));
			break;
		case Function.Encoding:
			GetEncoding = LineParser.ParseLiteral(ref input, "Encoding name/codepage");
			EncFunc = LineParser.ParseEnum(ref input, "Encoding Methods", typeof(EncodingMethods));
			break;
		case Function.SCrypt:
			ScryptMeth = LineParser.ParseEnum(ref input, "Scrypt Methods", typeof(ScryptMethods));
			if (ScryptMeth == ScryptMethods.Encode)
			{
				ScryptSalt = LineParser.ParseLiteral(ref input, "Scrypt salt");
				ScryptCost = LineParser.ParseInt(ref input, "Scrypt cost");
				ScryptBlockSize = LineParser.ParseInt(ref input, "Scrypt block size");
				ScryptOutputLength = LineParser.ParseInt(ref input, "Scrypt Output Length");
				if (LineParser.Lookahead(ref input) == TokenType.Boolean)
				{
					LineParser.SetBool(ref input, this);
				}
			}
			if (ScryptMeth == ScryptMethods.Compare)
			{
				ScryptHashedPassword = LineParser.ParseLiteral(ref input, "Hashed Password");
			}
			break;
		case Function.GenerateGUID:
			if (LineParser.Lookahead(ref input) == TokenType.Parameter)
			{
				GuidVer = LineParser.ParseEnum(ref input, "Version",  typeof(GuidVersion));
				GuidFmt = LineParser.ParseEnum(ref input, "Format",   typeof(GuidFormat));
			}
			if (LineParser.Lookahead(ref input) == TokenType.Boolean)
			{
				LineParser.SetBool(ref input, this);
			}
			break;
		case Function.BCrypt:
			BCryptMeth = LineParser.ParseEnum(ref input, "BCrypt Methods", typeof(BCryptMethods));
			BCryptSalt = LineParser.ParseLiteral(ref input, "Salt");
			while (LineParser.Lookahead(ref input) == TokenType.Boolean)
			{
				LineParser.SetBool(ref input, this);
			}
			if (UseWorkFactor)
			{
				BCryptWorkFactor = LineParser.ParseInt(ref input, "BCrypt Work Factor");
			}
			if (BCryptMeth == BCryptMethods.Verify)
			{
				BCryptHashedPassword = LineParser.ParseLiteral(ref input, "Hashed Password");
			}
			break;
		case Function.MergeByteArrays:
			SecondInput = LineParser.ParseLiteral(ref input, "SecondInput");
			break;
		case Function.RegexReplace:
			RegexMatch = LineParser.ParseLiteral(ref input, "Pattern");
			ReplaceWith = LineParser.ParseLiteral(ref input, "Replacement");
			break;
		case Function.XOR:
		case Function.XORStrings:
			SecondInput = LineParser.ParseLiteral(ref input, "Key");
			break;
		case Function.RSADecrypt:
			RsaN = LineParser.ParseLiteral(ref input, "Modulus");
			RsaD = LineParser.ParseLiteral(ref input, "PrivateExp");
			if (LineParser.Lookahead(ref input) == TokenType.Boolean)
				LineParser.SetBool(ref input, this);
			break;
		case Function.JWTEncode:
			SecondInput = LineParser.ParseLiteral(ref input, "Secret");
			JwtAlgorithm = LineParser.ParseLiteral(ref input, "Algorithm");
			break;
		case Function.MaxFloat:
		case Function.MinFloat:
		case Function.MaxInt:
		case Function.MinInt:
		case Function.GetKey:
		case Function.RemoveByKey:
		case Function.IndexOf:
			SecondInput = LineParser.ParseLiteral(ref input, "Second");
			break;
		case Function.RandomFloat:
			RandomMin = LineParser.ParseLiteral(ref input, "Min");
			RandomMax = LineParser.ParseLiteral(ref input, "Max");
			break;
		case Function.AddKeyValuePair:
			SecondInput = LineParser.ParseLiteral(ref input, "Key");
			ThirdInput  = LineParser.ParseLiteral(ref input, "Value");
			break;
		case Function.CreateListOfNumbers:
			SecondInput = LineParser.ParseLiteral(ref input, "Count");
			ThirdInput  = LineParser.ParseLiteral(ref input, "Step");
			break;
		case Function.BCryptVerify:
			SecondInput = LineParser.ParseLiteral(ref input, "Hash");
			break;
		case Function.ScryptDeriveKey:
			SecondInput       = LineParser.ParseLiteral(ref input, "Salt");
			ScryptCost        = LineParser.ParseInt(ref input, "Cost");
			ScryptBlockSize   = LineParser.ParseInt(ref input, "BlockSize");
			ScryptOutputLength = LineParser.ParseInt(ref input, "OutputLength");
			break;
		case Function.AWS4Signature:
			SecondInput = LineParser.ParseLiteral(ref input, "SecretKey");
			ThirdInput  = LineParser.ParseLiteral(ref input, "Date");
			AwsRegion   = LineParser.ParseLiteral(ref input, "Region");
			AwsService  = LineParser.ParseLiteral(ref input, "Service");
			break;
		}
		if (LineParser.Lookahead(ref input) == TokenType.Literal)
		{
			InputString = LineParser.ParseLiteral(ref input, "INPUT");
		}
		if (LineParser.ParseToken(ref input, TokenType.Arrow, essential: false) == string.Empty)
		{
			return this;
		}
		try
		{
			string text3 = LineParser.ParseToken(ref input, TokenType.Parameter, essential: true);
			if (text3.ToUpper() == "VAR" || text3.ToUpper() == "CAP")
			{
				IsCapture = text3.ToUpper() == "CAP";
			}
		}
		catch
		{
			throw new ArgumentException("Invalid or missing variable type");
		}
		try
		{
			VariableName = LineParser.ParseToken(ref input, TokenType.Literal, essential: true);
			return this;
		}
		catch
		{
			throw new ArgumentException("Variable name not specified");
		}
	}

	public override string ToLS(bool indent = true)
	{
		BlockWriter blockWriter = new BlockWriter(GetType(), indent, base.Disabled);
		blockWriter.Label(base.Label).Token("FUNCTION").Token(FunctionType);
		switch (FunctionType)
		{
		case Function.Hash:
			blockWriter.Token(HashType).Boolean(InputBase64, "InputBase64");
			break;
		case Function.HMAC:
			blockWriter.Token(HashType).Literal(HmacKey).Boolean(InputBase64, "InputBase64")
				.Boolean(HmacBase64, "HmacBase64")
				.Boolean(KeyBase64, "KeyBase64");
			break;
		case Function.Translate:
			blockWriter.Boolean(StopAfterFirstMatch, "StopAfterFirstMatch").Boolean(UseVar, "UseVar");
			foreach (KeyValuePair<string, string> item in TranslationDictionary)
			{
				blockWriter.Indent().Token("KEY").Literal(item.Key)
					.Token("VALUE")
					.Literal(item.Value);
			}
			blockWriter.Indent();
			break;
		case Function.DateToUnixTime:
		case Function.UnixTimeToDate:
			blockWriter.Literal(DateFormat);
			if (UnixTimeType != 0)
			{
				blockWriter.Token(UnixTimeType);
			}
			break;
		case Function.Replace:
			blockWriter.Literal(ReplaceWhat).Literal(ReplaceWith).Boolean(UseRegex, "UseRegex");
			break;
		case Function.RegexMatch:
			blockWriter.Literal(RegexMatch, "RegexMatch");
			break;
		case Function.RandomNum:
			blockWriter.Literal(RandomMin).Literal(RandomMax).Boolean(RandomZeroPad, "RandomZeroPad");
			break;
		case Function.CountOccurrences:
			blockWriter.Literal(StringToFind);
			break;
		case Function.CharAt:
			blockWriter.Literal(CharIndex);
			break;
		case Function.Split:
			if (StringSplitOption == StringSplitOptions.None)
			{
				blockWriter.Literal(Separator).Integer(SplitIndex);
			}
			else
			{
				blockWriter.Literal(Separator).Integer(SplitIndex).Token(StringSplitOption, "StringSplitOption");
			}
			break;
		case Function.Remove:
			blockWriter.Literal(RemoveSIndex).Literal(RemoveCount);
			break;
		case Function.Substring:
			blockWriter.Literal(SubstringIndex).Literal(SubstringLength);
			break;
		case Function.RSAEncrypt:
			blockWriter.Literal(RsaN).Literal(RsaE).Boolean(RsaOAEP, "RsaOAEP");
			break;
		case Function.RSAPKCS1PAD2:
			blockWriter.Literal(RsaN).Literal(RsaE);
			break;
		case Function.GetRandomUA:
			if (UserAgentSpecifyBrowser)
			{
				blockWriter.Token("BROWSER").Token(UserAgentBrowser);
			}
			break;
		case Function.AESEncrypt:
		case Function.AESDecrypt:
			blockWriter.Literal(AesKey).Literal(AesIV).Token(AesMode)
				.Token(AesPadding)
				.Boolean(HexKeys, "HexKeys");
			break;
		case Function.PBKDF2PKCS5:
			if (KdfSalt != string.Empty)
			{
				blockWriter.Literal(KdfSalt);
			}
			else
			{
				blockWriter.Integer(KdfSaltSize);
			}
			blockWriter.Integer(KdfIterations).Integer(KdfKeySize).Token(KdfAlgorithm);
			break;
		case Function.Encoding:
			blockWriter.Literal((GetEncoding ?? string.Empty).ToString()).Token(EncFunc);
			break;
		case Function.SCrypt:
			blockWriter.Token(ScryptMeth);
			if (ScryptMeth == ScryptMethods.Encode)
			{
				blockWriter.Literal(ScryptSalt).Integer(ScryptCost).Integer(ScryptBlockSize)
					.Integer(ScryptOutputLength);
				if (Base64Output)
				{
					blockWriter.Boolean(Base64Output, "Base64Output");
				}
			}
			else if (ScryptMeth == ScryptMethods.Compare)
			{
				blockWriter.Literal(ScryptHashedPassword);
			}
			break;
		case Function.GenerateGUID:
			blockWriter.Token(GuidVer).Token(GuidFmt);
			if (GuidUppercase)
				blockWriter.Boolean(GuidUppercase, "GuidUppercase");
			break;
		case Function.MergeByteArrays:
			blockWriter.Literal(SecondInput);
			break;
		case Function.RegexReplace:
			blockWriter.Literal(RegexMatch).Literal(ReplaceWith);
			break;
		case Function.XOR:
		case Function.XORStrings:
			blockWriter.Literal(SecondInput);
			break;
		case Function.RSADecrypt:
			blockWriter.Literal(RsaN).Literal(RsaD).Boolean(RsaOAEP, "RsaOAEP");
			break;
		case Function.JWTEncode:
			blockWriter.Literal(SecondInput).Literal(JwtAlgorithm);
			break;
		case Function.MaxFloat:
		case Function.MinFloat:
		case Function.MaxInt:
		case Function.MinInt:
		case Function.GetKey:
		case Function.RemoveByKey:
		case Function.IndexOf:
			blockWriter.Literal(SecondInput);
			break;
		case Function.RandomFloat:
			blockWriter.Literal(RandomMin).Literal(RandomMax);
			break;
		case Function.AddKeyValuePair:
			blockWriter.Literal(SecondInput).Literal(ThirdInput);
			break;
		case Function.CreateListOfNumbers:
			blockWriter.Literal(SecondInput).Literal(ThirdInput);
			break;
		case Function.BCryptVerify:
			blockWriter.Literal(SecondInput);
			break;
		case Function.ScryptDeriveKey:
			blockWriter.Literal(SecondInput).Integer(ScryptCost).Integer(ScryptBlockSize).Integer(ScryptOutputLength);
			break;
		case Function.AWS4Signature:
			blockWriter.Literal(SecondInput).Literal(ThirdInput).Literal(AwsRegion).Literal(AwsService);
			break;
		case Function.BCrypt:
			blockWriter.Token(BCryptMeth).Literal(BCryptSalt);
			if (UseWorkFactor)
			{
				blockWriter.Boolean(UseWorkFactor, "UseWorkFactor").Integer(BCryptWorkFactor, "BCryptWorkFactor");
			}
			if (BCryptMeth == BCryptMethods.Verify)
			{
				blockWriter.Literal(BCryptHashedPassword);
			}
			break;
		}
		blockWriter.Literal(InputString, "InputString");
		if (!blockWriter.CheckDefault(VariableName, "VariableName"))
		{
			blockWriter.Arrow().Token(IsCapture ? "CAP" : "VAR").Literal(VariableName);
		}
		return blockWriter.ToString();
	}

	public override void Process(BotData data)
	{
		base.Process(data);

		// Functions that output a list or dictionary bypass the standard string-output loop.
		if (functionType == Function.CreateListOfNumbers)
		{
			try
			{
				double start = double.Parse(BlockBase.ReplaceValues(inputString, data), CultureInfo.InvariantCulture);
				int    count = int.Parse(BlockBase.ReplaceValues(secondInput, data));
				double step  = string.IsNullOrWhiteSpace(thirdInput) ? 1
				             : double.Parse(BlockBase.ReplaceValues(thirdInput, data), CultureInfo.InvariantCulture);
				var nums = Enumerable.Range(0, count)
					.Select(i => (start + i * step).ToString(CultureInfo.InvariantCulture))
					.ToList();
				if (!string.IsNullOrEmpty(variableName))
					data.Variables.Set(new CVar(variableName, nums, isCapture));
				data.Log(new LogEntry($"CreateListOfNumbers: {count} items", Colors.GreenYellow));
			}
			catch (Exception ex)
			{
				data.LogBuffer.Add(new LogEntry("ERROR CreateListOfNumbers: " + ex.Message, Colors.Tomato));
			}
			return;
		}
		if (functionType == Function.ListToDict)
		{
			try
			{
				string listName = BlockBase.ReplaceValues(inputString, data);
				var srcList = data.Variables.GetList(listName) ?? data.GlobalVariables.GetList(listName);
				var dict = new Dictionary<string, string>();
				if (srcList != null)
					for (int i = 0; i + 1 < srcList.Count; i += 2)
						dict[srcList[i]] = srcList[i + 1];
				if (!string.IsNullOrEmpty(variableName))
					data.Variables.Set(new CVar(variableName, dict, isCapture));
				data.Log(new LogEntry($"ListToDict: {dict.Count} pairs", Colors.GreenYellow));
			}
			catch (Exception ex)
			{
				data.LogBuffer.Add(new LogEntry("ERROR ListToDict: " + ex.Message, Colors.Tomato));
			}
			return;
		}
		if (functionType == Function.AddKeyValuePair)
		{
			try
			{
				string dictName = BlockBase.ReplaceValues(inputString, data);
				string key = BlockBase.ReplaceValues(secondInput, data);
				string val = BlockBase.ReplaceValues(thirdInput, data);
				var cvar = data.Variables.Get(dictName, CVar.VarType.Dictionary)
				        ?? data.GlobalVariables.Get(dictName, CVar.VarType.Dictionary);
				if (cvar != null) ((Dictionary<string, string>)cvar.Value)[key] = val;
				data.Log(new LogEntry($"AddKeyValuePair: [{key}]={val}", Colors.GreenYellow));
			}
			catch (Exception ex)
			{
				data.LogBuffer.Add(new LogEntry("ERROR AddKeyValuePair: " + ex.Message, Colors.Tomato));
			}
			return;
		}
		if (functionType == Function.RemoveByKey)
		{
			try
			{
				string dictName = BlockBase.ReplaceValues(inputString, data);
				string key = BlockBase.ReplaceValues(secondInput, data);
				var cvar = data.Variables.Get(dictName, CVar.VarType.Dictionary)
				        ?? data.GlobalVariables.Get(dictName, CVar.VarType.Dictionary);
				if (cvar != null) ((Dictionary<string, string>)cvar.Value).Remove(key);
				data.Log(new LogEntry($"RemoveByKey: [{key}]", Colors.GreenYellow));
			}
			catch (Exception ex)
			{
				data.LogBuffer.Add(new LogEntry("ERROR RemoveByKey: " + ex.Message, Colors.Tomato));
			}
			return;
		}

		List<string> list = BlockBase.ReplaceValuesRecursive(inputString, data);
		List<string> list2 = new List<string>();
		for (int i = 0; i < list.Count; i++)
		{
			string text = list[i];
			string text2 = "";
			switch (FunctionType)
			{
			case Function.Constant:
				text2 = text;
				break;
			case Function.Base64Encode:
				text2 = text.ToBase64();
				break;
			case Function.Base64Decode:
				text2 = text.FromBase64();
				break;
			case Function.HTMLEntityEncode:
				text2 = WebUtility.HtmlEncode(text);
				break;
			case Function.HTMLEntityDecode:
				text2 = WebUtility.HtmlDecode(text);
				break;
			case Function.Hash:
				text2 = GetHash(text, hashType, InputBase64).ToLower();
				break;
			case Function.HMAC:
				text2 = Hmac(text, hashType, BlockBase.ReplaceValues(hmacKey, data), InputBase64, KeyBase64, HmacBase64);
				break;
			case Function.Translate:
				text2 = text;
				foreach (KeyValuePair<string, string> item in TranslationDictionary.OrderBy((KeyValuePair<string, string> e) => e.Key.Length).Reverse())
				{
					if (text2.Contains(item.Key))
					{
						text2 = ((!UseVar) ? text2.Replace(item.Key, item.Value) : text2.Replace(BlockBase.ReplaceValues(item.Key, data), BlockBase.ReplaceValues(item.Value, data)));
						if (StopAfterFirstMatch)
						{
							break;
						}
					}
				}
				break;
			case Function.DateToUnixTime:
				switch (UnixTimeType)
				{
				case DateToUnixTimeType.Seconds:
					text2 = (string.IsNullOrEmpty(text) ? DateTime.Now.ToUnixTimeSeconds().ToString() : text.ToDateTime(DateFormat).ToUnixTimeSeconds().ToString());
					break;
				case DateToUnixTimeType.Miliseconds:
					text2 = (string.IsNullOrEmpty(text) ? DateTime.Now.ToUnixTimeMilliseconds().ToString() : text.ToDateTime(DateFormat).ToUnixTimeMilliseconds().ToString());
					break;
				}
				break;
			case Function.DateToSolar:
			{
				PersianCalendar persianCalendar = new PersianCalendar();
				if (DateTime.TryParse(text, out var result4))
				{
					text2 = $"{persianCalendar.GetYear(result4)}/{persianCalendar.GetMonth(result4)}/{persianCalendar.GetDayOfMonth(result4)}";
				}
				break;
			}
			case Function.DateToGregorian:
			{
				PersianCalendar calendar = new PersianCalendar();
				DateTime dateTime2 = DateTime.Parse(text, CultureInfo.InvariantCulture);
				text2 = DateTime.Parse(new DateTime(dateTime2.Year, dateTime2.Month, dateTime2.Day, calendar).ToString(CultureInfo.CreateSpecificCulture("en-US"))).ToShortDateString();
				break;
			}
			case Function.Length:
				text2 = text.Length.ToString();
				break;
			case Function.ToLowercase:
				text2 = text.ToLower();
				break;
			case Function.ToUppercase:
				text2 = text.ToUpper();
				break;
			case Function.Replace:
				text2 = ((!useRegex) ? text.Replace(BlockBase.ReplaceValues(replaceWhat, data), BlockBase.ReplaceValues(replaceWith, data)) : Regex.Replace(text, BlockBase.ReplaceValues(replaceWhat, data), BlockBase.ReplaceValues(replaceWith, data)));
				break;
			case Function.RegexMatch:
				text2 = Regex.Match(text, BlockBase.ReplaceValues(regexMatch, data)).Value;
				break;
			case Function.Unescape:
				text2 = Regex.Unescape(text);
				break;
			case Function.URLEncode:
				text2 = string.Join("", from s in SplitInChunks(text, 2080)
					select Uri.EscapeDataString(s));
				break;
			case Function.URLDecode:
				text2 = Uri.UnescapeDataString(text);
				break;
			case Function.UnixTimeToDate:
				text2 = string.IsNullOrEmpty(text)
					? DateTime.Now.ToString(dateFormat)
					: double.Parse(text).ToDateTime().ToString(dateFormat);
				break;
			case Function.CurrentDate:
				text2 = DateTime.Now.ToShortDateString();
				break;
			case Function.CurrentDay:
				text2 = DateTime.Now.Day.ToString();
				break;
			case Function.CurrentMonth:
				text2 = DateTime.Now.Month.ToString();
				break;
			case Function.CurrentYear:
				text2 = DateTime.Now.Year.ToString();
				break;
			case Function.GetRemainingDay:
			{
				DateTime dateTime;
				if (double.TryParse(text, System.Globalization.NumberStyles.Any,
				    System.Globalization.CultureInfo.InvariantCulture, out double _unixD) && _unixD >= 1_000_000_000.0)
					dateTime = _unixD.ToDateTime();   // Unix timestamp (seconds or ms)
				else
					dateTime = Convert.ToDateTime(text, new CultureInfo("en-US"));
				DateTime now = DateTime.Now;
				text2 = (dateTime - now).Days.ToString();
				break;
			}
			case Function.CurrentTime:
				text2 = DateTime.Now.ToShortTimeString();
				break;
			case Function.DayOfWeek:
				text2 = DateTime.Now.DayOfWeek.ToString();
				break;
			case Function.CurrentUnixTime:
			{
				var _unixNow = DateTimeOffset.UtcNow;
				var _unixInput = text?.Trim() ?? "";
				if (_unixInput == "ms" || _unixInput == "1000")
					text2 = _unixNow.ToUnixTimeMilliseconds().ToString();
				else if (long.TryParse(_unixInput, out long _unixMult) && _unixMult > 1)
					text2 = (_unixNow.ToUnixTimeSeconds() * _unixMult).ToString();
				else
					text2 = _unixNow.ToUnixTimeSeconds().ToString();
				break;
			}
			case Function.UnixTimeToISO8601:
				text2 = double.Parse(text).ToDateTime().ToISO8601();
				break;
			case Function.RandomNum:
			{
				long min2 = long.Parse(BlockBase.ReplaceValues(randomMin, data));
				long max2 = long.Parse(BlockBase.ReplaceValues(randomMax, data));
				string text3 = LongRandom(min2, max2 < long.MaxValue ? max2 + 1 : max2, data.random).ToString();
				text2 = (randomZeroPad ? text3.PadLeft(max2.ToString().Length, '0') : text3);
				break;
			}
			case Function.RandomString:
				text2 = text;
				text2 = Regex.Replace(text2, "\\?l", (Match m) => _lowercase[data.random.Next(_lowercase.Length)].ToString());
				text2 = Regex.Replace(text2, "\\?u", (Match m) => _uppercase[data.random.Next(_uppercase.Length)].ToString());
				text2 = Regex.Replace(text2, "\\?d", (Match m) => _digits[data.random.Next(_digits.Length)].ToString());
				text2 = Regex.Replace(text2, "\\?s", (Match m) => _symbols[data.random.Next(_symbols.Length)].ToString());
				text2 = Regex.Replace(text2, "\\?h", (Match m) => _hex[data.random.Next(_hex.Length)].ToString());
				text2 = Regex.Replace(text2, "\\?a", (Match m) => _allChars[data.random.Next(_allChars.Length)].ToString());
				text2 = Regex.Replace(text2, "\\?m", (Match m) => _udChars[data.random.Next(_udChars.Length)].ToString());
				text2 = Regex.Replace(text2, "\\?n", (Match m) => _ldChars[data.random.Next(_ldChars.Length)].ToString());
				text2 = Regex.Replace(text2, "\\?i", (Match m) => _ludChars[data.random.Next(_ludChars.Length)].ToString());
				text2 = Regex.Replace(text2, "\\?f", (Match m) => _upperlwr[data.random.Next(_upperlwr.Length)].ToString());
				break;
			case Function.Ceil:
				text2 = Math.Ceiling(decimal.Parse(text, _style, _provider)).ToString();
				break;
			case Function.Floor:
				text2 = Math.Floor(decimal.Parse(text, _style, _provider)).ToString();
				break;
			case Function.Round:
				text2 = Math.Round(decimal.Parse(text, _style, _provider), 0, MidpointRounding.AwayFromZero).ToString();
				break;
			case Function.Abs:
				text2 = Math.Abs(decimal.Parse(text, _style, _provider)).ToString();
				break;
			case Function.Compute:
				text2 = new DataTable().Compute(text.Replace(',', '.'), null).ToString();
				break;
			case Function.CountOccurrences:
				text2 = CountStringOccurrences(text, stringToFind).ToString();
				break;
			case Function.ClearCookies:
				data.Cookies.Clear();
				break;
			case Function.RSAEncrypt:
				text2 = Crypto.RSAEncrypt(text, BlockBase.ReplaceValues(RsaN, data), BlockBase.ReplaceValues(RsaE, data), RsaOAEP);
				break;
			case Function.RSAPKCS1PAD2:
				text2 = Crypto.RSAPkcs1Pad2(text, BlockBase.ReplaceValues(RsaN, data), BlockBase.ReplaceValues(RsaE, data));
				break;
			case Function.Delay:
				try
				{
					Thread.Sleep(int.Parse(text));
				}
				catch
				{
				}
				break;
			case Function.CharAt:
				text2 = text.ToCharArray()[int.Parse(BlockBase.ReplaceValues(charIndex, data))].ToString();
				break;
			case Function.Split:
				text2 = text.Split(new string[1] { BlockBase.ReplaceValues(Separator, data) }, StringSplitOption)[int.Parse(BlockBase.ReplaceValues(SplitIndex.ToString(), data)) - 1];
				break;
			case Function.Remove:
				text2 = ((!string.IsNullOrEmpty(RemoveCount)) ? text.Remove(int.Parse(BlockBase.ReplaceValues(RemoveSIndex, data)), int.Parse(BlockBase.ReplaceValues(RemoveCount, data))) : text.Remove(int.Parse(BlockBase.ReplaceValues(removeSIndex, data))));
				break;
			case Function.Substring:
				text2 = text.Substring(int.Parse(BlockBase.ReplaceValues(substringIndex, data)), int.Parse(BlockBase.ReplaceValues(substringLength, data)));
				break;
			case Function.ReverseString:
			{
				char[] array3 = text.ToCharArray();
				Array.Reverse(array3);
				text2 = new string(array3);
				break;
			}
			case Function.Trim:
				text2 = text.Trim();
				break;
			case Function.GetRandomUA:
				text2 = (string.IsNullOrEmpty(text) ? ((!UserAgentSpecifyBrowser) ? UserAgent.Random(data.random) : UserAgent.ForBrowser(UserAgentBrowser)) : UserAgent.RandomFromList(text));
				break;
			case Function.AESEncrypt:
				text2 = Crypto.AESEncrypt(text, BlockBase.ReplaceValues(aesKey, data), BlockBase.ReplaceValues(aesIV, data), AesMode, AesPadding, HexKeys);
				break;
			case Function.AESDecrypt:
				text2 = Crypto.AESDecrypt(text, BlockBase.ReplaceValues(aesKey, data), BlockBase.ReplaceValues(aesIV, data), AesMode, AesPadding, HexKeys);
				break;
			case Function.PBKDF2PKCS5:
				text2 = Crypto.PBKDF2PKCS5(text, BlockBase.ReplaceValues(KdfSalt, data), KdfSaltSize, KdfIterations, KdfKeySize, KdfAlgorithm);
				break;
			case Function.ToLetter:
				text2 = new string(text.Where(char.IsLetter).ToArray());
				break;
			case Function.ToDigit:
				text2 = new string(text.Where(char.IsDigit).ToArray());
				break;
			case Function.ToLetterOrDigit:
				text2 = new string(text.Where(char.IsLetterOrDigit).ToArray());
				break;
			case Function.EvaluateMathString:
				text2 = new CodeDomCalculator(text).Calculate().ToString();
				break;
			case Function.NumberToWords:
			{
				if (long.TryParse(text, out var result3))
				{
					text2 = result3.ToWords(new CultureInfo("en-US"));
				}
				break;
			}
			case Function.WordsToNumber:
				text2 = WordToNumber.ToLong(text).ToString();
				break;
			case Function.GenerateOAuthVerifier:
			{
				byte[] array2 = new byte[32];
				RandomNumberGenerator.Fill(array2);
				text2 = Base64UrlEncoder.Encode(array2);
				break;
			}
			case Function.GenerateOAuthChallenge:
			{
				// PKCE S256: BASE64URL(SHA256(ASCII(code_verifier)))
				byte[] challengeBytes = SHA256.HashData(Encoding.ASCII.GetBytes(text));
				text2 = Base64UrlEncoder.Encode(challengeBytes);
				break;
			}
			case Function.GenerateGUID:
			{
				Guid g = GuidVer == GuidVersion.V7 ? CreateGuidV7() : Guid.NewGuid();
				string fmtStr = GuidFmt.ToString().ToLower(); // "d","n","b","p"
				text2 = g.ToString(fmtStr);
				if (GuidUppercase) text2 = text2.ToUpperInvariant();
				break;
			}
			case Function.GenerateBytes:
				try
				{
					byte[] array = new byte[Convert.ToInt32(text)];
					RandomNumberGenerator.Fill(array);
					text2 = BitConverter.ToString(array).Replace("-", "");
				}
				catch (FormatException ex)
				{
					data.Status = BotStatus.ERROR;
					data.LogBuffer.Add(new LogEntry("ERROR: " + ex.Message, Colors.Tomato));
					text2 = "INTEGERS ONLY";
				}
				catch (OverflowException ex2)
				{
					data.Status = BotStatus.ERROR;
					data.LogBuffer.Add(new LogEntry("ERROR: " + ex2.Message, Colors.Tomato));
					text2 = "BYTE SIZE TOO LARGE FOR 32BIT INTEGER";
				}
				break;
			case Function.Encoding:
				switch (EncFunc)
				{
				case EncodingMethods.GetBytes:
				{
					string encName2 = BlockBase.ReplaceValues((GetEncoding ?? string.Empty).ToString(), data);
					text2 = ((!int.TryParse(encName2, out var result2)) ? Encoding.GetEncoding(encName2).GetBytes(BlockBase.ReplaceValues(text, data)).ConvertToString() : Encoding.GetEncoding(result2).GetBytes(BlockBase.ReplaceValues(text, data)).ConvertToString());
					break;
				}
				case EncodingMethods.GetString:
				{
					string encName = BlockBase.ReplaceValues((GetEncoding ?? string.Empty).ToString(), data);
					text2 = ((!int.TryParse(encName, out var result)) ? Encoding.GetEncoding(encName).GetString(BlockBase.ReplaceValues(text, data).ConvertToByteArray()) : Encoding.GetEncoding(result).GetString(BlockBase.ReplaceValues(text, data).ConvertToByteArray()));
					break;
				}
				}
				break;
			case Function.Ntlm:
				text2 = Ntlm.Generate(BlockBase.ReplaceValues(text, data));
				break;
			case Function.SCrypt:
				switch (scryptMeth)
				{
				case ScryptMethods.Encode:
					text2 = Crypto.ScryptEncoder(BlockBase.ReplaceValues(text, data), ScryptSalt, ScryptCost, ScryptBlockSize, 1, ScryptOutputLength, Base64Output);
					break;
				case ScryptMethods.Compare:
					text2 = Crypto.ScryptCompare(BlockBase.ReplaceValues(text, data), BlockBase.ReplaceValues(ScryptHashedPassword, data)).ToString();
					break;
				case ScryptMethods.IsValid:
					text2 = Crypto.ScryptIsValid(BlockBase.ReplaceValues(text, data)).ToString();
					break;
				}
				break;
			case Function.MergeByteArrays:
			{
				byte[] first  = Convert.FromBase64String(text);
				byte[] second = Convert.FromBase64String(BlockBase.ReplaceValues(secondInput, data));
				byte[] merged = new byte[first.Length + second.Length];
				Buffer.BlockCopy(first,  0, merged, 0,            first.Length);
				Buffer.BlockCopy(second, 0, merged, first.Length, second.Length);
				text2 = Convert.ToBase64String(merged);
				break;
			}
			case Function.RegexReplace:
				text2 = Regex.Replace(text, BlockBase.ReplaceValues(regexMatch, data),
				                      BlockBase.ReplaceValues(replaceWith, data));
				break;
			case Function.XOR:
			{
				byte[] xorData = TryFromHexOrBase64(text.Trim());
				byte[] xorKey  = TryFromHexOrBase64(BlockBase.ReplaceValues(secondInput, data).Trim());
				byte[] xorOut  = new byte[xorData.Length];
				for (int xi = 0; xi < xorData.Length; xi++)
					xorOut[xi] = (byte)(xorData[xi] ^ xorKey[xi % xorKey.Length]);
				text2 = Convert.ToHexString(xorOut).ToLower();
				break;
			}
			case Function.XORStrings:
			{
				string xorKeyStr = BlockBase.ReplaceValues(secondInput, data);
				char[] xorChars = new char[text.Length];
				for (int xi = 0; xi < text.Length; xi++)
					xorChars[xi] = (char)(text[xi] ^ xorKeyStr[xi % xorKeyStr.Length]);
				text2 = new string(xorChars);
				break;
			}
			case Function.RSADecrypt:
			{
				byte[] rsaData = Convert.FromBase64String(text.Trim());
				byte[] nBytes  = Convert.FromHexString(BlockBase.ReplaceValues(rsaN, data).Trim());
				byte[] dBytes  = Convert.FromHexString(BlockBase.ReplaceValues(rsaD, data).Trim());
				using var rsa = System.Security.Cryptography.RSA.Create();
				rsa.ImportParameters(new System.Security.Cryptography.RSAParameters { Modulus = nBytes, D = dBytes });
				byte[] dec = rsa.Decrypt(rsaData, rsaOAEP
					? System.Security.Cryptography.RSAEncryptionPadding.OaepSHA1
					: System.Security.Cryptography.RSAEncryptionPadding.Pkcs1);
				text2 = Encoding.UTF8.GetString(dec);
				break;
			}
			case Function.JWTEncode:
			{
				string jwtSecret = BlockBase.ReplaceValues(secondInput, data);
				string algo      = (jwtAlgorithm ?? "HS256").ToUpperInvariant();
				byte[] jwtSecretBytes = Encoding.UTF8.GetBytes(jwtSecret);
				string header  = $"{{\"alg\":\"{algo}\",\"typ\":\"JWT\"}}";
				string hB64    = Base64UrlEncode(Encoding.UTF8.GetBytes(header));
				string pB64    = Base64UrlEncode(Encoding.UTF8.GetBytes(text));
				string message = hB64 + "." + pB64;
				System.Security.Cryptography.HMAC hmacJwt = algo switch
				{
					"HS384" => new System.Security.Cryptography.HMACSHA384(jwtSecretBytes),
					"HS512" => new System.Security.Cryptography.HMACSHA512(jwtSecretBytes),
					_       => new System.Security.Cryptography.HMACSHA256(jwtSecretBytes),
				};
				using (hmacJwt)
					text2 = message + "." + Base64UrlEncode(hmacJwt.ComputeHash(Encoding.UTF8.GetBytes(message)));
				break;
				static string Base64UrlEncode(byte[] b) =>
					Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');
			}
			case Function.MaxFloat:
			{
				double a = double.Parse(text, CultureInfo.InvariantCulture);
				double b = double.Parse(BlockBase.ReplaceValues(secondInput, data), CultureInfo.InvariantCulture);
				text2 = Math.Max(a, b).ToString(CultureInfo.InvariantCulture);
				break;
			}
			case Function.MinFloat:
			{
				double a = double.Parse(text, CultureInfo.InvariantCulture);
				double b = double.Parse(BlockBase.ReplaceValues(secondInput, data), CultureInfo.InvariantCulture);
				text2 = Math.Min(a, b).ToString(CultureInfo.InvariantCulture);
				break;
			}
			case Function.RandomFloat:
			{
				double fMin = double.Parse(BlockBase.ReplaceValues(randomMin, data), CultureInfo.InvariantCulture);
				double fMax = double.Parse(BlockBase.ReplaceValues(randomMax, data), CultureInfo.InvariantCulture);
				text2 = (fMin + data.random.NextDouble() * (fMax - fMin)).ToString(CultureInfo.InvariantCulture);
				break;
			}
			case Function.MaxInt:
			{
				long a = long.Parse(text);
				long b = long.Parse(BlockBase.ReplaceValues(secondInput, data));
				text2 = Math.Max(a, b).ToString();
				break;
			}
			case Function.MinInt:
			{
				long a = long.Parse(text);
				long b = long.Parse(BlockBase.ReplaceValues(secondInput, data));
				text2 = Math.Min(a, b).ToString();
				break;
			}
			case Function.GetKey:
			{
				string dictName2 = text;
				string key2 = BlockBase.ReplaceValues(secondInput, data);
				var dict2 = data.Variables.GetDictionary(dictName2) ?? data.GlobalVariables.GetDictionary(dictName2);
				text2 = dict2 != null && dict2.TryGetValue(key2, out string dictVal) ? dictVal : "";
				break;
			}
			case Function.IndexOf:
			{
				string lstName2 = text;
				string item2 = BlockBase.ReplaceValues(secondInput, data);
				var lst2 = data.Variables.GetList(lstName2) ?? data.GlobalVariables.GetList(lstName2);
				text2 = (lst2 != null ? lst2.IndexOf(item2) : -1).ToString();
				break;
			}
			case Function.BCryptVerify:
				text2 = Crypto.BcryptVerify(text, BlockBase.ReplaceValues(secondInput, data)).ToString();
				break;
			case Function.ScryptDeriveKey:
			{
				string scryptSaltVal = BlockBase.ReplaceValues(secondInput, data);
				text2 = Crypto.ScryptEncoder(text, scryptSaltVal, ScryptCost, ScryptBlockSize, 1, ScryptOutputLength, false);
				break;
			}
			case Function.AWS4Signature:
			{
				string awsSecret  = BlockBase.ReplaceValues(secondInput, data);
				string awsDate    = BlockBase.ReplaceValues(thirdInput, data);
				string awsRegionV = BlockBase.ReplaceValues(awsRegion, data);
				string awsSvcV    = BlockBase.ReplaceValues(awsService, data);
				byte[] sigKey = HmacSha256Bytes(
					HmacSha256Bytes(
						HmacSha256Bytes(
							HmacSha256Bytes(Encoding.UTF8.GetBytes("AWS4" + awsSecret), awsDate),
							awsRegionV),
						awsSvcV),
					"aws4_request");
				text2 = Convert.ToHexString(HmacSha256Bytes(sigKey, text)).ToLower();
				break;
			}
			case Function.BCrypt:
				switch (BCryptMeth)
				{
				case BCryptMethods.Encode:
					text2 = ((!UseWorkFactor) ? Crypto.BcryptEncoder(BlockBase.ReplaceValues(text, data), null, BCryptSalt) : Crypto.BcryptEncoder(BlockBase.ReplaceValues(text, data), BCryptWorkFactor, BCryptSalt));
					break;
				case BCryptMethods.GenerateSalt:
					text2 = ((!UseWorkFactor) ? Crypto.BcryptGenerateSalt(null) : Crypto.BcryptGenerateSalt(BCryptWorkFactor));
					break;
				case BCryptMethods.Verify:
					text2 = Crypto.BcryptVerify(BlockBase.ReplaceValues(text, data), BlockBase.ReplaceValues(BCryptHashedPassword, data)).ToString();
					break;
				}
				break;
			}
			data.Log(new LogEntry($"Executed function {functionType} on input {text} with outcome {text2}", Colors.GreenYellow));
			list2.Add(text2);
		}
		bool recursive = list2.Count > 1 || InputString.Contains("[*]") || InputString.Contains("(*)") || InputString.Contains("{*}");
		BlockBase.InsertVariable(data, isCapture, recursive, list2, variableName);
		static long LongRandom(long min, long max, Random rand)
		{
			if (max <= min) return min;
			ulong range = unchecked((ulong)(max - min));
			if (range <= (ulong)int.MaxValue)
				return min + rand.Next(0, (int)range);
			var buf = new byte[8];
			rand.NextBytes(buf);
			return min + (long)(BitConverter.ToUInt64(buf, 0) % range);
		}
	}

	private static byte[] TryFromHexOrBase64(string s)
	{
		try { return Convert.FromHexString(s); } catch { }
		return Convert.FromBase64String(s);
	}

	// When not using base64 flag, try hex first (StringToBytes outputs hex),
	// fall back to raw UTF-8 for non-hex strings (e.g. HMAC key literals).
	private static byte[] TryHexDecodeOrUtf8(string s)
	{
		if (s.Length > 0 && s.Length % 2 == 0 &&
			System.Text.RegularExpressions.Regex.IsMatch(s, @"^[0-9a-fA-F]+$"))
			return Convert.FromHexString(s);
		return Encoding.UTF8.GetBytes(s);
	}

	private static byte[] HmacSha256Bytes(byte[] key, string data) =>
		new HMACSHA256(key).ComputeHash(Encoding.UTF8.GetBytes(data));

	public static string GetHash(string baseString, Hash type, bool inputBase64)
	{
		byte[] input = (inputBase64 ? Convert.FromBase64String(baseString) : Encoding.UTF8.GetBytes(baseString));
		return (type switch
		{
			Hash.MD2 => Crypto.MD2(input), 
			Hash.MD4 => Crypto.MD4(input), 
			Hash.MD5 => Crypto.MD5(input), 
			Hash.SHA1 => Crypto.SHA1(input), 
			Hash.SHA256 => Crypto.SHA256(input), 
			Hash.SHA384 => Crypto.SHA384(input), 
			Hash.SHA512 => Crypto.SHA512(input), 
			Hash.SHA3_224 => Crypto.SHA3_224(input), 
			Hash.SHA3_256 => Crypto.SHA3_256(input), 
			Hash.SHA3_384 => Crypto.SHA3_384(input), 
			Hash.SHA3_512 => Crypto.SHA3_512(input), 
			_ => throw new NotSupportedException("Unsupported algorithm"), 
		}).ToHex();
	}

	public static string Hmac(string baseString, Hash type, string key, bool inputBase64, bool keyBase64, bool outputBase64)
	{
		byte[] input = (inputBase64 ? Convert.FromBase64String(baseString) : TryHexDecodeOrUtf8(baseString));
		byte[] key2 = (keyBase64 ? Convert.FromBase64String(key) : Encoding.UTF8.GetBytes(key));
		byte[] array = type switch
		{
			Hash.MD5 => Crypto.HMACMD5(input, key2), 
			Hash.SHA1 => Crypto.HMACSHA1(input, key2), 
			Hash.SHA256 => Crypto.HMACSHA256(input, key2), 
			Hash.SHA384 => Crypto.HMACSHA384(input, key2), 
			Hash.SHA512 => Crypto.HMACSHA512(input, key2), 
			_ => throw new NotSupportedException("Unsupported algorithm"), 
		};
		if (!outputBase64)
		{
			return array.ToHex();
		}
		return Convert.ToBase64String(array);
	}

	public string GetDictionary()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<string, string> item in TranslationDictionary)
		{
			stringBuilder.Append(item.Key + ": " + item.Value);
			if (!item.Equals(TranslationDictionary.Last()))
			{
				stringBuilder.Append(Environment.NewLine);
			}
		}
		return stringBuilder.ToString();
	}

	public void SetDictionary(string[] lines)
	{
		TranslationDictionary.Clear();
		foreach (string text in lines)
		{
			if (text.Contains(':'))
			{
				string[] array = text.Split(new char[1] { ':' }, 2);
				string key = array[0];
				string value = array[1].TrimStart();
				TranslationDictionary[key] = value;
			}
		}
	}

	public static int CountStringOccurrences(string input, string text)
	{
		int num = 0;
		int startIndex = 0;
		while ((startIndex = input.IndexOf(text, startIndex)) != -1)
		{
			startIndex += text.Length;
			num++;
		}
		return num;
	}

	public static string[] SplitInChunks(string str, int chunkSize)
	{
		if (str.Length < chunkSize)
		{
			return new string[1] { str };
		}
		return (from i in Enumerable.Range(0, (int)Math.Ceiling((double)str.Length / (double)chunkSize))
			select str.Substring(i * chunkSize, Math.Min(str.Length - i * chunkSize, chunkSize))).ToArray();
	}

	// Generates a UUID v7 (time-sortable) compatible with RFC 9562.
	// .NET 9 has Guid.CreateVersion7() natively; this manual impl supports .NET 8.
	private static Guid CreateGuidV7()
	{
		long ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		Span<byte> b = stackalloc byte[16];
		System.Security.Cryptography.RandomNumberGenerator.Fill(b);
		// Bytes 0-5: 48-bit big-endian Unix timestamp in ms
		b[0] = (byte)(ms >> 40); b[1] = (byte)(ms >> 32); b[2] = (byte)(ms >> 24);
		b[3] = (byte)(ms >> 16); b[4] = (byte)(ms >> 8);  b[5] = (byte)(ms);
		// Byte 6 high nibble: version = 7
		b[6] = (byte)((b[6] & 0x0F) | 0x70);
		// Byte 8 high 2 bits: variant = 10
		b[8] = (byte)((b[8] & 0x3F) | 0x80);
		return new Guid(b);
	}
}
