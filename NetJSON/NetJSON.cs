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

namespace NetJSON {

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class NetJSONPropertyAttribute : Attribute {
        public string Name { get; private set; }
        public NetJSONPropertyAttribute(string name) {
            Name = name;
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

    public class NetJSONInvalidJSONException : Exception {
        public NetJSONInvalidJSONException()
            : base("Input is not a valid JSON.") {
        }
    }

    public class NetJSONInvalidJSONPropertyException : Exception {
        public NetJSONInvalidJSONPropertyException()
            : base("Class cannot contain any NetJSONProperty with null or blank space character") {
        }
    }

    public class NetJSONInvalidAssemblyGeneration : Exception {
        public NetJSONInvalidAssemblyGeneration(string asmName) : base(String.Format("Could not generate assembly with name [{0}] due to empty list of types to include", asmName)) { }
    }

    public abstract class NetJSONSerializer<T> {

        public abstract string Serialize(T value);
        public abstract T Deserialize(string value);

        public abstract void Serialize(T value, TextWriter writer);
        public abstract T Deserialize(TextReader reader);

    }

    public enum NetJSONDateFormat {
        Default = 0,
        ISO = 2,
        EpochTime = 4,
        JsonNetISO = 6
    }

    public enum NetJSONTimeZoneFormat {
        Unspecified = 0,
        Utc = 2,
        Local = 4
    }

    public enum NetJSONQuote {
        Default = 0,
        Double = Default,
        Single = 2
    }

    public static class NetJSON {

        private static class NetJSONCachedSerializer<T> {
            public static readonly NetJSONSerializer<T> Serializer = (NetJSONSerializer<T>)Activator.CreateInstance(Generate(typeof(T)));
        }
        
        public static string QuotChar {
            get {
                return _ThreadQuoteString;
            }
        }

        

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
            _textWriterType = typeof(TextWriter),
            _tupleContainerType = typeof(TupleContainer),
            _netjsonPropertyType = typeof(NetJSONPropertyAttribute),
            _textReaderType = typeof(TextReader);

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
            _generatorDateToString = _jsonType.GetMethod("DateToString", MethodBinding),
            _generatorSByteToStr = _jsonType.GetMethod("SByteToStr", MethodBinding),
            _generatorDateToEpochTime = _jsonType.GetMethod("DateToEpochTime", MethodBinding),
            _generatorDateToISOFormat = _jsonType.GetMethod("DateToISOFormat", MethodBinding),
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
            _objectEquals = _objectType.GetMethod("Equals", new []{ _objectType}),
            _stringEqualCompare = _stringType.GetMethod("Equals", new []{_stringType, _stringType, typeof(StringComparison)}),
            _stringConcat = _stringType.GetMethod("Concat", new[] { _objectType, _objectType, _objectType, _objectType }),
            _threadQuoteCharGet = _jsonType.GetProperty("_ThreadQuoteChar", MethodBinding).GetGetMethod(),
            _QuotCharGet = _jsonType.GetProperty("QuotChar", MethodBinding).GetGetMethod(),
            _IsCurrentAQuotMethod = _jsonType.GetMethod("IsCurrentAQuot", MethodBinding);

        private static FieldInfo _guidEmptyGuid = _guidType.GetField("Empty"),
            _hasOverrideQuoteField = _jsonType.GetField("_hasOverrideQuoteChar", MethodBinding),
            _threadQuoteStringField = _jsonType.GetField("_threadQuoteString", MethodBinding),
            _threadQuoteCharField = _jsonType.GetField("_threadQuoteChar", MethodBinding);

        const int Delimeter = (int)',',
            ArrayOpen = (int)'[', ArrayClose = (int)']', ObjectOpen = (int)'{', ObjectClose = (int)'}';

        const string IsoFormat = "{0:yyyy-MM-ddTHH:mm:ss.fffZ}",
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
              SerializeStr = "Serialize", DeserializeStr = "Deserialize";

        const char QuotDoubleChar = '"',
                   QuotSingleChar = '\'';

        static ConstructorInfo _strCtorWithPtr = _stringType.GetConstructor(new[] { typeof(char*), _intType, _intType });
        static ConstructorInfo _invalidJSONCtor = _invalidJSONExceptionType.GetConstructor(Type.EmptyTypes);

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

        static ConcurrentDictionary<Type, bool> _primitiveTypes =
            new ConcurrentDictionary<Type, bool>();

        static ConcurrentDictionary<Type, Type> _nullableTypes =
            new ConcurrentDictionary<Type, Type>();


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
                if (key.IsGenericType &&
                    key.GetGenericTypeDefinition() == _nullableType)
                    key = key.GetGenericArguments()[0];

                return key == _stringType ||
                    key.IsPrimitive || key == _dateTimeType ||
                    key == _decimalType || key == _timeSpanType ||
                    key == _guidType || key == _charType ||
                    key == _typeType ||
                    key.IsEnum || key == _byteArrayType;
            });
        }

        private static Type GetNullableType(this Type type) {
            return _nullableTypes.GetOrAdd(type, key => {
                return key.Name.StartsWith("Nullable`") ? key.GetGenericArguments()[0] : null;
            });
        }

        internal static NetJSONMemberInfo[] GetTypeProperties(this Type type) {
            return _typeProperties.GetOrAdd(type, key => {
                var props = key.GetProperties(PropertyBinding).Select(x => new NetJSONMemberInfo{ Member = x, Attribute = x.GetCustomAttributes(_netjsonPropertyType, true).OfType<NetJSONPropertyAttribute>().FirstOrDefault()});
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
            il.Emit(OpCodes.Ldsfld, _threadQuoteStringField);
            //il.Emit(OpCodes.Call, _QuotCharGet);
            //il.Emit(OpCodes.Ldstr, QuotChar);
        }

        [ThreadStatic]
        private static StringBuilder _cachedStringBuilder;
        public static StringBuilder GetStringBuilder() {
            return _cachedStringBuilder ?? (_cachedStringBuilder = new StringBuilder(DefaultStringBuilderCapacity));
        }

        [ThreadStatic]
        public static bool _hasOverrideQuoteChar = false;

        [ThreadStatic]
        public static char _threadQuoteChar = QuotDoubleChar;
        public static char _ThreadQuoteChar {
            get {
                return _threadQuoteChar == '\0' ? (_threadQuoteChar = _quoteType == NetJSONQuote.Single ? QuotSingleChar : QuotDoubleChar) : _threadQuoteChar;
            }
            set{
                _threadQuoteChar = value;
                _threadQuoteString = _threadQuoteChar == '\'' ? "'" : "\"";
            }
        }

        [ThreadStatic]
        public static string _threadQuoteString = "\"";
        public static string _ThreadQuoteString {
            get {
                return _threadQuoteString ?? (_threadQuoteString = (_ThreadQuoteChar = _quoteType == NetJSONQuote.Single ? QuotSingleChar : QuotDoubleChar).ToString());
            }
            set {
                _threadQuoteString = value;
                _threadQuoteChar = _threadQuoteString == "'" ? '\'' : '"';
            }
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

        public static bool ShareAssembly {
            set {
                _useSharedAssembly = value;
            }
        }

        public static NetJSONDateFormat DateFormat {
            set {
                _dateFormat = value;
            }
        }

        public static NetJSONTimeZoneFormat TimeZoneFormat {
            set {
                _timeZoneFormat = value;
            }
        }

        private static NetJSONQuote _quoteType = NetJSONQuote.Default;

        public static NetJSONQuote QuoteType {
            set {
                _quoteType = value;
                _ThreadQuoteString = (_ThreadQuoteChar = value == NetJSONQuote.Single ? QuotSingleChar : QuotDoubleChar).ToString();
            }
        }

        private static bool _caseSensitive = true;

        public static bool CaseSensitive {
            set {
                _caseSensitive = value;
            }
        }

        private static bool _useEnumString = false;

        public static bool UseEnumString {
            set {
                _useEnumString = value;
            }
        }

        private static bool _includeFields = true;

        public static bool IncludeFields {
            set {
                _includeFields = value;
            }
        }

        private static bool _skipDefaultValue = true;

        public static bool SkipDefaultValue {
            set {
                _skipDefaultValue = value;
            }
        }

        private static bool _useStringOptimization = false;

        public static bool UseStringOptimization {
            set {
                _useStringOptimization = value;
            }
        }

        private static bool _generateAssembly = false;
        public static bool GenerateAssembly {
            set {
                _generateAssembly = value;
            }
        }

        [ThreadStatic]
        private static StringBuilder _cachedDateStringBuilder;

        public static string DateToISOFormat(DateTime date) {

            var minute = date.Minute;
            var hour = date.Hour;
            var second = date.Second;
            var millisecond = date.Millisecond;
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
            .Append(millisecond < 10 ? "00" : millisecond < 100 ? "0" : string.Empty).Append(IntToStr(millisecond));

            if (_timeZoneFormat == NetJSONTimeZoneFormat.Utc)
                value.Append('Z');
            else if (_timeZoneFormat == NetJSONTimeZoneFormat.Local) {
                var offset = TimeZone.CurrentTimeZone.GetUtcOffset(date);
                var hours = Math.Abs(offset.Hours);
                var minutes = Math.Abs(offset.Minutes);
                value.Append(offset.Ticks >= 0 ? '+' : '-').Append(hours < 10 ? "0" : string.Empty).Append(IntToStr(hours)).Append(minutes < 10 ? "0" : string.Empty).Append(IntToStr(minutes));
            }

            return value.ToString();
        }

        private static DateTime Epoch = new DateTime(1970, 1, 1),
            UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
        
        public static string DateToString(DateTime date) {
            if (date == DateTime.MinValue)
                return "\\/Date(-62135596800)\\/";
            else if (date == DateTime.MaxValue)
                return "\\/Date(253402300800)\\/";
            var offset = TimeZone.CurrentTimeZone.GetUtcOffset(date);
            var hours = Math.Abs(offset.Hours);
            var minutes = Math.Abs(offset.Minutes);
            var offsetText = _timeZoneFormat == NetJSONTimeZoneFormat.Local ? (string.Concat(offset.Ticks >= 0 ? "+" : "-", hours < 10 ? "0" : string.Empty,
                hours, minutes < 10 ? "0" : string.Empty, minutes)) : string.Empty;
            return String.Concat("\\/Date(", DateToEpochTime(date), offsetText, ")\\/");
        }

        public static string DateToEpochTime(DateTime date) {
            long epochTime = (long)(date.ToUniversalTime() - UnixEpoch).TotalMilliseconds;
            return IntUtility.ltoa(epochTime);
        }

        [ThreadStatic]
        private static StringBuilder _cachedObjectStringBuilder;

        public static StringBuilder CachedObjectStringBuilder() {
            return (_cachedObjectStringBuilder ?? (_cachedObjectStringBuilder = new StringBuilder(25))).Clear();
        }

        public static bool NeedQuotes(Type type) {
            return type == _stringType || type == _charType || type == _guidType || type == _timeSpanType || (type == _dateTimeType && _dateFormat != NetJSONDateFormat.EpochTime) || type == _byteArrayType || (_useEnumString && type.IsEnum);
        }

        public static bool CustomTypeEquality(Type type1, Type type2) {
            if (type1.IsEnum) {
                if(type1.IsEnum && type2 == typeof(Enum))
                    return true;
            }
            return type1 == type2;
        }

        public static string CustomEnumToStr(Enum @enum) {
            if (_useEnumString)
                return @enum.ToString();
            return IntToStr((int)((object)@enum));
        }

        public static string CharToStr(char chr) {
            return chr.ToString();
        }

        private static MethodBuilder GenerateFastObjectToString(TypeBuilder type) {

            return _readMethodBuilders.GetOrAdd("FastObjectToString", _ => {

                var method = type.DefineMethod("FastObjectToString", StaticMethodAttribute, _voidType,
                    new[] { _objectType, _stringBuilderType });

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
                il.Emit(OpCodes.Call, _needQuote);
                il.Emit(OpCodes.Stloc, needQuoteLocal);


                il.Emit(OpCodes.Ldloc, needQuoteLocal);
                il.Emit(OpCodes.Brfalse, needQuoteStartLabel);

                il.Emit(OpCodes.Ldarg_1);
                LoadQuotChar(il);
                il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                il.Emit(OpCodes.Pop);

                il.MarkLabel(needQuoteStartLabel);

                _defaultSerializerTypes[_dateTimeType] = (_dateFormat == NetJSONDateFormat.Default ? _generatorDateToString : _dateFormat == NetJSONDateFormat.ISO || _dateFormat == NetJSONDateFormat.JsonNetISO ? _generatorDateToISOFormat : _generatorDateToEpochTime);

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
                        il.Emit(OpCodes.Call, _generatorEnumToStr);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);
                    }
                    else if (objType == _boolType) {
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
                    } 
                    else {
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Ldarg_0);
                        if (objType.IsValueType)
                            il.Emit(OpCodes.Unbox_Any, objType);
                        else il.Emit(OpCodes.Castclass, objType);
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
            wil.Emit(OpCodes.Call, writeMethod);

            wil.Emit(OpCodes.Ldarg_2);
            wil.Emit(OpCodes.Ldloc, wsbLocal);
            wil.Emit(OpCodes.Callvirt, _stringBuilderToString);
            wil.Emit(OpCodes.Callvirt, _textWriterWrite);
            wil.Emit(OpCodes.Ret);

            var dil = deserializeMethod.GetILGenerator();
            var dilLocal = dil.DeclareLocal(objType);

            dil.Emit(OpCodes.Ldarg_1);
            dil.Emit(OpCodes.Call, readMethod);
            dil.Emit(OpCodes.Stloc, dilLocal);
            dil.Emit(OpCodes.Ldc_I4_0);
            dil.Emit(OpCodes.Stsfld, _hasOverrideQuoteField);
            dil.Emit(OpCodes.Ldloc, dilLocal);
            dil.Emit(OpCodes.Ret);

            var rdil = deserializeWithReaderMethod.GetILGenerator();
            var rdilLocal = rdil.DeclareLocal(objType);


            rdil.Emit(OpCodes.Ldarg_1);
            rdil.Emit(OpCodes.Callvirt, _textReaderReadToEnd);
            rdil.Emit(OpCodes.Call, readMethod);
            rdil.Emit(OpCodes.Stloc, rdilLocal);
            rdil.Emit(OpCodes.Ldc_I4_0);
            rdil.Emit(OpCodes.Stsfld, _hasOverrideQuoteField);
            rdil.Emit(OpCodes.Ldloc, rdilLocal);           
            rdil.Emit(OpCodes.Ret);

            type.DefineMethodOverride(serializeMethod,
                genericType.GetMethod(SerializeStr, new[] { objType }));

            type.DefineMethodOverride(serializeWithTextWriterMethod,
                genericType.GetMethod(SerializeStr, new[] { objType, _textWriterType }));

            type.DefineMethodOverride(deserializeMethod,
                genericType.GetMethod(DeserializeStr, new[] { _stringType }));

            type.DefineMethodOverride(deserializeWithReaderMethod,
                genericType.GetMethod(DeserializeStr, new[] { _textReaderType }));
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

        public static unsafe void SkipProperty(char* ptr, ref int index) {
            var currentIndex = index;
            char current = '\0';
            char bchar = '\0';
            char echar = '\0';
            bool isStringType = false;
            bool isNonStringType = false;
            int counter = 0;
            bool hasChar = false;
            var currentQuote = _threadQuoteChar;

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
                    GetStringBasedValue(ptr, ref index);
                else if (isNonStringType)
                    GetNonStringValue(ptr, ref index);
            }
        }

        public static unsafe void EncodedJSONString(StringBuilder sb, string str) {
            var quote = _threadQuoteChar;
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
                            }
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
                var index = key.IndexOf(CarrotQuoteChar, StringComparison.OrdinalIgnoreCase);
                var quoteText = index > -1 ? key.Substring(index, 2) : CarrotQuoteChar;
                var value = key.Replace(quoteText, string.Empty).Replace(ArrayLiteral, ArrayStr).Replace(AnonymousBracketStr, string.Empty);
                if (value.Contains(CarrotQuoteChar))
                    value = Fix(value);
                return value;
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
                _stringType, new[] { type });
            _writeEnumToStringMethodBuilders[key] = method;

            var eType = type.GetEnumUnderlyingType();

            var il = method.GetILGenerator();

            if (_useEnumString) {
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

            } else {
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

            il.Emit(OpCodes.Ldstr, "0");

            il.Emit(OpCodes.Ret);

            return method;
        }

        internal static MethodInfo WriteDeserializeMethodFor(TypeBuilder typeBuilder, Type type) {
            MethodBuilder method;
            var key = type.FullName;
            var typeName = type.GetName().Fix();
            if (_readDeserializeMethodBuilders.TryGetValue(key, out method))
                return method;
            var methodName = String.Concat(ReadStr, typeName);
            method = typeBuilder.DefineMethod(methodName, StaticMethodAttribute,
                type, new[] { _stringType });
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
                il.Emit(OpCodes.Call, GenerateExtractValueFor(typeBuilder, type));
                il.Emit(OpCodes.Ret);

                il.MarkLabel(notDictOrArrayLabel);


                il.Emit(OpCodes.Ldloc, startsWith);
                il.Emit(OpCodes.Brfalse, startsWithLabel);

                //IsArray
                il.Emit(OpCodes.Ldloc, ptr);
                il.Emit(OpCodes.Ldloca, index);
                il.Emit(OpCodes.Call, GenerateCreateListFor(typeBuilder, typeof(List<object>)));
                il.Emit(OpCodes.Ret);

                il.MarkLabel(startsWithLabel);

                il.Emit(OpCodes.Ldloc, startsWith);
                il.Emit(OpCodes.Brtrue, notStartsWithLabel);



                //IsDictionary
                il.Emit(OpCodes.Ldloc, ptr);
                il.Emit(OpCodes.Ldloca, index);
                il.Emit(OpCodes.Call, GenerateGetClassOrDictFor(typeBuilder, 
                    typeof(Dictionary<string, object>)));
                il.Emit(OpCodes.Ret);

                il.MarkLabel(notStartsWithLabel);

                il.Emit(OpCodes.Ldnull);
            } else {
                var isArray = type.IsListType() || type.IsArray;

                il.Emit(OpCodes.Ldloc, ptr);
                il.Emit(OpCodes.Ldloca, index);
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
                _voidType, new[] { type, _stringBuilderType });
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

                needQuote = needQuote && (type == _stringType || type == _charType || type == _guidType || type == _timeSpanType || (type == _dateTimeType && _dateFormat != NetJSONDateFormat.EpochTime) || type == _byteArrayType);

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
                        il.Emit(OpCodes.Call, GenerateFastObjectToString(typeBuilder));
                        il.Emit(OpCodes.Ldarg_1);
                        //il.Emit(OpCodes.Pop);
                    } else if (type == _stringType) {
                        if (isNullable)
                            il.Emit(OpCodes.Ldloc, valueLocal);
                        else
                            il.Emit(OpCodes.Ldarg_0);
                        //il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
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

                    if (type == _dateTimeType) {

                        var needDateQuote = _dateFormat != NetJSONDateFormat.EpochTime;

                        if (needDateQuote) {
                            il.Emit(OpCodes.Ldarg_1);
                            LoadQuotChar(il);
                            il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                            il.Emit(OpCodes.Pop);
                        }

                        il.Emit(OpCodes.Ldarg_1);
                        //il.Emit(OpCodes.Ldstr, IsoFormat);
                        if (isNullable)
                            il.Emit(OpCodes.Ldloc, valueLocal);
                        else
                            il.Emit(OpCodes.Ldarg_0);
                        //il.Emit(OpCodes.Box, _dateTimeType);
                        //il.Emit(OpCodes.Call, _stringFormat);
                        il.Emit(OpCodes.Call, _dateFormat == NetJSONDateFormat.Default ? _generatorDateToString :
                            _dateFormat == NetJSONDateFormat.ISO || _dateFormat == NetJSONDateFormat.JsonNetISO ? _generatorDateToISOFormat :
                            _generatorDateToEpochTime);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);

                        if (needDateQuote) {
                            il.Emit(OpCodes.Ldarg_1);
                            LoadQuotChar(il);
                            il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                            il.Emit(OpCodes.Pop);
                        }

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

                        if (_useEnumString) {
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
                        il.Emit(OpCodes.Call, WriteEnumToStringFor(typeBuilder, type));
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);

                        if (_useEnumString) {
                            il.Emit(OpCodes.Ldarg_1);
                            LoadQuotChar(il);
                            il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                            il.Emit(OpCodes.Pop);
                        }

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

            if (type.IsNotPublic && type.IsClass) {
                throw new InvalidOperationException("Non-Public Types is not supported yet");
            } 
            else if (type.IsCollectionType()) WriteCollection(typeBuilder, type, methodIL);
            else WritePropertiesFor(typeBuilder, type, methodIL);
            
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
            var needQuote = (itemType == _stringType || itemType == _charType || itemType == _guidType || itemType == _timeSpanType || (itemType == _dateTimeType && _dateFormat != NetJSONDateFormat.EpochTime) || itemType == _byteArrayType || (_useEnumString && itemType.IsEnum));


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


        internal static void WritePropertiesFor(TypeBuilder typeBuilder, Type type, ILGenerator il) {

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
                } else
                    il.Emit(OpCodes.Stloc, propValue);


                if (_skipDefaultValue) {
                    if (isNullable) {
                        var hasValueMethod = originPropType.GetMethod("get_HasValue");
                        il.Emit(OpCodes.Ldloca, nullablePropValue);
                        il.Emit(OpCodes.Call, hasValueMethod);
                        il.Emit(OpCodes.Brfalse, propNullLabel);

                        il.Emit(OpCodes.Ldloca, nullablePropValue);
                        il.Emit(OpCodes.Call, originPropType.GetMethod("GetValueOrDefault", Type.EmptyTypes));

                        il.Emit(OpCodes.Stloc, propValue);
                    } else {
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
                //il.Emit(OpCodes.Ldstr, String.Concat(QuotChar, name, QuotChar, Colon));
                LoadQuotChar(il);
                il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                il.Emit(OpCodes.Ldstr, name);
                il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                LoadQuotChar(il);
                il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                il.Emit(OpCodes.Ldstr, Colon);
                il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
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

        public static string Serialize(Type type, object value) {
            return _serializeWithTypes.GetOrAdd(type, _ => {
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
            })(value);
        }

        public static string Serialize(object value) {
            return Serialize(value.GetType(), value);
        }

        public static object Deserialize(Type type, string value) {

            return _deserializeWithTypes.GetOrAdd(type.FullName, _ => {
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
            
            })(value);
        }

        /// <summary>
        /// Register serializer primitive method
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

        public static string Serialize<T>(T value) {
           return GetSerializer<T>().Serialize(value);
        }

        public static void Serialize<T>(T value, TextWriter writer) {
            GetSerializer<T>().Serialize(value, writer);
        }

        public static T Deserialize<T>(string json) {
           return GetSerializer<T>().Deserialize(json);
        }

        public static T Deserialize<T>(TextReader reader) {
            return GetSerializer<T>().Deserialize(reader);
        }

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
                    new[] { _charPtrType, _intType.MakeByRefType() });


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
                il.Emit(OpCodes.Ldsfld, _threadQuoteCharField);

                il.Emit(OpCodes.Ldloc, current);
                il.Emit(OpCodes.Bne_Un, quoteLabel);

                //value = GetStringBasedValue(json, ref index)
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
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

        public unsafe static string DecodeJSONString(char* ptr, ref int index) {
            char current = '\0', next = '\0';
            bool hasQuote = false;
            char currentQuote = _threadQuoteChar;
            var sb = (_decodeJSONStringBuilder ?? (_decodeJSONStringBuilder = new StringBuilder())).Clear();

            while (true) {
                current = ptr[index];

                if (hasQuote) {
                    //if (current == '\0') break;

                    if (current == currentQuote) {
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
                                    if (currentQuote == next)
                                        sb.Append(currentQuote);

                                    break;
                            }

                        }
                    }
                } else {
                    if (current == currentQuote) {
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
                type, new[] { _charPtrType, _intType.MakeByRefType() });
            _extractMethodBuilders[key] = method;

            var il = method.GetILGenerator();
            var value = il.DeclareLocal(_stringType);
            
            if (type.IsPrimitiveType()) {
                
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                if (type.IsStringBasedType()) {
                    if (type == _stringType) {
                        il.Emit(OpCodes.Call, _decodeJSONString);
                    } else {
                        il.Emit(OpCodes.Call, _getStringBasedValue);
                    }
                } else
                    il.Emit(OpCodes.Call, _getNonStringValue);
                il.Emit(OpCodes.Stloc, value);

                GenerateChangeTypeFor(typeBuilder, type, il, value);
            } else {
                if (isObjectType) {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Call, GenerateExtractObject(typeBuilder));
                } else if (!(type.IsListType() || type.IsArray)) {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Call, GenerateGetClassOrDictFor(typeBuilder, type));
                }
                else {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Call, GenerateCreateListFor(typeBuilder, type));
                }
            }

            il.Emit(OpCodes.Ret);

            return method;
        }

        public static bool FastStringToBool(string value) {
            return value == "1" || String.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
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

        private static char[] _dateNegChars = new[] { '-' },
            _datePosChars = new[] { '+' };
        public static DateTime FastStringToDate(string value) {
            if (_dateFormat == NetJSONDateFormat.EpochTime) {
                var unixTimeStamp = FastStringToLong(value);
                var date = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                return date.AddSeconds(unixTimeStamp).ToLocalTime();
            }

            DateTime dt;
            string[] tokens = null;
            bool negative = false;
            string offsetText = null;
            bool hasZ = false;

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
                dt = dt.AddMilliseconds(ticks);

                if (_timeZoneFormat == NetJSONTimeZoneFormat.Unspecified || _timeZoneFormat == NetJSONTimeZoneFormat.Utc)
                    dt = dt.ToLocalTime();

                var kind = _timeZoneFormat == NetJSONTimeZoneFormat.Local ? DateTimeKind.Local :
                    _timeZoneFormat == NetJSONTimeZoneFormat.Utc ? DateTimeKind.Utc :
                    DateTimeKind.Unspecified;

                dt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, kind);

                offsetText = tokens.Length > 1 ? tokens[1] : offsetText;
            } else {
                var dateText = _timeZoneFormat != NetJSONTimeZoneFormat.Unspecified ? value.Substring(0, value.Length - 5) : value;
                var diff = value.Length - dateText.Length;
                var hasOffset = diff > 0;
                var utcOffsetText = hasOffset ? value.Substring(value.Length - 5, value.Length - (value.Length - 5)) : string.Empty;
                negative = diff > 0 && utcOffsetText[0] == '-';
                if (hasOffset) {
                    hasZ = utcOffsetText.IndexOf('Z') >= 0;
                    offsetText = utcOffsetText.Substring(1, utcOffsetText.Length - 1).Replace(":", string.Empty).Replace("Z", string.Empty);
                }
                dt = DateTime.Parse(dateText, CultureInfo.CurrentCulture, DateTimeStyles.AdjustToUniversal);
                if (_timeZoneFormat == NetJSONTimeZoneFormat.Local) {
                    dt = dt.ToUniversalTime();
                    dt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Millisecond, DateTimeKind.Local);
                } else if (_timeZoneFormat == NetJSONTimeZoneFormat.Utc) {
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
                var hours = hasZ ? 0 : FastStringToInt(offsetText.Substring(0, 2));
                var minutes = hasZ ? 0 : (offsetText.Length > 2 ? FastStringToInt(offsetText.Substring(2, 2)) : 0);
                var millseconds = hasZ ? FastStringToInt(offsetText) : 0d;
                if (negative)
                    hours *= -1;

                dt = dt.AddHours(hours).AddMinutes(minutes).AddMilliseconds(millseconds);
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

        private static void GenerateChangeTypeFor(TypeBuilder typeBuilder, Type type, ILGenerator il, LocalBuilder value, Type originalType = null) {

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
            else if (type == _dateTimeType)
                il.Emit(OpCodes.Call, _fastStringToDate);
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
                _voidType, new[] { _charPtrType, _intType.MakeByRefType(), isTypeValueType ? type.MakeByRefType() : type, _stringType });
            _setValueMethodBuilders[key] = method;

            const bool Optimized = true;

            var props = type.GetTypeProperties();
            var il = method.GetILGenerator();

            for (var i = 0; i < props.Length; i++) {
                var mem = props[i];
                var member = mem.Member;
                var prop = member.MemberType == MemberTypes.Property ? member as PropertyInfo : null;
                var field = member.MemberType == MemberTypes.Field ? member as FieldInfo : null;
                var attr = mem.Attribute;
                var isProp = prop != null;

                if (isProp && !prop.CanWrite) {
                    continue;
                }

                var propName = member.Name;
                var conditionLabel = il.DefineLabel();
                var propType = isProp ? prop.PropertyType : field.FieldType;
                var originPropType = propType;
                var nullableType = propType.GetNullableType();
                var isNullable = nullableType != null;
                propType = isNullable ? nullableType : propType;

                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Ldstr, attr != null ? (attr.Name ?? propName) : propName);
                if (_caseSensitive)
                    il.Emit(OpCodes.Call, _stringOpEquality);
                else {
                    il.Emit(OpCodes.Ldc_I4, (int)StringComparison.OrdinalIgnoreCase);
                    il.Emit(OpCodes.Call, _stringEqualCompare);
                }
                il.Emit(OpCodes.Brfalse, conditionLabel);


                if (!Optimized) {
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Call, GenerateExtractValueFor(typeBuilder, propType));
                    if (isProp) {
                        if (prop.CanWrite)
                            il.Emit(isTypeValueType ? OpCodes.Call : OpCodes.Callvirt, prop.GetSetMethod());
                        else
                            il.Emit(OpCodes.Pop);
                    } else il.Emit(OpCodes.Stfld, field);
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
                        if (prop.CanWrite)
                            il.Emit(isTypeValueType ? OpCodes.Call : OpCodes.Callvirt, prop.GetSetMethod());
                        else
                            il.Emit(OpCodes.Pop);

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
            il.Emit(OpCodes.Call, _skipProperty);

            il.Emit(OpCodes.Ret);

            return method;
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
                type, new[] { _charPtrType, _intType.MakeByRefType() });
            _createListMethodBuilders[key] = method;

            var il = method.GetILGenerator();

            var isArray = type.IsArray;
            var elementType = isArray ? type.GetElementType() : type.GetGenericArguments()[0];
            var nullableType = elementType.GetNullableType();
            nullableType = nullableType != null ? nullableType : elementType;

            var isPrimitive = elementType.IsPrimitiveType();
            var isStringType = elementType == _stringType;
            var isByteArray = elementType == _byteArrayType;
            var isStringBased = isStringType || (nullableType == _dateTimeType && _dateFormat != NetJSONDateFormat.EpochTime) || nullableType == _timeSpanType || isByteArray || (_useEnumString && nullableType.IsEnum);
            var isCollectionType = !isArray && !_listType.IsAssignableFrom(type);


            var obj = isCollectionType ? il.DeclareLocal(type) : il.DeclareLocal(typeof(List<>).MakeGenericType(elementType));
            var objArray = isArray ? il.DeclareLocal(elementType.MakeArrayType()) : null;
            var count = il.DeclareLocal(_intType);
            var startIndex = il.DeclareLocal(_intType);
            var endIndex = il.DeclareLocal(_intType);
            var prev = il.DeclareLocal(_charType);
            var addMethod = _genericCollectionType.MakeGenericType(elementType).GetMethod("Add");

            var prevLabel = il.DefineLabel();
            var ctor = obj.LocalType.GetConstructor(Type.EmptyTypes);

            if (ctor != null)
            {
                il.Emit(OpCodes.Newobj, ctor);
            }

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
                    if (isStringBased) {
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
                        if (isStringType) {
                            il.Emit(OpCodes.Call, _decodeJSONString);
                        } else {
                            il.Emit(OpCodes.Call, _getStringBasedValue);
                        }
                        il.Emit(OpCodes.Stloc, text);

                        il.Emit(OpCodes.Ldloc, obj);

                        if (!isStringType)
                            GenerateChangeTypeFor(typeBuilder, elementType, il, text);
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
                    } else {
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
                        GenerateChangeTypeFor(typeBuilder, elementType, il, text);
                        il.Emit(OpCodes.Callvirt, addMethod);

                        il.MarkLabel(blankNewLineLabel);
                    }
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

        public unsafe static string CreateString(string str, int startIndex, int length) {
            fixed (char* ptr = str)
                return new string(ptr, startIndex, length);
        }

        public unsafe static bool IsInRange(char* ptr, ref int index, int offset, string key) {
            var inRange = false;
            var inRangeChr = *(ptr + index + offset + 2);

            var value = new String(ptr, index + 1, offset);

            inRange = (*(ptr + index) == _threadQuoteChar && (inRangeChr == ':' || inRangeChr == ' ' || inRangeChr == '\t' || inRangeChr == '\n' || inRangeChr == '\r')) && value == key;

            return inRange;
        }

        public static bool IsCurrentAQuot(char current) {
            if (_hasOverrideQuoteChar)
                return current == _threadQuoteChar;
            var quote = _threadQuoteChar;
            var isQuote = current == QuotSingleChar || current == QuotDoubleChar;
            if (isQuote) {
                if (quote != current)
                    _ThreadQuoteChar = current;
                _hasOverrideQuoteChar = true;
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
                type, new[] { _charPtrType, _intType.MakeByRefType() });
            _readMethodBuilders[key] = method;

            
            var il = method.GetILGenerator();

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
            var isStringType = isTuple || isDict || keyType == _stringType || keyType == _objectType || (_useEnumString && keyType.IsEnum);
            var isTypeValueType = type.IsValueType;
            var tupleCountLocal = isTuple ? il.DeclareLocal(_intType) : null;

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
                    il.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
                    il.Emit(OpCodes.Stloc, obj);
                }
            }

            if (isDict) {
                il.Emit(OpCodes.Ldloc, obj);
                il.Emit(OpCodes.Isinst, _dictType);
                il.Emit(OpCodes.Stloc, dict);
            }

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

                if (isStringType) {

                    var currentQuoteLabel = il.DefineLabel();
                    var currentQuotePrevNotLabel = il.DefineLabel();
                    var keyLocal = il.DeclareLocal(_stringType);

                    var isCurrentLocal = il.DeclareLocal(_boolType);
                    var hasOverrideLabel = il.DefineLabel();
                    var hasOverrideLabel2 = il.DefineLabel();
                    var notHasOverrideLabel = il.DefineLabel();

                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Stloc, isCurrentLocal);

                    il.Emit(OpCodes.Ldsfld, _hasOverrideQuoteField);
                    il.Emit(OpCodes.Brfalse, hasOverrideLabel);

                    il.Emit(OpCodes.Ldloc, current);
                    il.Emit(OpCodes.Ldsfld, _threadQuoteCharField);
                    il.Emit(OpCodes.Bne_Un, hasOverrideLabel2);

                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Stloc, isCurrentLocal);

                    il.MarkLabel(hasOverrideLabel2);

                    il.MarkLabel(hasOverrideLabel);

                    il.Emit(OpCodes.Ldsfld, _hasOverrideQuoteField);
                    il.Emit(OpCodes.Brtrue, notHasOverrideLabel);

                    il.Emit(OpCodes.Ldloc, current);
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
                    if (!isDict && _useStringOptimization) {
                        var typeProps = type.GetTypeProperties();

                        var nextLabel = il.DefineLabel();

                        foreach (var prop in typeProps) {
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
                    #endregion String Skipping Optimization
                    
                    il.Emit(OpCodes.Br, currentQuotePrevNotLabel);
                    il.MarkLabel(currentQuoteLabel);
                    //else if(current == _ThreadQuoteChar && quotes > 0 && prev != '\\')
                    il.Emit(OpCodes.Ldloc, current);
                    //il.Emit(OpCodes.Ldc_I4, (int)_ThreadQuoteChar);
                    il.Emit(OpCodes.Ldsfld, _threadQuoteCharField);

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
                        il.Emit(OpCodes.Ldloc, obj);
                        if (isExpandoObject)
                            il.Emit(OpCodes.Isinst, _idictStringObject);

                        if (!keyType.IsStringBasedType())
                            GenerateChangeTypeFor(typeBuilder, keyType, il, keyLocal);
                        else
                            il.Emit(OpCodes.Ldloc, keyLocal);
                        
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Call, GenerateExtractValueFor(typeBuilder, valueType));
                        if (isKeyValuePair && !isExpandoObject) {
                            il.Emit(OpCodes.Newobj, _genericKeyValuePairType.MakeGenericType(keyType, valueType).GetConstructor(new []{keyType, valueType}));
                        } 
                        il.Emit(OpCodes.Callvirt, dictSetItem);
                    } else {
                        if (!isTuple) {
                            //Set property based on key
                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Ldarg_1);
                            il.Emit(isTypeValueType ? OpCodes.Ldloca : OpCodes.Ldloc, obj);
                            il.Emit(OpCodes.Ldloc, keyLocal);
                            il.Emit(OpCodes.Call, GenerateSetValueFor(typeBuilder, type));
                        } else {

                            for (var i = 0; i < tupleCount; i++)
                                GenerateTupleConvert(typeBuilder, i, il, tupleArguments, obj, tupleCountLocal);

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

                } else {

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

                    GenerateChangeTypeFor(typeBuilder, keyType, il, text);
                    
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
                    il.Emit(OpCodes.Call, GenerateExtractValueFor(typeBuilder, valueType));

                    if (isKeyValuePair && !isExpandoObject) {
                        il.Emit(OpCodes.Newobj, _genericKeyValuePairType.MakeGenericType(keyType, valueType).GetConstructor(new []{keyType, valueType}));
                        il.Emit(OpCodes.Callvirt, dictSetItem);
                    } else {
                        il.Emit(OpCodes.Callvirt, dictSetItem);
                    }
                    
                    GenerateUpdateCurrent(il, current, ptr);

                    
                    il.MarkLabel(startIndexGreaterIsEndOfCharLabel);
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
                }
                else                
                    il.Emit(OpCodes.Ldloc, obj);
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

        private static void GenerateTupleConvert(TypeBuilder typeBuilder, int tupleIndex, ILGenerator il, Type[] tupleArguments, LocalBuilder obj, LocalBuilder tupleCountLocal) {
            var compareTupleIndexLabel = il.DefineLabel();
            var tupleItemType = tupleArguments[tupleIndex];
            
            il.Emit(OpCodes.Ldloc, tupleCountLocal);
            il.Emit(OpCodes.Ldc_I4, tupleIndex);
            il.Emit(OpCodes.Bne_Un, compareTupleIndexLabel);

            il.Emit(OpCodes.Ldloc, obj);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
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


        public unsafe static string GetStringBasedValue(char* ptr, ref int index) {
            char current = '\0', prev = '\0';
            int count = 0, startIndex = 0;
            string value = string.Empty;
            var currentQuote = _threadQuoteChar;

            while (true) {
                current = ptr[index];
                if (count == 0 && current == currentQuote) {
                    startIndex = index + 1;
                    ++count;
                } else if (count > 0 && current == currentQuote && prev != '\\') {
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
                    }
                }
                ++index;
            }
            if (value == "null")
                return null;
            return value;
        }

        public static bool IsStringBasedType(this Type type) {
            return type == _stringType || type == _typeType || (type == _dateTimeType && _dateFormat != NetJSONDateFormat.EpochTime) || type == _timeSpanType || type == _byteArrayType || type == _guidType || (_useEnumString && type.IsEnum);
        }
    }
}
