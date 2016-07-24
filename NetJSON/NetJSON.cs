using System;
using System.Collections;

#if !NET_35
using System.Collections.Concurrent;
#endif
using System.Collections.Generic;

#if !NET_35
using System.Dynamic;
#endif

using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

#if !NET_PCL
using System.Security.Permissions;
#endif
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Runtime.Serialization;

namespace NetJSON {

    /// <summary>
    /// Attribute for renaming field/property name to use for serialization and deserialization
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class NetJSONPropertyAttribute : Attribute {
        /// <summary>
        /// Name of property/field
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="name"></param>
        public NetJSONPropertyAttribute(string name) {
            Name = name;
        }
    }

    public class NetJSONSettings {
        /// <summary>
        /// Determine date format: Default: Default
        /// </summary>
        public NetJSONDateFormat DateFormat { get; set; }
        /// <summary>
        /// Determine time zone format: Default : Unspecified
        /// </summary>
        public NetJSONTimeZoneFormat TimeZoneFormat { get; set; }
        /// <summary>
        /// Determine formatting for output json: Default: Default
        /// </summary>
        public NetJSONFormat Format { get; set; }
        /// <summary>
        /// Determine if Enum should be serialized as string or int value. Default: True
        /// </summary>
        public bool UseEnumString { get; set; }
        /// <summary>
        /// Determine if default value should be skipped: Default: True
        /// </summary>
        public bool SkipDefaultValue { get; set; }
        public StringComparison _caseComparison = StringComparison.Ordinal;
        private bool _caseSensitive;

        /// <summary>
        /// Determine case sensitive for property/field name: Default: True
        /// </summary>
        public bool CaseSensitive {
            get {
                return _caseSensitive;
            }
            set {
                _caseSensitive = value;
                _caseComparison = _caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            }
        }

        private NetJSONQuote _quoteType;
        /// <summary>
        /// Quote Type: Default: Double Quote
        /// </summary>
        public NetJSONQuote QuoteType {
            get {
                return _quoteType;
            }
            set {
                _quoteType = value;
                _quoteChar = _quoteType == NetJSONQuote.Single ? '\'' : '"';
                _quoteCharString = _quoteType == NetJSONQuote.Single ? "'" : "\"";
            }
        }

        public char _quoteChar;
        public string _quoteCharString;

        public bool HasOverrideQuoteChar {get; internal set;}

        public bool UseStringOptimization { get; set; }

        /// <summary>
        /// Enable including type information for serialization and deserialization
        /// </summary>
        public bool IncludeTypeInformation { get; set; }

        private StringComparison CaseComparison {
            get {
                return CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public NetJSONSettings() {
            IncludeTypeInformation = NetJSON.IncludeTypeInformation;
            DateFormat = NetJSON.DateFormat;
            TimeZoneFormat = NetJSON.TimeZoneFormat;
            UseEnumString = NetJSON.UseEnumString;
            SkipDefaultValue = NetJSON.SkipDefaultValue;
            CaseSensitive = NetJSON.CaseSensitive;
            QuoteType = NetJSON.QuoteType;
            UseStringOptimization = NetJSON.UseStringOptimization;
            Format = NetJSONFormat.Default;
        }

        /// <summary>
        /// Clone settings
        /// </summary>
        /// <returns></returns>
        public NetJSONSettings Clone() {
            return new NetJSONSettings {
                IncludeTypeInformation = IncludeTypeInformation,
                DateFormat = DateFormat,
                TimeZoneFormat = TimeZoneFormat,
                UseEnumString = UseEnumString,
                SkipDefaultValue = SkipDefaultValue,
                CaseSensitive = CaseSensitive,
                QuoteType = QuoteType,
                UseStringOptimization = UseStringOptimization,
                Format = Format
            };
        }

        [ThreadStatic]
        private static NetJSONSettings _currentSettings;
        /// <summary>
        /// Returns current NetJSONSettings that correspond to old use of settings
        /// </summary>
        public static NetJSONSettings CurrentSettings {
            get {
                return _currentSettings ?? (_currentSettings = new NetJSONSettings());
            }
        }
    }

    /// <summary>
    /// Attribute for configuration of Class that requires type information for serialization and deserialization
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true)]
    public class NetJSONKnownTypeAttribute : Attribute {
        /// <summary>
        /// Type
        /// </summary>
        public Type Type { private set; get; }
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="type"></param>
        public NetJSONKnownTypeAttribute(Type type) {
            Type = type;
        }
    }

    public class NetJSONMemberInfo {
        public MemberInfo Member { get; set; }
        public NetJSONPropertyAttribute Attribute { get; set; }
    }

#if NET_PCL
    public enum BindingFlags {
        Default = 0,
        IgnoreCase = 1,
        DeclaredOnly = 2,
        Instance = 4,
        Static = 8,
        Public = 16,
        NonPublic = 32,
        FlattenHierarchy = 64,
        InvokeMethod = 256,
        CreateInstance = 512,
        GetField = 1024,
        SetField = 2048,
        GetProperty = 4096,
        SetProperty = 8192,
        PutDispProperty = 16384,
        PutRefDispProperty = 32768,
        ExactBinding = 65536,
        SuppressChangeType = 131072,
        OptionalParamBinding = 262144,
        IgnoreReturn = 16777216
    }
#endif


#if NET_CORE
    
    public static class NetCoreExtensions {

        public static bool IsEnum(this Type type){
            return type.GetTypeInfo().IsEnum;
        }

        public static bool IsPrimitive(this Type type){
            return type.GetTypeInfo().IsPrimitive;
        }

        public static bool IsGenericType(this Type type){
            return type.GetTypeInfo().IsGenericType;
        }

        public static bool IsClass(this Type type){
            return type.GetTypeInfo().IsClass;
        }

        public static bool IsValueType(this Type type){
            return type.GetTypeInfo().IsValueType;
        }

        public static Type GetInterface(this Type type, string name){
            return type.GetTypeInfo().GetInterface(name);
        }
    }

#endif

#if NET_35 || NET_PCL

#if !NET_PCL
    public class ExpandoObject {

    }
#endif

    public class ConcurrentDictionary<K,V> : Dictionary<K, V> {

        public V GetOrAdd(K key, Func<K, V> func) {
            V value = default(V);
            if (!this.TryGetValue(key, out value)) {
                value = this[key] = func(key);
            }
            return value;
        }
        public V GetOrAdd(K key, V val) {
            V value = default(V);
            if (!this.TryGetValue(key, out value)) {
                value = this[key] = val;
            }
            return value;
        }
    }

    public static class String35Extension {
        public static bool IsNullOrWhiteSpace(this string value) {
            if (value != null) {
                for (int i = 0; i < value.Length; i++) {
                    if (!char.IsWhiteSpace(value[i])) {
                        return false;
                    }
                }
            }
            return true;
        }
    }

    public static class StringBuilder35Extension {
        public static StringBuilder Clear(this StringBuilder value) {
            value.Length = 0;
            return value;
        }
    }

    public static class Type35Extension {
        
#if NET_PCL
        public static FieldInfo[] GetFields(this Type type, BindingFlags binding) {
            return type.GetRuntimeFields().ToArray();
        }

        public static MethodInfo GetMethod(this Type type, string name, Type[] types) {
            return type.GetRuntimeMethod(name, types);
        }

        public static MethodInfo GetMethod(this Type type, string name, BindingFlags binding) {
            return type.GetRuntimeMethods().FirstOrDefault(x => x.Name == name);
        }

        public static MethodInfo GetMethod(this Type type, string name) {
            return GetMethod(type, name, BindingFlags.Public);
        }
#endif
        public static Type GetEnumUnderlyingType(this Type type) {
            FieldInfo[] fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if ((fields == null) || (fields.Length != 1)) {
                throw new ArgumentException("Invalid enum");
            }
            return fields[0].FieldType;
        }
    }
#endif

    public class TupleContainer {
        private int _size;
        private int _index;

        private object _1, _2, _3, _4, _5, _6, _7, _8;

        public TupleContainer(int size) {
            _size = size;
        }


#if NET_35
        public NetJSON.Tuple<T1> ToTuple<T1>() {
            return new NetJSON.Tuple<T1>((T1)_1);
        }
        
        public NetJSON.Tuple<T1, T2> ToTuple<T1, T2>() {
            return new NetJSON.Tuple<T1, T2>((T1)_1, (T2)_2);
        }

        public NetJSON.Tuple<T1, T2, T3> ToTuple<T1, T2, T3>() {
            return new NetJSON.Tuple<T1, T2, T3>((T1)_1, (T2)_2, (T3)_3);
        }

        public NetJSON.Tuple<T1, T2, T3, T4> ToTuple<T1, T2, T3, T4>() {
            return new NetJSON.Tuple<T1, T2, T3, T4>((T1)_1, (T2)_2, (T3)_3, (T4)_4);
        }


        public NetJSON.Tuple<T1, T2, T3, T4, T5> ToTuple<T1, T2, T3, T4, T5>() {
            return new NetJSON.Tuple<T1, T2, T3, T4, T5>((T1)_1, (T2)_2, (T3)_3, (T4)_4, (T5)_5);
        }

        public NetJSON.Tuple<T1, T2, T3, T4, T5, T6> ToTuple<T1, T2, T3, T4, T5, T6>() {
            return new NetJSON.Tuple<T1, T2, T3, T4, T5, T6>((T1)_1, (T2)_2, (T3)_3, (T4)_4, (T5)_5, (T6)_6);
        }

        public NetJSON.Tuple<T1, T2, T3, T4, T5, T6, T7> ToTuple<T1, T2, T3, T4, T5, T6, T7>() {
            return new NetJSON.Tuple<T1, T2, T3, T4, T5, T6, T7>((T1)_1, (T2)_2, (T3)_3, (T4)_4, (T5)_5, (T6)_6, (T7)_7);
        }

        public NetJSON.Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> ToTuple<T1, T2, T3, T4, T5, T6, T7, TRest>() {
            return new NetJSON.Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>((T1)_1, (T2)_2, (T3)_3, (T4)_4, (T5)_5, (T6)_6, (T7)_7, (TRest)_8);
        }
#else
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
#endif

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

    /// <summary>
    /// Exception thrown for invalid json string
    /// </summary>
    public class NetJSONInvalidJSONException : Exception {
        public NetJSONInvalidJSONException()
            : base("Input is not a valid JSON.") {
        }
    }

    /// <summary>
    /// Exception thrown for invalid json property attribute
    /// </summary>
    public class NetJSONInvalidJSONPropertyException : Exception {
        /// <summary>
        /// Default constructor
        /// </summary>
        public NetJSONInvalidJSONPropertyException()
            : base("Class cannot contain any NetJSONProperty with null or blank space character") {
        }
    }

    /// <summary>
    /// Exception thrown for invalid assembly generation when adding all assembly into a specified assembly file
    /// </summary>
    public class NetJSONInvalidAssemblyGeneration : Exception {
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="asmName"></param>
        public NetJSONInvalidAssemblyGeneration(string asmName) : base(String.Format("Could not generate assembly with name [{0}] due to empty list of types to include", asmName)) { }
    }

    public abstract class NetJSONSerializer<T> {

        public abstract string Serialize(T value);
        public abstract T Deserialize(string value);

        public abstract void Serialize(T value, TextWriter writer);
        public abstract T Deserialize(TextReader reader);

        //With Settings
        public abstract string Serialize(T value, NetJSONSettings settings);
        public abstract T Deserialize(string value, NetJSONSettings settings);
        public abstract void Serialize(T value, TextWriter writer, NetJSONSettings settings);
        public abstract T Deserialize(TextReader reader, NetJSONSettings settings);
    }

    /// <summary>
    /// Option for determining date formatting
    /// </summary>
    public enum NetJSONDateFormat {
        /// <summary>
        /// Default /Date(...)/
        /// </summary>
        Default = 0,
        /// <summary>
        /// ISO Format
        /// </summary>
        ISO = 2,
        /// <summary>
        /// Unix Epoch Milliseconds
        /// </summary>
        EpochTime = 4,
        /// <summary>
        /// JSON.NET Format for backward compatibility
        /// </summary>
        JsonNetISO = 6
    }


    /// <summary>
    /// Option for determining timezone formatting
    /// </summary>
    public enum NetJSONTimeZoneFormat {
        /// <summary>
        /// Default unspecified
        /// </summary>
        Unspecified = 0,
        /// <summary>
        /// Utc
        /// </summary>
        Utc = 2,
        /// <summary>
        /// Local time
        /// </summary>
        Local = 4
    }

    /// <summary>
    /// Option for determine what type of quote to use for serialization and deserialization
    /// </summary>
    public enum NetJSONQuote {
        /// <summary>
        /// Default: double quote
        /// </summary>
        Default = 0,
        /// <summary>
        /// Use double quote
        /// </summary>
        Double = Default,
        /// <summary>
        /// Use single quote
        /// </summary>
        Single = 2
    }

    /// <summary>
    /// Options for controlling serialize json format
    /// </summary>
    public enum NetJSONFormat {
        /// <summary>
        /// Default
        /// </summary>
        Default = 0,
        /// <summary>
        /// Prettify string
        /// </summary>
        Prettify = 2
    }

    public static class NetJSON {

        private static class NetJSONCachedSerializer<T> {
            public static readonly NetJSONSerializer<T> Serializer = (NetJSONSerializer<T>)Activator.CreateInstance(Generate(typeof(T)));
        }
        
        //public static string QuotChar {
        //    get {
        //        return _ThreadQuoteString;
        //    }
        //}

        

        const int BUFFER_SIZE = 11;

        const int BUFFER_SIZE_DIFF = BUFFER_SIZE - 2;

        const TypeAttributes TypeAttribute =
           TypeAttributes.Public | TypeAttributes.Serializable | TypeAttributes.Sealed;

        const BindingFlags PropertyBinding = BindingFlags.Instance | BindingFlags.Public;

        const BindingFlags MethodBinding = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

        const MethodAttributes MethodAttribute =
            MethodAttributes.Public
            | MethodAttributes.Virtual
            | MethodAttributes.Final
            | MethodAttributes.HideBySig
            | MethodAttributes.NewSlot
            | MethodAttributes.SpecialName;

        const MethodAttributes StaticMethodAttribute =
            MethodAttributes.Public
            | MethodAttributes.Static
            | MethodAttributes.HideBySig
            | MethodAttributes.SpecialName;


        static readonly Type _dateTimeType = typeof(DateTime),
            _dateTimeOffsetType = typeof(DateTimeOffset),
            _enumType = typeof(Enum),
            _stringType = typeof(String),
            _byteArrayType = typeof(byte[]),
            _charType = typeof(char),
            _charPtrType = typeof(char*),
            _guidType = typeof(Guid),
            _boolType = typeof(bool),
            _byteType = typeof(byte),
            _sbyteType = typeof(sbyte),
            _timeSpanType = typeof(TimeSpan),
            _stringBuilderType = typeof(StringBuilder),
            _listType = typeof(IList),
            _dictType = typeof(IDictionary),
            _dictStringObject = typeof(Dictionary<string, object>),
            _genericDictType = typeof(Dictionary<,>),
            _genericListType = typeof(List<>),
            _objectType = typeof(Object),
            _nullableType = typeof(Nullable<>),
            _decimalType = typeof(decimal),
            _genericCollectionType = typeof(ICollection<>),
            _floatType = typeof(float),
            _doubleType = typeof(double),
            _enumeratorTypeNonGeneric = typeof(IEnumerator),
            _idictStringObject = typeof(IDictionary<string, object>),
            _ienumerableType = typeof(IEnumerable<>),
            _enumeratorType = typeof(IEnumerator<>),
            _genericKeyValuePairType = typeof(KeyValuePair<,>),
            _invalidJSONExceptionType = typeof(NetJSONInvalidJSONException),
            _serializerType = typeof(NetJSONSerializer<>),
            _expandoObjectType =
            
#if !NET_35
      typeof(ExpandoObject),
#else
      typeof(ExpandoObject),
#endif
      
            
               
            _genericDictionaryEnumerator =
                Type.GetType("System.Collections.Generic.Dictionary`2+Enumerator"),
            _genericListEnumerator =
                Type.GetType("System.Collections.Generic.List`1+Enumerator"),
            _typeType = typeof(Type),
            _voidType = typeof(void),
            _intType = typeof(int),
            _shortType = typeof(short),
            _longType = typeof(long),
            _jsonType = typeof(NetJSON),
            _methodInfoType = typeof(MethodBase),
            _textWriterType = typeof(TextWriter),
            _tupleContainerType = typeof(TupleContainer),
            _netjsonPropertyType = typeof(NetJSONPropertyAttribute),
            _textReaderType = typeof(TextReader),
            _stringComparison = typeof(StringComparison),
            _settingsType = typeof(NetJSONSettings);

        static MethodInfo _stringBuilderToString =
            _stringBuilderType.GetMethod("ToString", Type.EmptyTypes),
            _stringBuilderAppend = _stringBuilderType.GetMethod("Append", new[] { _stringType }),
            _stringBuilderAppendObject = _stringBuilderType.GetMethod("Append", new[] { _objectType }),
            _stringBuilderAppendChar = _stringBuilderType.GetMethod("Append", new[] { _charType }),


#if NET_35
            _stringBuilderClear = typeof(StringBuilder35Extension).GetMethod("Clear"),
#else
            _stringBuilderClear = _stringBuilderType.GetMethod("Clear"),
#endif
            
            _stringOpEquality = _stringType.GetMethod("op_Equality", MethodBinding),
            _tupleContainerAdd = _tupleContainerType.GetMethod("Add"),
            _generatorGetStringBuilder = _jsonType.GetMethod("GetStringBuilder", MethodBinding),
            _generatorIntToStr = _jsonType.GetMethod("IntToStr", MethodBinding),
            _generatorCharToStr = _jsonType.GetMethod("CharToStr", MethodBinding),
            _generatorEnumToStr = _jsonType.GetMethod("CustomEnumToStr", MethodBinding),
            _generatorLongToStr = _jsonType.GetMethod("LongToStr", MethodBinding),
            _generatorFloatToStr = _jsonType.GetMethod("FloatToStr", MethodBinding),
            _generatorDoubleToStr = _jsonType.GetMethod("DoubleToStr", MethodBinding),
            _generatorDecimalToStr = _jsonType.GetMethod("DecimalToStr", MethodBinding),
            _generatorDateToString = _jsonType.GetMethod("AllDateToString", MethodBinding),
            _generatorDateOffsetToString = _jsonType.GetMethod("AllDateOffsetToString", MethodBinding),
            _generatorSByteToStr = _jsonType.GetMethod("SByteToStr", MethodBinding),
            _guidToStr = _jsonType.GetMethod("GuidToStr", MethodBinding),
            _byteArrayToStr = _jsonType.GetMethod("ByteArrayToStr", MethodBinding),
            _objectToString = _objectType.GetMethod("ToString", Type.EmptyTypes),
            _stringFormat = _stringType.GetMethod("Format", new[] { _stringType, _objectType }),
            _convertBase64 = typeof(Convert).GetMethod("ToBase64String", new[] { _byteArrayType }),
            _convertFromBase64 = typeof(Convert).GetMethod("FromBase64String", new[] { _stringType }),
            _getStringBasedValue = _jsonType.GetMethod("GetStringBasedValue", MethodBinding),
            _getNonStringValue = _jsonType.GetMethod("GetNonStringValue", MethodBinding),
            _isDateValue = _jsonType.GetMethod("IsValueDate", MethodBinding),
            _iDisposableDispose = typeof(IDisposable).GetMethod("Dispose"),
            _toExpectedType = typeof(AutomaticTypeConverter).GetMethod("ToExpectedType"),
            _fastStringToInt = _jsonType.GetMethod("FastStringToInt", MethodBinding),
            _fastStringToUInt = _jsonType.GetMethod("FastStringToUInt", MethodBinding),
            _fastStringToUShort = _jsonType.GetMethod("FastStringToUShort", MethodBinding),
            _fastStringToShort = _jsonType.GetMethod("FastStringToShort", MethodBinding),
            _fastStringToByte = _jsonType.GetMethod("FastStringToByte", MethodBinding),
            _fastStringToLong = _jsonType.GetMethod("FastStringToLong", MethodBinding),
            _fastStringToULong = _jsonType.GetMethod("FastStringToULong", MethodBinding),
            _fastStringToDecimal = _jsonType.GetMethod("FastStringToDecimal", MethodBinding),
            _fastStringToFloat = _jsonType.GetMethod("FastStringToFloat", MethodBinding),
            _fastStringToDate = _jsonType.GetMethod("FastStringToDate", MethodBinding),
            _fastStringToDateTimeoffset = _jsonType.GetMethod("FastStringToDateTimeoffset", MethodBinding),
            _fastStringToChar = _jsonType.GetMethod("FastStringToChar", MethodBinding),
            _fastStringToDouble = _jsonType.GetMethod("FastStringToDouble", MethodBinding),
            _fastStringToBool = _jsonType.GetMethod("FastStringToBool", MethodBinding),
            _fastStringToGuid = _jsonType.GetMethod("FastStringToGuid", MethodBinding),
            _fastStringToType = _jsonType.GetMethod("FastStringToType", MethodBinding),
            _moveToArrayBlock = _jsonType.GetMethod("MoveToArrayBlock", MethodBinding),
            _fastStringToByteArray = _jsonType.GetMethod("FastStringToByteArray", MethodBinding),
            _listToListObject = _jsonType.GetMethod("ListToListObject", MethodBinding),
            _isListType = _jsonType.GetMethod("IsListType", MethodBinding),
            _isDictType = _jsonType.GetMethod("IsDictionaryType", MethodBinding),
            _stringLength = _stringType.GetMethod("get_Length"),
            _createString = _jsonType.GetMethod("CreateString"),
            _isCharTag = _jsonType.GetMethod("IsCharTag"),
            _isEndChar = _jsonType.GetMethod("IsEndChar", MethodBinding),
            _isArrayEndChar = _jsonType.GetMethod("IsArrayEndChar", MethodBinding),
            _encodedJSONString = _jsonType.GetMethod("EncodedJSONString", MethodBinding),
            _decodeJSONString = _jsonType.GetMethod("DecodeJSONString", MethodBinding),
            _skipProperty = _jsonType.GetMethod("SkipProperty", MethodBinding),
            _prettifyJSONIfNeeded = _jsonType.GetMethod("PrettifyJSONIfNeeded", MethodBinding),
            _isRawPrimitive = _jsonType.GetMethod("IsRawPrimitive", MethodBinding),
            _isInRange = _jsonType.GetMethod("IsInRange", MethodBinding),
            _dateTimeParse = _dateTimeType.GetMethod("Parse", new[] { _stringType }),
            _timeSpanParse = _timeSpanType.GetMethod("Parse", new[] { _stringType }),
            _getChars = _stringType.GetMethod("get_Chars"),
            _dictSetItem = _dictType.GetMethod("set_Item"),
            _textWriterWrite = _textWriterType.GetMethod("Write", new []{ _stringType }),
            _fastObjectToStr = _jsonType.GetMethod("FastObjectToString", MethodBinding),
            _textReaderReadToEnd = _textReaderType.GetMethod("ReadToEnd"),
            _typeopEquality = _typeType.GetMethod("op_Equality", MethodBinding),
            _cTypeOpEquality = _jsonType.GetMethod("CustomTypeEquality", MethodBinding),
            _assemblyQualifiedName = _typeType.GetProperty("AssemblyQualifiedName").GetGetMethod(),
            _objectGetType = _objectType.GetMethod("GetType", MethodBinding),
            _needQuote = _jsonType.GetMethod("NeedQuotes", MethodBinding),
            _typeGetTypeFromHandle = _typeType.GetMethod("GetTypeFromHandle", MethodBinding),
            _methodGetMethodFromHandle = _methodInfoType.GetMethod("GetMethodFromHandle", new Type[] { typeof(RuntimeMethodHandle) }),
            _objectEquals = _objectType.GetMethod("Equals", new []{ _objectType}),
            _stringEqualCompare = _stringType.GetMethod("Equals", new []{_stringType, _stringType, typeof(StringComparison)}),
            _stringConcat = _stringType.GetMethod("Concat", new[] { _objectType, _objectType, _objectType, _objectType }),
            _IsCurrentAQuotMethod = _jsonType.GetMethod("IsCurrentAQuot", MethodBinding),
            _getTypeIdentifierInstanceMethod = _jsonType.GetMethod("GetTypeIdentifierInstance", MethodBinding),
            _settingsUseEnumStringProp = _settingsType.GetProperty("UseEnumString", MethodBinding).GetGetMethod(),
            _settingsUseStringOptimization = _settingsType.GetProperty("UseStringOptimization", MethodBinding).GetGetMethod(),
            _settingsHasOverrideQuoteChar = _settingsType.GetProperty("HasOverrideQuoteChar", MethodBinding).GetGetMethod(),
            _settingsDateFormat = _settingsType.GetProperty("DateFormat", MethodBinding).GetGetMethod(),
            _getUninitializedInstance = _jsonType.GetMethod("GetUninitializedInstance", MethodBinding),
            _setterPropertyValueMethod = _jsonType.GetMethod("SetterPropertyValue", MethodBinding),
            _settingsCurrentSettings = _settingsType.GetProperty("CurrentSettings", MethodBinding).GetGetMethod();

        private static FieldInfo _guidEmptyGuid = _guidType.GetField("Empty"),
            _settingQuoteChar = _settingsType.GetField("_quoteChar", MethodBinding),
            _settingsCaseComparison = _settingsType.GetField("_caseComparison", MethodBinding),
            _settingQuoteCharString = _settingsType.GetField("_quoteCharString", MethodBinding);

        const int Delimeter = (int)',', ColonChr = (int)':',
            ArrayOpen = (int)'[', ArrayClose = (int)']', ObjectOpen = (int)'{', ObjectClose = (int)'}';

        const string IsoFormat = "{0:yyyy-MM-ddTHH:mm:ss.fffZ}",
             TypeIdentifier = "$type",
             ClassStr = "Class", _dllStr = ".dll",
             NullStr = "null",
              IListStr = "IList`1",
              IDictStr = "IDictionary`2",
              KeyValueStr = "KeyValuePair`2",
              CreateListStr = "CreateList",
              ICollectionStr = "ICollection`1",
             IEnumerableStr = "IEnumerable`1",
              CreateClassOrDictStr = "CreateClassOrDict",
              ExtractStr = "Extract",
              SetStr = "Set",
              WriteStr = "Write", ReadStr = "Read", ReadEnumStr = "ReadEnum",
              CarrotQuoteChar = "`",
              ArrayStr = "Array", AnonymousBracketStr = "<>",
              ArrayLiteral = "[]",
              Colon = ":",
              ToTupleStr = "ToTuple",
              SerializeStr = "Serialize", DeserializeStr = "Deserialize", SettingsFieldName = "_settingsField";

        const char QuotDoubleChar = '"',
                   QuotSingleChar = '\'';

        static ConstructorInfo _strCtorWithPtr = _stringType.GetConstructor(new[] { typeof(char*), _intType, _intType });
        static ConstructorInfo _invalidJSONCtor = _invalidJSONExceptionType.GetConstructor(Type.EmptyTypes);
        static ConstructorInfo _settingsCtor = _settingsType.GetConstructor(Type.EmptyTypes);

        private static ConcurrentDictionary<string, object> _dictLockObjects = new ConcurrentDictionary<string, object>();
        static ConcurrentDictionary<Type, MethodInfo> _registeredSerializerMethods =
            new ConcurrentDictionary<Type, MethodInfo>();

        static Dictionary<Type, MethodInfo> _defaultSerializerTypes =
            new Dictionary<Type, MethodInfo> { 
                        {_enumType, null},
                        {_stringType, null},
                        {_charType, _generatorCharToStr},
                        {_intType, _generatorIntToStr},
                        {_shortType, _generatorIntToStr},
                        {_longType, _generatorLongToStr},
                        {_decimalType, _generatorDecimalToStr},
                        {_boolType, null},
                        {_doubleType, _generatorDoubleToStr},
                        {_floatType, _generatorFloatToStr},
                        {_byteArrayType, _byteArrayToStr},
                        {_guidType, _guidToStr},
                        {_objectType, null}
                    };


        static ConcurrentDictionary<Type, Type> _types =
            new ConcurrentDictionary<Type, Type>();
        static ConcurrentDictionary<string, MethodBuilder> _writeMethodBuilders =
            new ConcurrentDictionary<string, MethodBuilder>();

        static ConcurrentDictionary<string, MethodBuilder> _setValueMethodBuilders =
            new ConcurrentDictionary<string, MethodBuilder>();

        static ConcurrentDictionary<string, MethodBuilder> _readMethodBuilders =
            new ConcurrentDictionary<string, MethodBuilder>();

        static ConcurrentDictionary<string, MethodBuilder> _createListMethodBuilders =
            new ConcurrentDictionary<string, MethodBuilder>();

        static ConcurrentDictionary<string, MethodBuilder> _extractMethodBuilders =
            new ConcurrentDictionary<string, MethodBuilder>();

        static ConcurrentDictionary<string, MethodBuilder> _readDeserializeMethodBuilders =
            new ConcurrentDictionary<string, MethodBuilder>();

        static ConcurrentDictionary<string, MethodBuilder> _writeEnumToStringMethodBuilders =
            new ConcurrentDictionary<string, MethodBuilder>();

        static ConcurrentDictionary<string, MethodBuilder> _readEnumToStringMethodBuilders =
            new ConcurrentDictionary<string, MethodBuilder>();

        static readonly ConcurrentDictionary<MethodInfo, Delegate> _setMemberValues = new ConcurrentDictionary<MethodInfo, Delegate>();

        static ConcurrentDictionary<string, Func<object>> _typeIdentifierFuncs = new ConcurrentDictionary<string, Func<object>>();

        static ConcurrentDictionary<Type, bool> _primitiveTypes =
            new ConcurrentDictionary<Type, bool>();

        static ConcurrentDictionary<Type, Type> _nullableTypes =
            new ConcurrentDictionary<Type, Type>();

        static ConcurrentDictionary<Type, List<Type>> _includedTypeTypes = new ConcurrentDictionary<Type, List<Type>>();

        static ConcurrentDictionary<Type, object> _serializers = new ConcurrentDictionary<Type, object>();

        static ConcurrentDictionary<Type, Delegate> _nonPublicBuilder =
            new ConcurrentDictionary<Type, Delegate>();

        static ConcurrentDictionary<Type, NetJSONMemberInfo[]> _typeProperties =
            new ConcurrentDictionary<Type, NetJSONMemberInfo[]>();

        static ConcurrentDictionary<string, string> _fixes =
            new ConcurrentDictionary<string, string>();

        const int DefaultStringBuilderCapacity = 1024 * 2;

        private static object _lockObject = new object();

        public static string FloatToStr(float value) {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string DoubleToStr(double value) {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string SByteToStr(sbyte value) {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string DecimalToStr(decimal value) {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static unsafe void memcpy(char* dmem, char* smem, int charCount) {
            if ((((int)dmem) & 2) != 0) {
                dmem[0] = smem[0];
                dmem++;
                smem++;
                charCount--;
            }
            while (charCount >= 8) {
                *((int*)dmem) = *((int*)smem);
                *((int*)(dmem + 2)) = *((int*)(smem + 2));
                *((int*)(dmem + 4)) = *((int*)(smem + 4));
                *((int*)(dmem + 6)) = *((int*)(smem + 6));
                dmem += 8;
                smem += 8;
                charCount -= 8;
            }
            if ((charCount & 4) != 0) {
                *((int*)dmem) = *((int*)smem);
                *((int*)(dmem + 2)) = *((int*)(smem + 2));
                dmem += 4;
                smem += 4;
            }
            if ((charCount & 2) != 0) {
                *((int*)dmem) = *((int*)smem);
                dmem += 2;
                smem += 2;
            }
            if ((charCount & 1) != 0) {
                dmem[0] = smem[0];
            }
        }

        public unsafe static string IntToStr(int snum) {
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
            } else {
                num2 = num1 / 10000;
                num1 -= num2 * 10000;
                if (num2 < 10000) {
                    if (num2 < 10) goto L5;
                    if (num2 < 100) goto L6;
                    if (num2 < 1000) goto L7;
                } else {
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

        public unsafe static string LongToStr(long snum) {
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
            } else {
                num2 = num1 / 10000;
                num1 -= num2 * 10000;
                if (num2 < 10000) {
                    if (num2 < 10) goto L5;
                    if (num2 < 100) goto L6;
                    if (num2 < 1000) goto L7;
                } else {
                    num3 = num2 / 10000;
                    num2 -= num3 * 10000;
                    if (num3 < 10000) {
                        if (num3 < 10) goto L9;
                        if (num3 < 100) goto L10;
                        if (num3 < 1000) goto L11;
                    } else {
                        num4 = num3 / 10000;
                        num3 -= num4 * 10000;
                        if (num4 < 10000) {
                            if (num4 < 10) goto L13;
                            if (num4 < 100) goto L14;
                            if (num4 < 1000) goto L15;
                        } else {
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

        public static string GuidToStr(Guid value) {
            //TODO: Optimize
            return value.ToString();
        }

        public static string ByteArrayToStr(byte[] value) {
            //TODO: Optimize
            return Convert.ToBase64String(value);
        }

        public static List<object> ListToListObject(IList list) {
            return list.Cast<object>().ToList();
        }

        private static bool IsPrimitiveType(this Type type) {
            return _primitiveTypes.GetOrAdd(type, key => {
                lock (GetDictLockObject("IsPrimitiveType")) {
                    if (key.IsGenericType &&
                        key.GetGenericTypeDefinition() == _nullableType)
                        key = key.GetGenericArguments()[0];

                    return key == _stringType ||
                        key.IsPrimitive || key == _dateTimeType ||
                        key == _dateTimeOffsetType ||
                        key == _decimalType || key == _timeSpanType ||
                        key == _guidType || key == _charType ||
                        key == _typeType ||
                        key.IsEnum || key == _byteArrayType;
                }
            });
        }

        private static Type GetNullableType(this Type type) {
            return _nullableTypes.GetOrAdd(type, key => {
                lock (GetDictLockObject("GetNullableType"))
                    return key.Name.StartsWith("Nullable`") ? key.GetGenericArguments()[0] : null;
            });
        }

        internal static NetJSONMemberInfo[] GetTypeProperties(this Type type) {
            return _typeProperties.GetOrAdd(type, key => {
                lock (GetDictLockObject("GetTypeProperties")) {
                    var props = key.GetProperties(PropertyBinding)
                        .Where(x => x.GetIndexParameters().Length == 0)
                        .Select(x => new NetJSONMemberInfo { Member = x, Attribute = x.GetCustomAttributes(_netjsonPropertyType, true).OfType<NetJSONPropertyAttribute>().FirstOrDefault() });
                    if (_includeFields) {
                        props = props.Union(key.GetFields(PropertyBinding).Select(x => new NetJSONMemberInfo { Member = x, Attribute = x.GetCustomAttributes(_netjsonPropertyType, true).OfType<NetJSONPropertyAttribute>().FirstOrDefault() }));
                    }
                    var result = props.ToArray();

#if NET_35
                if (result.Where(x => x.Attribute != null).Any(x => x.Attribute.Name.IsNullOrWhiteSpace() || x.Attribute.Name.Contains(" ")))
                    throw new NetJSONInvalidJSONPropertyException();
#else
                    if (result.Where(x => x.Attribute != null).Any(x => string.IsNullOrWhiteSpace(x.Attribute.Name) || x.Attribute.Name.Contains(" ")))
                        throw new NetJSONInvalidJSONPropertyException();
#endif

                    return result;
                }
            });
        }


        public static bool IsListType(this Type type) {
            Type interfaceType = null;
            //Skip type == typeof(String) since String is same as IEnumerable<Char>
            return type != _stringType && ( _listType.IsAssignableFrom(type) || type.Name == IListStr ||
                (type.Name == ICollectionStr && type.GetGenericArguments()[0].Name != KeyValueStr) ||
                (type.Name == IEnumerableStr && type.GetGenericArguments()[0].Name != KeyValueStr) ||
                ((interfaceType = type.GetInterface(ICollectionStr)) != null && interfaceType.GetGenericArguments()[0].Name != KeyValueStr) ||
                ((interfaceType = type.GetInterface(IEnumerableStr)) != null && interfaceType.GetGenericArguments()[0].Name != KeyValueStr));
        }

        public static bool IsDictionaryType(this Type type) {
            Type interfaceType = null;
            return _dictType.IsAssignableFrom(type) || type.Name == IDictStr
                || ((interfaceType = type.GetInterface(IEnumerableStr)) != null && interfaceType.GetGenericArguments()[0].Name == KeyValueStr);
        }

        public static bool IsCollectionType(this Type type) {
            return type.IsListType() || type.IsDictionaryType();
        }

        public static bool IsClassType(this Type type) {
            return !type.IsCollectionType() && !type.IsPrimitiveType();
        }

        public static bool IsRawPrimitive(string value) {
            value = value.Trim();
            return !value.StartsWith("{") && !value.StartsWith("[");
        }

        private static void LoadQuotChar(ILGenerator il) {
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldfld, _settingQuoteCharString);
            //il.Emit(OpCodes.Ldsfld, _threadQuoteStringField);
        }

        [ThreadStatic]
        private static StringBuilder _cachedStringBuilder;
        public static StringBuilder GetStringBuilder() {
            return _cachedStringBuilder ?? (_cachedStringBuilder = new StringBuilder(DefaultStringBuilderCapacity));
        }

        private static bool _useTickFormat = true;

        [Obsolete("Use NetJSON.DateFormat = NetJSONDateFormat.ISO instead. UseISOFormat will be removed in the future")]
        public static bool UseISOFormat {
            set {
                _useTickFormat = !value;
                if (!_useTickFormat)
                    _dateFormat = NetJSONDateFormat.ISO;
            }
        }

        private static NetJSONDateFormat _dateFormat = NetJSONDateFormat.Default;
        private static NetJSONTimeZoneFormat _timeZoneFormat = NetJSONTimeZoneFormat.Unspecified;

        private static bool _useSharedAssembly = true;

        /// <summary>
        /// Flag to determine whether to store all generate serializer for types in a single assembly
        /// </summary>
        public static bool ShareAssembly {
            set {
                _useSharedAssembly = value;
            }
        }

        [Obsolete("Use NetJSONSettings.DateFormat")]
        public static NetJSONDateFormat DateFormat {
            set {
                _dateFormat = value;
            }
            internal get {
                return _dateFormat;
            }
        }

        [Obsolete("Use NetJSONSettings.TimeZoneFormat")]
        public static NetJSONTimeZoneFormat TimeZoneFormat {
            set {
                _timeZoneFormat = value;
            }
            internal get {
                return _timeZoneFormat;
            }
        }

        private static NetJSONQuote _quoteType = NetJSONQuote.Default;

        [Obsolete("Use NetJSONSettings.QuoteType")]
        public static NetJSONQuote QuoteType {
            set {
                _quoteType = value;
                //_ThreadQuoteString = (_ThreadQuoteChar = value == NetJSONQuote.Single ? QuotSingleChar : QuotDoubleChar).ToString();
            }
            internal get {
                return _quoteType;
            }
        }

        private static bool _caseSensitive = true;

        [Obsolete("Use NetJSONSettings.CaseSensitive")]
        public static bool CaseSensitive {
            set {
                _caseSensitive = value;
            }
            internal get {
                return _caseSensitive;
            }
        }

        private static bool _useEnumString = false;

        public static bool UseEnumString {
            set {
                _useEnumString = value;
            }
            internal get {
                return _useEnumString;
            }
        }

        private static bool _includeFields = true;

        public static bool IncludeFields {
            set {
                _includeFields = value;
            }
            internal get {
                return _includeFields;
            }
        }

        private static bool _skipDefaultValue = true;

        [Obsolete("Use NetJSONSettings.SkipDefaultValue")]
        public static bool SkipDefaultValue {
            set {
                _skipDefaultValue = value;
            }
            internal get {
                return _skipDefaultValue;
            }
        }

        private static bool _useStringOptimization = true;

        [Obsolete("Use NetJSONSettings.UseStringOptimization")]
        public static bool UseStringOptimization {
            set {
                _useStringOptimization = value;
            }
            internal get {
                return _useStringOptimization;
            }
        }

        private static bool _generateAssembly = false;
        [Obsolete("Use NetJSONSettings.GenerateAssembly")]
        public static bool GenerateAssembly {
            set {
                _generateAssembly = value;
            }
        }

        private static bool _includeTypeInformation = false;
        public static bool IncludeTypeInformation {
            set {
                _includeTypeInformation = value;
            }
            internal get {
                return _includeTypeInformation;
            }
        }

        public static T GetUninitializedInstance<T>() {
            return (T)FormatterServices.GetUninitializedObject(typeof(T));
        }

        public static object GetTypeIdentifierInstance(string typeName) {
            return _typeIdentifierFuncs.GetOrAdd(typeName, _ => {
                lock (GetDictLockObject("GetTypeIdentifier")) {
                    var type = Type.GetType(typeName, throwOnError: false);
                    if (type == null)
                        throw new InvalidOperationException(string.Format("Unable to resolve {0} with value = {1}", TypeIdentifier, typeName));

                    var ctor = type.GetConstructor(Type.EmptyTypes);

                    if (ctor == null)
                        throw new InvalidOperationException(string.Format("{0} with value = {1} must have a default constructor", TypeIdentifier, typeName));

                    var meth = new DynamicMethod(Guid.NewGuid().ToString("N"), _objectType, null, restrictedSkipVisibility: true);

                    var il = meth.GetILGenerator();

                    if (ctor == null)
                        il.Emit(OpCodes.Call, _getUninitializedInstance.MakeGenericMethod(type));
                    else
                        il.Emit(OpCodes.Newobj, ctor);//NewObjNoctor
                    il.Emit(OpCodes.Ret);

                    return meth.CreateDelegate(typeof(Func<object>)) as Func<object>;
                }
            })();
        }

        [ThreadStatic]
        private static StringBuilder _cachedDateStringBuilder;

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

        private static DateTime Epoch = new DateTime(1970, 1, 1),
            UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);


        public static string AllDateToString(DateTime date, NetJSONSettings settings) {
            var offset = TimeZone.CurrentTimeZone.GetUtcOffset(date);
            return DateToStringWithOffset(date, settings, offset);
        }

        private static string DateToStringWithOffset(DateTime date, NetJSONSettings settings, TimeSpan offset) {
            return settings.DateFormat == NetJSONDateFormat.Default ? DateToString(date, settings, offset) :
                settings.DateFormat == NetJSONDateFormat.EpochTime ? DateToEpochTime(date) :
                DateToISOFormat(date, settings, offset);
        }

        public static string AllDateOffsetToString(DateTimeOffset offset, NetJSONSettings settings) {
            return DateToStringWithOffset(offset.DateTime, settings, offset.Offset);
        }

        private static string DateToString(DateTime date, NetJSONSettings settings, TimeSpan offset) {
            var timeZoneFormat = settings.TimeZoneFormat;
            if (date == DateTime.MinValue)
                return "\\/Date(-62135596800)\\/";
            else if (date == DateTime.MaxValue)
                return "\\/Date(253402300800)\\/";
            //var offset = TimeZone.CurrentTimeZone.GetUtcOffset(date);
            var hours = Math.Abs(offset.Hours);
            var minutes = Math.Abs(offset.Minutes);
            var offsetText = timeZoneFormat == NetJSONTimeZoneFormat.Local ? (string.Concat(offset.Ticks >= 0 ? "+" : "-", hours < 10 ? "0" : string.Empty,
                hours, minutes < 10 ? "0" : string.Empty, minutes)) : string.Empty;

            if (date.Kind == DateTimeKind.Utc && timeZoneFormat == NetJSONTimeZoneFormat.Utc) {
                offset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
                hours = Math.Abs(offset.Hours);
                minutes = Math.Abs(offset.Minutes);
                date = date.AddHours(hours).AddMinutes(minutes);
            }

            return String.Concat("\\/Date(", DateToEpochTime(date), offsetText, ")\\/");
        }

        private static string DateToEpochTime(DateTime date) {
            long epochTime = (long)(date.ToUniversalTime() - UnixEpoch).Ticks;
            return IntUtility.ltoa(epochTime);
        }

        [ThreadStatic]
        private static StringBuilder _cachedObjectStringBuilder;

        public static StringBuilder CachedObjectStringBuilder() {
            return (_cachedObjectStringBuilder ?? (_cachedObjectStringBuilder = new StringBuilder(25))).Clear();
        }

        public static bool NeedQuotes(Type type, NetJSONSettings settings) {
            return type == _stringType || type == _charType || type == _guidType || type == _timeSpanType || ((type == _dateTimeType || type == _dateTimeOffsetType) && settings.DateFormat != NetJSONDateFormat.EpochTime) || type == _byteArrayType || (settings.UseEnumString && type.IsEnum);
        }

        public static bool CustomTypeEquality(Type type1, Type type2) {
            if (type1.IsEnum) {
                if(type1.IsEnum && type2 == typeof(Enum))
                    return true;
            }
            return type1 == type2;
        }

        public static string CustomEnumToStr(Enum @enum, NetJSONSettings settings) {
            if (settings.UseEnumString)
                return @enum.ToString();
            return IntToStr((int)((object)@enum));
        }

        public static string CharToStr(char chr) {
            return chr.ToString();
        }

        private static MethodBuilder GenerateFastObjectToString(TypeBuilder type) {
            return _readMethodBuilders.GetOrAdd("FastObjectToString", _ => {
                lock (GetDictLockObject("GenerateFastObjectToString")) {
                    var method = type.DefineMethod("FastObjectToString", StaticMethodAttribute, _voidType,
                        new[] { _objectType, _stringBuilderType, _settingsType });

                    var il = method.GetILGenerator();

                    var typeLocal = il.DeclareLocal(_typeType);
                    var needQuoteLocal = il.DeclareLocal(_boolType);
                    var needQuoteStartLabel = il.DefineLabel();
                    var needQuoteEndLabel = il.DefineLabel();

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Callvirt, _objectGetType);
                    il.Emit(OpCodes.Stloc, typeLocal);

                    var isListTypeLabel = il.DefineLabel();

                    il.Emit(OpCodes.Ldloc, typeLocal);
                    il.Emit(OpCodes.Call, _isListType);
                    il.Emit(OpCodes.Brfalse, isListTypeLabel);

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Castclass, _listType);
                    il.Emit(OpCodes.Call, _listToListObject);
                    il.Emit(OpCodes.Starg, 0);

                    WriteCollection(type, typeof(List<object>), il);
                    il.Emit(OpCodes.Ret);

                    il.MarkLabel(isListTypeLabel);

                    var isDictTypeLabel = il.DefineLabel();

                    il.Emit(OpCodes.Ldloc, typeLocal);
                    il.Emit(OpCodes.Call, _isDictType);
                    il.Emit(OpCodes.Brfalse, isDictTypeLabel);

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Castclass, _dictStringObject);
                    il.Emit(OpCodes.Starg, 0);

                    WriteCollection(type, _dictStringObject, il);
                    il.Emit(OpCodes.Ret);

                    il.MarkLabel(isDictTypeLabel);


                    il.Emit(OpCodes.Ldloc, typeLocal);
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Call, _needQuote);
                    il.Emit(OpCodes.Stloc, needQuoteLocal);


                    il.Emit(OpCodes.Ldloc, needQuoteLocal);
                    il.Emit(OpCodes.Brfalse, needQuoteStartLabel);

                    il.Emit(OpCodes.Ldarg_1);
                    LoadQuotChar(il);
                    il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                    il.Emit(OpCodes.Pop);

                    il.MarkLabel(needQuoteStartLabel);

                    _defaultSerializerTypes[_dateTimeType] = _generatorDateToString;
                    _defaultSerializerTypes[_dateTimeOffsetType] = _generatorDateOffsetToString;

                    var serializerTypeMethods = new Dictionary<Type, MethodInfo>();

                    foreach (var kv in _defaultSerializerTypes) {
                        serializerTypeMethods[kv.Key] = kv.Value;
                    }

                    foreach (var kv in _registeredSerializerMethods) {
                        serializerTypeMethods[kv.Key] = kv.Value;
                    }

                    foreach (var kv in serializerTypeMethods) {
                        var objType = kv.Key;
                        var compareLabel = il.DefineLabel();

                        il.Emit(OpCodes.Ldloc, typeLocal);

                        il.Emit(OpCodes.Ldtoken, objType);
                        il.Emit(OpCodes.Call, _typeGetTypeFromHandle);

                        il.Emit(OpCodes.Call, _cTypeOpEquality);

                        il.Emit(OpCodes.Brfalse, compareLabel);

                        if (objType == _stringType) {
                            il.Emit(OpCodes.Ldarg_1);
                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Castclass, _stringType);
                            il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                            il.Emit(OpCodes.Pop);
                        } else if (objType == _enumType) {
                            il.Emit(OpCodes.Ldarg_1);
                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Unbox_Any, objType);
                            il.Emit(OpCodes.Ldarg_2);
                            il.Emit(OpCodes.Call, _generatorEnumToStr);
                            il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                            il.Emit(OpCodes.Pop);
                        } else if (objType == _boolType) {
                            var boolLocal = il.DeclareLocal(_stringType);
                            var boolLabel = il.DefineLabel();
                            il.Emit(OpCodes.Ldstr, "true");
                            il.Emit(OpCodes.Stloc, boolLocal);

                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Unbox_Any, _boolType);
                            il.Emit(OpCodes.Brtrue, boolLabel);
                            il.Emit(OpCodes.Ldstr, "false");
                            il.Emit(OpCodes.Stloc, boolLocal);
                            il.MarkLabel(boolLabel);

                            il.Emit(OpCodes.Ldarg_1);
                            il.Emit(OpCodes.Ldloc, boolLocal);
                            il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                            il.Emit(OpCodes.Pop);
                        } else if (objType == _objectType) {
                            il.Emit(OpCodes.Ldarg_1);
                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Callvirt, _stringBuilderAppendObject);
                            il.Emit(OpCodes.Pop);
                        } else {
                            il.Emit(OpCodes.Ldarg_1);
                            il.Emit(OpCodes.Ldarg_0);
                            if (objType.IsValueType)
                                il.Emit(OpCodes.Unbox_Any, objType);
                            else il.Emit(OpCodes.Castclass, objType);
                            if (objType == _dateTimeType || objType == _dateTimeOffsetType)
                                il.Emit(OpCodes.Ldarg_2);
                            il.Emit(OpCodes.Call, kv.Value);
                            il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                            il.Emit(OpCodes.Pop);
                        }

                        il.MarkLabel(compareLabel);
                    }

                    il.Emit(OpCodes.Ldloc, needQuoteLocal);
                    il.Emit(OpCodes.Brfalse, needQuoteEndLabel);

                    il.Emit(OpCodes.Ldarg_1);
                    LoadQuotChar(il);
                    il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                    il.Emit(OpCodes.Pop);

                    il.MarkLabel(needQuoteEndLabel);

                    il.Emit(OpCodes.Ret);

                    return method;
                }
            });
        }

        public static void GenerateTypesInto(string asmName, params Type[] types) {
            if (!types.Any())
                throw new NetJSONInvalidAssemblyGeneration(asmName);

            var assembly = GenerateAssemblyBuilderNoShare(asmName);
            var module = GenerateModuleBuilder(assembly);

            foreach (var type in types) {
                GenerateTypeBuilder(type, module).CreateType();
            }

            assembly.Save(String.Concat(assembly.GetName().Name, _dllStr));
        }

        internal static Type Generate(Type objType) {

            var returnType = default(Type);
            if (_types.TryGetValue(objType, out returnType))
                return returnType;

            var asmName = String.Concat(objType.GetName(), ClassStr);

            var assembly = _useSharedAssembly ? GenerateAssemblyBuilder() : GenerateAssemblyBuilderNoShare(asmName);

            var module = _useSharedAssembly ? GenerateModuleBuilder(assembly) : GenerateModuleBuilderNoShare(assembly);

            var type = GenerateTypeBuilder(objType, module);

            returnType = type.CreateType();
            _types[objType] = returnType;

            if (_generateAssembly)
                assembly.Save(String.Concat(assembly.GetName().Name, _dllStr));

            return returnType;
        }

        private static void EmitSettingsGetProperty(ILGenerator il, string name) {

        }

        private static TypeBuilder GenerateTypeBuilder(Type objType, ModuleBuilder module) {

            var genericType = _serializerType.MakeGenericType(objType);

            var type = module.DefineType(String.Concat(objType.GetName(), ClassStr), TypeAttribute, genericType);

            var isPrimitive = objType.IsPrimitiveType();

            var writeMethod = WriteSerializeMethodFor(type, objType, needQuote: !isPrimitive || objType == _stringType);

            var readMethod = WriteDeserializeMethodFor(type, objType);

            var serializeMethod = type.DefineMethod(SerializeStr, MethodAttribute,
                _stringType, new[] { objType });

            var serializeWithTextWriterMethod = type.DefineMethod(SerializeStr, MethodAttribute,
                _voidType, new[] { objType, _textWriterType });

            var deserializeMethod = type.DefineMethod(DeserializeStr, MethodAttribute,
                objType, new[] { _stringType });

            var deserializeWithReaderMethod = type.DefineMethod(DeserializeStr, MethodAttribute,
                objType, new[] { _textReaderType });


            var serializeMethodWithSettings = type.DefineMethod(SerializeStr, MethodAttribute,
               _stringType, new[] { objType, _settingsType });

            var serializeWithTextWriterMethodWithSettings = type.DefineMethod(SerializeStr, MethodAttribute,
                _voidType, new[] { objType, _textWriterType, _settingsType });

            var deserializeMethodWithSettings = type.DefineMethod(DeserializeStr, MethodAttribute,
                objType, new[] { _stringType, _settingsType });

            var deserializeWithReaderMethodWithSettings = type.DefineMethod(DeserializeStr, MethodAttribute,
                objType, new[] { _textReaderType, _settingsType });

            var il = serializeMethod.GetILGenerator();

            var sbLocal = il.DeclareLocal(_stringBuilderType);
            il.Emit(OpCodes.Call, _generatorGetStringBuilder);

            il.Emit(
#if NET_35
OpCodes.Call,
#else
                    OpCodes.Callvirt,
#endif
 _stringBuilderClear);

            il.Emit(OpCodes.Stloc, sbLocal);

            //il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldloc, sbLocal);
            il.Emit(OpCodes.Call, _settingsCurrentSettings);
            il.Emit(OpCodes.Call, writeMethod);

            il.Emit(OpCodes.Ldloc, sbLocal);
            il.Emit(OpCodes.Callvirt, _stringBuilderToString);
            il.Emit(OpCodes.Ret);

            var wil = serializeWithTextWriterMethod.GetILGenerator();

            var wsbLocal = wil.DeclareLocal(_stringBuilderType);
            wil.Emit(OpCodes.Call, _generatorGetStringBuilder);
            wil.Emit(
#if NET_35
OpCodes.Call,
#else
                    OpCodes.Callvirt,
#endif
 _stringBuilderClear);
            wil.Emit(OpCodes.Stloc, wsbLocal);

            //il.Emit(OpCodes.Ldarg_0);
            wil.Emit(OpCodes.Ldarg_1);
            wil.Emit(OpCodes.Ldloc, wsbLocal);
            wil.Emit(OpCodes.Call, _settingsCurrentSettings);
            wil.Emit(OpCodes.Call, writeMethod);

            wil.Emit(OpCodes.Ldarg_2);
            wil.Emit(OpCodes.Ldloc, wsbLocal);
            wil.Emit(OpCodes.Callvirt, _stringBuilderToString);
            wil.Emit(OpCodes.Callvirt, _textWriterWrite);
            wil.Emit(OpCodes.Ret);

            var dil = deserializeMethod.GetILGenerator();
            
            dil.Emit(OpCodes.Ldarg_1);
            dil.Emit(OpCodes.Call, _settingsCurrentSettings);
            dil.Emit(OpCodes.Call, readMethod);
            dil.Emit(OpCodes.Ret);

            var rdil = deserializeWithReaderMethod.GetILGenerator();

            rdil.Emit(OpCodes.Ldarg_1);
            rdil.Emit(OpCodes.Callvirt, _textReaderReadToEnd);
            rdil.Emit(OpCodes.Call, _settingsCurrentSettings);
            rdil.Emit(OpCodes.Call, readMethod);
            rdil.Emit(OpCodes.Ret);

            //With Settings
            var isValueType = objType.IsValueType;
            var ilWithSettings = serializeMethodWithSettings.GetILGenerator();

            var sbLocalWithSettings = ilWithSettings.DeclareLocal(_stringBuilderType);
            ilWithSettings.Emit(OpCodes.Call, _generatorGetStringBuilder);

            ilWithSettings.Emit(
#if NET_35
OpCodes.Call,
#else
OpCodes.Callvirt,
#endif
 _stringBuilderClear);

            ilWithSettings.Emit(OpCodes.Stloc, sbLocalWithSettings);

            //il.Emit(OpCodes.Ldarg_0);
            ilWithSettings.Emit(OpCodes.Ldarg_1);
            ilWithSettings.Emit(OpCodes.Ldloc, sbLocalWithSettings);
            ilWithSettings.Emit(OpCodes.Ldarg_2);
            ilWithSettings.Emit(OpCodes.Call, writeMethod);

            ilWithSettings.Emit(OpCodes.Ldloc, sbLocalWithSettings);
            ilWithSettings.Emit(OpCodes.Callvirt, _stringBuilderToString);

            ilWithSettings.Emit(OpCodes.Ldarg_2);
            
            ilWithSettings.Emit(OpCodes.Call, _prettifyJSONIfNeeded);

            ilWithSettings.Emit(OpCodes.Ret);

            var wilWithSettings = serializeWithTextWriterMethodWithSettings.GetILGenerator();

            var wsbLocalWithSettings = wilWithSettings.DeclareLocal(_stringBuilderType);
            wilWithSettings.Emit(OpCodes.Call, _generatorGetStringBuilder);
            wilWithSettings.Emit(
#if NET_35
OpCodes.Call,
#else
OpCodes.Callvirt,
#endif
 _stringBuilderClear);
            wilWithSettings.Emit(OpCodes.Stloc, wsbLocalWithSettings);

            //il.Emit(OpCodes.Ldarg_0);
            wilWithSettings.Emit(OpCodes.Ldarg_1);
            wilWithSettings.Emit(OpCodes.Ldloc, wsbLocalWithSettings);
            wilWithSettings.Emit(OpCodes.Ldarg_3);
            wilWithSettings.Emit(OpCodes.Call, writeMethod);

            wilWithSettings.Emit(OpCodes.Ldarg_2);
            wilWithSettings.Emit(OpCodes.Ldloc, wsbLocalWithSettings);
            wilWithSettings.Emit(OpCodes.Callvirt, _stringBuilderToString);

            wilWithSettings.Emit(OpCodes.Ldarg_3);

            wilWithSettings.Emit(OpCodes.Call, _prettifyJSONIfNeeded);

            wilWithSettings.Emit(OpCodes.Callvirt, _textWriterWrite);
            wilWithSettings.Emit(OpCodes.Ret);

            var dilWithSettings = deserializeMethodWithSettings.GetILGenerator();

            dilWithSettings.Emit(OpCodes.Ldarg_1);
            dilWithSettings.Emit(OpCodes.Ldarg_2);
            dilWithSettings.Emit(OpCodes.Call, readMethod);
            dilWithSettings.Emit(OpCodes.Ret);

            var rdilWithSettings = deserializeWithReaderMethodWithSettings.GetILGenerator();

            rdilWithSettings.Emit(OpCodes.Ldarg_1);
            rdilWithSettings.Emit(OpCodes.Callvirt, _textReaderReadToEnd);
            rdilWithSettings.Emit(OpCodes.Ldarg_2);
            rdilWithSettings.Emit(OpCodes.Call, readMethod);
            rdilWithSettings.Emit(OpCodes.Ret);

            //With Settings End

            type.DefineMethodOverride(serializeMethod,
                genericType.GetMethod(SerializeStr, new[] { objType }));

            type.DefineMethodOverride(serializeWithTextWriterMethod,
                genericType.GetMethod(SerializeStr, new[] { objType, _textWriterType }));

            type.DefineMethodOverride(deserializeMethod,
                genericType.GetMethod(DeserializeStr, new[] { _stringType }));

            type.DefineMethodOverride(deserializeWithReaderMethod,
                genericType.GetMethod(DeserializeStr, new[] { _textReaderType }));

            //With Settings
            type.DefineMethodOverride(serializeMethodWithSettings,
               genericType.GetMethod(SerializeStr, new Type[] { objType, _settingsType }));

            type.DefineMethodOverride(serializeWithTextWriterMethodWithSettings,
                genericType.GetMethod(SerializeStr, new Type[] { objType, _textWriterType, _settingsType }));

            type.DefineMethodOverride(deserializeMethodWithSettings,
                genericType.GetMethod(DeserializeStr, new Type[] { _stringType, _settingsType }));

            type.DefineMethodOverride(deserializeWithReaderMethodWithSettings,
                genericType.GetMethod(DeserializeStr, new[] { _textReaderType, _settingsType }));
            //With Settings End
            return type;
        }

        private static ModuleBuilder GenerateModuleBuilderNoShare(AssemblyBuilder assembly) {
            var module = assembly.DefineDynamicModule(String.Concat(assembly.GetName().Name, _dllStr));
            return module;
        }

        private static ModuleBuilder GenerateModuleBuilder(AssemblyBuilder assembly) {
            if (_module == null) {
                lock (_lockAsmObject) {
                    if (_module == null)
                        _module = assembly.DefineDynamicModule(String.Concat(assembly.GetName().Name, _dllStr));
                }
            }
            return _module;
        }

        private readonly static object _lockAsmObject = new object();
        private static AssemblyBuilder _assembly = null;
        private static ModuleBuilder _module = null;
        private const string NET_JSON_GENERATED_ASSEMBLY_NAME = "NetJSONGeneratedAssembly";

        private static AssemblyBuilder GenerateAssemblyBuilder() {
            if (_assembly == null) {
                lock (_lockAsmObject) {
                    if (_assembly == null) {
                        _assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(
                            new AssemblyName(NET_JSON_GENERATED_ASSEMBLY_NAME) {
                                Version = new Version(1, 0, 0, 0)
                            },
                            AssemblyBuilderAccess.RunAndSave);


                        //[assembly: CompilationRelaxations(8)]
                        _assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(CompilationRelaxationsAttribute).GetConstructor(new[] { _intType }), new object[] { 8 }));

                        //[assembly: RuntimeCompatibility(WrapNonExceptionThrows=true)]
                        _assembly.SetCustomAttribute(new CustomAttributeBuilder(
                            typeof(RuntimeCompatibilityAttribute).GetConstructor(Type.EmptyTypes),
                            new object[] { },
                            new[] {  typeof(RuntimeCompatibilityAttribute).GetProperty("WrapNonExceptionThrows")
                },
                            new object[] { true }));

                        //[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification=true)]
                        _assembly.SetCustomAttribute(new CustomAttributeBuilder(
                            typeof(SecurityPermissionAttribute).GetConstructor(new[] { typeof(SecurityAction) }),
                            new object[] { SecurityAction.RequestMinimum },
                            new[] {  typeof(SecurityPermissionAttribute).GetProperty("SkipVerification")
                },
                            new object[] { true }));
                        //return _assembly;
                    }
                }
            }
            return _assembly;
        }

        private static AssemblyBuilder GenerateAssemblyBuilderNoShare(string asmName) {
            var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName(asmName) {
                    Version = new Version(1, 0, 0, 0)
                },
                AssemblyBuilderAccess.RunAndSave);


            //[assembly: CompilationRelaxations(8)]
            assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(CompilationRelaxationsAttribute).GetConstructor(new[] { _intType }), new object[] { 8 }));

            //[assembly: RuntimeCompatibility(WrapNonExceptionThrows=true)]
            assembly.SetCustomAttribute(new CustomAttributeBuilder(
                typeof(RuntimeCompatibilityAttribute).GetConstructor(Type.EmptyTypes),
                new object[] { },
                new[] {  typeof(RuntimeCompatibilityAttribute).GetProperty("WrapNonExceptionThrows")
                },
                new object[] { true }));

            //[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification=true)]
            assembly.SetCustomAttribute(new CustomAttributeBuilder(
                typeof(SecurityPermissionAttribute).GetConstructor(new[] { typeof(SecurityAction) }),
                new object[] { SecurityAction.RequestMinimum },
                new[] {  typeof(SecurityPermissionAttribute).GetProperty("SkipVerification")
                },
                new object[] { true }));
            return assembly;
        }

        public static unsafe void SkipProperty(char* ptr, ref int index, NetJSONSettings settings) {
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
                    } else {
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

        public static string PrettifyJSONIfNeeded(string str, NetJSONSettings settings) {
            if (settings.Format == NetJSONFormat.Prettify)
                return PrettifyJSON(str);
            return str;
        }

        public static unsafe string PrettifyJSON(string str) {
            var sb = new StringBuilder();
            
            var horizontal = 0;
            var horizontals = new int[10000];
            var hrIndex = -1;
            bool @return = false;
            char c;

            fixed (char* chr = str) {
                char* ptr = chr;
                while ((c = *(ptr++)) != '\0') {
                    switch (c) {
                        case '{':
                        case '[':
                            sb.Append(c);
                            hrIndex++;
                            horizontals[hrIndex] = horizontal;
                            @return = true;
                            break;
                        case '}':
                        case ']':
                            @return = false;
                            sb.Append('\n');
                            horizontal = horizontals[hrIndex];
                            hrIndex--;
                            for (var i = 0; i < horizontal; i++) {
                                sb.Append(' ');
                            }
                            sb.Append(c);
                            break;
                        case ',':
                            sb.Append(c);
                            @return = true;
                            break;
                        default:
                            if (@return) {
                                @return = false;
                                sb.Append('\n');
                                horizontal = horizontals[hrIndex] + 1;
                                for (var i = 0; i < horizontal; i++) {
                                    sb.Append(' ');
                                }
                            }
                            sb.Append(c);
                            break;
                    }

                    horizontal++;
                }
            }

            return sb.ToString();
        }

        public static unsafe void EncodedJSONString(StringBuilder sb, string str, NetJSONSettings settings) {
            var quote = settings._quoteChar;
            char c;
            fixed (char* chr = str) {
                char* ptr = chr;
                while ((c = *(ptr++)) != '\0') {
                    switch (c) {
                        //case '"': sb.Append("\\\""); break;
                        case '\\': sb.Append(@"\\"); break;
                        case '\u0000': sb.Append(@"\u0000"); break;
                        case '\u0001': sb.Append(@"\u0001"); break;
                        case '\u0002': sb.Append(@"\u0002"); break;
                        case '\u0003': sb.Append(@"\u0003"); break;
                        case '\u0004': sb.Append(@"\u0004"); break;
                        case '\u0005': sb.Append(@"\u0005"); break;
                        case '\u0006': sb.Append(@"\u0006"); break;
                        case '\u0007': sb.Append(@"\u0007"); break;
                        case '\u0008': sb.Append(@"\b"); break;
                        case '\u0009': sb.Append(@"\t"); break;
                        case '\u000A': sb.Append(@"\n"); break;
                        case '\u000B': sb.Append(@"\u000B"); break;
                        case '\u000C': sb.Append(@"\f"); break;
                        case '\u000D': sb.Append(@"\r"); break;
                        case '\u000E': sb.Append(@"\u000E"); break;
                        case '\u000F': sb.Append(@"\u000F"); break;
                        case '\u0010': sb.Append(@"\u0010"); break;
                        case '\u0011': sb.Append(@"\u0011"); break;
                        case '\u0012': sb.Append(@"\u0012"); break;
                        case '\u0013': sb.Append(@"\u0013"); break;
                        case '\u0014': sb.Append(@"\u0014"); break;
                        case '\u0015': sb.Append(@"\u0015"); break;
                        case '\u0016': sb.Append(@"\u0016"); break;
                        case '\u0017': sb.Append(@"\u0017"); break;
                        case '\u0018': sb.Append(@"\u0018"); break;
                        case '\u0019': sb.Append(@"\u0019"); break;
                        case '\u001A': sb.Append(@"\u001A"); break;
                        case '\u001B': sb.Append(@"\u001B"); break;
                        case '\u001C': sb.Append(@"\u001C"); break;
                        case '\u001D': sb.Append(@"\u001D"); break;
                        case '\u001E': sb.Append(@"\u001E"); break;
                        case '\u001F': sb.Append(@"\u001F"); break;
                        default:
                            if (quote == c) {
                                if (quote == '"')
                                    sb.Append("\\\"");
                                else if (quote == '\'')
                                    sb.Append("\\\'");
                            } else
                                sb.Append(c);
                            break;
                    }

                }
            }
        }

        public static string GetName(this Type type) {
            var sb = new StringBuilder();
            var arguments =
                !type.IsGenericType ? Type.EmptyTypes :
                type.GetGenericArguments();
            if (!type.IsGenericType) {
                sb.Append(type.Name);
            } else {
                sb.Append(type.Name);
                foreach (var argument in arguments)
                    sb.Append(GetName(argument));
            }
            return sb.ToString();
        }

        public static string Fix(this string name) {
            return _fixes.GetOrAdd(name, key => {
                lock (GetDictLockObject("FixName")) {
                    var index = key.IndexOf(CarrotQuoteChar, StringComparison.OrdinalIgnoreCase);
                    var quoteText = index > -1 ? key.Substring(index, 2) : CarrotQuoteChar;
                    var value = key.Replace(quoteText, string.Empty).Replace(ArrayLiteral, ArrayStr).Replace(AnonymousBracketStr, string.Empty);
                    if (value.Contains(CarrotQuoteChar))
                        value = Fix(value);
                    return value;
                }
            });
        }

        internal static MethodInfo ReadStringToEnumFor(TypeBuilder typeBuilder, Type type) {
            MethodBuilder method;
            var key = type.FullName;
            var typeName = type.GetName().Fix();
            if (_readEnumToStringMethodBuilders.TryGetValue(key, out method))
                return method;
            var methodName = String.Concat(ReadEnumStr, typeName);
            method = typeBuilder.DefineMethod(methodName, StaticMethodAttribute,
                type, new[] { _stringType });
            _readEnumToStringMethodBuilders[key] = method;

            var eType = type.GetEnumUnderlyingType();
            var il = method.GetILGenerator();

            var values = Enum.GetValues(type).Cast<object>().ToArray();
            var keys = Enum.GetNames(type);

            for (var i = 0; i < values.Length; i++) {

                var value = values[i];
                var k = keys[i];
                
                var label = il.DefineLabel();
                var label2 = il.DefineLabel();

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldstr, k);
                il.Emit(OpCodes.Call, _stringOpEquality);
                il.Emit(OpCodes.Brfalse, label);

                if (eType == _intType)
                    il.Emit(OpCodes.Ldc_I4, (int)value);
                else if (eType == _longType)
                    il.Emit(OpCodes.Ldc_I8, (long)value);
                else if (eType == typeof(ulong))
                    il.Emit(OpCodes.Ldc_I8, (long)((ulong)value));
                else if (eType == typeof(uint))
                    il.Emit(OpCodes.Ldc_I4, (uint)value);
                else if (eType == typeof(byte)) {
                    il.Emit(OpCodes.Ldc_I4, (int)((byte)value));
                    il.Emit(OpCodes.Conv_U1);
                } else if (eType == typeof(ushort)) {
                    il.Emit(OpCodes.Ldc_I4, (int)((ushort)value));
                    il.Emit(OpCodes.Conv_U2);
                } else if (eType == typeof(short)) {
                    il.Emit(OpCodes.Ldc_I4, (int)((short)value));
                    il.Emit(OpCodes.Conv_I2);
                }

                
                il.Emit(OpCodes.Ret);

                il.MarkLabel(label);

                il.Emit(OpCodes.Ldarg_0);


                if (eType == _intType)
                    il.Emit(OpCodes.Ldstr, IntToStr((int)value));
                else if (eType == _longType)
                    il.Emit(OpCodes.Ldstr, LongToStr((long)value));
                else if (eType == typeof(ulong))
                    il.Emit(OpCodes.Ldstr, IntUtility.ultoa((ulong)value));
                else if (eType == typeof(uint))
                    il.Emit(OpCodes.Ldstr, IntUtility.uitoa((uint)value));
                else if (eType == typeof(byte))
                    il.Emit(OpCodes.Ldstr, IntToStr((int)((byte)value)));
                else if (eType == typeof(ushort))
                    il.Emit(OpCodes.Ldstr, IntToStr((int)((ushort)value)));
                else if (eType == typeof(short))
                    il.Emit(OpCodes.Ldstr, IntToStr((int)((short)value)));
                 
                il.Emit(OpCodes.Call, _stringOpEquality);
                il.Emit(OpCodes.Brfalse, label2);

                if (eType == _intType)
                    il.Emit(OpCodes.Ldc_I4, (int)value);
                else if (eType == _longType)
                    il.Emit(OpCodes.Ldc_I8, (long)value);
                else if (eType == typeof(ulong))
                    il.Emit(OpCodes.Ldc_I8, (long)((ulong)value));
                else if (eType == typeof(uint))
                    il.Emit(OpCodes.Ldc_I4, (uint)value);
                else if (eType == typeof(byte)) {
                    il.Emit(OpCodes.Ldc_I4, (int)((byte)value));
                    il.Emit(OpCodes.Conv_U1);
                } else if (eType == typeof(ushort)) {
                    il.Emit(OpCodes.Ldc_I4, (int)((ushort)value));
                    il.Emit(OpCodes.Conv_U2);
                } else if (eType == typeof(short)) {
                    il.Emit(OpCodes.Ldc_I4, (int)((short)value));
                    il.Emit(OpCodes.Conv_I2);
                }

                il.Emit(OpCodes.Ret);

                il.MarkLabel(label2);
            }

            //Return default enum if no match is found
            LoadDefaultValueByType(il, eType);
            il.Emit(OpCodes.Ret);

            return method;
        }

        internal static MethodInfo WriteEnumToStringFor(TypeBuilder typeBuilder, Type type) {
            MethodBuilder method;
            var key = type.FullName;
            var typeName = type.GetName().Fix();
            if (_writeEnumToStringMethodBuilders.TryGetValue(key, out method))
                return method;
            var methodName = String.Concat(WriteStr, typeName);
            method = typeBuilder.DefineMethod(methodName, StaticMethodAttribute,
                _stringType, new[] { type, _settingsType });
            _writeEnumToStringMethodBuilders[key] = method;

            var eType = type.GetEnumUnderlyingType();

            var il = method.GetILGenerator();
            var useEnumLabel = il.DefineLabel();


            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Brfalse, useEnumLabel);

            WriteEnumToStringForWithString(type, eType, il);

            il.MarkLabel(useEnumLabel);

            WriteEnumToStringForWithInt(type, eType, il);

            il.Emit(OpCodes.Ldstr, "0");

            il.Emit(OpCodes.Ret);

            return method;
        }

        private static void WriteEnumToStringForWithInt(Type type, Type eType, ILGenerator il) {
            var values = Enum.GetValues(type).Cast<object>().ToArray();

            var count = values.Length;

            for (var i = 0; i < count; i++) {

                var value = values[i];

                var label = il.DefineLabel();

                il.Emit(OpCodes.Ldarg_0);

                if (eType == _intType)
                    il.Emit(OpCodes.Ldc_I4, (int)value);
                else if (eType == _longType)
                    il.Emit(OpCodes.Ldc_I8, (long)value);
                else if (eType == typeof(ulong))
                    il.Emit(OpCodes.Ldc_I8, (long)((ulong)value));
                else if (eType == typeof(uint))
                    il.Emit(OpCodes.Ldc_I4, (uint)value);
                else if (eType == typeof(byte)) {
                    il.Emit(OpCodes.Ldc_I4, (int)((byte)value));
                    il.Emit(OpCodes.Conv_U1);
                } else if (eType == typeof(ushort)) {
                    il.Emit(OpCodes.Ldc_I4, (int)((ushort)value));
                    il.Emit(OpCodes.Conv_U2);
                } else if (eType == typeof(short)) {
                    il.Emit(OpCodes.Ldc_I4, (int)((short)value));
                    il.Emit(OpCodes.Conv_I2);
                }

                il.Emit(OpCodes.Bne_Un, label);

                if (eType == _intType)
                    il.Emit(OpCodes.Ldstr, IntToStr((int)value));
                else if (eType == _longType)
                    il.Emit(OpCodes.Ldstr, LongToStr((long)value));
                else if (eType == typeof(ulong))
                    il.Emit(OpCodes.Ldstr, IntUtility.ultoa((ulong)value));
                else if (eType == typeof(uint))
                    il.Emit(OpCodes.Ldstr, IntUtility.uitoa((uint)value));
                else if (eType == typeof(byte))
                    il.Emit(OpCodes.Ldstr, IntToStr((int)((byte)value)));
                else if (eType == typeof(ushort))
                    il.Emit(OpCodes.Ldstr, IntToStr((int)((ushort)value)));
                else if (eType == typeof(short))
                    il.Emit(OpCodes.Ldstr, IntToStr((int)((short)value)));


                il.Emit(OpCodes.Ret);

                il.MarkLabel(label);
            }
        }

        private static void WriteEnumToStringForWithString(Type type, Type eType, ILGenerator il) {
            var values = Enum.GetValues(type).Cast<object>().ToArray();
            var names = Enum.GetNames(type);

            var count = values.Length;

            for (var i = 0; i < count; i++) {

                var value = values[i];

                var label = il.DefineLabel();

                il.Emit(OpCodes.Ldarg_0);

                if (eType == _intType)
                    il.Emit(OpCodes.Ldc_I4, (int)value);
                else if (eType == _longType)
                    il.Emit(OpCodes.Ldc_I8, (long)value);
                else if (eType == typeof(ulong))
                    il.Emit(OpCodes.Ldc_I8, (long)((ulong)value));
                else if (eType == typeof(uint))
                    il.Emit(OpCodes.Ldc_I4, (uint)value);
                else if (eType == typeof(byte)) {
                    il.Emit(OpCodes.Ldc_I4, (int)((byte)value));
                    il.Emit(OpCodes.Conv_U1);
                } else if (eType == typeof(ushort)) {
                    il.Emit(OpCodes.Ldc_I4, (int)((ushort)value));
                    il.Emit(OpCodes.Conv_U2);
                } else if (eType == typeof(short)) {
                    il.Emit(OpCodes.Ldc_I4, (int)((short)value));
                    il.Emit(OpCodes.Conv_I2);
                }

                il.Emit(OpCodes.Bne_Un, label);

                il.Emit(OpCodes.Ldstr, names[i]);
                il.Emit(OpCodes.Ret);

                il.MarkLabel(label);
            }
        }

        internal static MethodInfo WriteDeserializeMethodFor(TypeBuilder typeBuilder, Type type) {
            MethodBuilder method;
            var key = type.FullName;
            var typeName = type.GetName().Fix();
            if (_readDeserializeMethodBuilders.TryGetValue(key, out method))
                return method;
            var methodName = String.Concat(ReadStr, typeName);
            method = typeBuilder.DefineMethod(methodName, StaticMethodAttribute,
                type, new[] { _stringType, _settingsType });
            _readDeserializeMethodBuilders[key] = method;
            var il = method.GetILGenerator();

            var index = il.DeclareLocal(_intType);

            var ptr = il.DeclareLocal(typeof(char*));
            var pinned = il.DeclareLocal(typeof(string), true);

            var @fixed = il.DefineLabel();

            //fixed
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Stloc, pinned);

            il.Emit(OpCodes.Ldloc, pinned);
            il.Emit(OpCodes.Conv_I);
            il.Emit(OpCodes.Dup);

            il.Emit(OpCodes.Brfalse, @fixed);
            il.Emit(OpCodes.Call, typeof(RuntimeHelpers).GetMethod("get_OffsetToStringData"));
            il.Emit(OpCodes.Add);
            il.MarkLabel(@fixed);

            //char* ptr = str;
            il.Emit(OpCodes.Stloc, ptr);

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, index);


            if (type == _objectType) {
                var startsWithLabel = il.DefineLabel();
                var notStartsWithLabel = il.DefineLabel();
                var startsWith = il.DeclareLocal(_boolType);
                var notDictOrArrayLabel = il.DefineLabel();
                var notDictOrArray = il.DeclareLocal(_boolType);


                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, _isRawPrimitive);
                il.Emit(OpCodes.Stloc, notDictOrArray);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldstr, "[");
                il.Emit(OpCodes.Callvirt, _stringType.GetMethod("StartsWith", new []{ _stringType }));
                il.Emit(OpCodes.Stloc, startsWith);

                il.Emit(OpCodes.Ldloc, notDictOrArray);
                il.Emit(OpCodes.Brfalse, notDictOrArrayLabel);

                //IsPrimitive
                il.Emit(OpCodes.Ldloc, ptr);
                il.Emit(OpCodes.Ldloca, index);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Call, GenerateExtractValueFor(typeBuilder, type));
                il.Emit(OpCodes.Ret);

                il.MarkLabel(notDictOrArrayLabel);


                il.Emit(OpCodes.Ldloc, startsWith);
                il.Emit(OpCodes.Brfalse, startsWithLabel);

                //IsArray
                il.Emit(OpCodes.Ldloc, ptr);
                il.Emit(OpCodes.Ldloca, index);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Call, GenerateCreateListFor(typeBuilder, typeof(List<object>)));
                il.Emit(OpCodes.Ret);

                il.MarkLabel(startsWithLabel);

                il.Emit(OpCodes.Ldloc, startsWith);
                il.Emit(OpCodes.Brtrue, notStartsWithLabel);



                //IsDictionary
                il.Emit(OpCodes.Ldloc, ptr);
                il.Emit(OpCodes.Ldloca, index);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Call, GenerateGetClassOrDictFor(typeBuilder, 
                    typeof(Dictionary<string, object>)));
                il.Emit(OpCodes.Ret);

                il.MarkLabel(notStartsWithLabel);

                il.Emit(OpCodes.Ldnull);
            } else {
                var isArray = type.IsListType() || type.IsArray;

                il.Emit(OpCodes.Ldloc, ptr);
                il.Emit(OpCodes.Ldloca, index);
                il.Emit(OpCodes.Ldarg_1);
                if (isArray)
                    il.Emit(OpCodes.Call, GenerateCreateListFor(typeBuilder, type));
                else {
                    if (type.IsPrimitiveType()) {
                        il.Emit(OpCodes.Call, GenerateExtractValueFor(typeBuilder, type));
                    }else
                        il.Emit(OpCodes.Call, GenerateGetClassOrDictFor(typeBuilder, type));
                }
            }
            
            il.Emit(OpCodes.Ret);

            return method;
        }

        internal static MethodInfo WriteSerializeMethodFor(TypeBuilder typeBuilder, Type type, bool needQuote = true) {
            MethodBuilder method;
            var key = type.FullName;
            var typeName = type.GetName().Fix();
            if (_writeMethodBuilders.TryGetValue(key, out method))
                return method;
            var methodName = String.Concat(WriteStr, typeName);

            method = typeBuilder.DefineMethod(methodName, StaticMethodAttribute,
                _voidType, new[] { type, _stringBuilderType, _settingsType });
            _writeMethodBuilders[key] = method;
            var il = method.GetILGenerator();
            var isTypeObject = type == _objectType;
            var originalType = type;
            var nullableType = type.GetNullableType();
            var isNullable = nullableType != null && !originalType.IsArray;
            type = isNullable ? nullableType : type;

            if (type.IsPrimitiveType() || isTypeObject) {
                var nullLabel = il.DefineLabel();
                var valueLocal = isNullable ? il.DeclareLocal(type) : null;

                if (isNullable) {
                    var nullableValue = il.DeclareLocal(originalType);
                    var nullableLabel = il.DefineLabel();

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Stloc, nullableValue);

                    var hasValueMethod = originalType.GetMethod("get_HasValue");

                    if (hasValueMethod != null) {
                        il.Emit(OpCodes.Ldloca, nullableValue);
                        il.Emit(OpCodes.Call, hasValueMethod);
                    } else
                        il.Emit(OpCodes.Ldloc, nullableValue);
                    il.Emit(OpCodes.Brtrue, nullableLabel);


                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldstr, NullStr);
                    il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                    il.Emit(OpCodes.Pop);

                    il.Emit(OpCodes.Ret);

                    il.MarkLabel(nullableLabel);

                    il.Emit(OpCodes.Ldloca, nullableValue);
                    il.Emit(OpCodes.Call, originalType.GetMethod("GetValueOrDefault", Type.EmptyTypes));

                    il.Emit(OpCodes.Stloc, valueLocal);
                }

                needQuote = needQuote && (type == _stringType || type == _charType || type == _guidType || type == _timeSpanType || type == _byteArrayType);

                if (type == _stringType || isTypeObject) {

                    if (isNullable)
                        il.Emit(OpCodes.Ldloc, valueLocal);
                    else
                        il.Emit(OpCodes.Ldarg_0);
                    if (type == _stringType) {
                        il.Emit(OpCodes.Ldnull);
                        il.Emit(OpCodes.Call, _stringOpEquality);
                        il.Emit(OpCodes.Brfalse, nullLabel);
                    } else
                        il.Emit(OpCodes.Brtrue, nullLabel);

                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldstr, NullStr);
                    il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                    il.Emit(OpCodes.Pop);

                    il.Emit(OpCodes.Ret);

                    il.MarkLabel(nullLabel);

                    if (needQuote) {
                        il.Emit(OpCodes.Ldarg_1);
                        LoadQuotChar(il);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        //il.Emit(OpCodes.Pop);
                    }

                    //il.Emit(OpCodes.Ldarg_2);

                    if (type == _objectType) {
                        if (isNullable)
                            il.Emit(OpCodes.Ldloc, valueLocal);
                        else
                            il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Ldarg_2);
                        il.Emit(OpCodes.Call, GenerateFastObjectToString(typeBuilder));
                        il.Emit(OpCodes.Ldarg_1);
                        //il.Emit(OpCodes.Pop);
                    } else if (type == _stringType) {
                        if (isNullable)
                            il.Emit(OpCodes.Ldloc, valueLocal);
                        else
                            il.Emit(OpCodes.Ldarg_0);
                        //il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Ldarg_2);
                        il.Emit(OpCodes.Call, _encodedJSONString);
                        il.Emit(OpCodes.Ldarg_1);
                    }

                    if (needQuote) {
                        //il.Emit(OpCodes.Ldarg_2);
                        LoadQuotChar(il);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);
                    } else il.Emit(OpCodes.Pop);
                } else {

                    if (type == _dateTimeType || type == _dateTimeOffsetType) {

                        var needDateQuoteLocal = il.DeclareLocal(_boolType);
                        var needDateCheck = il.DefineLabel();
                        var needDateQuoteLabel1 = il.DefineLabel();
                        var needDateQuoteLabel2 = il.DefineLabel();

                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Stloc, needDateQuoteLocal);

                        il.Emit(OpCodes.Ldarg_2);
                        il.Emit(OpCodes.Callvirt, _settingsDateFormat);
                        il.Emit(OpCodes.Ldc_I4, (int)NetJSONDateFormat.EpochTime);
                        il.Emit(OpCodes.Ceq);
                        il.Emit(OpCodes.Brfalse, needDateCheck);

                        il.Emit(OpCodes.Ldc_I4_1);
                        il.Emit(OpCodes.Stloc, needDateQuoteLocal);

                        il.MarkLabel(needDateCheck);

                        il.Emit(OpCodes.Ldloc, needDateQuoteLocal);
                        il.Emit(OpCodes.Brtrue, needDateQuoteLabel1);

                        il.Emit(OpCodes.Ldarg_1);
                        LoadQuotChar(il);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);

                        il.MarkLabel(needDateQuoteLabel1);

                        //if (needDateQuote) {
                        //    il.Emit(OpCodes.Ldarg_1);
                        //    LoadQuotChar(il);
                        //    il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        //    il.Emit(OpCodes.Pop);
                        //}

                        il.Emit(OpCodes.Ldarg_1);
                        //il.Emit(OpCodes.Ldstr, IsoFormat);
                        if (isNullable)
                            il.Emit(OpCodes.Ldloc, valueLocal);
                        else
                            il.Emit(OpCodes.Ldarg_0);
                        //il.Emit(OpCodes.Box, _dateTimeType);
                        //il.Emit(OpCodes.Call, _stringFormat);
                        il.Emit(OpCodes.Ldarg_2);
                        il.Emit(OpCodes.Call, type == _dateTimeType ? _generatorDateToString : _generatorDateOffsetToString);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);

                        //if (needDateQuote) {
                        //    il.Emit(OpCodes.Ldarg_1);
                        //    LoadQuotChar(il);
                        //    il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        //    il.Emit(OpCodes.Pop);
                        //}

                        il.Emit(OpCodes.Ldloc, needDateQuoteLocal);
                        il.Emit(OpCodes.Brtrue, needDateQuoteLabel2);

                        il.Emit(OpCodes.Ldarg_1);
                        LoadQuotChar(il);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);

                        il.MarkLabel(needDateQuoteLabel2);

                    } else if (type == _byteArrayType) {

                        if (isNullable)
                            il.Emit(OpCodes.Ldloc, valueLocal);
                        else
                            il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Brtrue, nullLabel);

                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Ldstr, NullStr);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);

                        il.Emit(OpCodes.Ret);
                        il.MarkLabel(nullLabel);

                        il.Emit(OpCodes.Ldarg_1);
                        LoadQuotChar(il);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);

                        il.Emit(OpCodes.Ldarg_1);
                        if (isNullable)
                            il.Emit(OpCodes.Ldloc, valueLocal);
                        else
                            il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Call, _convertBase64);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);
                        il.Emit(OpCodes.Ldarg_1);
                        LoadQuotChar(il);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);

                    } else if (type == _boolType) {
                        var boolLocal = il.DeclareLocal(_stringType);
                        var boolLabel = il.DefineLabel();
                        il.Emit(OpCodes.Ldstr, "true");
                        il.Emit(OpCodes.Stloc, boolLocal);

                        if (isNullable)
                            il.Emit(OpCodes.Ldloc, valueLocal);
                        else
                            il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Brtrue, boolLabel);
                        il.Emit(OpCodes.Ldstr, "false");
                        il.Emit(OpCodes.Stloc, boolLocal);
                        il.MarkLabel(boolLabel);

                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Ldloc, boolLocal);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);
                    } else if (type.IsEnum) {
                        var useEnumStringLocal = il.DeclareLocal(_boolType);
                        var useEnumLabel1 = il.DefineLabel();
                        var useEnumLabel2 = il.DefineLabel();

                        il.Emit(OpCodes.Ldarg_2);
                        il.Emit(OpCodes.Callvirt, _settingsUseEnumStringProp);
                        il.Emit(OpCodes.Stloc, useEnumStringLocal);

                        il.Emit(OpCodes.Ldloc, useEnumStringLocal);
                        il.Emit(OpCodes.Brfalse, useEnumLabel1);
                        il.Emit(OpCodes.Ldarg_1);
                        LoadQuotChar(il);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);
                        
                        il.MarkLabel(useEnumLabel1);

                        il.Emit(OpCodes.Ldarg_1);
                        if (isNullable)
                            il.Emit(OpCodes.Ldloc, valueLocal);
                        else
                            il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldarg_2);
                        il.Emit(OpCodes.Call, WriteEnumToStringFor(typeBuilder, type));
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);

                        il.Emit(OpCodes.Ldloc, useEnumStringLocal);
                        il.Emit(OpCodes.Brfalse, useEnumLabel2);
                        il.Emit(OpCodes.Ldarg_1);
                        LoadQuotChar(il);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);
                        il.MarkLabel(useEnumLabel2);

                    } else if (type == _floatType) {
                        il.Emit(OpCodes.Ldarg_1);
                        if (isNullable)
                            il.Emit(OpCodes.Ldloc, valueLocal);
                        else
                            il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Call, _generatorFloatToStr);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);
                    } else if (type == _doubleType) {
                        il.Emit(OpCodes.Ldarg_1);
                        if (isNullable)
                            il.Emit(OpCodes.Ldloc, valueLocal);
                        else
                            il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Call, _generatorDoubleToStr);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);
                    } else if (type == _sbyteType) {
                        il.Emit(OpCodes.Ldarg_1);
                        if (isNullable)
                            il.Emit(OpCodes.Ldloc, valueLocal);
                        else
                            il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Call, _generatorSByteToStr);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);
                    } else if (type == _decimalType) {
                        il.Emit(OpCodes.Ldarg_1);
                        if (isNullable)
                            il.Emit(OpCodes.Ldloc, valueLocal);
                        else
                            il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Call, _generatorDecimalToStr);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);
                    } else if (type == _typeType) {
                        il.Emit(OpCodes.Ldarg_1);
                        LoadQuotChar(il);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);

                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Callvirt, _assemblyQualifiedName);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);

                        LoadQuotChar(il);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);
                    } else if (type == _guidType) {

                        il.Emit(OpCodes.Ldarg_1);
                        LoadQuotChar(il);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);

                        il.Emit(OpCodes.Ldarg_1);
                        if (isNullable)
                            il.Emit(OpCodes.Ldloc, valueLocal);
                        else
                            il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Call, _guidToStr);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);

                        il.Emit(OpCodes.Ldarg_1);
                        LoadQuotChar(il);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);
                    } else {
                        if (needQuote) {
                            il.Emit(OpCodes.Ldarg_1);
                            LoadQuotChar(il);
                            il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                            il.Emit(OpCodes.Pop);
                        }
                        il.Emit(OpCodes.Ldarg_1);
                        if (isNullable)
                            il.Emit(OpCodes.Ldloc, valueLocal);
                        else
                            il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Box, type);
                        il.Emit(OpCodes.Callvirt, _objectToString);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);

                        if (needQuote) {
                            il.Emit(OpCodes.Ldarg_1);
                            LoadQuotChar(il);
                            il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                            il.Emit(OpCodes.Pop);
                        }
                    }


                }
            } else {
                WriteSerializeFor(typeBuilder, type, il);
            }
            il.Emit(OpCodes.Ret);
            return method;
        }

        internal static void WriteSerializeFor(TypeBuilder typeBuilder, Type type, ILGenerator methodIL) {
            var conditionLabel = methodIL.DefineLabel();

            if (type.IsValueType) {
                var defaultValue = methodIL.DeclareLocal(type);

                methodIL.Emit(OpCodes.Ldarga, 0);

                methodIL.Emit(OpCodes.Ldloca, defaultValue);
                methodIL.Emit(OpCodes.Initobj, type);
                methodIL.Emit(OpCodes.Ldloc, defaultValue);
                methodIL.Emit(OpCodes.Box, type);
                methodIL.Emit(OpCodes.Constrained, type);

                methodIL.Emit(OpCodes.Callvirt, _objectEquals);

                methodIL.Emit(OpCodes.Brfalse, conditionLabel);
            } else {
                methodIL.Emit(OpCodes.Ldarg_0);
                methodIL.Emit(OpCodes.Brtrue, conditionLabel);
            }
            
            methodIL.Emit(OpCodes.Ldarg_1);
            methodIL.Emit(OpCodes.Ldstr, NullStr);
            methodIL.Emit(OpCodes.Callvirt, _stringBuilderAppend);
            methodIL.Emit(OpCodes.Pop);
            methodIL.Emit(OpCodes.Ret);
            methodIL.MarkLabel(conditionLabel);

            if (type.IsNotPublic) {
                throw new InvalidOperationException("Non-Public Types is not supported yet");
            } else if (type.IsCollectionType()) WriteCollection(typeBuilder, type, methodIL);
            else {
                if (!_includeTypeInformation) {
                    WritePropertiesFor(typeBuilder, type, methodIL);
                } else {
                    var pTypes = GetIncludedTypeTypes(type);
                    if (pTypes.Count == 1) {
                        WritePropertiesFor(typeBuilder, type, methodIL);
                    } else {
                        var typeLocal = methodIL.DeclareLocal(typeof(Type));
                        methodIL.Emit(OpCodes.Ldarg_0);
                        methodIL.Emit(OpCodes.Callvirt, _objectGetType);
                        methodIL.Emit(OpCodes.Stloc, typeLocal);

                        foreach (var pType in pTypes) {
                            var compareLabel = methodIL.DefineLabel();

                            methodIL.Emit(OpCodes.Ldloc, typeLocal);

                            methodIL.Emit(OpCodes.Ldtoken, pType);
                            methodIL.Emit(OpCodes.Call, _typeGetTypeFromHandle);

                            methodIL.Emit(OpCodes.Call, _cTypeOpEquality);

                            methodIL.Emit(OpCodes.Brfalse, compareLabel);

                            WritePropertiesFor(typeBuilder, pType, methodIL, isPoly: true);

                            methodIL.MarkLabel(compareLabel);
                        }
                    }
                }
            }
        }

        private static object GetDictLockObject(params string[] keys) {
            return _dictLockObjects.GetOrAdd(String.Concat(keys), new object());
        }

        private static List<Type> GetIncludedTypeTypes(Type type) {
            var pTypes = _includedTypeTypes.GetOrAdd(type, _ => {
                lock (GetDictLockObject("GetIncludeTypeTypes")) {
                    var attrs = type.GetCustomAttributes(typeof(NetJSONKnownTypeAttribute), true).OfType<NetJSONKnownTypeAttribute>();
                    var types = attrs.Any() ? attrs.Where(x => !x.Type.IsAbstract).Select(x => x.Type).ToList() : null;
                    
                    //Expense call to auto-magically figure all subclass of current type
                    if (types == null) {
                        types = new List<Type>();
                        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                        foreach (var asm in assemblies) {
                            try {
                                types.AddRange(asm.GetTypes().Where(x => x.IsSubclassOf(type)));
                            } catch (ReflectionTypeLoadException ex) {
                                var exTypes = ex.Types != null ? ex.Types.Where(x => x != null && x.IsSubclassOf(type)) : null;
                                if (exTypes != null)
                                    types.AddRange(exTypes);
                            }
                        }
                    }

                    if (!types.Contains(type) && !type.IsAbstract)
                        types.Insert(0, type);

                    return types;
                }
            });
            return pTypes;
        }

        internal static void WriteCollection(TypeBuilder typeBuilder, Type type, ILGenerator il) {

            var isDict = type.IsDictionaryType();

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_S, isDict ? ObjectOpen : ArrayOpen);
            il.Emit(OpCodes.Callvirt, _stringBuilderAppendChar);
            il.Emit(OpCodes.Pop);

            if (isDict)
                WriteDictionary(typeBuilder, type, il);
            else WriteListArray(typeBuilder, type, il);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_S, isDict ? ObjectClose : ArrayClose);
            il.Emit(OpCodes.Callvirt, _stringBuilderAppendChar);
            il.Emit(OpCodes.Pop);
        }

        internal static void WriteDictionary(TypeBuilder typeBuilder, Type type, ILGenerator il) {
            var arguments = type.GetGenericArguments();
            var keyType = arguments != null &&  arguments.Length > 0 ? arguments[0] : null;
            var valueType = arguments != null && arguments.Length > 1 ? arguments[1] : null;


            if (keyType == null || valueType == null) {
                var baseType = type.BaseType;
                if (baseType == _objectType) {
                    baseType = type.GetInterface(IEnumerableStr);
                    if (baseType == null) {
                        throw new InvalidOperationException(String.Format("Type {0} must be a validate dictionary type such as IDictionary<Key,Value>", type.FullName));
                    }
                }
                if (baseType.Name != IEnumerableStr && !baseType.IsDictionaryType())
                    throw new InvalidOperationException(String.Format("Type {0} must be a validate dictionary type such as IDictionary<Key,Value>", type.FullName));
                arguments = baseType.GetGenericArguments();
                keyType = arguments[0];
                valueType = arguments.Length > 1 ? arguments[1] : null;
            }

            if (keyType.Name == KeyValueStr) {
                arguments = keyType.GetGenericArguments();
                keyType = arguments[0];
                valueType = arguments[1];
            }
           
            var isKeyPrimitive = keyType.IsPrimitiveType();
            var isValuePrimitive = valueType.IsPrimitiveType();
            var keyValuePairType = _genericKeyValuePairType.MakeGenericType(keyType, valueType);
            var enumerableType = _ienumerableType.MakeGenericType(keyValuePairType);
            var enumeratorType = _enumeratorType.MakeGenericType(keyValuePairType);//_genericDictionaryEnumerator.MakeGenericType(keyType, valueType);
            var enumeratorLocal = il.DeclareLocal(enumeratorType);
            var entryLocal = il.DeclareLocal(keyValuePairType);
            var startEnumeratorLabel = il.DefineLabel();
            var moveNextLabel = il.DefineLabel();
            var endEnumeratorLabel = il.DefineLabel();
            var hasItem = il.DeclareLocal(_boolType);
            var hasItemLabel = il.DefineLabel();
            

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, hasItem);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Callvirt,
                enumerableType.GetMethod("GetEnumerator"));
            il.Emit(OpCodes.Stloc, enumeratorLocal);
            il.BeginExceptionBlock();
            il.Emit(OpCodes.Br, startEnumeratorLabel);
            il.MarkLabel(moveNextLabel);
            il.Emit(OpCodes.Ldloc, enumeratorLocal);
            il.Emit(OpCodes.Callvirt,
                enumeratorLocal.LocalType.GetProperty("Current")
                .GetGetMethod());
            il.Emit(OpCodes.Stloc, entryLocal);

            il.Emit(OpCodes.Ldloc, hasItem);
            il.Emit(OpCodes.Brfalse, hasItemLabel);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_S, Delimeter);
            il.Emit(OpCodes.Callvirt, _stringBuilderAppendChar);
            il.Emit(OpCodes.Pop);

            il.MarkLabel(hasItemLabel);

            il.Emit(OpCodes.Ldarg_1);

            LoadQuotChar(il);
            il.Emit(OpCodes.Callvirt, _stringBuilderAppend); 

            il.Emit(OpCodes.Ldloca, entryLocal);
            il.Emit(OpCodes.Call, keyValuePairType.GetProperty("Key").GetGetMethod());

            if (keyType == _intType || keyType == _longType) {
                
                il.Emit(OpCodes.Call, keyType == _intType ? _generatorIntToStr : _generatorLongToStr);
                il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
            } else {
                if (keyType.IsValueType)
                    il.Emit(OpCodes.Box, keyType);
                il.Emit(OpCodes.Callvirt, _stringBuilderAppendObject);
            }


            LoadQuotChar(il);
            il.Emit(OpCodes.Callvirt, _stringBuilderAppend);

            il.Emit(OpCodes.Ldstr, Colon);
            il.Emit(OpCodes.Callvirt, _stringBuilderAppend);

            il.Emit(OpCodes.Pop);

            //il.Emit(OpCodes.Ldarg_0);
            if (valueType == _intType || valueType == _longType) {
                il.Emit(OpCodes.Ldarg_1);

                il.Emit(OpCodes.Ldloca, entryLocal);
                il.Emit(OpCodes.Call, keyValuePairType.GetProperty("Value").GetGetMethod());

                il.Emit(OpCodes.Call, valueType == _intType ? _generatorIntToStr : _generatorLongToStr);
                il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                il.Emit(OpCodes.Pop);
            } else {
                il.Emit(OpCodes.Ldloca, entryLocal);
                il.Emit(OpCodes.Call, keyValuePairType.GetProperty("Value").GetGetMethod());

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Call, WriteSerializeMethodFor(typeBuilder, valueType));
            }

            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Stloc, hasItem);

            il.MarkLabel(startEnumeratorLabel);
            il.Emit(OpCodes.Ldloc, enumeratorLocal);
            il.Emit(OpCodes.Callvirt, _enumeratorTypeNonGeneric.GetMethod("MoveNext", MethodBinding));
            il.Emit(OpCodes.Brtrue, moveNextLabel);
            il.Emit(OpCodes.Leave, endEnumeratorLabel);
            il.BeginFinallyBlock();
            il.Emit(OpCodes.Ldloc, enumeratorLocal);
            il.Emit(OpCodes.Callvirt, _iDisposableDispose);
            il.EndExceptionBlock();
            il.MarkLabel(endEnumeratorLabel);
        }

        internal static void WriteICollectionArray(TypeBuilder typeBuilder, Type type, ILGenerator il) {
            var arguments = type.GetGenericArguments();
            var itemType = arguments[0];
            
            
            var isItemPrimitive = itemType.IsPrimitiveType();
            
            var enumerableType = _ienumerableType.MakeGenericType(itemType);
            var enumeratorType = _enumeratorType.MakeGenericType(itemType);
            var enumeratorLocal = il.DeclareLocal(enumeratorType);
            var entryLocal = il.DeclareLocal(itemType);
            var startEnumeratorLabel = il.DefineLabel();
            var moveNextLabel = il.DefineLabel();
            var endEnumeratorLabel = il.DefineLabel();
            var hasItem = il.DeclareLocal(_boolType);
            var hasItemLabel = il.DefineLabel();


            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, hasItem);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Callvirt,
                enumerableType.GetMethod("GetEnumerator"));
            il.Emit(OpCodes.Stloc, enumeratorLocal);
            il.BeginExceptionBlock();
            il.Emit(OpCodes.Br, startEnumeratorLabel);
            il.MarkLabel(moveNextLabel);
            il.Emit(OpCodes.Ldloc, enumeratorLocal);
            il.Emit(OpCodes.Callvirt,
                enumeratorLocal.LocalType.GetProperty("Current")
                .GetGetMethod());
            il.Emit(OpCodes.Stloc, entryLocal);

            il.Emit(OpCodes.Ldloc, hasItem);
            il.Emit(OpCodes.Brfalse, hasItemLabel);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_S, Delimeter);
            il.Emit(OpCodes.Callvirt, _stringBuilderAppendChar);
            il.Emit(OpCodes.Pop);

            il.MarkLabel(hasItemLabel);

            
            //il.Emit(OpCodes.Ldarg_0);
            if (itemType == _intType || itemType == _longType) {
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldloc, entryLocal);
                il.Emit(OpCodes.Call, itemType == _intType ? _generatorIntToStr : _generatorLongToStr);
                il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                il.Emit(OpCodes.Pop);
            } else {
                il.Emit(OpCodes.Ldloc, entryLocal);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Call, WriteSerializeMethodFor(typeBuilder, itemType));
            }

            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Stloc, hasItem);

            il.MarkLabel(startEnumeratorLabel);
            il.Emit(OpCodes.Ldloc, enumeratorLocal);
            il.Emit(OpCodes.Callvirt, _enumeratorTypeNonGeneric.GetMethod("MoveNext", MethodBinding));
            il.Emit(OpCodes.Brtrue, moveNextLabel);
            il.Emit(OpCodes.Leave, endEnumeratorLabel);
            il.BeginFinallyBlock();
            il.Emit(OpCodes.Ldloc, enumeratorLocal);
            il.Emit(OpCodes.Callvirt, _iDisposableDispose);
            il.EndExceptionBlock();
            il.MarkLabel(endEnumeratorLabel);
        }

        internal static void WriteListArray(TypeBuilder typeBuilder, Type type, ILGenerator il) {

            var isArray = type.IsArray;
            var isCollectionList = !isArray && !_listType.IsAssignableFrom(type);

            if (isCollectionList) {
                WriteICollectionArray(typeBuilder, type, il);
                return;
            }

            var itemType = isArray ? type.GetElementType() : type.GetGenericArguments()[0];
            var isPrimitive = itemType.IsPrimitiveType();
            var itemLocal = il.DeclareLocal(itemType);
            var indexLocal = il.DeclareLocal(_intType);
            var startLabel = il.DefineLabel();
            var endLabel = il.DefineLabel();
            var countLocal = il.DeclareLocal(typeof(int));
            var diffLocal = il.DeclareLocal(typeof(int));
            var checkCountLabel = il.DefineLabel();
            var listLocal = isArray ? default(LocalBuilder) : il.DeclareLocal(_listType);

            if (listLocal != null) {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Stloc, listLocal);
            }

            if (isArray) {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldlen);
                il.Emit(OpCodes.Conv_I4);
                il.Emit(OpCodes.Stloc, countLocal);
            } else {
                //il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldloc, listLocal);
                il.Emit(OpCodes.Callvirt, type.GetMethod("get_Count"));
                il.Emit(OpCodes.Stloc, countLocal);
            }

            il.Emit(OpCodes.Ldloc, countLocal);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Sub);
            il.Emit(OpCodes.Stloc, diffLocal);


            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, indexLocal);
            il.Emit(OpCodes.Br, startLabel);
            il.MarkLabel(endLabel);
            if (isArray)
                il.Emit(OpCodes.Ldarg_0);
            else
                il.Emit(OpCodes.Ldloc, listLocal);
            il.Emit(OpCodes.Ldloc, indexLocal);
            if (isArray)
                il.Emit(OpCodes.Ldelem, itemType);
            else
                il.Emit(OpCodes.Callvirt, type.GetMethod("get_Item"));
            il.Emit(OpCodes.Stloc, itemLocal);


            //il.Emit(OpCodes.Ldarg_0);

            if (itemType == _intType || itemType == _longType) {
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldloc, itemLocal);
                il.Emit(OpCodes.Call, itemType == _intType ? _generatorIntToStr : _generatorLongToStr);
                il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                il.Emit(OpCodes.Pop);
            } else {
                il.Emit(OpCodes.Ldloc, itemLocal);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Call, WriteSerializeMethodFor(typeBuilder, itemType));
            }


            il.Emit(OpCodes.Ldloc, indexLocal);
            il.Emit(OpCodes.Ldloc, diffLocal);
            il.Emit(OpCodes.Beq, checkCountLabel);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_S, Delimeter);
            il.Emit(OpCodes.Callvirt, _stringBuilderAppendChar);
            il.Emit(OpCodes.Pop);


            il.MarkLabel(checkCountLabel);


            il.Emit(OpCodes.Ldloc, indexLocal);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, indexLocal);
            il.MarkLabel(startLabel);
            il.Emit(OpCodes.Ldloc, indexLocal);
            il.Emit(OpCodes.Ldloc, countLocal);
            il.Emit(OpCodes.Blt, endLabel);
        }


        internal static void WritePropertiesFor(TypeBuilder typeBuilder, Type type, ILGenerator il, bool isPoly = false) {

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_S, ObjectOpen);
            il.Emit(OpCodes.Callvirt, _stringBuilderAppendChar);
            il.Emit(OpCodes.Pop);

            var hasValue = il.DeclareLocal(_boolType);
            var props = type.GetTypeProperties();
            var count = props.Length - 1;
            var counter = 0;
            var isClass = type.IsClass;

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, hasValue);

            if (isPoly) {
                il.Emit(OpCodes.Ldarg_1);
                LoadQuotChar(il);
                il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                il.Emit(OpCodes.Ldstr, TypeIdentifier);
                il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                LoadQuotChar(il);
                il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                il.Emit(OpCodes.Ldc_I4, ColonChr);
                il.Emit(OpCodes.Callvirt, _stringBuilderAppendChar);
                LoadQuotChar(il);
                il.Emit(OpCodes.Callvirt, _stringBuilderAppend);

                il.Emit(OpCodes.Ldstr, string.Format("{0}, {1}", type.FullName, type.Assembly.GetName().Name));
                il.Emit(OpCodes.Callvirt, _stringBuilderAppend);

                LoadQuotChar(il);
                il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                il.Emit(OpCodes.Pop);
                counter = 1;

                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Stloc, hasValue);
            }

            foreach (var mem in props) {
                var member = mem.Member;
                var name = member.Name;
                var prop = member.MemberType == MemberTypes.Property ? member as PropertyInfo : null;
                var field = member.MemberType == MemberTypes.Field ? member as FieldInfo : null;
                var attr = mem.Attribute;
                var isProp = prop != null;

                if (attr != null)
                    name = attr.Name ?? name;

                var memberType = isProp ? prop.PropertyType : field.FieldType;
                var propType = memberType;
                var originPropType = memberType;
                var isPrimitive = propType.IsPrimitiveType();
                var nullableType = propType.GetNullableType();
                var isNullable = nullableType != null && !originPropType.IsArray;

                propType = isNullable ? nullableType : propType;
                var isValueType = propType.IsValueType;
                var propNullLabel = _skipDefaultValue ? il.DefineLabel() : default(Label);
                var equalityMethod = propType.GetMethod("op_Equality");
                var propValue = il.DeclareLocal(propType);
                var isStruct = isValueType && !isPrimitive;
                var nullablePropValue = isNullable ? il.DeclareLocal(originPropType) : null;

                if (isClass) {
                    il.Emit(OpCodes.Ldarg_0);
                    if (isProp)
                        il.Emit(OpCodes.Callvirt, prop.GetGetMethod());
                    else
                        il.Emit(OpCodes.Ldfld, field);
                } else {
                    il.Emit(OpCodes.Ldarga, 0);
                    if (isProp)
                        il.Emit(OpCodes.Call, prop.GetGetMethod());
                    else il.Emit(OpCodes.Ldfld, field);
                }

                if (isNullable) {
                    il.Emit(OpCodes.Stloc, nullablePropValue);

                    var hasValueMethod = originPropType.GetMethod("get_HasValue");
                    il.Emit(OpCodes.Ldloca, nullablePropValue);
                    il.Emit(OpCodes.Call, hasValueMethod);
                    il.Emit(OpCodes.Brfalse, propNullLabel);

                    il.Emit(OpCodes.Ldloca, nullablePropValue);
                    il.Emit(OpCodes.Call, originPropType.GetMethod("GetValueOrDefault", Type.EmptyTypes));

                    il.Emit(OpCodes.Stloc, propValue);
                } else
                    il.Emit(OpCodes.Stloc, propValue);

                if (_skipDefaultValue) {
                    if (isStruct)
                        il.Emit(OpCodes.Ldloca, propValue);
                    else
                        il.Emit(OpCodes.Ldloc, propValue);
                    if (isValueType && isPrimitive) {
                        LoadDefaultValueByType(il, propType);
                    } else {
                        if (!isValueType)
                            il.Emit(OpCodes.Ldnull);
                    }

                    if (equalityMethod != null) {
                        il.Emit(OpCodes.Call, equalityMethod);
                        il.Emit(OpCodes.Brtrue, propNullLabel);
                    } else {
                        if (isStruct) {

                            var tempValue = il.DeclareLocal(propType);

                            il.Emit(OpCodes.Ldloca, tempValue);
                            il.Emit(OpCodes.Initobj, propType);
                            il.Emit(OpCodes.Ldloc, tempValue);
                            il.Emit(OpCodes.Box, propType);
                            il.Emit(OpCodes.Constrained, propType);

                            il.Emit(OpCodes.Callvirt, _objectEquals);

                            il.Emit(OpCodes.Brtrue, propNullLabel);

                        } else
                            il.Emit(OpCodes.Beq, propNullLabel);
                    }
                }


                if (counter > 0) {

                    var hasValueDelimeterLabel = il.DefineLabel();

                    il.Emit(OpCodes.Ldloc, hasValue);
                    il.Emit(OpCodes.Brfalse, hasValueDelimeterLabel);

                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldc_I4, Delimeter);
                    il.Emit(OpCodes.Callvirt, _stringBuilderAppendChar);
                    il.Emit(OpCodes.Pop);

                    il.MarkLabel(hasValueDelimeterLabel);
                }

                il.Emit(OpCodes.Ldarg_1);
                LoadQuotChar(il);
                il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                il.Emit(OpCodes.Ldstr, name);
                il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                LoadQuotChar(il);
                il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                il.Emit(OpCodes.Ldc_I4, ColonChr);
                il.Emit(OpCodes.Callvirt, _stringBuilderAppendChar);
                il.Emit(OpCodes.Pop);


                if (propType == _intType || propType == _longType) {
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldloc, propValue);

                    il.Emit(OpCodes.Call, propType == _longType ? _generatorLongToStr : _generatorIntToStr);
                    il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                    il.Emit(OpCodes.Pop);
                } else {
                    il.Emit(OpCodes.Ldloc, propValue);

                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Call, WriteSerializeMethodFor(typeBuilder, propType));
                }

                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Stloc, hasValue);

                if (_skipDefaultValue) {
                    il.MarkLabel(propNullLabel);
                }

                counter++;
            }

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_S, ObjectClose);
            il.Emit(OpCodes.Callvirt, _stringBuilderAppendChar);
            il.Emit(OpCodes.Pop);
        }

        private static void LoadDefaultValueByType(ILGenerator il, Type type) {
            if (type == _intType)
                il.Emit(OpCodes.Ldc_I4_0);
            else if (type == _sbyteType || type == _byteType || type == typeof(short) || type == typeof(ushort)) {
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(_byteType == type ? OpCodes.Conv_U1 :
                    _sbyteType == type ? OpCodes.Conv_I1 :
                    typeof(short) == type ? OpCodes.Conv_I2 : OpCodes.Conv_U2);
            } else if (type == typeof(uint))
                il.Emit(OpCodes.Ldc_I4_0);
            else if (type == _charType)
                il.Emit(OpCodes.Ldc_I4_0);
            else if (type == typeof(long))
                il.Emit(OpCodes.Ldc_I8, 0L);
            else if (type == typeof(ulong))
                il.Emit(OpCodes.Ldc_I8, 0L);
            else if (type == typeof(double))
                il.Emit(OpCodes.Ldc_R8, 0d);
            else if (type == typeof(float))
                il.Emit(OpCodes.Ldc_R4, 0f);
            else if (type == _dateTimeType)
                il.Emit(OpCodes.Ldsfld, _dateTimeType.GetField("MinValue"));
            else if (type == _dateTimeOffsetType)
                il.Emit(OpCodes.Ldsfld, _dateTimeOffsetType.GetField("MinValue"));
            else if (type == _timeSpanType)
                il.Emit(OpCodes.Ldsfld, _timeSpanType.GetField("MinValue"));
            else if (type == _boolType)
                il.Emit(OpCodes.Ldc_I4_0);
            else if (type == _guidType)
                il.Emit(OpCodes.Ldsfld, _guidEmptyGuid);
            else if (type == _decimalType) {
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Newobj, _decimalType.GetConstructor(new[] { _intType }));
            } else if (type.IsEnum)
                il.Emit(OpCodes.Ldc_I4_0);
        }

        internal static NetJSONSerializer<T> GetSerializer<T>() {
            return NetJSONCachedSerializer<T>.Serializer;
        }

        public delegate object DeserializeWithTypeDelegate(string value);
        public delegate string SerializeWithTypeDelegate(object value);

        static ConcurrentDictionary<string, DeserializeWithTypeDelegate> _deserializeWithTypes =
            new ConcurrentDictionary<string, DeserializeWithTypeDelegate>();

        static ConcurrentDictionary<Type, SerializeWithTypeDelegate> _serializeWithTypes =
            new ConcurrentDictionary<Type, SerializeWithTypeDelegate>();

        static MethodInfo _getSerializerMethod = _jsonType.GetMethod("GetSerializer", BindingFlags.NonPublic | BindingFlags.Static);
        static Type _netJSONSerializerType = typeof(NetJSONSerializer<>);

        /// <summary>
        /// Serialize value using the specified type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Serialize(Type type, object value) {
            return _serializeWithTypes.GetOrAdd(type, _ => {
                lock (GetDictLockObject("SerializeType", type.Name)) {
                    var name = String.Concat(SerializeStr, type.FullName);
                    var method = new DynamicMethod(name, _stringType, new[] { _objectType }, restrictedSkipVisibility: true);

                    var il = method.GetILGenerator();
                    var genericMethod = _getSerializerMethod.MakeGenericMethod(type);
                    var genericType = _netJSONSerializerType.MakeGenericType(type);

                    var genericSerialize = genericType.GetMethod(SerializeStr, new[] { type });

                    il.Emit(OpCodes.Call, genericMethod);

                    il.Emit(OpCodes.Ldarg_0);
                    if (type.IsClass)
                        il.Emit(OpCodes.Isinst, type);
                    else il.Emit(OpCodes.Unbox_Any, type);

                    il.Emit(OpCodes.Callvirt, genericSerialize);

                    il.Emit(OpCodes.Ret);

                    return method.CreateDelegate(typeof(SerializeWithTypeDelegate)) as SerializeWithTypeDelegate;
                }
            })(value);
        }

        /// <summary>
        /// Serialize value using the underlying type of specified value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Serialize(object value) {
            return Serialize(value.GetType(), value);
        }

        /// <summary>
        /// Deserialize json to specified type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object Deserialize(Type type, string value) {
            return _deserializeWithTypes.GetOrAdd(type.FullName, _ => {
                lock (GetDictLockObject("DeserializeType", type.Name)) {
                    var name = String.Concat(DeserializeStr, type.FullName);
                    var method = new DynamicMethod(name, _objectType, new[] { _stringType }, restrictedSkipVisibility: true);

                    var il = method.GetILGenerator();
                    var genericMethod = _getSerializerMethod.MakeGenericMethod(type);
                    var genericType = _netJSONSerializerType.MakeGenericType(type);

                    var genericDeserialize = genericType.GetMethod(DeserializeStr, new[] { _stringType });

                    il.Emit(OpCodes.Call, genericMethod);

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Callvirt, genericDeserialize);

                    if (type.IsClass)
                        il.Emit(OpCodes.Isinst, type);
                    else {
                        il.Emit(OpCodes.Box, type);
                    }

                    il.Emit(OpCodes.Ret);

                    return method.CreateDelegate(typeof(DeserializeWithTypeDelegate)) as DeserializeWithTypeDelegate;
                }
            })(value);
        }

        /// <summary>
        /// Register serializer primitive method for <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializeFunc"></param>
        public static void RegisterTypeSerializer<T>(Func<T, string> serializeFunc) {
            var type = typeof(T);

            if (serializeFunc == null)
                throw new InvalidOperationException("serializeFunc cannot be null");

            var method = serializeFunc.Method;

            if (!(method.IsPublic && method.IsStatic)) {
                throw new InvalidOperationException("serializeFun must be a public and static method");
            }

            _registeredSerializerMethods[type] = method;
        }

        /// <summary>
        /// Serialize <typeparamref name="T"/> to json
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Serialize<T>(T value) {
           return GetSerializer<T>().Serialize(value);
        }

        /// <summary>
        /// Serialize <typeparamref name="T"/> to specified writer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="writer"></param>
        public static void Serialize<T>(T value, TextWriter writer) {
            GetSerializer<T>().Serialize(value, writer);
        }

        /// <summary>
        /// Deserialize json to <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T Deserialize<T>(string json) {
           return GetSerializer<T>().Deserialize(json);
        }

        /// <summary>
        /// Deserialize reader content to <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static T Deserialize<T>(TextReader reader) {
            return GetSerializer<T>().Deserialize(reader);
        }

        /// <summary>
        /// Serialize specified <typeparamref name="T"/> using settings
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static string Serialize<T>(T value, NetJSONSettings settings) {
            return GetSerializer<T>().Serialize(value, settings);
        }

        /// <summary>
        /// Serialize specified <typeparamref name="T"/> to writer using settings
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="writer"></param>
        /// <param name="settings"></param>
        public static void Serialize<T>(T value, TextWriter writer, NetJSONSettings settings) {
            GetSerializer<T>().Serialize(value, writer, settings);
        }

        /// <summary>
        /// Deserialize json to <typeparamref name="T"/> using specified settings
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static T Deserialize<T>(string json, NetJSONSettings settings) {
            return GetSerializer<T>().Deserialize(json, settings);
        }

        /// <summary>
        /// Deserialize content of reader to <typeparamref name="T"/> using specified settings
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static T Deserialize<T>(TextReader reader, NetJSONSettings settings) {
            return GetSerializer<T>().Deserialize(reader, settings);
        }

        /// <summary>
        /// Deserialize json into Dictionary[string, object]
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static object DeserializeObject(string json) {
            return GetSerializer<object>().Deserialize(json);
        }

        private unsafe static void MoveToNextKey(char* str, ref int index) {
            var current = *(str + index);
            while (current != ':') {
                index++;
                current = *(str + index);
            }
            index++;
        }

        public static Regex //_dateRegex = new Regex(@"\\/Date\((?<ticks>-?\d+)\)\\/", RegexOptions.Compiled),
            _dateISORegex = new Regex(@"(\d){4}-(\d){2}-(\d){2}T(\d){2}:(\d){2}:(\d){2}.(\d){3}Z", RegexOptions.Compiled);

        private static MethodBuilder GenerateExtractObject(TypeBuilder type) {

            MethodBuilder method;
            var key = "ExtractObjectValue";
            if (_readMethodBuilders.TryGetValue(key, out method))
                return method;

            method = type.DefineMethod("ExtractObjectValue", StaticMethodAttribute, _objectType,
                    new[] { _charPtrType, _intType.MakeByRefType(), _settingsType });


            _readMethodBuilders[key] = method;

            var il = method.GetILGenerator();

            var obj = il.DeclareLocal(_objectType);
            var @return = il.DefineLabel();

            ILFixedWhile(il, whileAction: (msil, current, ptr, startLoop, bLabel) => {
                var valueLocal = il.DeclareLocal(_stringType);

                var tokenLabel = il.DefineLabel();
                var quoteLabel = il.DefineLabel();
                var bracketLabel = il.DefineLabel();
                var curlyLabel = il.DefineLabel();
                var dateLabel = il.DefineLabel();

                il.Emit(OpCodes.Ldc_I4, (int)' ');
                il.Emit(OpCodes.Ldloc, current);
                il.Emit(OpCodes.Beq, tokenLabel);

                il.Emit(OpCodes.Ldc_I4, (int)':');
                il.Emit(OpCodes.Ldloc, current);
                il.Emit(OpCodes.Beq, tokenLabel);

                il.Emit(OpCodes.Ldc_I4, (int)',');
                il.Emit(OpCodes.Ldloc, current);
                il.Emit(OpCodes.Beq, tokenLabel);

                il.Emit(OpCodes.Ldc_I4, (int)'\n');
                il.Emit(OpCodes.Ldloc, current);
                il.Emit(OpCodes.Beq, tokenLabel);

                il.Emit(OpCodes.Ldc_I4, (int)'\t');
                il.Emit(OpCodes.Ldloc, current);
                il.Emit(OpCodes.Beq, tokenLabel);

                il.Emit(OpCodes.Ldc_I4, (int)'\r');
                il.Emit(OpCodes.Ldloc, current);
                il.Emit(OpCodes.Beq, tokenLabel);


                //if(current == _ThreadQuoteChar) {
                //il.Emit(OpCodes.Ldc_I4, (int)_ThreadQuoteChar);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldfld, _settingQuoteChar);

                il.Emit(OpCodes.Ldloc, current);
                il.Emit(OpCodes.Bne_Un, quoteLabel);

                //value = GetStringBasedValue(json, ref index)
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Call, _getStringBasedValue);
                il.Emit(OpCodes.Stloc, valueLocal);

                //if(IsDateValue(value)){
                il.Emit(OpCodes.Ldloc, valueLocal);
                il.Emit(OpCodes.Call, _isDateValue);
                il.Emit(OpCodes.Brfalse, dateLabel);

                il.Emit(OpCodes.Ldloc, valueLocal);
                il.Emit(OpCodes.Call, _toExpectedType);
                il.Emit(OpCodes.Stloc, obj);

                il.Emit(OpCodes.Leave, @return);

                il.MarkLabel(dateLabel);
                //}

                il.Emit(OpCodes.Ldloc, valueLocal);
                il.Emit(OpCodes.Stloc, obj);

                il.Emit(OpCodes.Leave, @return);

                il.MarkLabel(quoteLabel);
                //}


                //if(current == '[')
                il.Emit(OpCodes.Ldc_I4, (int)'[');
                il.Emit(OpCodes.Ldloc, current);
                il.Emit(OpCodes.Bne_Un, bracketLabel);

                //CreateList(json, typeof(List<object>), ref index)
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Call, GenerateCreateListFor(type, typeof(List<object>)));

                il.Emit(OpCodes.Stloc, obj);

                il.Emit(OpCodes.Leave, @return);

                il.MarkLabel(bracketLabel);
                //}

                //if(current == '{')
                il.Emit(OpCodes.Ldc_I4, (int)'{');
                il.Emit(OpCodes.Ldloc, current);
                il.Emit(OpCodes.Bne_Un, curlyLabel);

                //GetClassOrDict(json, typeof(Dictionary<string, object>), ref index)
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Call, GenerateGetClassOrDictFor(type, typeof(Dictionary<string, object>)));

                il.Emit(OpCodes.Stloc, obj);

                il.Emit(OpCodes.Leave, @return);

                il.MarkLabel(curlyLabel);
                //}

                //value = GetNonStringValue(json, ref index)
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Call, _getNonStringValue);
                il.Emit(OpCodes.Stloc, valueLocal);

                il.Emit(OpCodes.Ldloc, valueLocal);
                il.Emit(OpCodes.Call, _toExpectedType);

                il.Emit(OpCodes.Stloc, obj);

                il.Emit(OpCodes.Leave, @return);

                il.MarkLabel(tokenLabel);
            },
            returnAction: msil => {
                il.MarkLabel(@return);
                il.Emit(OpCodes.Ldloc, obj);
            });

            return method;
        }

#if NET_35
        public delegate void Action<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);

        public class Tuple<T1> {

            public Tuple(T1 item1) {
                Item1 = item1;
            }

            public T1 Item1 { get; set; }
        }

        public class Tuple<T1, T2> {

            public Tuple(T1 item1, T2 item2) {
                Item1 = item1;
                Item2 = item2;
            }

            public T1 Item1 { get; set;}
            public T2 Item2 { get; set;}
        }

        public class Tuple<T1, T2, T3> {

            public Tuple(T1 item1, T2 item2, T3 item3) {
                Item1 = item1;
                Item2 = item2;
                Item3 = item3;
            }
            
            public T1 Item1 { get; set;}
            public T2 Item2 { get; set;}
            public T3 Item3 { get; set;}
        }

        public class Tuple<T1, T2, T3, T4> {

            public Tuple(T1 item1, T2 item2, T3 item3, T4 item4) {
                Item1 = item1;
                Item2 = item2;
                Item3 = item3;
                Item4 = item4;
            }
            
            public T1 Item1 { get; set;}
            public T2 Item2 { get; set;}
            public T3 Item3 { get; set;}
            public T4 Item4 { get; set;}
        }

        public class Tuple<T1, T2, T3, T4, T5> {

            public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5) {
                Item1 = item1;
                Item2 = item2;
                Item3 = item3;
                Item4 = item4;
                Item5 = item5;
            }
            
            public T1 Item1 { get; set;}
            public T2 Item2 { get; set;}
            public T3 Item3 { get; set;}
            public T4 Item4 { get; set;}
            public T5 Item5 { get; set;}
        }

        public class Tuple<T1, T2, T3, T4, T5, T6> {

            public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6) {
                Item1 = item1;
                Item2 = item2;
                Item3 = item3;
                Item4 = item4;
                Item5 = item5;
                Item6 = item6;
            }
            
            public T1 Item1 { get; set;}
            public T2 Item2 { get; set;}
            public T3 Item3 { get; set;}
            public T4 Item4 { get; set;}
            public T5 Item5 { get; set;}
            public T6 Item6 { get; set;}
        }

        public class Tuple<T1, T2, T3, T4, T5, T6, T7> {

            public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7) {
                Item1 = item1;
                Item2 = item2;
                Item3 = item3;
                Item4 = item4;
                Item5 = item5;
                Item6 = item6;
                Item7 = item7;
            }
            
            public T1 Item1 { get; set;}
            public T2 Item2 { get; set;}
            public T3 Item3 { get; set;}
            public T4 Item4 { get; set;}
            public T5 Item5 { get; set;}
            public T6 Item6 { get; set;}
            public T7 Item7 { get; set;}
        }

        public class Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> {


            public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest) {
                Item1 = item1;
                Item2 = item2;
                Item3 = item3;
                Item4 = item4;
                Item5 = item5;
                Item6 = item6;
                Item7 = item7;
                Rest = rest;
            }
            
            public T1 Item1 { get; set;}
            public T2 Item2 { get; set;}
            public T3 Item3 { get; set;}
            public T4 Item4 { get; set;}
            public T5 Item5 { get; set;}
            public T6 Item6 { get; set;}
            public T7 Item7 { get; set;}
            public TRest Rest { get; set;}
        }
#endif

        private static void ILFixedWhile(ILGenerator il, Action<ILGenerator, LocalBuilder, LocalBuilder, Label, Label> whileAction,
            bool needBreak = false, Action<ILGenerator> returnAction = null,
            Action<ILGenerator, LocalBuilder> beforeAction = null,
            Action<ILGenerator, LocalBuilder> beginIndexIf = null,
            Action<ILGenerator, LocalBuilder> endIndexIf = null) {

            var current = il.DeclareLocal(_charType);
            var ptr = il.DeclareLocal(_charPtrType);
            
            var startLoop = il.DefineLabel();
            var @break = needBreak ? il.DefineLabel() : default(Label);

            //Logic before loop

            //current = '\0';
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, current);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Stloc, ptr);
            
            if (beforeAction != null)
                beforeAction(il, ptr);

            //Begin while loop
            il.MarkLabel(startLoop);

            GenerateUpdateCurrent(il, current, ptr);

            //Logic within loop
            if (whileAction != null)
                whileAction(il, current, ptr, startLoop, @break);


            if (beginIndexIf != null)
                beginIndexIf(il, current);

            IncrementIndexRef(il);

            if (endIndexIf != null)
                endIndexIf(il, current);


            il.Emit(OpCodes.Br, startLoop);

            if (needBreak) {
                il.MarkLabel(@break);
            }

            if (returnAction != null)
                returnAction(il);

            il.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// current = *(ptr + index);
        /// </summary>
        /// <param name="il"></param>
        /// <param name="current"></param>
        /// <param name="ptr"></param>
        private static void GenerateUpdateCurrent(ILGenerator il, LocalBuilder current, LocalBuilder ptr) {
            //current = *(ptr + index);
            il.Emit(OpCodes.Ldloc, ptr);
            il.Emit(OpCodes.Ldarg_1);
            //Extract direct value of ref value
            il.Emit(OpCodes.Ldind_I4);
            il.Emit(OpCodes.Conv_I);
            il.Emit(OpCodes.Ldc_I4_2);
            il.Emit(OpCodes.Mul);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Ldind_U2);
            il.Emit(OpCodes.Stloc, current);
        }

        public static bool IsValueDate(string value) {
            return value.StartsWith("\\/Date") || _dateISORegex.IsMatch(value);
        }

        public static unsafe byte FastStringToByte(string str) {
            unchecked {
                return (byte)FastStringToInt(str);
            }
        }

        public static unsafe short FastStringToShort(string str) {
            unchecked {
                return (short)FastStringToInt(str);
            }
        }

        public static unsafe ushort FastStringToUShort(string str) {
            unchecked {
                return (ushort)FastStringToInt(str);
            }
        }

        public static unsafe int FastStringToInt(string strNum) {
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

        public static unsafe uint FastStringToUInt(string strNum) {
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

        public static unsafe long FastStringToLong(string strNum) {
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

        public static unsafe ulong FastStringToULong(string strNum) {
            ulong val = 0;
            fixed (char* ptr = strNum) {
                char* str = ptr;
                while (*str != '\0') {
                    val = val * 10 + (ulong)(*str++ - '0');
                }
            }
            return val;
        }

        public static unsafe double FastStringToDouble(string numStr) {
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
        
        public static unsafe float FastStringToFloat(string numStr) {
            return (float)FastStringToDouble(numStr);
        }

        public static decimal FastStringToDecimal(string numStr) {
            return new Decimal(FastStringToDouble(numStr));
        }

        [ThreadStatic]
        static StringBuilder _decodeJSONStringBuilder;

        public unsafe static string DecodeJSONString(char* ptr, ref int index, NetJSONSettings settings) {
            char current = '\0', next = '\0';
            bool hasQuote = false;
            //char currentQuote = settings._quoteChar;
            var sb = (_decodeJSONStringBuilder ?? (_decodeJSONStringBuilder = new StringBuilder())).Clear();

            while (true) {
                current = ptr[index];

                if (hasQuote) {
                    //if (current == '\0') break;

                    if (current == settings._quoteChar/*IsCurrentAQuot(current, settings)*/) {
                        ++index;
                        break;
                    } else {
                        if (current != '\\') {
                            sb.Append(current);
                        } else {
                            next = ptr[++index];
                            switch (next) {
                                case 'r': sb.Append('\r'); break;
                                case 'n': sb.Append('\n'); break;
                                case 't': sb.Append('\t'); break;
                                case 'f': sb.Append('\f'); break;
                                case '\\': sb.Append('\\'); break;
                                case '/': sb.Append('/'); break;
                                case 'b': sb.Append('\b'); break;
                                case 'u':
                                    const int offset = 0x10000;
                                    var str = new string(ptr, index + 1, 4);
                                    var uu = Int32.Parse(str, NumberStyles.HexNumber);
                                    var u = uu < offset ? new string((char)uu, 1) :
                                        new string(
                                            new char[]{
                                                (char)(((uu - offset) >> 10) + 0xD800),
                                                (char)((uu - offset) % 0x0400 + 0xDC00)
                                            }
                                        );
                                    sb.Append(u);
                                    index += 4;
                                    break;
                                default:
                                    if (next == settings._quoteChar/*IsCurrentAQuot(next, settings)*/)
                                        sb.Append(next);

                                    break;
                            }

                        }
                    }
                } else {
                    if (current == settings._quoteChar/*IsCurrentAQuot(current, settings)*/) {
                        hasQuote = true;
                    } else if (current == 'n') {
                        index += 3;
                        return null;
                    }
                }

                ++index;
            }

            return sb.ToString();
        }

        private static MethodInfo GenerateExtractValueFor(TypeBuilder typeBuilder, Type type) {
            MethodBuilder method;
            var key = type.FullName;
            var typeName = type.GetName().Fix();
            if (_extractMethodBuilders.TryGetValue(key, out method))
                return method;
            var methodName = String.Concat(ExtractStr, typeName);
            var isObjectType = type == _objectType;
            method = typeBuilder.DefineMethod(methodName, StaticMethodAttribute,
                type, new[] { _charPtrType, _intType.MakeByRefType(), _settingsType });
            _extractMethodBuilders[key] = method;

            var il = method.GetILGenerator();
            var value = il.DeclareLocal(_stringType);

            var settings = il.DeclareLocal(_settingsType);
            var nullableType = type.GetNullableType() ?? type;

            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Stloc, settings);

            var isStringBasedLocal = il.DeclareLocal(_boolType);

            il.Emit(OpCodes.Ldc_I4, type.IsStringBasedType() ? 1 : 0);
            il.Emit(OpCodes.Stloc, isStringBasedLocal);

            if (type.IsEnum) {
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Callvirt, _settingsUseEnumStringProp);
                il.Emit(OpCodes.Stloc, isStringBasedLocal);
            }

            if (nullableType == _dateTimeType || nullableType == _dateTimeOffsetType) {
                var dateCheckLabel = il.DefineLabel();
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Callvirt, _settingsDateFormat);
                il.Emit(OpCodes.Ldc_I4, (int)NetJSONDateFormat.EpochTime);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brtrue, dateCheckLabel);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Stloc, isStringBasedLocal);
                il.MarkLabel(dateCheckLabel);
            }
            
            if (type.IsPrimitiveType()) {

                var isStringBasedLabel1 = il.DefineLabel();
                var isStringBasedLabel2 = il.DefineLabel();

                il.Emit(OpCodes.Ldloc, isStringBasedLocal);
                il.Emit(OpCodes.Brfalse, isStringBasedLabel1);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                if (type == _stringType) {
                    il.Emit(OpCodes.Call, _decodeJSONString);
                } else {
                    il.Emit(OpCodes.Call, _getStringBasedValue);
                }

                il.Emit(OpCodes.Stloc, value);

                il.MarkLabel(isStringBasedLabel1);


                il.Emit(OpCodes.Ldloc, isStringBasedLocal);
                il.Emit(OpCodes.Brtrue, isStringBasedLabel2);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);

                il.Emit(OpCodes.Call, _getNonStringValue);

                il.Emit(OpCodes.Stloc, value);

                il.MarkLabel(isStringBasedLabel2);

                GenerateChangeTypeFor(typeBuilder, type, il, value, settings);
                il.Emit(OpCodes.Ret);
            } else {
                if (isObjectType) {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Call, GenerateExtractObject(typeBuilder));
                } else if (!(type.IsListType() || type.IsArray)) {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Call, GenerateGetClassOrDictFor(typeBuilder, type));
                }
                else {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Call, GenerateCreateListFor(typeBuilder, type));
                }
                il.Emit(OpCodes.Ret);
            } 

            return method;
        }

        public unsafe static bool FastStringToBool(string value) {
            return value[0] == 't';
        }

        public static byte[] FastStringToByteArray(string value) {

#if NET_35
            if (value.IsNullOrWhiteSpace())
                return null;
#else
            if (string.IsNullOrWhiteSpace(value))
                return null;
#endif
            return Convert.FromBase64String(value);
        }

        public static char FastStringToChar(string value) {
            return value[0];
        }

        public static DateTimeOffset FastStringToDateTimeoffset(string value, NetJSONSettings settings) {
            TimeSpan offset;
            var date = StringToDate(value, settings, out offset, isDateTimeOffset: true);
            return new DateTimeOffset(date.Ticks, offset);
        }

        private static char[] _dateNegChars = new[] { '-' },
            _datePosChars = new[] { '+' };
        public static DateTime FastStringToDate(string value, NetJSONSettings settings) {
            TimeSpan offset;
            return StringToDate(value, settings, out offset, isDateTimeOffset: false);
        }

        private static DateTime StringToDate(string value, NetJSONSettings settings, out TimeSpan offset, bool isDateTimeOffset) {
            offset = TimeSpan.Zero;
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

                dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                dt = dt.AddTicks(ticks);

                if (timeZoneFormat == NetJSONTimeZoneFormat.Unspecified || timeZoneFormat == NetJSONTimeZoneFormat.Utc)
                    dt = dt.ToLocalTime();

                var kind = timeZoneFormat == NetJSONTimeZoneFormat.Local ? DateTimeKind.Local :
                    timeZoneFormat == NetJSONTimeZoneFormat.Utc ? DateTimeKind.Utc :
                    DateTimeKind.Unspecified;

                dt = new DateTime(dt.Ticks, kind);

                offsetText = tokens.Length > 1 ? tokens[1] : offsetText;
            } else {
                var dateText = value.Substring(0, 19);
                var diff = value.Length - dateText.Length;
                var hasOffset = diff > 0;
                var utcOffsetText = hasOffset ? value.Substring(dateText.Length, diff) : string.Empty;
                var firstChar = utcOffsetText[0];
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
                    } else {
                        tickMilliseconds = FastStringToInt(offsetText);
                    }
                }
                dt = DateTime.Parse(dateText, CultureInfo.CurrentCulture, DateTimeStyles.AdjustToUniversal);
                if (timeZoneFormat == NetJSONTimeZoneFormat.Local) {
                    if (!isDateTimeOffset)
                        dt = dt.ToUniversalTime();
                    dt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, DateTimeKind.Local);
                } else if (timeZoneFormat == NetJSONTimeZoneFormat.Utc) {
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

        public static Guid FastStringToGuid(string value) {
            //TODO: Optimize
            return new Guid(value);
        }

        public static Type FastStringToType(string value) {
            return Type.GetType(value, false);
        }

        private static void GenerateChangeTypeFor(TypeBuilder typeBuilder, Type type, ILGenerator il, LocalBuilder value, LocalBuilder settings, Type originalType = null) {

            var nullableType = type;
            type = nullableType.GetNullableType();

            var isNullable = type != null;
            if (type == null)
                type = nullableType;

            var local = il.DeclareLocal(originalType ?? nullableType);

            var defaultLabel = default(Label);
            var nullLabelCheck = isNullable ? il.DefineLabel() : defaultLabel;
            var notNullLabel = isNullable ? il.DefineLabel() : defaultLabel;

            //Check for null
            if (isNullable) {
                il.Emit(OpCodes.Ldloc, value);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Call, _stringOpEquality);
                il.Emit(OpCodes.Brfalse, nullLabelCheck);

                il.Emit(OpCodes.Ldloca, local);
                il.Emit(OpCodes.Initobj, _nullableType.MakeGenericType(type));
                
                il.Emit(OpCodes.Br, notNullLabel);

                il.MarkLabel(nullLabelCheck);
            }

           

            il.Emit(OpCodes.Ldloc, value);
            if (type == _intType)
                il.Emit(OpCodes.Call, _fastStringToInt);
            else if (type == typeof(short))
                il.Emit(OpCodes.Call, _fastStringToShort);
            else if (type == typeof(ushort))
                il.Emit(OpCodes.Call, _fastStringToUShort);
            else if (type == typeof(byte))
                il.Emit(OpCodes.Call, _fastStringToByte);
            else if (type == typeof(sbyte)) {
                il.Emit(OpCodes.Call, _fastStringToShort);
                il.Emit(OpCodes.Conv_I1);
            } else if (type == typeof(uint))
                il.Emit(OpCodes.Call, _fastStringToUInt);
            else if (type == _decimalType)
                il.Emit(OpCodes.Call, _fastStringToDecimal);
            else if (type == typeof(long))
                il.Emit(OpCodes.Call, _fastStringToLong);
            else if (type == typeof(ulong))
                il.Emit(OpCodes.Call, _fastStringToULong);
            else if (type == typeof(double))
                il.Emit(OpCodes.Call, _fastStringToDouble);
            else if (type == typeof(float))
                il.Emit(OpCodes.Call, _fastStringToFloat);
            else if (type == _dateTimeType) {
                il.Emit(OpCodes.Ldloc, settings);
                il.Emit(OpCodes.Call, _fastStringToDate);
            } else if (type == _dateTimeOffsetType) {
                il.Emit(OpCodes.Ldloc, settings);
                il.Emit(OpCodes.Call, _fastStringToDateTimeoffset);
            } 
            else if (type == _charType)
                il.Emit(OpCodes.Call, _fastStringToChar);
            else if (type == _timeSpanType)
                il.Emit(OpCodes.Call, _timeSpanParse);
            else if (type == _byteArrayType)
                il.Emit(OpCodes.Call, _fastStringToByteArray);
            else if (type == _boolType)
                il.Emit(OpCodes.Call, _fastStringToBool);
            else if (type == _guidType) {
                il.Emit(OpCodes.Call, _fastStringToGuid);
            } else if (type.IsEnum)
                il.Emit(OpCodes.Call, ReadStringToEnumFor(typeBuilder, type));
            else if (type == _typeType) {
                il.Emit(OpCodes.Call, _fastStringToType);
            }

            if (isNullable) {
                il.Emit(OpCodes.Newobj, _nullableType.MakeGenericType(type).GetConstructor(new[] { type }));
                il.Emit(OpCodes.Stloc, local);

                il.MarkLabel(notNullLabel);

                il.Emit(OpCodes.Ldloc, local);
            }
        }

        private static MethodInfo GenerateSetValueFor(TypeBuilder typeBuilder, Type type) {
            MethodBuilder method;
            var key = type.FullName;
            var typeName = type.GetName().Fix();
            if (_setValueMethodBuilders.TryGetValue(key, out method))
                return method;

            var isTypeValueType = type.IsValueType;
            var methodName = String.Concat(SetStr, typeName);
            var isObjectType = type == _objectType;
            method = typeBuilder.DefineMethod(methodName, StaticMethodAttribute,
                _voidType, new[] { _charPtrType, _intType.MakeByRefType(), isTypeValueType ? type.MakeByRefType() : type, _stringType, _settingsType });
            _setValueMethodBuilders[key] = method;

            const bool Optimized = true;

            var il = method.GetILGenerator();

            if (!_includeTypeInformation)
                GenerateTypeSetValueFor(typeBuilder, type, isTypeValueType, Optimized, il);
            else {
                var pTypes = GetIncludedTypeTypes(type);

                if (pTypes.Count == 1)
                    GenerateTypeSetValueFor(typeBuilder, type, isTypeValueType, Optimized, il);
                else {
                    var typeLocal = il.DeclareLocal(typeof(Type));

                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Callvirt, _objectGetType);
                    il.Emit(OpCodes.Stloc, typeLocal);

                    foreach (var pType in pTypes) {
                        var compareLabel = il.DefineLabel();

                        il.Emit(OpCodes.Ldloc, typeLocal);

                        il.Emit(OpCodes.Ldtoken, pType);
                        il.Emit(OpCodes.Call, _typeGetTypeFromHandle);

                        il.Emit(OpCodes.Call, _cTypeOpEquality);

                        il.Emit(OpCodes.Brfalse, compareLabel);

                        GenerateTypeSetValueFor(typeBuilder, pType, pType.IsValueType, Optimized, il);

                        il.MarkLabel(compareLabel);
                    }
                }
            }

            il.Emit(OpCodes.Ret);

            return method;
        }

        delegate void SetterPropertyDelegate<T>(T instance, object value, MethodInfo methodInfo);

        public static void SetterPropertyValue<T>(T instance, object value, MethodInfo methodInfo) {
            (_setMemberValues.GetOrAdd(methodInfo, key => {
                lock (GetDictLockObject("SetDynamicMemberValue")) {
                    var propType = key.GetParameters()[0].ParameterType;

                    var type = key.DeclaringType;

                    var name = String.Concat(type.Name, "_", key.Name);
                    var meth = new DynamicMethod(name + "_setPropertyValue", _voidType, new[] { type, 
                    _objectType, _methodInfoType }, restrictedSkipVisibility: true);

                    var il = meth.GetILGenerator();

                    il.Emit(OpCodes.Ldarg_0);

                    il.Emit(OpCodes.Ldarg_1);

                    if (propType.IsValueType)
                        il.Emit(OpCodes.Unbox_Any, propType);
                    else
                        il.Emit(OpCodes.Isinst, propType);

                    il.Emit(OpCodes.Callvirt, key);

                    il.Emit(OpCodes.Ret);

                    return meth.CreateDelegate(typeof(SetterPropertyDelegate<T>));
                }
            }) as SetterPropertyDelegate<T>)(instance, value, methodInfo);
        }

        private static void GenerateTypeSetValueFor(TypeBuilder typeBuilder, Type type, bool isTypeValueType, bool Optimized, ILGenerator il) {

            var props = type.GetTypeProperties();
            var caseLocal = il.DeclareLocal(_stringComparison);

            il.Emit(OpCodes.Ldarg, 4);
            il.Emit(OpCodes.Ldfld, _settingsCaseComparison);
            il.Emit(OpCodes.Stloc, caseLocal);

            for (var i = 0; i < props.Length; i++) {
                var mem = props[i];
                var member = mem.Member;
                var prop = member.MemberType == MemberTypes.Property ? member as PropertyInfo : null;
                var field = member.MemberType == MemberTypes.Field ? member as FieldInfo : null;
                var attr = mem.Attribute;
                MethodInfo setter = null;
                var isProp = prop != null;

                var canWrite = isProp ? prop.CanWrite : false;
                var propName = member.Name;
                var conditionLabel = il.DefineLabel();
                var propType = isProp ? prop.PropertyType : field.FieldType;
                var originPropType = propType;
                var nullableType = propType.GetNullableType();
                var isNullable = nullableType != null;
                propType = isNullable ? nullableType : propType;

                if (canWrite) {
                    setter = prop.GetSetMethod();
                    if (setter == null) {
                        setter = type.GetMethod(String.Concat("set_", propName), MethodBinding);
                    }
                }

                var isPublicSetter = canWrite && setter.IsPublic;

                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Ldstr, attr != null ? (attr.Name ?? propName) : propName);

                il.Emit(OpCodes.Ldloc, caseLocal);
                il.Emit(OpCodes.Call, _stringEqualCompare);

                il.Emit(OpCodes.Brfalse, conditionLabel);

                if (!Optimized) {
                    //il.Emit(OpCodes.Ldarg_0);
                    //il.Emit(OpCodes.Ldarg_1);
                    //il.Emit(OpCodes.Ldarg, 4);
                    //il.Emit(OpCodes.Call, GenerateExtractValueFor(typeBuilder, propType));
                    //if (isProp) {
                    //    if (setter != null) {
                    //        if (!isPublicSetter) {
                    //            if (propType.IsValueType)
                    //                il.Emit(OpCodes.Box, propType);
                    //            il.Emit(OpCodes.Ldtoken, setter);
                    //            il.Emit(OpCodes.Call, _methodGetMethodFromHandle);
                    //            il.Emit(OpCodes.Call, _setterPropertyValueMethod.MakeGenericMethod(type));
                    //        } else
                    //            il.Emit(isTypeValueType ? OpCodes.Call : OpCodes.Callvirt, setter);
                    //    } else {
                    //        il.Emit(OpCodes.Pop);
                    //        il.Emit(OpCodes.Pop);
                    //    }
                    //} else il.Emit(OpCodes.Stfld, field);
                } else {
                    var propValue = il.DeclareLocal(originPropType);
                    var isValueType = propType.IsValueType;
                    var isPrimitiveType = propType.IsPrimitiveType();
                    var isStruct = isValueType && !isPrimitiveType;
                    var propNullLabel = !isNullable ? il.DefineLabel() : default(Label);
                    var nullablePropValue = isNullable ? il.DeclareLocal(originPropType) : null;
                    var equalityMethod = propType.GetMethod("op_Equality");


                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldarg, 4);
                    il.Emit(OpCodes.Call, GenerateExtractValueFor(typeBuilder, originPropType));

                    il.Emit(OpCodes.Stloc, propValue);

                    if (!isNullable) {
                        if (isStruct)
                            il.Emit(OpCodes.Ldloca, propValue);
                        else
                            il.Emit(OpCodes.Ldloc, propValue);

                        if (isValueType && isPrimitiveType) {
                            LoadDefaultValueByType(il, propType);
                        } else {
                            if (!isValueType)
                                il.Emit(OpCodes.Ldnull);
                        }

                        if (equalityMethod != null) {
                            il.Emit(OpCodes.Call, equalityMethod);
                            il.Emit(OpCodes.Brtrue, propNullLabel);
                        } else {
                            if (isStruct) {

                                var tempValue = il.DeclareLocal(propType);

                                il.Emit(OpCodes.Ldloca, tempValue);
                                il.Emit(OpCodes.Initobj, propType);
                                il.Emit(OpCodes.Ldloc, tempValue);
                                il.Emit(OpCodes.Box, propType);
                                il.Emit(OpCodes.Constrained, propType);

                                il.Emit(OpCodes.Callvirt, _objectEquals);

                                il.Emit(OpCodes.Brtrue, propNullLabel);

                            } else
                                il.Emit(OpCodes.Beq, propNullLabel);
                        }
                    }

                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Ldloc, propValue);
                    //if (isNullable) {
                    //    il.Emit(OpCodes.Newobj, _nullableType.MakeGenericType(propType).GetConstructor(new[] { propType }));
                    //}

                    if (isProp) {
                        if (setter != null) {
                            if (!setter.IsPublic) {
                                if (propType.IsValueType)
                                    il.Emit(OpCodes.Box, isNullable ? prop.PropertyType : propType);
                                il.Emit(OpCodes.Ldtoken, setter);
                                il.Emit(OpCodes.Call, _methodGetMethodFromHandle);
                                il.Emit(OpCodes.Call, _setterPropertyValueMethod.MakeGenericMethod(type));
                            } else
                                il.Emit(isTypeValueType ? OpCodes.Call : OpCodes.Callvirt, setter);
                        } else {
                            il.Emit(OpCodes.Pop);
                            il.Emit(OpCodes.Pop);
                        }

                    } else il.Emit(OpCodes.Stfld, field);

                    il.Emit(OpCodes.Ret);

                    if (!isNullable)
                        il.MarkLabel(propNullLabel);
                }

                il.Emit(OpCodes.Ret);

                il.MarkLabel(conditionLabel);
            }

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg, 4);
            il.Emit(OpCodes.Call, _skipProperty);
        }

        public unsafe static int MoveToArrayBlock(char* str, ref int index) {
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

        private static MethodInfo GenerateCreateListFor(TypeBuilder typeBuilder, Type type) {
            MethodBuilder method;
            var key = type.FullName;
            var typeName = type.GetName().Fix();
            if (_createListMethodBuilders.TryGetValue(key, out method))
                return method;
            var methodName = String.Concat(CreateListStr, typeName);
            var isObjectType = type == _objectType;
            method = typeBuilder.DefineMethod(methodName, StaticMethodAttribute,
                type, new[] { _charPtrType, _intType.MakeByRefType(), _settingsType });
            _createListMethodBuilders[key] = method;

            var il = method.GetILGenerator();

            var isArray = type.IsArray;
            var elementType = isArray ? type.GetElementType() : type.GetGenericArguments()[0];
            var nullableType = elementType.GetNullableType();
            nullableType = nullableType != null ? nullableType : elementType;

            var isPrimitive = elementType.IsPrimitiveType();
            var isStringType = elementType == _stringType;
            var isByteArray = elementType == _byteArrayType;
            var isStringBased = isStringType || nullableType == _timeSpanType || isByteArray;
            var isCollectionType = !isArray && !_listType.IsAssignableFrom(type);

            var isStringBasedLocal = il.DeclareLocal(_boolType);
            
            var settings = il.DeclareLocal(_settingsType);
            var obj = isCollectionType ? il.DeclareLocal(type) : il.DeclareLocal(typeof(List<>).MakeGenericType(elementType));
            var objArray = isArray ? il.DeclareLocal(elementType.MakeArrayType()) : null;
            var count = il.DeclareLocal(_intType);
            var startIndex = il.DeclareLocal(_intType);
            var endIndex = il.DeclareLocal(_intType);
            var prev = il.DeclareLocal(_charType);
            var addMethod = _genericCollectionType.MakeGenericType(elementType).GetMethod("Add");

            var prevLabel = il.DefineLabel();

            il.Emit(OpCodes.Ldc_I4, isStringBased ? 1 : 0);
            il.Emit(OpCodes.Stloc, isStringBasedLocal);

            if (nullableType.IsEnum) {
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Callvirt, _settingsUseEnumStringProp);
                il.Emit(OpCodes.Stloc, isStringBasedLocal);
            }

            if (nullableType == _dateTimeType || nullableType == _dateTimeOffsetType) {
                var dateCheckLabel = il.DefineLabel();
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Callvirt, _settingsDateFormat);
                il.Emit(OpCodes.Ldc_I4, (int)NetJSONDateFormat.EpochTime);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brtrue, dateCheckLabel);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Stloc, isStringBasedLocal);
                il.MarkLabel(dateCheckLabel);
            }

            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Stloc, settings);

            il.Emit(OpCodes.Newobj, obj.LocalType.GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Stloc, obj);

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, count);

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, startIndex);

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, endIndex);


            il.Emit(OpCodes.Ldc_I4, (int)'\0');
            il.Emit(OpCodes.Stloc, prev);


            ILFixedWhile(il, whileAction: (msil, current, ptr, startLoop, bLabel) => {

                //if prev == ']'
                il.Emit(OpCodes.Ldloc, prev);
                il.Emit(OpCodes.Ldc_I4, (int)']');
                il.Emit(OpCodes.Bne_Un, prevLabel);

                //break
                il.Emit(OpCodes.Br, bLabel);

                il.MarkLabel(prevLabel);

                if (isPrimitive) {
                    var isStringBasedLabel1 = il.DefineLabel();
                    var isStringBasedLabel2 = il.DefineLabel();

                    il.Emit(OpCodes.Ldloc, isStringBasedLocal);
                    il.Emit(OpCodes.Brfalse, isStringBasedLabel1);
                    GenerateCreateListForStringBased(typeBuilder, il, elementType, isStringType, settings, obj, addMethod, current, ptr, bLabel);
                    il.MarkLabel(isStringBasedLabel1);

                    il.Emit(OpCodes.Ldloc, isStringBasedLocal);
                    il.Emit(OpCodes.Brtrue, isStringBasedLabel2);
                    GenerateCreateListForNonStringBased(typeBuilder, il, elementType, settings, obj, addMethod, current);
                    il.MarkLabel(isStringBasedLabel2);
                } else {
                    var currentBlank = il.DefineLabel();
                    var currentBlockEnd = il.DefineLabel();

                    il.Emit(OpCodes.Ldloc, current);
                    il.Emit(OpCodes.Ldc_I4, (int)' ');
                    il.Emit(OpCodes.Beq, currentBlank);

                    il.Emit(OpCodes.Ldloc, current);
                    il.Emit(OpCodes.Ldc_I4, (int)'\n');
                    il.Emit(OpCodes.Beq, currentBlank);

                    il.Emit(OpCodes.Ldloc, current);
                    il.Emit(OpCodes.Ldc_I4, (int)'\r');
                    il.Emit(OpCodes.Beq, currentBlank);

                    il.Emit(OpCodes.Ldloc, current);
                    il.Emit(OpCodes.Ldc_I4, (int)'\t');
                    il.Emit(OpCodes.Beq, currentBlank);

                    il.Emit(OpCodes.Ldloc, current);
                    il.Emit(OpCodes.Ldc_I4, (int)']');
                    il.Emit(OpCodes.Bne_Un, currentBlockEnd);

                    IncrementIndexRef(il);
                    il.Emit(OpCodes.Br, bLabel);

                    il.MarkLabel(currentBlockEnd);

                    il.Emit(OpCodes.Ldloc, obj);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Call, GenerateExtractValueFor(typeBuilder, elementType));
                    il.Emit(OpCodes.Callvirt, addMethod);

                    GenerateUpdateCurrent(il, current, ptr);

                    il.MarkLabel(currentBlank);
                }

                il.Emit(OpCodes.Ldloc, current);
                il.Emit(OpCodes.Stloc, prev);
            }, beforeAction: (msil, ptr) => {
                var isNullArrayLabel = il.DefineLabel();
                
                il.Emit(OpCodes.Ldloc, ptr);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Call, _moveToArrayBlock);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Bne_Un, isNullArrayLabel);

                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ret);

                il.MarkLabel(isNullArrayLabel);
            },
            needBreak: true,
            returnAction: msil => {
                if (isArray) {
                    il.Emit(OpCodes.Ldloc, obj);
                    il.Emit(OpCodes.Callvirt, obj.LocalType.GetMethod("get_Count"));
                    il.Emit(OpCodes.Newarr, elementType);
                    il.Emit(OpCodes.Stloc, objArray);

                    il.Emit(OpCodes.Ldloc, obj);
                    il.Emit(OpCodes.Ldloc, objArray);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Callvirt, obj.LocalType.GetMethod("CopyTo",
                        new[] { objArray.LocalType, _intType }));
                    
                    il.Emit(OpCodes.Ldloc, objArray);
                } else {
                    il.Emit(OpCodes.Ldloc, obj);
                }
            });
            return method;
        }

        private static void GenerateCreateListForNonStringBased(TypeBuilder typeBuilder, ILGenerator il, Type elementType, LocalBuilder settings, LocalBuilder obj, MethodInfo addMethod, LocalBuilder current) {
            var text = il.DeclareLocal(_stringType);

            var blankNewLineLabel = il.DefineLabel();

            il.Emit(OpCodes.Ldloc, current);
            il.Emit(OpCodes.Ldc_I4, (int)' ');
            il.Emit(OpCodes.Beq, blankNewLineLabel);

            il.Emit(OpCodes.Ldloc, current);
            il.Emit(OpCodes.Ldc_I4, (int)',');
            il.Emit(OpCodes.Beq, blankNewLineLabel);

            il.Emit(OpCodes.Ldloc, current);
            il.Emit(OpCodes.Ldc_I4, (int)']');
            il.Emit(OpCodes.Beq, blankNewLineLabel);

            il.Emit(OpCodes.Ldloc, current);
            il.Emit(OpCodes.Ldc_I4, (int)'\n');
            il.Emit(OpCodes.Beq, blankNewLineLabel);

            il.Emit(OpCodes.Ldloc, current);
            il.Emit(OpCodes.Ldc_I4, (int)'\r');
            il.Emit(OpCodes.Beq, blankNewLineLabel);

            il.Emit(OpCodes.Ldloc, current);
            il.Emit(OpCodes.Ldc_I4, (int)'\t');
            il.Emit(OpCodes.Beq, blankNewLineLabel);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, _getNonStringValue);

            il.Emit(OpCodes.Stloc, text);

            il.Emit(OpCodes.Ldloc, obj);
            GenerateChangeTypeFor(typeBuilder, elementType, il, text, settings);
            il.Emit(OpCodes.Callvirt, addMethod);

            il.MarkLabel(blankNewLineLabel);
        }

        private static void GenerateCreateListForStringBased(TypeBuilder typeBuilder, ILGenerator il, Type elementType, bool isStringType, LocalBuilder settings, LocalBuilder obj, MethodInfo addMethod, LocalBuilder current, LocalBuilder ptr, Label bLabel) {
            var text = il.DeclareLocal(_stringType);

            var blankNewLineLabel = il.DefineLabel();

            il.Emit(OpCodes.Ldloc, current);
            il.Emit(OpCodes.Ldc_I4, (int)' ');
            il.Emit(OpCodes.Beq, blankNewLineLabel);

            il.Emit(OpCodes.Ldloc, current);
            il.Emit(OpCodes.Ldc_I4, (int)',');
            il.Emit(OpCodes.Beq, blankNewLineLabel);

            il.Emit(OpCodes.Ldloc, current);
            il.Emit(OpCodes.Ldc_I4, (int)']');
            il.Emit(OpCodes.Beq, blankNewLineLabel);

            il.Emit(OpCodes.Ldloc, current);
            il.Emit(OpCodes.Ldc_I4, (int)'\n');
            il.Emit(OpCodes.Beq, blankNewLineLabel);

            il.Emit(OpCodes.Ldloc, current);
            il.Emit(OpCodes.Ldc_I4, (int)'\r');
            il.Emit(OpCodes.Beq, blankNewLineLabel);

            il.Emit(OpCodes.Ldloc, current);
            il.Emit(OpCodes.Ldc_I4, (int)'\t');
            il.Emit(OpCodes.Beq, blankNewLineLabel);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            if (isStringType) {
                il.Emit(OpCodes.Call, _decodeJSONString);
            } else {
                il.Emit(OpCodes.Call, _getStringBasedValue);
            }
            il.Emit(OpCodes.Stloc, text);

            il.Emit(OpCodes.Ldloc, obj);

            if (!isStringType)
                GenerateChangeTypeFor(typeBuilder, elementType, il, text, settings);
            else
                il.Emit(OpCodes.Ldloc, text);

            il.Emit(OpCodes.Callvirt, addMethod);

            GenerateUpdateCurrent(il, current, ptr);

            var currentLabel = il.DefineLabel();
            il.Emit(OpCodes.Ldloc, current);
            il.Emit(OpCodes.Ldc_I4, (int)']');
            il.Emit(OpCodes.Bne_Un, currentLabel);
            //break
            il.Emit(OpCodes.Br, bLabel);
            il.MarkLabel(currentLabel);

            il.MarkLabel(blankNewLineLabel);
        }

        public unsafe static string CreateString(string str, int startIndex, int length) {
            fixed (char* ptr = str)
                return new string(ptr, startIndex, length);
        }

        public unsafe static bool IsInRange(char* ptr, ref int index, int offset, string key, NetJSONSettings settings) {
            var inRangeChr = *(ptr + index + offset + 2);
            return (*(ptr + index) == settings._quoteChar && (inRangeChr == ':' || inRangeChr == ' ' || inRangeChr == '\t' || inRangeChr == '\n' || inRangeChr == '\r'));
        }

        public static bool IsCurrentAQuot(char current, NetJSONSettings settings) {
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
        
        private static MethodInfo GenerateGetClassOrDictFor(TypeBuilder typeBuilder, Type type) {
            MethodBuilder method;
            var key = type.FullName;
            var typeName = type.GetName().Fix();
            if (_readMethodBuilders.TryGetValue(key, out method))
                return method;
            var methodName = String.Concat(CreateClassOrDictStr, typeName);
            var isObjectType = type == _objectType;
            method = typeBuilder.DefineMethod(methodName, StaticMethodAttribute,
                type, new[] { _charPtrType, _intType.MakeByRefType(), _settingsType });
            _readMethodBuilders[key] = method;

            
            var il = method.GetILGenerator();

            var settings = il.DeclareLocal(_settingsType);
            var foundQuote = il.DeclareLocal(_boolType);
            var dict = il.DeclareLocal(_dictType);
            var prev = il.DeclareLocal(_charType);
            var count = il.DeclareLocal(_intType);
            var startIndex = il.DeclareLocal(_intType);
            var quotes = il.DeclareLocal(_intType);
            var isTag = il.DeclareLocal(_boolType);
            
            var incLabel = il.DefineLabel();
            var openCloseBraceLabel = il.DefineLabel();
            var isTagLabel = il.DefineLabel();

            var countLabel = il.DefineLabel();
            var isNullObjectLabel = il.DefineLabel();


            var isDict = type.IsDictionaryType();
            var arguments = isDict ? type.GetGenericArguments() : null;
            var hasArgument = arguments != null;
            var keyType = hasArgument ? (arguments.Length > 0 ? arguments[0] : null) : _objectType;
            var valueType = hasArgument && arguments.Length > 1 ? arguments[1] : _objectType;
            var isKeyValuePair = false;
            var isExpandoObject = type == _expandoObjectType;
            ConstructorInfo selectedCtor = null;

            if (isDict && keyType == null) {
                var baseType = type.BaseType;
                if (baseType == _objectType) {
                    baseType = type.GetInterface(IEnumerableStr);
                    if (baseType == null)
                        throw new InvalidOperationException(String.Format("Type {0} must be a validate dictionary type such as IDictionary<Key,Value>", type.FullName));
                }
                arguments = baseType.GetGenericArguments();
                keyType = arguments[0];
                valueType = arguments.Length > 1 ? arguments[1] : null;
            }

            if (keyType.Name == KeyValueStr) {
                arguments = keyType.GetGenericArguments();
                keyType = arguments[0];
                valueType = arguments[1];
                isKeyValuePair = true;
            }


            var isTuple = type.IsGenericType && type.Name.StartsWith("Tuple");
            var tupleType = isTuple ? type : null;
            var tupleArguments = tupleType != null ? tupleType.GetGenericArguments() : null;
            var tupleCount = tupleType != null ? tupleArguments.Length : 0;

            if (isTuple) {
                type = _tupleContainerType;
            }

            var obj = il.DeclareLocal(type);
            var isStringType = isTuple || isDict || keyType == _stringType || keyType == _objectType;
            var isTypeValueType = type.IsValueType;
            var tupleCountLocal = isTuple ? il.DeclareLocal(_intType) : null;
            var isStringTypeLocal = il.DeclareLocal(_boolType);

            MethodInfo addMethod = null;

            var isNotTagLabel = il.DefineLabel();

            
            var dictSetItem = isDict ? (isKeyValuePair ? 
                ((addMethod = type.GetMethod("Add")) != null ? addMethod :
                (addMethod = type.GetMethod("Enqueue")) != null ? addMethod :
                (addMethod = type.GetMethod("Push")) != null ? addMethod : null)
                : type.GetMethod("set_Item")) : null;

            if (isExpandoObject) 
                dictSetItem = _idictStringObject.GetMethod("Add");

            if (isDict) {
                if (type.Name == IDictStr) {
                    type = _genericDictType.MakeGenericType(keyType, valueType);
                }
            }

            il.Emit(OpCodes.Ldc_I4, isStringType ? 1 : 0);
            il.Emit(OpCodes.Stloc, isStringTypeLocal);


            if (keyType.IsEnum) {
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Callvirt, _settingsUseEnumStringProp);
                il.Emit(OpCodes.Stloc, isStringTypeLocal);
            }

            if (tupleCountLocal != null) {
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Stloc, tupleCountLocal);
            }

            
            if (isTypeValueType) {
                il.Emit(OpCodes.Ldloca, obj);
                il.Emit(OpCodes.Initobj, type);
            } else {
                if (isTuple) {
                    il.Emit(OpCodes.Ldc_I4, tupleCount);
                    il.Emit(OpCodes.Newobj, type.GetConstructor(new []{ _intType}));
                    il.Emit(OpCodes.Stloc, obj);
                } else {
                    var ctor = type.GetConstructor(Type.EmptyTypes);
                    if (ctor == null) {
                        selectedCtor = type.GetConstructors().OrderBy(x => x.GetParameters().Length).LastOrDefault();
                        il.Emit(OpCodes.Call, _getUninitializedInstance.MakeGenericMethod(type));
                    } else
                        il.Emit(OpCodes.Newobj, ctor);//NewObjNoctor
                    il.Emit(OpCodes.Stloc, obj);
                }
            }

            if (isDict) {
                il.Emit(OpCodes.Ldloc, obj);
                il.Emit(OpCodes.Isinst, _dictType);
                il.Emit(OpCodes.Stloc, dict);
            }

            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Stloc, settings);

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, count);

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, quotes);

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, startIndex);

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, isTag);

            il.Emit(OpCodes.Ldc_I4, (int)'\0');
            il.Emit(OpCodes.Stloc, prev);

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, foundQuote);

            ILFixedWhile(il, whileAction: (msil, current, ptr, startLoop, bLabel) => {

                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Stloc, isTag);

                //if (count == 0 && current == 'n') {
                //    index += 3;
                //    return null;
                //}
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ldloc, count);
                il.Emit(OpCodes.Bne_Un, isNullObjectLabel);

                il.Emit(OpCodes.Ldc_I4, (int)'n');
                il.Emit(OpCodes.Ldloc, current);
                il.Emit(OpCodes.Bne_Un, isNullObjectLabel);

                IncrementIndexRef(il, count: 3);

                if (isTypeValueType) {
                    var nullLocal = il.DeclareLocal(type);

                    il.Emit(OpCodes.Ldloca, nullLocal);
                    il.Emit(OpCodes.Initobj, type);

                    il.Emit(OpCodes.Ldloc, nullLocal);
                } else {
                    il.Emit(OpCodes.Ldnull);
                }

                il.Emit(OpCodes.Ret);

                il.MarkLabel(isNullObjectLabel);


                //current == '{' || current == '}'
                //il.Emit(OpCodes.Ldloc, current);
                //il.Emit(OpCodes.Call, _isCharTag);
                //il.Emit(OpCodes.Brfalse, openCloseBraceLabel);

                
                var currentisCharTagLabel = il.DefineLabel();
                var countCheckLabel = il.DefineLabel();

                //current == '{' || current == '}';

                il.Emit(OpCodes.Ldloc, current);
                il.Emit(OpCodes.Ldc_I4, (int)'{');
                il.Emit(OpCodes.Beq, currentisCharTagLabel);

                il.Emit(OpCodes.Ldloc, current);
                il.Emit(OpCodes.Ldc_I4, (int)'}');
                il.Emit(OpCodes.Bne_Un, openCloseBraceLabel);
                il.MarkLabel(currentisCharTagLabel);
                
                //quotes == 0
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ldloc, quotes);
                il.Emit(OpCodes.Bne_Un, openCloseBraceLabel);

                //isTag = true
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Stloc, isTag);

                il.MarkLabel(openCloseBraceLabel);

                //if(isTag == true)
                il.Emit(OpCodes.Ldloc, isTag);
                il.Emit(OpCodes.Brfalse, isTagLabel);

                //count++
                il.Emit(OpCodes.Ldloc, count);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Stloc, count);

                il.MarkLabel(isTagLabel);


                //if(count > 0 && flag == false && quoteCount == 0 && char == ':')
                //Err, No quotes was found
                il.Emit(OpCodes.Ldloc, count);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ble, countCheckLabel);
                il.Emit(OpCodes.Ldloc, isTag);
                il.Emit(OpCodes.Brtrue, countCheckLabel);
                il.Emit(OpCodes.Ldloc, current);
                il.Emit(OpCodes.Ldc_I4, (int)':');
                il.Emit(OpCodes.Bne_Un, countCheckLabel);
                il.Emit(OpCodes.Ldloc, foundQuote);
                il.Emit(OpCodes.Brtrue, countCheckLabel);

                il.Emit(OpCodes.Newobj, _invalidJSONCtor);
                il.Emit(OpCodes.Throw);

                il.MarkLabel(countCheckLabel);

                //count == 2
                il.Emit(OpCodes.Ldloc, count);
                il.Emit(OpCodes.Ldc_I4_2);
                il.Emit(OpCodes.Bne_Un, countLabel);

                //index += 1;
                IncrementIndexRef(msil);

                il.Emit(OpCodes.Br, bLabel);

                il.MarkLabel(countLabel);


                //!isTag
                il.Emit(OpCodes.Ldloc, isTag);
                il.Emit(OpCodes.Brtrue, isNotTagLabel);

                var isStringTypeLabel1 = il.DefineLabel();

                il.Emit(OpCodes.Ldloc, isStringTypeLocal);
                il.Emit(OpCodes.Brfalse, isStringTypeLabel1);
                GenerateGetClassOrDictStringType(typeBuilder, type, il, settings, foundQuote, prev, startIndex, quotes, isDict, keyType, valueType, isKeyValuePair, isExpandoObject, isTuple, tupleArguments, tupleCount, obj, isTypeValueType, tupleCountLocal, dictSetItem, current, ptr, startLoop);
                il.MarkLabel(isStringTypeLabel1);

                if (dictSetItem != null) {
                    var isStringTypeLabel2 = il.DefineLabel();
                    il.Emit(OpCodes.Ldloc, isStringTypeLocal);
                    il.Emit(OpCodes.Brtrue, isStringTypeLabel2);
                    GenerateGetClassOrDictNonStringType(typeBuilder, il, settings, startIndex, keyType, valueType, isKeyValuePair, isExpandoObject, obj, dictSetItem, current, ptr);
                    il.MarkLabel(isStringTypeLabel2);
                }

                il.MarkLabel(isNotTagLabel);

                il.Emit(OpCodes.Ldloc, current);
                il.Emit(OpCodes.Stloc, prev);

            }, needBreak: true,
            returnAction: msil => {
                if (isTuple) {
                    var toTupleMethod = _tupleContainerType.GetMethods().FirstOrDefault(x => x.Name == ToTupleStr && x.GetGenericArguments().Length == tupleCount);
                    if (toTupleMethod != null) {
                        toTupleMethod = toTupleMethod.MakeGenericMethod(tupleType.GetGenericArguments());
                        il.Emit(OpCodes.Ldloc, obj);
                        il.Emit(OpCodes.Callvirt, toTupleMethod);
                    }
                } else {
                    if(selectedCtor != null) {
                        var sObj = il.DeclareLocal(type);
                        var parameters = selectedCtor.GetParameters();
                        var props = type.GetTypeProperties();
                        var paramProps = props.Where(x => parameters.Any(y => y.Name.Equals(x.Member.Name, StringComparison.OrdinalIgnoreCase)));
                        var excludedParams = props.Where(x => !parameters.Any(y => y.Name.Equals(x.Member.Name, StringComparison.OrdinalIgnoreCase)));

                        if (paramProps.Any()) {
                            foreach (var parameter in paramProps) {
                                il.Emit(OpCodes.Ldloc, obj);
                                GetMemberInfoValue(il, parameter);
                            }

                            il.Emit(OpCodes.Newobj, selectedCtor);
                            il.Emit(OpCodes.Stloc, sObj);

                            //Set field/prop not accounted for in constructor parameters
                            foreach (var param in excludedParams) {
                                il.Emit(OpCodes.Ldloc, sObj);
                                il.Emit(OpCodes.Ldloc, obj);
                                GetMemberInfoValue(il, param);
                                var prop = param.Member.MemberType == MemberTypes.Property ? param.Member as PropertyInfo : null;
                                if (prop != null) {
                                    var setter = prop.GetSetMethod();
                                    if (setter == null) {
                                        setter = type.GetMethod(string.Concat("set_", prop.Name), MethodBinding);
                                    }
                                    var propType = prop.PropertyType;

                                    if (!setter.IsPublic) {
                                        if (propType.IsValueType)
                                            il.Emit(OpCodes.Box, propType);
                                        il.Emit(OpCodes.Ldtoken, setter);
                                        il.Emit(OpCodes.Call, _methodGetMethodFromHandle);
                                        il.Emit(OpCodes.Call, _setterPropertyValueMethod.MakeGenericMethod(type));
                                    } else
                                        il.Emit(isTypeValueType ? OpCodes.Call : OpCodes.Callvirt, setter);
                                } else
                                    il.Emit(OpCodes.Stfld, (FieldInfo)param.Member);
                            }

                            il.Emit(OpCodes.Ldloc, sObj);
                        } else
                            il.Emit(OpCodes.Ldloc, obj);
                    }else
                        il.Emit(OpCodes.Ldloc, obj);
                }
            },
            beginIndexIf: (msil, current) => {
                il.Emit(OpCodes.Ldloc, current);
                il.Emit(OpCodes.Ldc_I4, (int)'}');
                il.Emit(OpCodes.Beq, incLabel);
            },
            endIndexIf: (msil, current) => {
                il.MarkLabel(incLabel);
            });


            return method;
        }

        private static void GetMemberInfoValue(ILGenerator il, NetJSONMemberInfo parameter) {
            var prop = parameter.Member.MemberType == MemberTypes.Property ? parameter.Member as PropertyInfo : null;
            if (prop != null)
                il.Emit(OpCodes.Callvirt, prop.GetGetMethod());
            else il.Emit(OpCodes.Ldfld, (FieldInfo)parameter.Member);
        }

        private static void GenerateGetClassOrDictNonStringType(TypeBuilder typeBuilder, ILGenerator il, LocalBuilder settings, LocalBuilder startIndex, Type keyType, Type valueType, bool isKeyValuePair, bool isExpandoObject, LocalBuilder obj, MethodInfo dictSetItem, LocalBuilder current, LocalBuilder ptr) {
            var isEndOfChar = il.DeclareLocal(_boolType);
            var text = il.DeclareLocal(_stringType);
            var keyLocal = il.DeclareLocal(keyType);
            var startIndexIsEndCharLabel = il.DefineLabel();
            var startIndexGreaterIsEndOfCharLabel = il.DefineLabel();


            var currentEndCharLabel = il.DefineLabel();
            var currentEndCharLabel2 = il.DefineLabel();

            //current == ':' || current == '{' || current == ' ';

            il.Emit(OpCodes.Ldloc, current);
            il.Emit(OpCodes.Ldc_I4, (int)':');
            il.Emit(OpCodes.Beq, currentEndCharLabel);

            il.Emit(OpCodes.Ldloc, current);
            il.Emit(OpCodes.Ldc_I4, (int)',');
            il.Emit(OpCodes.Beq, currentEndCharLabel);

            il.Emit(OpCodes.Ldloc, current);
            il.Emit(OpCodes.Ldc_I4, (int)'\n');
            il.Emit(OpCodes.Beq, currentEndCharLabel);

            il.Emit(OpCodes.Ldloc, current);
            il.Emit(OpCodes.Ldc_I4, (int)'\r');
            il.Emit(OpCodes.Beq, currentEndCharLabel);

            il.Emit(OpCodes.Ldloc, current);
            il.Emit(OpCodes.Ldc_I4, (int)'\t');
            il.Emit(OpCodes.Beq, currentEndCharLabel);

            il.Emit(OpCodes.Ldloc, current);
            il.Emit(OpCodes.Ldc_I4, (int)'{');
            il.Emit(OpCodes.Beq, currentEndCharLabel);

            il.Emit(OpCodes.Ldloc, current);
            il.Emit(OpCodes.Ldc_I4, (int)' ');
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Br, currentEndCharLabel2);

            il.MarkLabel(currentEndCharLabel);
            il.Emit(OpCodes.Ldc_I4_1);
            il.MarkLabel(currentEndCharLabel2);

            il.Emit(OpCodes.Stloc, isEndOfChar);

            il.Emit(OpCodes.Ldloc, startIndex);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Bne_Un, startIndexIsEndCharLabel);
            il.Emit(OpCodes.Ldloc, isEndOfChar);
            il.Emit(OpCodes.Brtrue, startIndexIsEndCharLabel);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldind_I4);
            il.Emit(OpCodes.Stloc, startIndex);

            il.MarkLabel(startIndexIsEndCharLabel);

            il.Emit(OpCodes.Ldloc, startIndex);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ble, startIndexGreaterIsEndOfCharLabel);
            il.Emit(OpCodes.Ldloc, isEndOfChar);
            il.Emit(OpCodes.Brfalse, startIndexGreaterIsEndOfCharLabel);

            il.Emit(OpCodes.Ldloc, ptr);
            il.Emit(OpCodes.Ldloc, startIndex);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldind_I4);
            il.Emit(OpCodes.Ldloc, startIndex);
            il.Emit(OpCodes.Sub);
            il.Emit(OpCodes.Newobj, _strCtorWithPtr);


            il.Emit(OpCodes.Stloc, text);

            GenerateChangeTypeFor(typeBuilder, keyType, il, text, settings);

            il.Emit(OpCodes.Stloc, keyLocal);

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, startIndex);

            IncrementIndexRef(il);

            il.Emit(OpCodes.Ldloc, obj);
            if (isExpandoObject)
                il.Emit(OpCodes.Isinst, _idictStringObject);
            il.Emit(OpCodes.Ldloc, keyLocal);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, GenerateExtractValueFor(typeBuilder, valueType));

            if (isKeyValuePair && !isExpandoObject) {
                il.Emit(OpCodes.Newobj, _genericKeyValuePairType.MakeGenericType(keyType, valueType).GetConstructor(new[] { keyType, valueType }));
                il.Emit(OpCodes.Callvirt, dictSetItem);
            } else {
                il.Emit(OpCodes.Callvirt, dictSetItem);
            }

            GenerateUpdateCurrent(il, current, ptr);


            il.MarkLabel(startIndexGreaterIsEndOfCharLabel);
        }

        private static void GenerateGetClassOrDictStringType(TypeBuilder typeBuilder, Type type, ILGenerator il, LocalBuilder settings, LocalBuilder foundQuote, LocalBuilder prev, LocalBuilder startIndex, LocalBuilder quotes, bool isDict, Type keyType, Type valueType, bool isKeyValuePair, bool isExpandoObject, bool isTuple, Type[] tupleArguments, int tupleCount, LocalBuilder obj, bool isTypeValueType, LocalBuilder tupleCountLocal, MethodInfo dictSetItem, LocalBuilder current, LocalBuilder ptr, Label startLoop) {
            var currentQuoteLabel = il.DefineLabel();
            var currentQuotePrevNotLabel = il.DefineLabel();
            var keyLocal = il.DeclareLocal(_stringType);

            var isCurrentLocal = il.DeclareLocal(_boolType);
            var hasOverrideLabel = il.DefineLabel();
            var hasOverrideLabel2 = il.DefineLabel();
            var notHasOverrideLabel = il.DefineLabel();

            var isStringBasedLocal = il.DeclareLocal(_boolType);

            il.Emit(OpCodes.Ldc_I4, keyType.IsStringBasedType() ? 1 : 0);
            il.Emit(OpCodes.Stloc, isStringBasedLocal);

            if (keyType.IsEnum) {
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Callvirt, _settingsUseEnumStringProp);
                il.Emit(OpCodes.Stloc, isStringBasedLocal);
            }

            if (keyType == _dateTimeType || keyType == _dateTimeOffsetType) {
                var dateCheckLabel = il.DefineLabel();
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Callvirt, _settingsDateFormat);
                il.Emit(OpCodes.Ldc_I4, (int)NetJSONDateFormat.EpochTime);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Brtrue, dateCheckLabel);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Stloc, isStringBasedLocal);
                il.MarkLabel(dateCheckLabel);
            }

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, isCurrentLocal);

            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Callvirt, _settingsHasOverrideQuoteChar);
            il.Emit(OpCodes.Brfalse, hasOverrideLabel);

            il.Emit(OpCodes.Ldloc, current);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldfld, _settingQuoteChar);
            il.Emit(OpCodes.Bne_Un, hasOverrideLabel2);

            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Stloc, isCurrentLocal);

            il.MarkLabel(hasOverrideLabel2);

            il.MarkLabel(hasOverrideLabel);

            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Callvirt, _settingsHasOverrideQuoteChar);
            il.Emit(OpCodes.Brtrue, notHasOverrideLabel);

            il.Emit(OpCodes.Ldloc, current);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, _IsCurrentAQuotMethod);
            il.Emit(OpCodes.Stloc, isCurrentLocal);

            il.MarkLabel(notHasOverrideLabel);

            il.Emit(OpCodes.Ldloc, isCurrentLocal);

            //if(current == _ThreadQuoteChar && quotes == 0)

            //il.Emit(OpCodes.Ldloc, current);
            //il.Emit(OpCodes.Call, _IsCurrentAQuotMethod);


            il.Emit(OpCodes.Brfalse, currentQuoteLabel);

            il.Emit(OpCodes.Ldloc, quotes);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Bne_Un, currentQuoteLabel);

            //foundQuote = true
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Stloc, foundQuote);

            //quotes++
            il.Emit(OpCodes.Ldloc, quotes);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, quotes);

            //startIndex = index + 1;
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldind_I4);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, startIndex);


#region String Skipping Optimization
            var skipOptimizeLabel = il.DefineLabel();
            var skipOptimizeLocal = il.DeclareLocal(_boolType);

            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Callvirt, _settingsUseStringOptimization);
            il.Emit(OpCodes.Brfalse, skipOptimizeLabel);

            if (!isDict) {
                var typeProps = type.GetTypeProperties();

                var nextLabel = il.DefineLabel();

                foreach (var prop in typeProps.OrderBy(x => x.Member.Name.Length)) {
                    var propName = prop.Member.Name;
                    var attr = prop.Attribute;
                    if (attr != null)
                        propName = attr.Name ?? propName;
                    var set = propName.Length;
                    var checkCharByIndexLabel = il.DefineLabel();

                    il.Emit(OpCodes.Ldloc, ptr);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldc_I4, set);
                    il.Emit(OpCodes.Ldstr, propName);
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Call, _isInRange);
                    il.Emit(OpCodes.Brfalse, checkCharByIndexLabel);

                    IncrementIndexRef(il, count: set - 1);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Stloc, foundQuote);

                    il.Emit(OpCodes.Br, nextLabel);

                    il.MarkLabel(checkCharByIndexLabel);

                }

                il.MarkLabel(nextLabel);
            }

            il.MarkLabel(skipOptimizeLabel);
#endregion String Skipping Optimization

            il.Emit(OpCodes.Br, currentQuotePrevNotLabel);
            il.MarkLabel(currentQuoteLabel);
            //else if(current == _ThreadQuoteChar && quotes > 0 && prev != '\\')
            il.Emit(OpCodes.Ldloc, current);
            //il.Emit(OpCodes.Ldc_I4, (int)_ThreadQuoteChar);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldfld, _settingQuoteChar);

            il.Emit(OpCodes.Bne_Un, currentQuotePrevNotLabel);
            il.Emit(OpCodes.Ldloc, quotes);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ble, currentQuotePrevNotLabel);
            il.Emit(OpCodes.Ldloc, prev);
            il.Emit(OpCodes.Ldc_I4, (int)'\\');
            il.Emit(OpCodes.Beq, currentQuotePrevNotLabel);

            //var key = new string(ptr, startIndex, index - startIndex)
            il.Emit(OpCodes.Ldloc, ptr);
            il.Emit(OpCodes.Ldloc, startIndex);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldind_I4);
            il.Emit(OpCodes.Ldloc, startIndex);
            il.Emit(OpCodes.Sub);
            il.Emit(OpCodes.Newobj, _strCtorWithPtr);
            //il.Emit(OpCodes.Call, _createString);
            il.Emit(OpCodes.Stloc, keyLocal);

            //il.EmitWriteLine(String.Format("{0}", type));
            //il.EmitWriteLine(keyLocal);

            //index++
            IncrementIndexRef(il);

            if (isDict) {


                var isStringBasedLabel1 = il.DefineLabel();
                var isStringBasedLabel2 = il.DefineLabel();

                il.Emit(OpCodes.Ldloc, isStringBasedLocal);
                il.Emit(OpCodes.Brfalse, isStringBasedLabel1);

#region true
                il.Emit(OpCodes.Ldloc, obj);
                if (isExpandoObject)
                    il.Emit(OpCodes.Isinst, _idictStringObject);

                GenerateChangeTypeFor(typeBuilder, keyType, il, keyLocal, settings);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Call, GenerateExtractValueFor(typeBuilder, valueType));
                if (isKeyValuePair && !isExpandoObject) {
                    il.Emit(OpCodes.Newobj, _genericKeyValuePairType.MakeGenericType(keyType, valueType).GetConstructor(new[] { keyType, valueType }));
                }
                il.Emit(OpCodes.Callvirt, dictSetItem);
#endregion

                il.MarkLabel(isStringBasedLabel1);


                il.Emit(OpCodes.Ldloc, isStringBasedLocal);
                il.Emit(OpCodes.Brtrue, isStringBasedLabel2);

#region false
                il.Emit(OpCodes.Ldloc, obj);
                if (isExpandoObject)
                    il.Emit(OpCodes.Isinst, _idictStringObject);

                il.Emit(OpCodes.Ldloc, keyLocal);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Call, GenerateExtractValueFor(typeBuilder, valueType));
                if (isKeyValuePair && !isExpandoObject) {
                    il.Emit(OpCodes.Newobj, _genericKeyValuePairType.MakeGenericType(keyType, valueType).GetConstructor(new[] { keyType, valueType }));
                }
                il.Emit(OpCodes.Callvirt, dictSetItem);
#endregion

                il.MarkLabel(isStringBasedLabel2);
            } else {
                if (!isTuple) {
                    //Set property based on key
                    if (_includeTypeInformation) {
                        var typeIdentifierLabel = il.DefineLabel();
                        var notTypeIdentifierLabel = il.DefineLabel();

                        il.Emit(OpCodes.Ldloc, keyLocal);
                        il.Emit(OpCodes.Ldstr, TypeIdentifier);
                        il.Emit(OpCodes.Call, _stringOpEquality);
                        il.Emit(OpCodes.Brfalse, typeIdentifierLabel);

                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Ldarg_2);
                        il.Emit(OpCodes.Call, _getStringBasedValue);
                        il.Emit(OpCodes.Call, _getTypeIdentifierInstanceMethod);
                        il.Emit(OpCodes.Isinst, type);
                        il.Emit(OpCodes.Stloc, obj);

                        il.Emit(OpCodes.Br, notTypeIdentifierLabel);
                        il.MarkLabel(typeIdentifierLabel);

                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(isTypeValueType ? OpCodes.Ldloca : OpCodes.Ldloc, obj);
                        il.Emit(OpCodes.Ldloc, keyLocal);
                        il.Emit(OpCodes.Ldarg_2);
                        il.Emit(OpCodes.Call, GenerateSetValueFor(typeBuilder, type));

                        il.MarkLabel(notTypeIdentifierLabel);
                    } else {
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(isTypeValueType ? OpCodes.Ldloca : OpCodes.Ldloc, obj);
                        il.Emit(OpCodes.Ldloc, keyLocal);
                        il.Emit(OpCodes.Ldarg_2);
                        il.Emit(OpCodes.Call, GenerateSetValueFor(typeBuilder, type));
                    }
                } else {

                    for (var i = 0; i < tupleCount; i++)
                        GenerateTupleConvert(typeBuilder, i, il, tupleArguments, obj, tupleCountLocal, settings);

                    il.Emit(OpCodes.Ldloc, tupleCountLocal);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Add);
                    il.Emit(OpCodes.Stloc, tupleCountLocal);
                }
            }

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, quotes);

            il.Emit(OpCodes.Br, startLoop);

            il.MarkLabel(currentQuotePrevNotLabel);
        }

        private static void GenerateTupleConvert(TypeBuilder typeBuilder, int tupleIndex, ILGenerator il, Type[] tupleArguments, LocalBuilder obj, LocalBuilder tupleCountLocal, LocalBuilder settings) {
            var compareTupleIndexLabel = il.DefineLabel();
            var tupleItemType = tupleArguments[tupleIndex];
            
            il.Emit(OpCodes.Ldloc, tupleCountLocal);
            il.Emit(OpCodes.Ldc_I4, tupleIndex);
            il.Emit(OpCodes.Bne_Un, compareTupleIndexLabel);

            il.Emit(OpCodes.Ldloc, obj);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldloc, settings);
            il.Emit(OpCodes.Call, GenerateExtractValueFor(typeBuilder, tupleItemType));
            if (tupleItemType.IsValueType)
                il.Emit(OpCodes.Box, tupleItemType);

            il.Emit(OpCodes.Callvirt, _tupleContainerAdd);

            il.MarkLabel(compareTupleIndexLabel);
        }

        /// <summary>
        /// index++
        /// </summary>
        /// <param name="il"></param>
        private static void IncrementIndexRef(ILGenerator il, int count = 1) {
            //index += 1;
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldind_I4);
            il.Emit(OpCodes.Ldc_I4, count);
            il.Emit(OpCodes.Add);
            //Store updated index at index address
            il.Emit(OpCodes.Stind_I4);
        }

        public static bool IsEndChar(char current) {
            return current == ':' || current == '{' || current == ' ';
        }

        public static bool IsArrayEndChar(char current) {
            return current == ',' || current == ']' || current == ' ';
        }

        public static bool IsCharTag(char current) {
            return current == '{' || current == '}';
        }


        public unsafe static string GetStringBasedValue(char* ptr, ref int index, NetJSONSettings settings) {
            char current = '\0', prev = '\0';
            int count = 0, startIndex = 0;
            string value = string.Empty;
            var currentQuote = settings._quoteChar;

            while (true) {
                current = ptr[index];
                if (count == 0 && current == settings._quoteChar/*IsCurrentAQuot(current, settings)*/) {
                    startIndex = index + 1;
                    ++count;
                } else if (count > 0 && current == settings._quoteChar/*IsCurrentAQuot(current, settings)*/ && prev != '\\') {
                    value = new string(ptr, startIndex, index - startIndex);
                    ++index;
                    break;
                } else if (count == 0 && current == 'n') {
                    index += 3;
                    return null;
                }

                prev = current;
                ++index;
            }

            return value;
        }

        public unsafe static string GetNonStringValue(char* ptr, ref int index) {
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
                    } else if (current == 't') {
                        index += 4;
                        return "true";
                    } else if (current == 'f') {
                        index += 5;
                        return "false";
                    } else if (current == 'n') {
                        index += 4;
                        return null;
                    }
                }
                ++index;
            }
            if (value == "null")
                return null;
            return value;
        }

        public static bool IsStringBasedType(this Type type) {
            var nullableType = type.GetNullableType() ?? type;
            type = nullableType;
            return type == _stringType || type == _typeType || type == _timeSpanType || type == _byteArrayType || type == _guidType;
        }
    }
}
