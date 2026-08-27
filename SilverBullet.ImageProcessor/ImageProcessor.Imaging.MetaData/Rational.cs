using System;
using System.Globalization;
using System.Reflection;

namespace ImageProcessor.Imaging.MetaData;

[Serializable]
public readonly struct Rational<T> : IConvertible, IComparable, IComparable<T> where T : IConvertible
{
	private delegate T ParseDelegate(string value);

	private delegate bool TryParseDelegate(string value, out T rational);

	public static readonly Rational<T> Empty = default(Rational<T>);

	private const char Delim = '/';

	private static readonly char[] DelimSet = new char[1] { '/' };

	private static ParseDelegate parser;

	private static TryParseDelegate tryParser;

	private static decimal maxValue;

	private readonly T numerator;

	private readonly T denominator;

	public T Numerator => numerator;

	public T Denominator => denominator;

	public bool IsEmpty => Equals(Empty);

	private static decimal MaxValue
	{
		get
		{
			if (maxValue == 0m)
			{
				FieldInfo field = typeof(T).GetField("MaxValue", BindingFlags.Static | BindingFlags.Public);
				if (field != null)
				{
					try
					{
						maxValue = Convert.ToDecimal(field.GetValue(null));
					}
					catch (OverflowException)
					{
						maxValue = decimal.MaxValue;
					}
				}
				else
				{
					maxValue = 2147483647m;
				}
			}
			return maxValue;
		}
	}

	public Rational(T numerator, T denominator)
		: this(numerator, denominator, reduce: false)
	{
	}

	public Rational(T numerator, T denominator, bool reduce)
	{
		this.numerator = numerator;
		this.denominator = denominator;
		if (reduce)
		{
			Reduce(ref this.numerator, ref this.denominator);
		}
	}

	public static Rational<T> Approximate(decimal value)
	{
		return Approximate(value, 0.000001m);
	}

	public static Rational<T> Approximate(decimal value, decimal epsilon)
	{
		decimal num = decimal.Truncate(value);
		decimal num2 = 1m;
		decimal num3 = decimal.Divide(num, num2);
		decimal num4 = MaxValue;
		while (Math.Abs(num3 - value) > epsilon && num2 < num4 && num < num4)
		{
			if (num3 < value)
			{
				++num;
			}
			else
			{
				++num2;
				decimal num5 = Math.Round(decimal.Multiply(value, num2));
				if (num5 > num4)
				{
					--num2;
					break;
				}
				num = num5;
			}
			num3 = decimal.Divide(num, num2);
		}
		return new Rational<T>((T)Convert.ChangeType(num, typeof(T)), (T)Convert.ChangeType(num2, typeof(T)));
	}

	public static Rational<T> Parse(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return Empty;
		}
		if (parser == null)
		{
			parser = BuildParser();
		}
		string[] array = value.Split(DelimSet, 2, StringSplitOptions.RemoveEmptyEntries);
		T val = parser(array[0]);
		T val2 = ((array.Length > 1) ? parser(array[1]) : default(T));
		return new Rational<T>(val, val2);
	}

	public static bool TryParse(string value, out Rational<T> rational)
	{
		if (string.IsNullOrEmpty(value))
		{
			rational = Empty;
			return false;
		}
		if (tryParser == null)
		{
			tryParser = BuildTryParser();
		}
		string[] array = value.Split(DelimSet, 2, StringSplitOptions.RemoveEmptyEntries);
		if (!tryParser(array[0], out var rational2))
		{
			rational = Empty;
			return false;
		}
		T rational3;
		if (array.Length > 1)
		{
			if (!tryParser(array[1], out rational3))
			{
				rational = Empty;
				return false;
			}
		}
		else
		{
			rational3 = default(T);
		}
		rational = new Rational<T>(rational2, rational3);
		return array.Length == 2;
	}

	private static ParseDelegate BuildParser()
	{
		MethodInfo parse = typeof(T).GetMethod("Parse", BindingFlags.Static | BindingFlags.Public, null, new Type[1] { typeof(string) }, null);
		if (parse == null)
		{
			throw new InvalidOperationException("Underlying Rational type T must support Parse in order to parse Rational<T>.");
		}
		return delegate(string value)
		{
			try
			{
				return (T)parse.Invoke(null, new object[1] { value });
			}
			catch (TargetInvocationException ex)
			{
				if (ex.InnerException != null)
				{
					throw ex.InnerException;
				}
				throw;
			}
		};
	}

	private static TryParseDelegate BuildTryParser()
	{
		MethodInfo tryParse = typeof(T).GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public, null, new Type[2]
		{
			typeof(string),
			typeof(T).MakeByRefType()
		}, null);
		if (tryParse == null)
		{
			throw new InvalidOperationException("Underlying Rational type T must support TryParse in order to try-parse Rational<T>.");
		}
		return delegate(string value, out T output)
		{
			object[] array = new object[2]
			{
				value,
				default(T)
			};
			try
			{
				bool result = (bool)tryParse.Invoke(null, array);
				output = (T)array[1];
				return result;
			}
			catch (TargetInvocationException ex)
			{
				if (ex.InnerException != null)
				{
					throw ex.InnerException;
				}
				throw;
			}
		};
	}

	public Rational<T> Reduce()
	{
		T val = numerator;
		T val2 = denominator;
		Reduce(ref val, ref val2);
		return new Rational<T>(val, val2);
	}

	private static void Reduce(ref T numerator, ref T denominator)
	{
		bool flag = false;
		decimal num = Convert.ToDecimal(numerator);
		decimal num2 = Convert.ToDecimal(denominator);
		decimal num3 = Gcd(num, num2);
		if (num3 != 1m && num3 != 0m)
		{
			flag = true;
			num /= num3;
			num2 /= num3;
		}
		if (num2 < 0m)
		{
			flag = true;
			num = -num;
			num2 = -num2;
		}
		if (flag)
		{
			numerator = (T)Convert.ChangeType(num, typeof(T));
			denominator = (T)Convert.ChangeType(num2, typeof(T));
		}
	}

	private static decimal Lcd(decimal a, decimal b)
	{
		if (a == 0m && b == 0m)
		{
			return 0m;
		}
		return a * b / Gcd(a, b);
	}

	private static decimal Gcd(decimal a, decimal b)
	{
		if (a < 0m)
		{
			a = -a;
		}
		if (b < 0m)
		{
			b = -b;
		}
		while (a != b)
		{
			if (a == 0m)
			{
				return b;
			}
			if (b == 0m)
			{
				return a;
			}
			if (a > b)
			{
				a %= b;
			}
			else
			{
				b %= a;
			}
		}
		return a;
	}

	public string ToString(IFormatProvider provider)
	{
		return numerator.ToString(provider) + '/' + denominator.ToString(provider);
	}

	public decimal ToDecimal(IFormatProvider provider)
	{
		try
		{
			decimal num = denominator.ToDecimal(provider);
			if (num == 0m)
			{
				return 0m;
			}
			return numerator.ToDecimal(provider) / num;
		}
		catch (InvalidCastException)
		{
			long num2 = denominator.ToInt64(provider);
			if (num2 == 0L)
			{
				return 0m;
			}
			return ((IConvertible)numerator.ToInt64(provider)).ToDecimal(provider) / ((IConvertible)num2).ToDecimal(provider);
		}
	}

	public double ToDouble(IFormatProvider provider)
	{
		double num = denominator.ToDouble(provider);
		if (Math.Abs(num) < 1E-06)
		{
			return 0.0;
		}
		return numerator.ToDouble(provider) / num;
	}

	public float ToSingle(IFormatProvider provider)
	{
		float num = denominator.ToSingle(provider);
		if ((double)Math.Abs(num) < 1E-06)
		{
			return 0f;
		}
		return numerator.ToSingle(provider) / num;
	}

	bool IConvertible.ToBoolean(IFormatProvider provider)
	{
		return ((IConvertible)ToDecimal(provider)).ToBoolean(provider);
	}

	byte IConvertible.ToByte(IFormatProvider provider)
	{
		return ((IConvertible)ToDecimal(provider)).ToByte(provider);
	}

	char IConvertible.ToChar(IFormatProvider provider)
	{
		return ((IConvertible)ToDecimal(provider)).ToChar(provider);
	}

	short IConvertible.ToInt16(IFormatProvider provider)
	{
		return ((IConvertible)ToDecimal(provider)).ToInt16(provider);
	}

	int IConvertible.ToInt32(IFormatProvider provider)
	{
		return ((IConvertible)ToDecimal(provider)).ToInt32(provider);
	}

	long IConvertible.ToInt64(IFormatProvider provider)
	{
		return ((IConvertible)ToDecimal(provider)).ToInt64(provider);
	}

	sbyte IConvertible.ToSByte(IFormatProvider provider)
	{
		return ((IConvertible)ToDecimal(provider)).ToSByte(provider);
	}

	ushort IConvertible.ToUInt16(IFormatProvider provider)
	{
		return ((IConvertible)ToDecimal(provider)).ToUInt16(provider);
	}

	uint IConvertible.ToUInt32(IFormatProvider provider)
	{
		return ((IConvertible)ToDecimal(provider)).ToUInt32(provider);
	}

	ulong IConvertible.ToUInt64(IFormatProvider provider)
	{
		return ((IConvertible)ToDecimal(provider)).ToUInt64(provider);
	}

	DateTime IConvertible.ToDateTime(IFormatProvider provider)
	{
		return new DateTime(((IConvertible)this).ToInt64(provider));
	}

	TypeCode IConvertible.GetTypeCode()
	{
		return numerator.GetTypeCode();
	}

	object IConvertible.ToType(Type conversionType, IFormatProvider provider)
	{
		if (conversionType == null)
		{
			throw new ArgumentNullException("conversionType");
		}
		if (GetType() == conversionType)
		{
			return this;
		}
		if (!conversionType.IsGenericType || typeof(Rational<>) != conversionType.GetGenericTypeDefinition())
		{
			return Convert.ChangeType(this, conversionType, provider);
		}
		Type type = conversionType.GetGenericArguments()[0];
		object[] parameters = new object[2]
		{
			Convert.ChangeType(Numerator, type, provider),
			Convert.ChangeType(Denominator, type, provider)
		};
		ConstructorInfo constructor = conversionType.GetConstructor(new Type[2] { type, type });
		if (constructor == null)
		{
			throw new InvalidCastException("Unable to find constructor for Rational<" + type.Name + ">.");
		}
		return constructor.Invoke(parameters);
	}

	public int CompareTo(object obj)
	{
		if (obj is Rational<T> rational)
		{
			Rational<T> rational2 = rational;
			if (Convert.ToDecimal(denominator) == 0m)
			{
				if (Convert.ToDecimal(rational2.denominator) == 0m)
				{
					return Convert.ToDecimal(numerator).CompareTo(Convert.ToDecimal(rational2.numerator));
				}
				if (Convert.ToDecimal(rational2.numerator) == 0m)
				{
					return Convert.ToDecimal(denominator).CompareTo(Convert.ToDecimal(rational2.denominator));
				}
			}
			else if (Convert.ToDecimal(rational2.denominator) == 0m && Convert.ToDecimal(numerator) == 0m)
			{
				return Convert.ToDecimal(denominator).CompareTo(Convert.ToDecimal(rational2.denominator));
			}
		}
		return Convert.ToDecimal(this).CompareTo(Convert.ToDecimal(obj));
	}

	public int CompareTo(T other)
	{
		return decimal.Compare(Convert.ToDecimal(this), Convert.ToDecimal(other));
	}

	public static Rational<T> operator -(Rational<T> rational)
	{
		return new Rational<T>((T)Convert.ChangeType(-Convert.ToDecimal(rational.numerator), typeof(T)), rational.denominator);
	}

	public static Rational<T> operator +(Rational<T> r1, Rational<T> r2)
	{
		decimal num = Convert.ToDecimal(r1.numerator);
		decimal num2 = Convert.ToDecimal(r1.denominator);
		decimal num3 = Convert.ToDecimal(r2.numerator);
		decimal num4 = Convert.ToDecimal(r2.denominator);
		decimal num5 = Lcd(num2, num4);
		if (num5 > num2)
		{
			num *= num5 / num2;
		}
		if (num5 > num4)
		{
			num3 *= num5 / num4;
		}
		return new Rational<T>((T)Convert.ChangeType(num + num3, typeof(T)), (T)Convert.ChangeType(num5, typeof(T)));
	}

	public static Rational<T> operator -(Rational<T> r1, Rational<T> r2)
	{
		return r1 + -r2;
	}

	public static Rational<T> operator *(Rational<T> r1, Rational<T> r2)
	{
		decimal num = Convert.ToDecimal(r1.numerator) * Convert.ToDecimal(r2.numerator);
		return new Rational<T>(denominator: (T)Convert.ChangeType(Convert.ToDecimal(r1.denominator) * Convert.ToDecimal(r2.denominator), typeof(T)), numerator: (T)Convert.ChangeType(num, typeof(T)));
	}

	public static Rational<T> operator /(Rational<T> r1, Rational<T> r2)
	{
		return r1 * new Rational<T>(r2.denominator, r2.numerator);
	}

	public static bool operator <(Rational<T> r1, Rational<T> r2)
	{
		return r1.CompareTo(r2) < 0;
	}

	public static bool operator <=(Rational<T> r1, Rational<T> r2)
	{
		return r1.CompareTo(r2) <= 0;
	}

	public static bool operator >(Rational<T> r1, Rational<T> r2)
	{
		return r1.CompareTo(r2) > 0;
	}

	public static bool operator >=(Rational<T> r1, Rational<T> r2)
	{
		return r1.CompareTo(r2) >= 0;
	}

	public static bool operator ==(Rational<T> r1, Rational<T> r2)
	{
		return r1.CompareTo(r2) == 0;
	}

	public static bool operator !=(Rational<T> r1, Rational<T> r2)
	{
		return r1.CompareTo(r2) != 0;
	}

	public override string ToString()
	{
		return Convert.ToString(this, CultureInfo.InvariantCulture);
	}

	public override bool Equals(object obj)
	{
		return CompareTo(obj) == 0;
	}

	public override int GetHashCode()
	{
		return (Numerator, Denominator).GetHashCode();
	}
}
