using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;

namespace NetJSON.Internals
{
	public sealed class TupleContainer
	{
		private int _size;
		private int _index;

		private object _1, _2, _3, _4, _5, _6, _7, _8;

		public TupleContainer(int size) {
			_size = size;
		}


		public Tuple<T1> ToTuple<T1>() {
			return new Tuple<T1>((T1)_1);
		}

		public Tuple<T1, T2> ToTuple<T1, T2>() {
			return new Tuple<T1, T2>((T1)_1, (T2)_2);
		}

		public Tuple<T1, T2, T3> ToTuple<T1, T2, T3>() {
			return new Tuple<T1, T2, T3>((T1)_1, (T2)_2, (T3)_3);
		}

		public Tuple<T1, T2, T3, T4> ToTuple<T1, T2, T3, T4>() {
			return new Tuple<T1, T2, T3, T4>((T1)_1, (T2)_2, (T3)_3, (T4)_4);
		}

		public Tuple<T1, T2, T3, T4, T5> ToTuple<T1, T2, T3, T4, T5>() {
			return new Tuple<T1, T2, T3, T4, T5>((T1)_1, (T2)_2, (T3)_3, (T4)_4, (T5)_5);
		}

		public Tuple<T1, T2, T3, T4, T5, T6> ToTuple<T1, T2, T3, T4, T5, T6>() {
			return new Tuple<T1, T2, T3, T4, T5, T6>((T1)_1, (T2)_2, (T3)_3, (T4)_4, (T5)_5, (T6)_6);
		}

		public Tuple<T1, T2, T3, T4, T5, T6, T7> ToTuple<T1, T2, T3, T4, T5, T6, T7>() {
			return new Tuple<T1, T2, T3, T4, T5, T6, T7>((T1)_1, (T2)_2, (T3)_3, (T4)_4, (T5)_5, (T6)_6, (T7)_7);
		}

		public Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> ToTuple<T1, T2, T3, T4, T5, T6, T7, TRest>() {
			return new Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>((T1)_1, (T2)_2, (T3)_3, (T4)_4, (T5)_5, (T6)_6, (T7)_7, (TRest)_8);
		}

		public void Add(object value) {
			switch (_index) {
				case 0:
					_1 = value;
					break;
				case 1:
					_2 = value;
					break;
				case 2:
					_3 = value;
					break;
				case 3:
					_4 = value;
					break;
				case 4:
					_5 = value;
					break;
				case 5:
					_6 = value;
					break;
				case 6:
					_7 = value;
					break;
				case 7:
					_8 = value;
					break;
			}
			_index++;
		}
	}

	sealed class NetJSONMemberInfo
	{
		internal MemberInfo Member { get; set; }
		internal NetJSONPropertyAttribute Attribute { get; set; }
	}

	static partial class CompatibleExtensions
	{
#if !NET_STANDARD && !NET_PCL && !NET_46 && !NET_47
		internal static Type GetTypeInfo(this Type type) {
			return type;
		}
#endif

        internal static void EmitClearStringBuilder(this ILGenerator il) {
#if !NET_35
			il.Emit(OpCodes.Callvirt, typeof(StringBuilder).GetMethod("Clear"));
#else
			il.Emit(OpCodes.Call, typeof(StringBuilder35Extension).GetMethod("Clear"));
#endif
		}

        internal static bool IsClass(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            return typeInfo.IsClass || typeInfo.IsInterface;
        }
	}
	internal static class SerializerUtilities
	{
		const char QuotDoubleChar = '"',
				   QuotSingleChar = '\'';
		const int DefaultStringBuilderCapacity = 1024 * 2;

		private static char[] _dateNegChars = new[] { '-' },
			_datePosChars = new[] { '+' };
		private static Regex //_dateRegex = new Regex(@"\\/Date\((?<ticks>-?\d+)\)\\/", RegexOptions.Compiled),
			_dateISORegex = new Regex(@"(\d){4}-(\d){2}-(\d){2}T(\d){2}:(\d){2}:(\d){2}.(\d){3}Z", RegexOptions.Compiled);

		[ThreadStatic]
		private static StringBuilder _cachedDateStringBuilder;

		private static DateTime Epoch = new DateTime(1970, 1, 1),
			UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);

		[ThreadStatic]
		private static StringBuilder _cachedObjectStringBuilder;

		internal static StringBuilder CachedObjectStringBuilder() {
			return (_cachedObjectStringBuilder ?? (_cachedObjectStringBuilder = new StringBuilder(25))).Clear();
		}

		[ThreadStatic]
		private static StringBuilder _cachedStringBuilder;
		internal static StringBuilder GetStringBuilder() {
			return _cachedStringBuilder ?? (_cachedStringBuilder = new StringBuilder(DefaultStringBuilderCapacity));
		}


		internal unsafe static string CreateString(string str, int startIndex, int length) {
			fixed (char* ptr = str)
				return new string(ptr, startIndex, length);
		}

#if NET_STANDARD
        // Retrieved from https://github.com/dotnet/corefx/pull/10088
        private static readonly Func<Type, object> s_getUninitializedObjectDelegate = (Func<Type, object>)
 typeof(string).GetTypeInfo().Assembly.GetType("System.Runtime.Serialization.FormatterServices")
            ?.GetMethod("GetUninitializedObject", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
            ?.CreateDelegate(typeof(Func<Type, object>));

        internal static object GetUninitializedObject(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return s_getUninitializedObjectDelegate(type);
        }
#endif

        internal static T GetUninitializedInstance<T>() {
#if NET_STANDARD
            return (T)GetUninitializedObject(typeof(T));
#else
			return (T)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(T));
#endif
		}

		internal static object GetTypeIdentifierInstance(string typeName) {
			return NetJSON.GetTypeIdentifierInstance(typeName);
		}

        internal unsafe static void ThrowIfInvalidJSON(string json, char chr)
        {
#if NET_35
            if(json.IsNullOrWhiteSpace())
#else
            if (String.IsNullOrWhiteSpace(json))
#endif
                throw new NetJSONInvalidJSONException();

            var endChar = chr == '[' ? ']' : '}';

            if(json[0] != chr)
            {
                throw new NetJSONInvalidJSONException();
            }

            var length = json.Length - 1;
            var lastChr = '\0';
            fixed (char* ptr = json)
            {
                do
                {
                    lastChr = *(ptr + length);
                    if (lastChr != '\n' && lastChr != '\r' && lastChr != '\t' && lastChr != ' ')
                    {
                        break;
                    }
                    length--;
                } while (lastChr != '\0');
            }

            if (!(json[0] == chr && lastChr == endChar))
            {
                throw new NetJSONInvalidJSONException();
            }
        }

        internal unsafe static bool IsInRange(char* ptr, ref int index, int offset, string key, NetJSONSettings settings) {
			var inRangeChr = *(ptr + index + offset + 2);
            fixed (char* kPtr = key)
            {
                return (*(ptr + index) == settings._quoteChar && 
                    (inRangeChr == ':' || inRangeChr == ' ' || 
                    inRangeChr == '\t' || inRangeChr == '\n' || inRangeChr == '\r')) &&
                    *(ptr + index + 1) == *kPtr;
            }
        }

		internal unsafe static bool FastStringToBool(string value) {
			return value[0] == 't';
		}

		internal static byte[] FastStringToByteArray(string value) {

#if NET_35
            if (value.IsNullOrWhiteSpace())
                return null;
#else
			if (string.IsNullOrWhiteSpace(value))
				return null;
#endif
			return Convert.FromBase64String(value);
		}

		internal static char FastStringToChar(string value) {
			return value[0];
		}

		internal static DateTimeOffset FastStringToDateTimeoffset(string value, NetJSONSettings settings) {
			TimeSpan offset;
			var date = StringToDate(value, settings, out offset, isDateTimeOffset: true);
			return new DateTimeOffset(date.Ticks, offset);
		}

		internal static DateTime FastStringToDate(string value, NetJSONSettings settings) {
			TimeSpan offset;
			return StringToDate(value, settings, out offset, isDateTimeOffset: false);
		}

		internal unsafe static string ToCamelCase(string str) {
			fixed (char* p = str) {
				char* buffer = stackalloc char[str.Length];
				int c = 0;
				char* ptr = p;
				pc:
				char f = *ptr;
				if (c == 0 && f >= 65 && f <= 90)
					*buffer = (char)(f + 32);
				else if (c > 0 && f >= 97 && f <= 122)
					*buffer = (char)(f - 32);
				else
					*buffer = *ptr;
				++c;
				ptr++;
				buffer++;
				while ((f = *(ptr++)) != '\0') {
					if (f == ' ' || f == '_') {
						goto pc;
					}
					*(buffer++) = f;
					++c;
				}
				buffer -= c;
				return new string(buffer, 0, c);
			}
		}

		internal static string GuidToStr(Guid value) {
			//TODO: Optimize
			return value.ToString();
		}

		internal static string ByteArrayToStr(byte[] value) {
			//TODO: Optimize
			return Convert.ToBase64String(value);
		}

		internal static bool IsListType(Type type) {
			return type.IsListType();
		}

		internal static bool IsDictionaryType(Type type) {
			return type.IsDictionaryType();
		}

		internal static bool IsCollectionType(this Type type) {
			return type.IsListType() || type.IsDictionaryType();
		}

		internal static bool IsRawPrimitive(string value) {
			value = value.Trim();
			return !value.StartsWith("{") && !value.StartsWith("[");
		}

		internal static bool IsCurrentAQuot(char current, NetJSONSettings settings) {
			if (settings.HasOverrideQuoteChar)
				return current == settings._quoteChar;
			var quote = settings._quoteChar;
			var isQuote = current == QuotSingleChar || current == QuotDoubleChar;
			if (isQuote) {
				if (quote != current)
					settings._quoteCharString = (settings._quoteChar = current).ToString();
				settings.HasOverrideQuoteChar = true;
			}
			return isQuote;
		}

		internal static List<object> ListToListObject(IList list) {
			return list.Cast<object>().ToList();
		}

		internal static bool NeedQuotes(Type type, NetJSONSettings settings) {
			return NetJSON.NeedQuotes(type, settings);
		}

		internal static bool IsValueDate(string value) {
			return value.StartsWith("\\/Date") || _dateISORegex.IsMatch(value);
		}

        internal unsafe static string ToStringIfString(object value, NetJSONSettings settings)
        {
            var str = value as string;
            if(str != null)
            {
                StringBuilder sb = new StringBuilder();
                NetJSON.EncodedJSONString(sb, str, settings);
                return sb.ToString();
            }

            return str;
        }

        internal unsafe static object ToStringIfStringObject(object value, NetJSONSettings settings)
        {
            var str = value as string;
            if (str != null)
            {
                str = string.Concat('"', str, '"');
                fixed (char* p = str)
                {
                    char* ptr = p;
                    int index = 0;
                    return NetJSON.DecodeJSONString(ptr, ref index, settings, fromObject: true);
                }
            }

            return value;
        }

        private static DateTime StringToDate(string value, NetJSONSettings settings, out TimeSpan offset, bool isDateTimeOffset) {
			offset = TimeSpan.Zero;

            if (settings._hasDateStringFormat)
            {
                return DateTime.ParseExact(value, settings.DateStringFormat, CultureInfo.CurrentCulture);
            }

			if (settings.DateFormat == NetJSONDateFormat.EpochTime) {
				var unixTimeStamp = FastStringToLong(value);
				var date = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
				return date.AddTicks(unixTimeStamp).ToLocalTime();
			}

			DateTime dt;
			string[] tokens = null;
			bool negative = false;
			string offsetText = null;
			int tickMilliseconds = 0;
			bool noOffSetValue = false;
			var timeZoneFormat = settings.TimeZoneFormat;

			if (value == "\\/Date(-62135596800)\\/")
				return DateTime.MinValue;
			else if (value == "\\/Date(253402300800)\\/")
				return DateTime.MaxValue;
			else if (value[0] == '\\') {
				var dateText = value.Substring(7, value.IndexOf(')', 7) - 7);
				negative = dateText.IndexOf('-') >= 0;
				tokens = negative ? dateText.Split(_dateNegChars, StringSplitOptions.RemoveEmptyEntries)
					: dateText.Split(_datePosChars, StringSplitOptions.RemoveEmptyEntries);
				dateText = tokens[0];

				var ticks = FastStringToLong(dateText);
                var multiply = settings.DateFormat == NetJSONDateFormat.JavascriptSerializer ? TimeSpan.TicksPerMillisecond : 1;

				dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

				dt = dt.AddTicks(ticks * multiply);

				if (timeZoneFormat == NetJSONTimeZoneFormat.Unspecified || timeZoneFormat == NetJSONTimeZoneFormat.Utc)
					dt = dt.ToLocalTime();

				var kind = timeZoneFormat == NetJSONTimeZoneFormat.Local ? DateTimeKind.Local :
					timeZoneFormat == NetJSONTimeZoneFormat.Utc ? DateTimeKind.Utc :
					DateTimeKind.Unspecified;

				dt = new DateTime(dt.Ticks, kind);

				offsetText = tokens.Length > 1 ? tokens[1] : offsetText;
			}
			else {
				var dateText = value.Substring(0, 19);
				var diff = value.Length - dateText.Length;
				var hasOffset = diff > 0;
				var utcOffsetText = hasOffset ? value.Substring(dateText.Length, diff) : string.Empty;
				var firstChar = utcOffsetText.Length > 0 ? utcOffsetText[0] : '\0';
				negative = diff > 0 && firstChar == '-';
				if (hasOffset) {
					noOffSetValue = timeZoneFormat == NetJSONTimeZoneFormat.Utc || timeZoneFormat == NetJSONTimeZoneFormat.Unspecified;
					offsetText = utcOffsetText.Substring(1, utcOffsetText.Length - 1).Replace(":", string.Empty).Replace("Z", string.Empty);
					if (timeZoneFormat == NetJSONTimeZoneFormat.Local) {
						int indexOfSign = offsetText.IndexOf('-');
						negative = indexOfSign >= 0;
						if (!negative) {
							indexOfSign = offsetText.IndexOf('+');
						}
						tickMilliseconds = FastStringToInt(offsetText.Substring(0, indexOfSign));
						offsetText = offsetText.Substring(indexOfSign + 1, offsetText.Length - indexOfSign - 1);
						if (negative)
							offsetText = offsetText.Replace("-", string.Empty);
						else
							offsetText = offsetText.Replace("+", string.Empty);
					}
					else {
						tickMilliseconds = FastStringToInt(offsetText);
					}
				}
				dt = DateTime.Parse(dateText, CultureInfo.CurrentCulture, DateTimeStyles.AdjustToUniversal);
				if (timeZoneFormat == NetJSONTimeZoneFormat.Local) {
					if (!isDateTimeOffset)
						dt = dt.ToUniversalTime();
					dt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, DateTimeKind.Local);
				}
				else if (timeZoneFormat == NetJSONTimeZoneFormat.Utc) {
					dt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, DateTimeKind.Utc);
				}
			}

			var isNullOrWhiteSpace = false;

#if NET_35
            isNullOrWhiteSpace = offsetText.IsNullOrWhiteSpace();
#else
			isNullOrWhiteSpace = string.IsNullOrWhiteSpace(offsetText);
#endif

			if (!isNullOrWhiteSpace) {
				var hours = noOffSetValue ? 0 : FastStringToInt(offsetText.Substring(0, 2));
				var minutes = noOffSetValue ? 0 : (offsetText.Length > 2 ? FastStringToInt(offsetText.Substring(2, 2)) : 0);
				if (negative)
					hours *= -1;
				offset = new TimeSpan(hours, minutes, 0);
				if (!isDateTimeOffset)
					dt = dt.AddHours(hours).AddMinutes(minutes);
				dt = dt.AddTicks(tickMilliseconds);
			}

			return dt;
		}

		internal static Guid FastStringToGuid(string value) {
            //TODO: Optimize
            return new Guid(value);
        }

		internal static Type FastStringToType(string value) {
			return Type.GetType(value, false);
		}

		internal static unsafe byte FastStringToByte(string str) {
			unchecked {
				return (byte)FastStringToInt(str);
			}
		}

		internal static unsafe short FastStringToShort(string str) {
			unchecked {
				return (short)FastStringToInt(str);
			}
		}

		internal static unsafe ushort FastStringToUShort(string str) {
			unchecked {
				return (ushort)FastStringToInt(str);
			}
		}

		internal static unsafe int FastStringToInt(string strNum) {
			int val = 0;
			int neg = 1;
			fixed (char* ptr = strNum) {
				char* str = ptr;
				if (*str == '-') {
					neg = -1;
					++str;
				}
				while (*str != '\0') {
					val = val * 10 + (*str++ - '0');
				}
			}
			return val * neg;
		}

		internal static unsafe uint FastStringToUInt(string strNum) {
			uint val = 0;
			fixed (char* ptr = strNum) {
				char* str = ptr;
				if (*str == '-') {
					val = (uint)-val;
					++str;
				}
				while (*str != '\0') {
					val = val * 10 + (uint)(*str++ - '0');
				}
			}
			return val;
		}

		internal static unsafe long FastStringToLong(string strNum) {
			long val = 0;
			long neg = 1;
			fixed (char* ptr = strNum) {
				char* str = ptr;
				if (*str == '-') {
					neg = -1;
					++str;
				}
				while (*str != '\0') {
					val = val * 10 + (*str++ - '0');
				}
			}
			return val * neg;
		}

		internal static unsafe ulong FastStringToULong(string strNum) {
			ulong val = 0;
			fixed (char* ptr = strNum) {
				char* str = ptr;
				while (*str != '\0') {
					val = val * 10 + (ulong)(*str++ - '0');
				}
			}
			return val;
		}

		internal static unsafe double FastStringToDouble(string numStr) {
			double val = 0.0;
			double neg = 1;
			fixed (char* ptr = numStr) {
				char* p = ptr;
				if (*p == '-') {
					neg = -1;
					++p;
				}
				int count = 0;
				while (*p != '\0') {
					if (*p == '.') {
						double rem = 0.0;
						double div = 1;
						++p;
						while (*p != '\0') {
							if (*p == 'E' || *p == 'e') {
								var e = 0;
								val += rem * (Math.Pow(10, -1 * count));
								++p;
								var ePlusMinus = 1;
								if (*p == '-' || *p == '+') {
									if (*p == '-')
										ePlusMinus = -1;
									++p;
								}
								while (*p != '\0') {
									e = e * 10 + (*p++ - '0');
								}
								val *= Math.Pow(10, e * ePlusMinus);
								return val * neg;
							}
							rem = (rem * 10.0) + (*p - '0');
							div *= 10.0;
							++p;
							count++;
						}
						val += rem / div;
						return ((double)(decimal)val) * neg;
					}
					val = (val * 10) + (*p - '0');
					++p;
				}
			}
			return ((double)(decimal)val) * neg;
		}

		internal static unsafe float FastStringToFloat(string numStr) {
			return (float)FastStringToDouble(numStr);
		}

		internal static decimal FastStringToDecimal(string numStr) {
			return new Decimal(FastStringToDouble(numStr));
		}

		internal static string FloatToStr(float value) {
			return value.ToString(CultureInfo.InvariantCulture);
		}

		internal static string DoubleToStr(double value) {
			return value.ToString(CultureInfo.InvariantCulture);
		}

		internal static string SByteToStr(sbyte value) {
			return value.ToString(CultureInfo.InvariantCulture);
		}

		internal static string DecimalToStr(decimal value) {
			return value.ToString(CultureInfo.InvariantCulture);
		}

		internal static unsafe void SkipProperty(char* ptr, ref int index, NetJSONSettings settings) {
			var currentIndex = index;
			char current = '\0';
			char bchar = '\0';
			char echar = '\0';
			bool isStringType = false;
			bool isNonStringType = false;
			int counter = 0;
			bool hasChar = false;
			var currentQuote = settings._quoteChar;

			while (true) {
				current = *(ptr + index);
				if (current != ' ' && current != ':' && current != '\n' && current != '\r' && current != '\t') {
					if (!hasChar) {
						isStringType = current == currentQuote;
						if (!isStringType)
							isNonStringType = current != '{' && current != '[';
						if (isStringType || isNonStringType)
							break;
						bchar = current;
						echar = current == '{' ? '}' :
							current == '[' ? ']' : '\0';
						counter = 1;
						hasChar = true;
					}
					else {
						if ((current == '{' && bchar == '{') || (current == '[' && bchar == '['))
							counter++;
						else if ((current == '}' && echar == '}') || (current == ']' && echar == ']'))
							counter--;
					}
				}
				index++;
				if (hasChar && counter == 0)
					break;
			}

			if (isStringType || isNonStringType) {
				index = currentIndex;
				if (isStringType)
					GetStringBasedValue(ptr, ref index, settings);
				else if (isNonStringType)
					GetNonStringValue(ptr, ref index);
			}
		}

		internal unsafe static string GetStringBasedValue(char* ptr, ref int index, NetJSONSettings settings) {
			char current = '\0', prev = '\0';
			int count = 0, startIndex = 0;
			string value = string.Empty;

			while (true) {
				current = ptr[index];
				if (count == 0 && current == settings._quoteChar) {
					startIndex = index + 1;
					++count;
				}
				else if (count > 0 && current == settings._quoteChar && prev != '\\') {
					value = new string(ptr, startIndex, index - startIndex);
					++index;
					break;
				}
				else if (count == 0 && current == 'n') {
					index += 3;
					return null;
				}

				prev = current;
				++index;
			}

			return value;
		}

		internal unsafe static string GetNonStringValue(char* ptr, ref int index) {
			char current = '\0';
			int startIndex = -1;
			string value = string.Empty;
			int indexDiff = 0;

			while (true) {
				current = ptr[index];
				if (startIndex > -1) {
					switch (current) {
						case '\n':
						case '\r':
						case '\t':
						case ' ':
							++indexDiff;
							break;
					}
				}
				if (current != ' ' && current != ':') {
					if (startIndex == -1)
						startIndex = index;
					if (current == ',' || current == ']' || current == '}' || current == '\0') {
						value = new string(ptr, startIndex, index - startIndex - indexDiff);
						--index;
						break;
					}
					else if (current == 't') {
						index += 4;
						return "true";
					}
					else if (current == 'f') {
						index += 5;
						return "false";
					}
					else if (current == 'n') {
						index += 4;
						return null;
					}
                    else if(!(((int)current) >= 48 && ((int)current) <= 57))
                    {
                        value = new string(ptr, startIndex, index - startIndex - indexDiff);
                        --index;
                        break;
                    }
				}
				++index;
			}
			if (value == "null")
				return null;
			return value;
		}

		internal unsafe static int MoveToArrayBlock(char* str, ref int index) {
			char* ptr = str + index;

			if (*ptr == '[')
				index++;
			else {
				do {
					index++;
					if (*(ptr = str + index) == 'n') {
						index += 3;
						return 0;
					}
				} while (*ptr != '[');
				index++;
			}
			return 1;
		}

		internal static bool IsEndChar(char current) {
			return current == ':' || current == '{' || current == ' ';
		}

		internal static bool IsArrayEndChar(char current) {
			return current == ',' || current == ']' || current == ' ';
		}

		internal static bool IsCharTag(char current) {
			return current == '{' || current == '}';
		}

		internal static bool CustomTypeEquality(Type type1, Type type2) {
			if (type1
#if NET_STANDARD
    .GetTypeInfo()
#endif
				.IsEnum) {
				if (type1
#if NET_STANDARD
    .GetTypeInfo()
#endif
					.IsEnum && type2 == typeof(Enum))
					return true;
			}
			return type1 == type2;
		}

		internal static string CustomEnumToStr(Enum @enum, NetJSONSettings settings) {
			if (settings.UseEnumString)
				return @enum.ToString();
			return IntToStr((int)((object)@enum));
		}

		internal static string CharToStr(char chr) {
			return chr.ToString();
		}

		internal unsafe static string IntToStr(int snum) {
			char* s = stackalloc char[12];
			char* ps = s;
			int num1 = snum, num2, num3, div;
			if (snum < 0) {
				*ps++ = '-';
				//Can't negate int min
				if (snum == -2147483648)
					return "-2147483648";
				num1 = -num1;
			}
			if (num1 < 10000) {
				if (num1 < 10) goto L1;
				if (num1 < 100) goto L2;
				if (num1 < 1000) goto L3;
			}
			else {
				num2 = num1 / 10000;
				num1 -= num2 * 10000;
				if (num2 < 10000) {
					if (num2 < 10) goto L5;
					if (num2 < 100) goto L6;
					if (num2 < 1000) goto L7;
				}
				else {
					num3 = num2 / 10000;
					num2 -= num3 * 10000;
					if (num3 >= 10) {
						*ps++ = (char)('0' + (char)(div = (num3 * 6554) >> 16));
						num3 -= div * 10;
					}
					*ps++ = (char)('0' + (num3));
				}
				*ps++ = (char)('0' + (div = (num2 * 8389) >> 23));
				num2 -= div * 1000;
				L7:
				*ps++ = (char)('0' + (div = (num2 * 5243) >> 19));
				num2 -= div * 100;
				L6:
				*ps++ = (char)('0' + (div = (num2 * 6554) >> 16));
				num2 -= div * 10;
				L5:
				*ps++ = (char)('0' + (num2));
			}
			*ps++ = (char)('0' + (div = (num1 * 8389) >> 23));
			num1 -= div * 1000;
			L3:
			*ps++ = (char)('0' + (div = (num1 * 5243) >> 19));
			num1 -= div * 100;
			L2:
			*ps++ = (char)('0' + (div = (num1 * 6554) >> 16));
			num1 -= div * 10;
			L1:
			*ps++ = (char)('0' + (num1));

			return new string(s);
		}

		internal unsafe static string LongToStr(long snum) {
			char* s = stackalloc char[21];
			char* ps = s;
			long num1 = snum, num2, num3, num4, num5, div;

			if (snum < 0) {
				*ps++ = '-';
				if (snum == -9223372036854775808) return "-9223372036854775808";
				num1 = -snum;
			}

			if (num1 < 10000) {
				if (num1 < 10) goto L1;
				if (num1 < 100) goto L2;
				if (num1 < 1000) goto L3;
			}
			else {
				num2 = num1 / 10000;
				num1 -= num2 * 10000;
				if (num2 < 10000) {
					if (num2 < 10) goto L5;
					if (num2 < 100) goto L6;
					if (num2 < 1000) goto L7;
				}
				else {
					num3 = num2 / 10000;
					num2 -= num3 * 10000;
					if (num3 < 10000) {
						if (num3 < 10) goto L9;
						if (num3 < 100) goto L10;
						if (num3 < 1000) goto L11;
					}
					else {
						num4 = num3 / 10000;
						num3 -= num4 * 10000;
						if (num4 < 10000) {
							if (num4 < 10) goto L13;
							if (num4 < 100) goto L14;
							if (num4 < 1000) goto L15;
						}
						else {
							num5 = num4 / 10000;
							num4 -= num5 * 10000;
							if (num5 < 10000) {
								if (num5 < 10) goto L17;
								if (num5 < 100) goto L18;
							}
							*ps++ = (char)('0' + (div = (num5 * 5243) >> 19));
							num5 -= div * 100;
							L18:
							*ps++ = (char)('0' + (div = (num5 * 6554) >> 16));
							num5 -= div * 10;
							L17:
							*ps++ = (char)('0' + (num5));
						}
						*ps++ = (char)('0' + (div = (num4 * 8389) >> 23));
						num4 -= div * 1000;
						L15:
						*ps++ = (char)('0' + (div = (num4 * 5243) >> 19));
						num4 -= div * 100;
						L14:
						*ps++ = (char)('0' + (div = (num4 * 6554) >> 16));
						num4 -= div * 10;
						L13:
						*ps++ = (char)('0' + (num4));
					}
					*ps++ = (char)('0' + (div = (num3 * 8389) >> 23));
					num3 -= div * 1000;
					L11:
					*ps++ = (char)('0' + (div = (num3 * 5243) >> 19));
					num3 -= div * 100;
					L10:
					*ps++ = (char)('0' + (div = (num3 * 6554) >> 16));
					num3 -= div * 10;
					L9:
					*ps++ = (char)('0' + (num3));
				}
				*ps++ = (char)('0' + (div = (num2 * 8389) >> 23));
				num2 -= div * 1000;
				L7:
				*ps++ = (char)('0' + (div = (num2 * 5243) >> 19));
				num2 -= div * 100;
				L6:
				*ps++ = (char)('0' + (div = (num2 * 6554) >> 16));
				num2 -= div * 10;
				L5:
				*ps++ = (char)('0' + (num2));
			}
			*ps++ = (char)('0' + (div = (num1 * 8389) >> 23));
			num1 -= div * 1000;
			L3:
			*ps++ = (char)('0' + (div = (num1 * 5243) >> 19));
			num1 -= div * 100;
			L2:
			*ps++ = (char)('0' + (div = (num1 * 6554) >> 16));
			num1 -= div * 10;
			L1:
			*ps++ = (char)('0' + (num1));

			return new string(s);
		}

		internal static string AllDateToString(DateTime date, NetJSONSettings settings) {
			var offset =
#if NET_STANDARD
                    TimeZoneInfo.Local.GetUtcOffset(date);
#else
					TimeZone.CurrentTimeZone.GetUtcOffset(date);
#endif
			return DateToStringWithOffset(date, settings, offset);
		}

		internal static string AllDateOffsetToString(DateTimeOffset offset, NetJSONSettings settings) {
			return DateToStringWithOffset(offset.DateTime, settings, offset.Offset);
		}

        internal static T FlagStringToEnum<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value);
        }

        internal static string FlagEnumToString(object value, NetJSONSettings settings)
        {
            if (settings.UseEnumString)
            {
                return ((Enum)value).ToString();
            }
            var eType = value.GetType().GetTypeInfo().GetEnumUnderlyingType();
            if (eType == NetJSON._intType)
                return IntToStr((int)value);
            else if (eType == NetJSON._longType)
                return LongToStr((long)value);
            else if (eType == typeof(ulong))
                return LongToStr((long)((ulong)value));
            else if (eType == typeof(uint))
                return IntUtility.uitoa((uint)value);
            else if (eType == typeof(byte))
            {
                return IntToStr((int)((byte)value));
            }
            else if (eType == typeof(ushort))
            {
                return IntToStr((int)((ushort)value));
            }
            else if (eType == typeof(short))
            {
                return IntToStr((int)((short)value));
            }
            return IntToStr((int)value);
        }

        private static string DateToStringWithOffset(DateTime date, NetJSONSettings settings, TimeSpan offset) {
			return 
                settings._hasDateStringFormat ? date.ToString(settings._dateStringFormat) :
                settings.DateFormat == NetJSONDateFormat.Default ? DateToString(date, settings, offset) :
				settings.DateFormat == NetJSONDateFormat.EpochTime ? DateToEpochTime(date) :
				DateToISOFormat(date, settings, offset);
		}

		private static string DateToISOFormat(DateTime date, NetJSONSettings settings, TimeSpan offset) {
			var timeZoneFormat = settings.TimeZoneFormat;
			var minute = date.Minute;
			var hour = date.Hour;
			var second = date.Second;
			var timeOfDay = date.TimeOfDay;
			int totalSeconds = (int)(date.Ticks - (Math.Floor((decimal)date.Ticks / TimeSpan.TicksPerSecond) * TimeSpan.TicksPerSecond));
			var day = date.Day;
			var month = date.Month;
			var year = date.Year;

			var value = (_cachedDateStringBuilder ?? (_cachedDateStringBuilder = new StringBuilder(25)))
				.Clear().Append(IntToStr(year)).Append('-')
				.Append(month < 10 ? "0" : string.Empty)
				.Append(IntToStr(month))
			.Append('-').Append(day < 10 ? "0" : string.Empty).Append(IntToStr(day)).Append('T').Append(hour < 10 ? "0" : string.Empty).Append(IntToStr(hour)).Append(':')
			.Append(minute < 10 ? "0" : string.Empty).Append(IntToStr(minute)).Append(':')
			.Append(second < 10 ? "0" : string.Empty).Append(IntToStr(second)).Append('.')
			.Append(IntToStr(totalSeconds));

			if (timeZoneFormat == NetJSONTimeZoneFormat.Utc)
				value.Append('Z');
			else if (timeZoneFormat == NetJSONTimeZoneFormat.Local) {
				//var offset = TimeZone.CurrentTimeZone.GetUtcOffset(date);
				var hours = Math.Abs(offset.Hours);
				var minutes = Math.Abs(offset.Minutes);
				value.Append(offset.Ticks >= 0 ? '+' : '-').Append(hours < 10 ? "0" : string.Empty).Append(IntToStr(hours)).Append(minutes < 10 ? "0" : string.Empty).Append(IntToStr(minutes));
			}

			return value.ToString();
		}

		private static string DateToString(DateTime date, NetJSONSettings settings, TimeSpan offset) {
			if (date == DateTime.MinValue)
				return "\\/Date(-62135596800)\\/";
			else if (date == DateTime.MaxValue)
				return "\\/Date(253402300800)\\/";

			var timeZoneFormat = settings.TimeZoneFormat;
			var hours = Math.Abs(offset.Hours);
			var minutes = Math.Abs(offset.Minutes);
			var offsetText = timeZoneFormat == NetJSONTimeZoneFormat.Local ? (string.Concat(offset.Ticks >= 0 ? "+" : "-", hours < 10 ? "0" : string.Empty,
				hours, minutes < 10 ? "0" : string.Empty, minutes)) : string.Empty;

			if (date.Kind == DateTimeKind.Utc && timeZoneFormat == NetJSONTimeZoneFormat.Utc) {
				offset =
#if NET_STANDARD
                    TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
#else
					TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
#endif
				hours = Math.Abs(offset.Hours);
				minutes = Math.Abs(offset.Minutes);
				date = date.AddHours(hours).AddMinutes(minutes);
			}

			return string.Concat("\\/Date(", DateToEpochTime(date), offsetText, ")\\/");
		}

		private static string DateToEpochTime(DateTime date) {
			long epochTime = (long)(date.ToUniversalTime() - UnixEpoch).Ticks;
			return LongToStr(epochTime);// IntUtility.ltoa(epochTime);
		}

		internal static void SetterPropertyValue<T>(ref T instance, object value, MethodInfo methodInfo) {
			NetJSON.SetterPropertyValue(ref instance, value, methodInfo);
		}

        internal static void SetterFieldValue<T>(ref T instance, object value, FieldInfo fieldInfo)
        {
            NetJSON.SetterFieldValue(ref instance, value, fieldInfo);
        }
    }
}
