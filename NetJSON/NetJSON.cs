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
using System.Security;

#if !(NET_PCL || NET_STANDARD)
using System.Security.Permissions;
#endif
using System.Text;
using System.Xml.Serialization;


#if NET_STANDARD && !NET_STANDARD_20 && !NET5_0 && !NET6_0_OR_GREATER
using Microsoft.Extensions.DependencyModel;
#endif

using NetJSON.Internals;

namespace NetJSON {

    public unsafe static partial class NetJSON {

        sealed class DynamicNetJSONSerializer<T> : NetJSONSerializer<T>
        {
			readonly Func<TextReader, T> _DeserializeTextReader;
			readonly Func<string, T> _Deserialize;
			readonly Func<TextReader, NetJSONSettings, T> _DeserializeTextReaderWithSettings;
			readonly Func<string, NetJSONSettings, T> _DeserializeWithSettings;
			readonly Func<T, string> _Serialize;
			readonly Func<T, NetJSONSettings, string> _SerializeWithSettings;
			readonly Action<T, TextWriter> _SerializeTextWriter;
			readonly Action<T, TextWriter, NetJSONSettings> _SerializeTextWriterWithSettings;

            private Type _objType;
            public Type ObjType
            {
                get
                {
                    return _objType ?? (_objType = typeof(T));
                }
            }

            public bool IsPrimitive
            {
                get
                {
                    return ObjType.IsPrimitiveType();
                }
            }

            private Module ManifestModule
            {
                get
                {
                    return
#if NET_STANDARD
    ObjType.GetTypeInfo().Assembly.ManifestModule
#else
    Assembly.GetExecutingAssembly().ManifestModule
#endif
                        ;
                }
            }

			public DynamicNetJSONSerializer() {
				_DeserializeTextReader = CreateDeserializerWithTextReader();
				_Deserialize = CreateDeserializer();
				_DeserializeTextReaderWithSettings = CreateDeserializerWithTextReaderSettings();
				_DeserializeWithSettings = CreateDeserializerWithSettings();
				_Serialize = CreateSerializer();
				_SerializeTextWriter = CreateSerializerWithTextWriter();
				_SerializeTextWriterWithSettings = CreateSerializerWithTextWriterSettings();
				_SerializeWithSettings = CreateSerializerWithSettings();
			}
			Func<TextReader, T> CreateDeserializerWithTextReader() {
				var meth = new DynamicMethod("DeserializeValueTextReader", ObjType, new[] { _textReaderType }, ManifestModule, true);

				var rdil = meth.GetILGenerator();

				var readMethod = WriteDeserializeMethodFor(null, ObjType);

				rdil.Emit(OpCodes.Ldarg_0);
				rdil.Emit(OpCodes.Callvirt, _textReaderReadToEnd);
				rdil.Emit(OpCodes.Call, _settingsCurrentSettings);
				rdil.Emit(OpCodes.Call, readMethod);
				rdil.Emit(OpCodes.Ret);

				return meth.CreateDelegate(typeof(Func<TextReader, T>)) as Func<TextReader, T>;
			}
			Func<string, T> CreateDeserializer() {
                var meth = new DynamicMethod("DeserializeValue", ObjType, new[] { _stringType }, ManifestModule, true);

                var dil = meth.GetILGenerator();

                var readMethod = WriteDeserializeMethodFor(null, ObjType);

                dil.Emit(OpCodes.Ldarg_0);
                dil.Emit(OpCodes.Call, _settingsCurrentSettings);
                dil.Emit(OpCodes.Call, readMethod);
                dil.Emit(OpCodes.Ret);

                return meth.CreateDelegate(typeof(Func<string, T>)) as Func<string, T>;
			}
			Func<TextReader, NetJSONSettings, T> CreateDeserializerWithTextReaderSettings() {
                var meth = new DynamicMethod("DeserializeValueTextReaderSettings", ObjType, new[] { _textReaderType, _settingsType }, ManifestModule, true);

                var rdilWithSettings = meth.GetILGenerator();

                var readMethod = WriteDeserializeMethodFor(null, ObjType);

                rdilWithSettings.Emit(OpCodes.Ldarg_1);
                rdilWithSettings.Emit(OpCodes.Callvirt, _textReaderReadToEnd);
                rdilWithSettings.Emit(OpCodes.Ldarg_2);
                rdilWithSettings.Emit(OpCodes.Call, readMethod);
                rdilWithSettings.Emit(OpCodes.Ret);

                return meth.CreateDelegate(typeof(Func<TextReader, NetJSONSettings, T>)) as Func<TextReader, NetJSONSettings, T>;
			}
			Func<string, NetJSONSettings, T> CreateDeserializerWithSettings() {
                var meth = new DynamicMethod("DeserializeValueSettings", ObjType, new[] { _stringType, _settingsType }, ManifestModule, true);

                var dilWithSettings = meth.GetILGenerator();

                var readMethod = WriteDeserializeMethodFor(null, ObjType);

                dilWithSettings.Emit(OpCodes.Ldarg_0);
                dilWithSettings.Emit(OpCodes.Ldarg_1);
                dilWithSettings.Emit(OpCodes.Call, readMethod);
                dilWithSettings.Emit(OpCodes.Ret);

                return meth.CreateDelegate(typeof(Func<string, NetJSONSettings, T>)) as Func<string, NetJSONSettings, T>;

			}
			Func<T, string> CreateSerializer() {
                var meth = new DynamicMethod("SerializeValue", _stringType, new[] { ObjType }, ManifestModule, true);

                var il = meth.GetILGenerator();

                var writeMethod = WriteSerializeMethodFor(null, ObjType, needQuote: !IsPrimitive || ObjType == _stringType);

                var sbLocal = il.DeclareLocal(_stringBuilderType);
                il.Emit(OpCodes.Call, _generatorGetStringBuilder);

                il.EmitClearStringBuilder();

                il.Emit(OpCodes.Stloc, sbLocal);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldloc, sbLocal);
                il.Emit(OpCodes.Call, _settingsCurrentSettings);
                il.Emit(OpCodes.Call, writeMethod);

                il.Emit(OpCodes.Ldloc, sbLocal);
                il.Emit(OpCodes.Callvirt, _stringBuilderToString);
                il.Emit(OpCodes.Ret);

                return meth.CreateDelegate(typeof(Func<T, string>)) as Func<T, string>;

			}
			Func<T, NetJSONSettings, string> CreateSerializerWithSettings() {
                var meth = new DynamicMethod("SerializeValueSettings", _stringType, new[] { ObjType, _settingsType }, ManifestModule, true);


				var ilWithSettings = meth.GetILGenerator();

                var writeMethod = WriteSerializeMethodFor(null, ObjType, needQuote: !IsPrimitive || ObjType == _stringType);

                var isValueType = ObjType.GetTypeInfo().IsValueType;
                    
                var sbLocalWithSettings = ilWithSettings.DeclareLocal(_stringBuilderType);
                ilWithSettings.Emit(OpCodes.Call, _generatorGetStringBuilder);

                ilWithSettings.EmitClearStringBuilder();

                ilWithSettings.Emit(OpCodes.Stloc, sbLocalWithSettings);

                ilWithSettings.Emit(OpCodes.Ldarg_0);
                ilWithSettings.Emit(OpCodes.Ldloc, sbLocalWithSettings);
                ilWithSettings.Emit(OpCodes.Ldarg_1);
                ilWithSettings.Emit(OpCodes.Call, writeMethod);

                ilWithSettings.Emit(OpCodes.Ldloc, sbLocalWithSettings);
                ilWithSettings.Emit(OpCodes.Callvirt, _stringBuilderToString);

                ilWithSettings.Emit(OpCodes.Ldarg_1);

                ilWithSettings.Emit(OpCodes.Call, _prettifyJSONIfNeeded);

                ilWithSettings.Emit(OpCodes.Ret);

                return meth.CreateDelegate(typeof(Func<T, NetJSONSettings, string>)) as Func<T, NetJSONSettings, string>;
			}
			Action<T, TextWriter> CreateSerializerWithTextWriter() {
                var meth = new DynamicMethod("SerializeValueTextWriter", _voidType, new[] { typeof(T), _textWriterType }, ManifestModule, true);


				var wil = meth.GetILGenerator();

                var writeMethod = WriteSerializeMethodFor(null, ObjType, needQuote: !IsPrimitive || ObjType == _stringType);

                var wsbLocal = wil.DeclareLocal(_stringBuilderType);
                wil.Emit(OpCodes.Call, _generatorGetStringBuilder);
                wil.EmitClearStringBuilder();
				wil.Emit(OpCodes.Stloc, wsbLocal);

                wil.Emit(OpCodes.Ldarg_0);
                wil.Emit(OpCodes.Ldloc, wsbLocal);
                wil.Emit(OpCodes.Call, _settingsCurrentSettings);
                wil.Emit(OpCodes.Call, writeMethod);

                wil.Emit(OpCodes.Ldarg_1);
                wil.Emit(OpCodes.Ldloc, wsbLocal);
                wil.Emit(OpCodes.Callvirt, _stringBuilderToString);
                wil.Emit(OpCodes.Callvirt, _textWriterWrite);
                wil.Emit(OpCodes.Ret);

                return meth.CreateDelegate(typeof(Action<T, TextWriter>)) as Action<T, TextWriter>;

			}
			Action<T, TextWriter, NetJSONSettings> CreateSerializerWithTextWriterSettings() {
                var meth = new DynamicMethod("SerializeValueTextWriterSettings", _voidType, new[] { ObjType, _textWriterType, _settingsType }, ManifestModule, true);


				var wilWithSettings = meth.GetILGenerator();

                var writeMethod = WriteSerializeMethodFor(null, ObjType, needQuote: !IsPrimitive || ObjType == _stringType);
                    
                var wsbLocalWithSettings = wilWithSettings.DeclareLocal(_stringBuilderType);
                wilWithSettings.Emit(OpCodes.Call, _generatorGetStringBuilder);
                wilWithSettings.EmitClearStringBuilder();
				wilWithSettings.Emit(OpCodes.Stloc, wsbLocalWithSettings);

                wilWithSettings.Emit(OpCodes.Ldarg_0);
                wilWithSettings.Emit(OpCodes.Ldloc, wsbLocalWithSettings);
                wilWithSettings.Emit(OpCodes.Ldarg_2);
                wilWithSettings.Emit(OpCodes.Call, writeMethod);

                wilWithSettings.Emit(OpCodes.Ldarg_1);
                wilWithSettings.Emit(OpCodes.Ldloc, wsbLocalWithSettings);
                wilWithSettings.Emit(OpCodes.Callvirt, _stringBuilderToString);

                wilWithSettings.Emit(OpCodes.Ldarg_2);

                wilWithSettings.Emit(OpCodes.Call, _prettifyJSONIfNeeded);

                wilWithSettings.Emit(OpCodes.Callvirt, _textWriterWrite);
                wilWithSettings.Emit(OpCodes.Ret);

                return meth.CreateDelegate(typeof(Action<T, TextWriter, NetJSONSettings>)) as Action<T, TextWriter, NetJSONSettings>;
			}
			public override T Deserialize(TextReader reader)
            {
                return _DeserializeTextReader(reader);
            }

            public override T Deserialize(string value)
            {
                return _Deserialize(value);
            }

            public override T Deserialize(TextReader reader, NetJSONSettings settings)
            {
                return _DeserializeTextReaderWithSettings(reader, settings);
            }

            public override T Deserialize(string value, NetJSONSettings settings)
            {
                return _DeserializeWithSettings(value, settings);
            }

            public override string Serialize(T value)
            {
                return _Serialize(value);
            }

            public override string Serialize(T value, NetJSONSettings settings)
            {
                return _SerializeWithSettings(value, settings);
            }

            public override void Serialize(T value, TextWriter writer)
            {
                _SerializeTextWriter(value, writer);
            }

            public override void Serialize(T value, TextWriter writer, NetJSONSettings settings)
            {
                _SerializeTextWriterWithSettings(value, writer, settings);
            }
        }

        private static class NetJSONCachedSerializer<T> {
            public static readonly NetJSONSerializer<T> Serializer = GetSerializer();

            private static NetJSONSerializer<T> GetSerializer()
            {     
#if NET_STANDARD_20 || NET5_0 || NET6_0_OR_GREATER
                return new DynamicNetJSONSerializer<T>();
#else
                NetJSONSerializer<T> serializer = null;
                var type = typeof(T);
                if (type.GetTypeInfo().IsGenericType)
                {
                    foreach (var item in type.GetGenericArguments())
                    {
                        if (IsPrivate(item))
                        {
                            type = item;
                            break;
                        }
                    }
                }
                if (IsPrivate(type))
                    serializer = new DynamicNetJSONSerializer<T>();
                else serializer = (NetJSONSerializer<T>)Activator.CreateInstance(Generate(typeof(T)));
                
                return serializer;
#endif
            }

            private static bool IsPrivate(Type type)
            {
                return type.GetTypeInfo().IsNotPublic ||
                        type.GetTypeInfo().IsNestedPrivate ||
                        !type.GetTypeInfo().IsVisible;
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


        internal static readonly Type 
			_dateTimeType = typeof(DateTime),
            _dateTimeOffsetType = typeof(DateTimeOffset),
            _enumType = typeof(Enum),
            _stringType = typeof(string),
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
            _objectType = typeof(object),
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
            _typeMismatchExceptionType = typeof(NetJSONTypeMismatchException),
            _serializerType = typeof(NetJSONSerializer<>),
            _expandoObjectType = typeof(ExpandoObject),
            _genericDictionaryEnumerator = typeof(Dictionary<,>.Enumerator),
            _genericListEnumerator = typeof(List<>.Enumerator),
            _typeType = typeof(Type),
            _voidType = typeof(void),
            _intType = typeof(int),
            _shortType = typeof(short),
            _longType = typeof(long),
            _jsonType = typeof(NetJSON),
			_internalJsonType = typeof(SerializerUtilities),
            _methodInfoType = typeof(MethodBase),
            _fieldInfoType = typeof(FieldInfo),
            _textWriterType = typeof(TextWriter),
            _tupleContainerType = typeof(TupleContainer),
            _netjsonPropertyType = typeof(NetJSONPropertyAttribute),
            _textReaderType = typeof(TextReader),
            _stringComparison = typeof(StringComparison),
            _settingsType = typeof(NetJSONSettings),
            _netjsonStringReaderType = typeof(NetJSONStringReader);

        static MethodInfo _stringBuilderToString =
            _stringBuilderType.GetMethod("ToString", Type.EmptyTypes),
            _stringBuilderAppend = _stringBuilderType.GetMethod("Append", new[] { _stringType }),
            _stringBuilderAppendObject = _stringBuilderType.GetMethod("Append", new[] { _objectType }),
            _stringBuilderAppendChar = _stringBuilderType.GetMethod("Append", new[] { _charType }),
            _stringOpEquality = _stringType.GetMethod("op_Equality", MethodBinding),
            _tupleContainerAdd = _tupleContainerType.GetMethod("Add"),
            _generatorGetStringBuilder = _internalJsonType.GetMethod("GetStringBuilder", MethodBinding),
            _generatorIntToStr = _internalJsonType.GetMethod("IntToStr", MethodBinding),
            _generatorCharToStr = _internalJsonType.GetMethod("CharToStr", MethodBinding),
            _generatorEnumToStr = _internalJsonType.GetMethod("CustomEnumToStr", MethodBinding),
            _generatorLongToStr = _internalJsonType.GetMethod("LongToStr", MethodBinding),
            _generatorFloatToStr = _internalJsonType.GetMethod("FloatToStr", MethodBinding),
            _generatorDoubleToStr = _internalJsonType.GetMethod("DoubleToStr", MethodBinding),
            _generatorDecimalToStr = _internalJsonType.GetMethod("DecimalToStr", MethodBinding),
            _generatorDateToString = _internalJsonType.GetMethod("AllDateToString", MethodBinding),
            _generatorDateOffsetToString = _internalJsonType.GetMethod("AllDateOffsetToString", MethodBinding),
            _generatorSByteToStr = _internalJsonType.GetMethod("SByteToStr", MethodBinding),
            _guidToStr = _internalJsonType.GetMethod("GuidToStr", MethodBinding),
            _byteArrayToStr = _internalJsonType.GetMethod("ByteArrayToStr", MethodBinding),
            _objectToString = _objectType.GetMethod("ToString", Type.EmptyTypes),
            _stringFormat = _stringType.GetMethod("Format", new[] { _stringType, _objectType }),
            _convertBase64 = typeof(Convert).GetMethod("ToBase64String", new[] { _byteArrayType }),
            _convertFromBase64 = typeof(Convert).GetMethod("FromBase64String", new[] { _stringType }),
            _getStringBasedValue = _internalJsonType.GetMethod("GetStringBasedValue", MethodBinding),
            _getNonStringValue = _internalJsonType.GetMethod("GetNonStringValue", MethodBinding),
            _isDateValue = _internalJsonType.GetMethod("IsValueDate", MethodBinding),
            _toStringIfString = _internalJsonType.GetMethod("ToStringIfString", MethodBinding),
            _toStringIfStringObject = _internalJsonType.GetMethod("ToStringIfStringObject", MethodBinding),
            _iDisposableDispose = typeof(IDisposable).GetMethod("Dispose"),
            _toExpectedType = typeof(AutomaticTypeConverter).GetMethod("ToExpectedType"),
            _fastStringToInt = _internalJsonType.GetMethod("FastStringToInt", MethodBinding),
            _fastStringToUInt = _internalJsonType.GetMethod("FastStringToUInt", MethodBinding),
            _fastStringToUShort = _internalJsonType.GetMethod("FastStringToUShort", MethodBinding),
            _fastStringToShort = _internalJsonType.GetMethod("FastStringToShort", MethodBinding),
            _fastStringToByte = _internalJsonType.GetMethod("FastStringToByte", MethodBinding),
            _fastStringToLong = _internalJsonType.GetMethod("FastStringToLong", MethodBinding),
            _fastStringToULong = _internalJsonType.GetMethod("FastStringToULong", MethodBinding),
            _fastStringToDecimal = _internalJsonType.GetMethod("FastStringToDecimal", MethodBinding),
            _fastStringToFloat = _internalJsonType.GetMethod("FastStringToFloat", MethodBinding),
            _fastStringToDate = _internalJsonType.GetMethod("FastStringToDate", MethodBinding),
            _fastStringToDateTimeoffset = _internalJsonType.GetMethod("FastStringToDateTimeoffset", MethodBinding),
            _fastStringToChar = _internalJsonType.GetMethod("FastStringToChar", MethodBinding),
            _fastStringToDouble = _internalJsonType.GetMethod("FastStringToDouble", MethodBinding),
            _fastStringToBool = _internalJsonType.GetMethod("FastStringToBool", MethodBinding),
            _fastStringToGuid = _internalJsonType.GetMethod("FastStringToGuid", MethodBinding),
            _fastStringToType = _internalJsonType.GetMethod("FastStringToType", MethodBinding),
            _moveToArrayBlock = _internalJsonType.GetMethod("MoveToArrayBlock", MethodBinding),
            _fastStringToByteArray = _internalJsonType.GetMethod("FastStringToByteArray", MethodBinding),
            _listToListObject = _internalJsonType.GetMethod("ListToListObject", MethodBinding),
            _isListType = _internalJsonType.GetMethod("IsListType", MethodBinding),
            _isDictType = _internalJsonType.GetMethod("IsDictionaryType", MethodBinding),
            _stringLength = _stringType.GetMethod("get_Length"),
            _createString = _internalJsonType.GetMethod("CreateString"),
            _isCharTag = _internalJsonType.GetMethod("IsCharTag"),
            _isEndChar = _internalJsonType.GetMethod("IsEndChar", MethodBinding),
            _isArrayEndChar = _internalJsonType.GetMethod("IsArrayEndChar", MethodBinding),
            _encodedJSONString = _jsonType.GetMethod("EncodedJSONString", MethodBinding),
            _decodeJSONString = _jsonType.GetMethod("DecodeJSONString", MethodBinding),
            _readerDeserializer = _jsonType.GetMethod("ReaderDeserializer", MethodBinding),
            _skipProperty = _internalJsonType.GetMethod("SkipProperty", MethodBinding),
            _prettifyJSONIfNeeded = _jsonType.GetMethod("PrettifyJSONIfNeeded", MethodBinding),
            _isRawPrimitive = _internalJsonType.GetMethod("IsRawPrimitive", MethodBinding),
            _isInRange = _internalJsonType.GetMethod("IsInRange", MethodBinding),
            _dateTimeParse = _dateTimeType.GetMethod("Parse", new[] { _stringType }),
            _timeSpanParse = _timeSpanType.GetMethod("Parse", new[] { _stringType }),
            _getChars = _stringType.GetMethod("get_Chars"),
            _dictSetItem = _dictType.GetMethod("set_Item"),
            _textWriterWrite = _textWriterType.GetMethod("Write", new[] { _stringType }),
            _textReaderReadToEnd = _textReaderType.GetMethod("ReadToEnd"),
            _typeopEquality = _typeType.GetMethod("op_Equality", MethodBinding),
            _cTypeOpEquality = _internalJsonType.GetMethod("CustomTypeEquality", MethodBinding),
            _assemblyQualifiedName = _typeType.GetProperty("AssemblyQualifiedName").GetGetMethod(),
            _objectGetType = _objectType.GetMethod("GetType", MethodBinding),
            _needQuote = _internalJsonType.GetMethod("NeedQuotes", MethodBinding),
            _typeGetTypeFromHandle = _typeType.GetMethod("GetTypeFromHandle", MethodBinding),
            _methodGetMethodFromHandle = _methodInfoType.GetMethod("GetMethodFromHandle", new Type[] { typeof(RuntimeMethodHandle) }),
            _methodGetFieldFromHandle = _fieldInfoType.GetMethod("GetFieldFromHandle", new Type[] { typeof(RuntimeFieldHandle) }),
            _objectEquals = _objectType.GetMethod("Equals", new[] { _objectType }),
            _stringEqualCompare = _stringType.GetMethod("Equals", new[] { _stringType, _stringType, typeof(StringComparison) }),
            _stringConcat = _stringType.GetMethod("Concat", new[] { _objectType, _objectType, _objectType, _objectType }),
            _IsCurrentAQuotMethod = _internalJsonType.GetMethod("IsCurrentAQuot", MethodBinding),
            _getTypeIdentifierInstanceMethod = _internalJsonType.GetMethod("GetTypeIdentifierInstance", MethodBinding),
            _settingsUseEnumStringProp = _settingsType.GetProperty("UseEnumString", MethodBinding).GetGetMethod(),
            _settingsUseStringOptimization = _settingsType.GetProperty("UseStringOptimization", MethodBinding).GetGetMethod(),
            _settingsHasOverrideQuoteChar = _settingsType.GetProperty("HasOverrideQuoteChar", MethodBinding).GetGetMethod(),
            _settingsDateFormat = _settingsType.GetProperty("DateFormat", MethodBinding).GetGetMethod(),
            _settingsSkipDefaultValue = _settingsType.GetProperty("SkipDefaultValue", MethodBinding).GetGetMethod(),
            _getUninitializedInstance = _internalJsonType.GetMethod("GetUninitializedInstance", MethodBinding),
            _flagEnumToString = _internalJsonType.GetMethod("FlagEnumToString", MethodBinding),
            _flagStringToEnum = _internalJsonType.GetMethod("FlagStringToEnum", MethodBinding),
            _setterPropertyValueMethod = _internalJsonType.GetMethod("SetterPropertyValue", MethodBinding),
            _setterFieldValueMethod = _internalJsonType.GetMethod("SetterFieldValue", MethodBinding),
            _settingsCurrentSettings = _settingsType.GetProperty("CurrentSettings", MethodBinding).GetGetMethod(),
            _settingsCamelCase = _settingsType.GetProperty("CamelCase", MethodBinding).GetGetMethod(),
            _throwIfInvalidJSON = _internalJsonType.GetMethod("ThrowIfInvalidJSON", MethodBinding),
            _failIfInvalidCharacter = _internalJsonType.GetMethod("FailIfInvalidCharacter", MethodBinding),
            _toCamelCase = _internalJsonType.GetMethod("ToCamelCase", MethodBinding);

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
              IReadOnlyDictionaryStr = "IReadOnlyDictionary`2",
			  KeyValueStr = "KeyValuePair`2",
			  ICollectionStr = "ICollection`1",
			  IEnumerableStr = "IEnumerable`1",
              IReadOnlyCollectionStr = "IReadOnlyCollection`1",
              IReadOnlyListStr = "IReadOnlyList`1",
              CreateListStr = "CreateList",
              CreateClassOrDictStr = "CreateClassOrDict",
              Dynamic = "Dynamic",
              ExtractStr = "Extract",
              SetStr = "Set",
              WriteStr = "Write", ReadStr = "Read", ReadEnumStr = "ReadEnum",
              CarrotQuoteChar = "`",
              ArrayStr = "Array", AnonymousBracketStr = "<>",
              ArrayLiteral = "[]",
              Colon = ":",
              ToTupleStr = "ToTuple",
              SerializeStr = "Serialize", DeserializeStr = "Deserialize", SettingsFieldName = "_settingsField";

        static ConstructorInfo _strCtorWithPtr = _stringType.GetConstructor(new[] { typeof(char*), _intType, _intType });
        static ConstructorInfo _invalidJSONCtor = _invalidJSONExceptionType.GetConstructor(Type.EmptyTypes);
        static ConstructorInfo _typeMismatchExceptionCtor = _typeMismatchExceptionType.GetConstructor(Type.EmptyTypes);
        static ConstructorInfo _settingsCtor = _settingsType.GetConstructor(Type.EmptyTypes);

        private static ConcurrentDictionary<string, object> _dictLockObjects = new ConcurrentDictionary<string, object>();
        static ConcurrentDictionary<Type, MethodInfo> _registeredSerializerMethods =
            new ConcurrentDictionary<Type, MethodInfo>();

        static ConcurrentDictionary<string, MethodInfo> _registeredCustomSerializerMethods =
            new ConcurrentDictionary<string, MethodInfo>();

        static ConcurrentDictionary<string, MethodInfo> _registeredCustomDeserializerMethods =
            new ConcurrentDictionary<string, MethodInfo>();

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
        static ConcurrentDictionary<string, MethodInfo> _writeMethodBuilders =
            new ConcurrentDictionary<string, MethodInfo>();

        static ConcurrentDictionary<string, MethodInfo> _setValueMethodBuilders =
            new ConcurrentDictionary<string, MethodInfo>();

        static ConcurrentDictionary<string, MethodInfo> _readMethodBuilders =
            new ConcurrentDictionary<string, MethodInfo>();

        static ConcurrentDictionary<string, MethodInfo> _createListMethodBuilders =
            new ConcurrentDictionary<string, MethodInfo>();

        static ConcurrentDictionary<string, MethodInfo> _extractMethodBuilders =
            new ConcurrentDictionary<string, MethodInfo>();

        static ConcurrentDictionary<string, MethodInfo> _readDeserializeMethodBuilders =
            new ConcurrentDictionary<string, MethodInfo>();

        static ConcurrentDictionary<string, MethodInfo> _writeEnumToStringMethodBuilders =
            new ConcurrentDictionary<string, MethodInfo>();

        static ConcurrentDictionary<string, MethodInfo> _readEnumToStringMethodBuilders =
            new ConcurrentDictionary<string, MethodInfo>();

        static readonly ConcurrentDictionary<MethodInfo, Delegate> _setMemberValues = new ConcurrentDictionary<MethodInfo, Delegate>();

        static readonly ConcurrentDictionary<FieldInfo, Delegate> _setMemberFieldValues = new ConcurrentDictionary<FieldInfo, Delegate>();

        static ConcurrentDictionary<string, Func<object>> _typeIdentifierFuncs = new ConcurrentDictionary<string, Func<object>>();

        static ConcurrentDictionary<string, Func<NetJSONStringReader, NetJSONSettings, object>> _customDeserializerForNetJSONStringReaders = 
            new ConcurrentDictionary<string, Func<NetJSONStringReader, NetJSONSettings, object>>();

        static ConcurrentDictionary<Type, bool> _primitiveTypes =
            new ConcurrentDictionary<Type, bool>();

        static ConcurrentDictionary<Type, Type> _nullableTypes =
            new ConcurrentDictionary<Type, Type>();

        static ConcurrentDictionary<Type, List<Type>> _includedTypeTypes = new ConcurrentDictionary<Type, List<Type>>();

        static ConcurrentDictionary<Type, object> _serializers = new ConcurrentDictionary<Type, object>();

        static ConcurrentDictionary<Type, NetJSONMemberInfo[]> _typeProperties =
            new ConcurrentDictionary<Type, NetJSONMemberInfo[]>();

        static ConcurrentDictionary<string, string> _fixes =
            new ConcurrentDictionary<string, string>();

        private static object _lockObject = new object();

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

        static bool IsPrimitiveType(this Type type) {
            return _primitiveTypes.GetOrAdd(type, key => {
                lock (GetDictLockObject("IsPrimitiveType")) {
                    if (key.GetTypeInfo().IsGenericType &&
                        key.GetGenericTypeDefinition() == _nullableType)
                        key = key.GetGenericArguments()[0];

                    return key == _stringType ||
                        key.GetTypeInfo().IsPrimitive || key == _dateTimeType ||
                        key == _dateTimeOffsetType ||
                        key == _decimalType || key == _timeSpanType ||
                        key == _guidType || key == _charType ||
                        key == _typeType ||
                        key.GetTypeInfo().IsEnum || key == _byteArrayType;
                }
            });
        }

        private static Type GetNullableType(this Type type) {
            return _nullableTypes.GetOrAdd(type, key => {
                lock (GetDictLockObject("GetNullableType"))
                    return key.Name.StartsWith("Nullable`") ? key.GetGenericArguments()[0] : null;
            });
        }

        private static void LookupAttribute<T>(ref NetJSONPropertyAttribute attr, MemberInfo memberInfo, Func<T, string> func) where T : Attribute
        {
            if (attr != null)
                return;
            var memberAttr = memberInfo.GetCustomAttributes(typeof(T), true).OfType<T>().FirstOrDefault();
            if (memberAttr == null)
                return;
            attr = new NetJSONPropertyAttribute(func(memberAttr));
        }

        private static NetJSONPropertyAttribute GetSerializeAs(MemberInfo memberInfo)
        {
            NetJSONPropertyAttribute attr = null;
            if (SerializeAs != null)
            {
                var name = SerializeAs(memberInfo);
                if (!string.IsNullOrEmpty(name))
                {
                    attr = new NetJSONPropertyAttribute(name);
                }
            }

#if !NET_STANDARD
            LookupAttribute<XmlAttributeAttribute>(ref attr, memberInfo, it => it.AttributeName);
            LookupAttribute<XmlElementAttribute>(ref attr, memberInfo, it => it.ElementName);
            LookupAttribute<XmlArrayAttribute>(ref attr, memberInfo, it => it.ElementName);
#endif
            return attr;
        }

        private static bool GetCanSerialize(MemberInfo memberInfo)
        {
            if(CanSerialize != null)
            {
                return CanSerialize(memberInfo);
            }

            return true;
        }

        internal static NetJSONMemberInfo[] GetTypeProperties(this Type type) {
            return _typeProperties.GetOrAdd(type, key => {
                lock (GetDictLockObject("GetTypeProperties")) {
                    var props = key.GetProperties(PropertyBinding)
                        .Where(x => x.GetIndexParameters().Length == 0 && GetCanSerialize(x))
                        .Select(x =>
                        {
                            var attr = x.GetCustomAttributes(_netjsonPropertyType, true).OfType<NetJSONPropertyAttribute>().FirstOrDefault();

                            if (attr == null)
                            {
                                attr = GetSerializeAs(x);
                            }

                            return new NetJSONMemberInfo
                            {
                                Member = x,
                                Attribute = attr
                            };
                        });
                    if (_includeFields) {
                        props = props.Union(key.GetFields(PropertyBinding).Where(x => GetCanSerialize(x)).Select(x => new NetJSONMemberInfo { Member = x, Attribute = x.GetCustomAttributes(_netjsonPropertyType, true).OfType<NetJSONPropertyAttribute>().FirstOrDefault() ?? GetSerializeAs(x) }));
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

        private static void LoadQuotChar(ILGenerator il) {
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldfld, _settingQuoteCharString);
            //il.Emit(OpCodes.Ldsfld, _threadQuoteStringField);
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

        /// <summary>
        /// Delegate to override what member can get serialized
        /// </summary>
        public static Func<MemberInfo, bool> CanSerialize { private get; set; }

        /// <summary>
        /// Delegate to override what name to use for members when serialized
        /// </summary>
        public static Func<MemberInfo, string> SerializeAs { private get; set; }

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

        [Obsolete("All fields will be included by default. This property will be removed in future release")]
        public static bool IncludeFields {
            set {
                _includeFields = true;
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


        internal static object GetTypeIdentifierInstance(string typeName) {
            return _typeIdentifierFuncs.GetOrAdd(typeName, _ => {
                lock (GetDictLockObject("GetTypeIdentifier")) {
                    var type = Type.GetType(typeName, throwOnError: false);
                    if (type == null)
                        throw new InvalidOperationException(string.Format("Unable to resolve {0} with value = {1}", TypeIdentifier, typeName));

                    var ctor = type.GetConstructor(Type.EmptyTypes);

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


        internal static bool NeedQuotes(Type type, NetJSONSettings settings) {
            return type == _stringType || type == _charType || type == _guidType || type == _timeSpanType || ((type == _dateTimeType || type == _dateTimeOffsetType) && settings.DateFormat != NetJSONDateFormat.EpochTime) || type == _byteArrayType || (settings.UseEnumString && type.GetTypeInfo().IsEnum);
        }

        private static MethodInfo GenerateFastObjectToString(TypeBuilder type) {
            return _readMethodBuilders.GetOrAdd("FastObjectToString", _ => {
                lock (GetDictLockObject("GenerateFastObjectToString")) {
                    var method = type.DefineMethodEx("FastObjectToString", StaticMethodAttribute, _voidType,
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
                            il.Emit(OpCodes.Ldarg_2);
                            il.Emit(OpCodes.Call, _toStringIfString);
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
                            if (objType.GetTypeInfo().IsValueType)
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
                GenerateTypeBuilder(type, module)
#if NET_STANDARD
    .CreateTypeInfo().AsType();
#else
                    .CreateType();
#endif

            }

#if !NET_STANDARD && !NET5_0 && !NET6_0_OR_GREATER
            assembly.Save(String.Concat(assembly.GetName().Name, _dllStr));
#endif
        }

        internal static Type Generate(Type objType) {

            var returnType = default(Type);
            if (_types.TryGetValue(objType, out returnType))
                return returnType;

            var asmName = String.Concat(objType.GetName(), ClassStr);

            var assembly = _useSharedAssembly ? GenerateAssemblyBuilder() : GenerateAssemblyBuilderNoShare(asmName);

            var module = _useSharedAssembly ? GenerateModuleBuilder(assembly) : GenerateModuleBuilderNoShare(assembly);

            var type = GenerateTypeBuilder(objType, module);

            returnType = type
#if NET_STANDARD
    .CreateTypeInfo().AsType();
#else
    .CreateType();
#endif
                
            _types[objType] = returnType;

#if !NET_STANDARD && !NET5_0 && !NET6_0_OR_GREATER
            if (_generateAssembly)
                assembly.Save(String.Concat(assembly.GetName().Name, _dllStr));
#endif
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

            il.EmitClearStringBuilder();

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
            wil.EmitClearStringBuilder();
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
            var isValueType = objType.GetTypeInfo().IsValueType;
            var ilWithSettings = serializeMethodWithSettings.GetILGenerator();

            var sbLocalWithSettings = ilWithSettings.DeclareLocal(_stringBuilderType);
            ilWithSettings.Emit(OpCodes.Call, _generatorGetStringBuilder);

            ilWithSettings.EmitClearStringBuilder();

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
            wilWithSettings.EmitClearStringBuilder();
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
        internal const string NET_JSON_GENERATED_ASSEMBLY_NAME = "NetJSONGeneratedAssembly";

        private static AssemblyBuilder GenerateAssemblyBuilder() {
            if (_assembly == null) {
                lock (_lockAsmObject) {
                    if (_assembly == null) {
                        _assembly =
#if NET_STANDARD || NET5_0 || NET6_0_OR_GREATER
                AssemblyBuilder
#else
                AppDomain.CurrentDomain
#endif
                        .DefineDynamicAssembly(
                            new AssemblyName(NET_JSON_GENERATED_ASSEMBLY_NAME) {
                                Version = new Version(1, 0, 0, 0)
                            },
#if NET_STANDARD || NET5_0 || NET6_0_OR_GREATER
                            AssemblyBuilderAccess.Run
#else
                            AssemblyBuilderAccess.RunAndSave
#endif
                            );


                        //[assembly: CompilationRelaxations(8)]
                        _assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(CompilationRelaxationsAttribute).GetConstructor(new[] { _intType }), new object[] { 8 }));

                        //[assembly: RuntimeCompatibility(WrapNonExceptionThrows=true)]
                        _assembly.SetCustomAttribute(new CustomAttributeBuilder(
                            typeof(RuntimeCompatibilityAttribute).GetConstructor(Type.EmptyTypes),
                            new object[] { },
                            new[] {  typeof(RuntimeCompatibilityAttribute).GetProperty("WrapNonExceptionThrows")
                },
                            new object[] { true }));

#if !NET_STANDARD
                        //[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification=true)]
                        _assembly.SetCustomAttribute(new CustomAttributeBuilder(
                            typeof(SecurityPermissionAttribute).GetConstructor(new[] { typeof(SecurityAction) }),
                            new object[] { SecurityAction.RequestMinimum },
                            new[] {  typeof(SecurityPermissionAttribute).GetProperty("SkipVerification")
                },
                            new object[] { true }));

                        //[assembly: SecurityRules(SecurityRuleSet.Level2,SkipVerificationInFullTrust=true)]
#if !NET_35
                        _assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(SecurityRulesAttribute).GetConstructor(new[] { typeof(SecurityRuleSet) }), 
                            new object[] { SecurityRuleSet.Level2 },
                            new[] { typeof(SecurityRulesAttribute).GetProperty("SkipVerificationInFullTrust") }, new object[] { true }));
#endif
#endif
                    }
                }
            }
            return _assembly;
        }

        private static AssemblyBuilder GenerateAssemblyBuilderNoShare(string asmName) {
            var assembly =
#if NET_STANDARD || NET5_0 || NET6_0_OR_GREATER
                AssemblyBuilder
#else
                AppDomain.CurrentDomain
#endif
                .DefineDynamicAssembly(
                new AssemblyName(asmName) {
                    Version = new Version(1, 0, 0, 0)
                },
#if !NET_STANDARD && !NET5_0 && !NET6_0_OR_GREATER
                AssemblyBuilderAccess.RunAndSave
#else
                AssemblyBuilderAccess.Run
#endif
                );


            //[assembly: CompilationRelaxations(8)]
            assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(CompilationRelaxationsAttribute).GetConstructor(new[] { _intType }), new object[] { 8 }));

            //[assembly: RuntimeCompatibility(WrapNonExceptionThrows=true)]
            assembly.SetCustomAttribute(new CustomAttributeBuilder(
                typeof(RuntimeCompatibilityAttribute).GetConstructor(Type.EmptyTypes),
                new object[] { },
                new[] {  typeof(RuntimeCompatibilityAttribute).GetProperty("WrapNonExceptionThrows")
                },
                new object[] { true }));

#if !NET_STANDARD
            //[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification=true)]
            assembly.SetCustomAttribute(new CustomAttributeBuilder(
                typeof(SecurityPermissionAttribute).GetConstructor(new[] { typeof(SecurityAction) }),
                new object[] { SecurityAction.RequestMinimum },
                new[] {  typeof(SecurityPermissionAttribute).GetProperty("SkipVerification")
                },
                new object[] { true }));
#endif
            return assembly;
        }

        internal static string PrettifyJSONIfNeeded(string str, NetJSONSettings settings) {
            if (settings.Format == NetJSONFormat.Prettify)
                return PrettifyJSON(str);
            return str;
        }

        internal static unsafe string PrettifyJSON(string str) {
            var sb = new StringBuilder();
            
            var horizontal = 0;
            var horizontals = new int[10000];
            var hrIndex = -1;
            var @return = false;
            var quote = 0;

            char c;

            fixed (char* chr = str) {
                char* ptr = chr;
                while ((c = *(ptr++)) != '\0') {
                    switch (c) {
                        case '{':
                        case '[':
                            sb.Append(c);
                            if (quote == 0)
                            {
                                hrIndex++;
                                horizontals[hrIndex] = horizontal;
                                @return = true;
                            }
                            break;
                        case '}':
                        case ']':
                            if (quote == 0)
                            {
                                @return = false;
                                sb.Append('\n');
                                horizontal = horizontals[hrIndex];
                                hrIndex--;
                                for (var i = 0; i < horizontal; i++)
                                {
                                    sb.Append(' ');
                                }
                            }
                            sb.Append(c);
                            break;
                        case ',':
                            sb.Append(c);
                            if (quote == 0)
                            {
                                @return = true;
                            }
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
                            var escaped = *(ptr - 2) == '\\';
                            if (c == '"' && !escaped)
                            {
                                quote++;
                                quote %= 2;
                            }
                            sb.Append(c);
                            break;
                    }

                    horizontal++;
                }
            }

            return sb.ToString();
        }

        internal static unsafe void EncodedJSONString(StringBuilder sb, string str, NetJSONSettings settings) {
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

        static string GetName(this Type type) {
            var sb = new StringBuilder();
            var arguments =
                !type.GetTypeInfo().IsGenericType ? Type.EmptyTypes :
                type.GetGenericArguments();
            if (!type.GetTypeInfo().IsGenericType) {
                sb.Append(type.Name);
            } else {
                sb.Append(type.Name);
                foreach (var argument in arguments)
                    sb.Append(GetName(argument));
            }
            return sb.ToString();
        }

        static string Fix(this string name) {
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
            MethodInfo method;
            var key = string.Concat(type.FullName, typeBuilder == null ? Dynamic : string.Empty);
            var typeName = type.GetName().Fix();
            if (_readEnumToStringMethodBuilders.TryGetValue(key, out method))
                return method;
            var methodName = String.Concat(ReadEnumStr, typeName);
            method = typeBuilder.DefineMethodEx(methodName, StaticMethodAttribute,
                type, new[] { _stringType });
            _readEnumToStringMethodBuilders[key] = method;

            var eType = type.GetTypeInfo().GetEnumUnderlyingType();
            var il = method.GetILGenerator();

            var values = Enum.GetValues(type).Cast<object>()
                .Select(x => new {
                    Value = x,
                    Attr = type.GetTypeInfo().GetMember(x.ToString()).FirstOrDefault()
                })
                    .Select(x => new {
                        Value = x.Value,
                        Attr = x.Attr != null ?
                    (x.Attr.GetCustomAttributes(typeof(NetJSONPropertyAttribute), true).FirstOrDefault() as NetJSONPropertyAttribute) : null
                    }).ToArray();
            var keys = Enum.GetNames(type);

            for (var i = 0; i < values.Length; i++) {

                var valueInfo = values[i];
                var value = valueInfo.Value;
                var attr = valueInfo.Attr;
                var k = keys[i];

                if (attr != null)
                    k = attr.Name;
                
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
                    il.Emit(OpCodes.Ldstr, SerializerUtilities.IntToStr((int)value));
                else if (eType == _longType)
                    il.Emit(OpCodes.Ldstr, SerializerUtilities.LongToStr((long)value));
                else if (eType == typeof(ulong))
                    il.Emit(OpCodes.Ldstr, SerializerUtilities.LongToStr((long)Convert.ToUInt64(value)));
                else if (eType == typeof(uint))
                    il.Emit(OpCodes.Ldstr, IntUtility.uitoa((uint)value));
                else if (eType == typeof(byte))
                    il.Emit(OpCodes.Ldstr, SerializerUtilities.IntToStr((int)((byte)value)));
                else if (eType == typeof(ushort))
                    il.Emit(OpCodes.Ldstr, SerializerUtilities.IntToStr((int)((ushort)value)));
                else if (eType == typeof(short))
                    il.Emit(OpCodes.Ldstr, SerializerUtilities.IntToStr((int)((short)value)));
                 
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
            
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, _flagStringToEnum.MakeGenericMethod(type));

            il.Emit(OpCodes.Ret);

            return method;
        }

        internal static MethodInfo WriteEnumToStringFor(TypeBuilder typeBuilder, Type type) {
            MethodInfo method;
            var key = String.Concat(type.FullName, typeBuilder == null ? Dynamic : string.Empty);
            var typeName = type.GetName().Fix();
            if (_writeEnumToStringMethodBuilders.TryGetValue(key, out method))
                return method;
            var methodName = String.Concat(WriteStr, typeName);
            method = typeBuilder.DefineMethodEx(methodName, StaticMethodAttribute,
                _stringType, new[] { type, _settingsType });
            _writeEnumToStringMethodBuilders[key] = method;

            var eType = type.GetTypeInfo().GetEnumUnderlyingType();

            var il = method.GetILGenerator();
            var useEnumLabel = il.DefineLabel();


            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Callvirt, _settingsUseEnumStringProp);
            il.Emit(OpCodes.Brfalse, useEnumLabel);

            WriteEnumToStringForWithString(type, eType, il);

            il.MarkLabel(useEnumLabel);

            WriteEnumToStringForWithInt(type, eType, il);
            
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Box, type);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, _flagEnumToString);

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
                    il.Emit(OpCodes.Ldstr, SerializerUtilities.IntToStr((int)value));
                else if (eType == _longType)
                    il.Emit(OpCodes.Ldstr, SerializerUtilities.LongToStr((long)value));
                else if (eType == typeof(ulong))
                    il.Emit(OpCodes.Ldstr, IntUtility.ultoa((ulong)value));
                else if (eType == typeof(uint))
                    il.Emit(OpCodes.Ldstr, IntUtility.uitoa((uint)value));
                else if (eType == typeof(byte))
                    il.Emit(OpCodes.Ldstr, SerializerUtilities.IntToStr((int)((byte)value)));
                else if (eType == typeof(ushort))
                    il.Emit(OpCodes.Ldstr, SerializerUtilities.IntToStr((int)((ushort)value)));
                else if (eType == typeof(short))
                    il.Emit(OpCodes.Ldstr, SerializerUtilities.IntToStr((int)((short)value)));


                il.Emit(OpCodes.Ret);

                il.MarkLabel(label);
            }
        }

        private static void WriteEnumToStringForWithString(Type type, Type eType, ILGenerator il) {
            var values = Enum.GetValues(type).Cast<object>()
                .Select(x => new { Value = x,
                    Attr = type.GetTypeInfo().GetMember(x.ToString()).FirstOrDefault() })
                    .Select(x => new { Value = x.Value, Attr = x.Attr != null ? 
                    (x.Attr.GetCustomAttributes(typeof(NetJSONPropertyAttribute), true).FirstOrDefault() as NetJSONPropertyAttribute) : null  }).ToArray();
            var names = Enum.GetNames(type);

            var count = values.Length;

            for (var i = 0; i < count; i++) {

                var valueInfo = values[i];
                var attr = valueInfo.Attr;
                var value = valueInfo.Value;

                var name = names[i];
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

                if(attr != null)
                    name = attr.Name;

                il.Emit(OpCodes.Ldstr, name);
                il.Emit(OpCodes.Ret);

                il.MarkLabel(label);
            }
        }

        internal static MethodInfo DefineMethodEx(this TypeBuilder builder, string methodName, MethodAttributes methodAttribute, Type returnType, Type[] parameterTypes)
        {
            if (builder == null)
                return new DynamicMethod(methodName, returnType, parameterTypes,
#if NET_STANDARD
#if NET_STANDARD_20 || NET5_0 || NET6_0_OR_GREATER
                    Assembly.GetExecutingAssembly().ManifestModule
#else
                    Assembly.GetEntryAssembly().ManifestModule
#endif
#else
                    Assembly.GetExecutingAssembly().ManifestModule
#endif
                    ,
                    true);
            return builder.DefineMethod(methodName, methodAttribute, returnType, parameterTypes);
        }

        internal static ILGenerator GetILGenerator(this MethodInfo methodInfo)
        {
            var dynamicMethod = methodInfo as DynamicMethod;
            return dynamicMethod != null ? dynamicMethod.GetILGenerator() : (methodInfo as MethodBuilder).GetILGenerator();
        }

        internal static MethodInfo WriteDeserializeMethodFor(TypeBuilder typeBuilder, Type type) {
            MethodInfo method;
            var key = string.Concat(type.FullName, typeBuilder == null ? Dynamic : string.Empty);
            var typeName = type.GetName().Fix();
            if (_readDeserializeMethodBuilders.TryGetValue(key, out method))
                return method;

            var methodName = String.Concat(ReadStr, typeName);
            method = typeBuilder.DefineMethodEx(methodName, StaticMethodAttribute,
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

                //Fast fail when invalid json exists
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, (int)'[');
                il.Emit(OpCodes.Call, _throwIfInvalidJSON);

                //IsArray
                il.Emit(OpCodes.Ldloc, ptr);
                il.Emit(OpCodes.Ldloca, index);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Call, GenerateCreateListFor(typeBuilder, typeof(List<object>)));
                il.Emit(OpCodes.Ret);

                il.MarkLabel(startsWithLabel);

                il.Emit(OpCodes.Ldloc, startsWith);
                il.Emit(OpCodes.Brtrue, notStartsWithLabel);

                //Fast fail when invalid json exists
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, (int)'{');
                il.Emit(OpCodes.Call, _throwIfInvalidJSON);

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
                var isPrimitive = type.IsPrimitiveType(); 
                var isArray = (type.IsListType() || type.IsArray) && !isPrimitive;
                var isComplex = isArray || type.IsDictionaryType() || !isPrimitive;

                if (isComplex)
                {
                    //Fast fail when invalid json exists
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, (int)(isArray ? '[' : '{'));
                    il.Emit(OpCodes.Call, _throwIfInvalidJSON);
                }

                il.Emit(OpCodes.Ldloc, ptr);
                il.Emit(OpCodes.Ldloca, index);
                il.Emit(OpCodes.Ldarg_1);
                if (isArray)
                    il.Emit(OpCodes.Call, GenerateCreateListFor(typeBuilder, type));
                else {
                    if (isPrimitive) {
                        il.Emit(OpCodes.Call, GenerateExtractValueFor(typeBuilder, type));
                    }else
                        il.Emit(OpCodes.Call, GenerateGetClassOrDictFor(typeBuilder, type));
                }
            }
            
            il.Emit(OpCodes.Ret);

            return method;
        }

        internal static MethodInfo WriteSerializeMethodFor(TypeBuilder typeBuilder, Type type, bool needQuote = true) {
            MethodInfo method;
            var key = string.Concat(type.FullName, typeBuilder == null ? Dynamic : string.Empty);
            var typeName = type.GetName().Fix();
            if (_writeMethodBuilders.TryGetValue(key, out method))
                return method;

            if (_registeredCustomSerializerMethods.TryGetValue(typeName, out method))
            {
                return _writeMethodBuilders[key] = method;
            }

            var methodName = String.Concat(WriteStr, typeName);

            method = typeBuilder.DefineMethodEx(methodName, StaticMethodAttribute,
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
                    } else if (type.GetTypeInfo().IsEnum) {
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

            if (type.GetTypeInfo().IsValueType) {
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
 
            if (type.IsCollectionType()) WriteCollection(typeBuilder, type, methodIL);
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
                    var attrs = type.GetTypeInfo().GetCustomAttributes(typeof(NetJSONKnownTypeAttribute), false).OfType<NetJSONKnownTypeAttribute>();
                    var types = attrs.Any() ? attrs.Where(x => !x.Type.GetTypeInfo().IsAbstract).Select(x => x.Type).ToList() : null;
                    
                    //Expense call to auto-magically figure all subclass of current type
                    if (types == null) {
                        types = new List<Type>();
#if NET_STANDARD
#if NET_STANDARD_20 || NET5_0 || NET6_0_OR_GREATER
    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
#else
    var assemblies = DependencyContext.Default.GetDefaultAssemblyNames().Select(x => Assembly.Load(x));
#endif
#else
                        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
#endif
                        foreach (var asm in assemblies) {
                            try {
                                types.AddRange(asm.GetTypes().Where(x => x.GetTypeInfo().IsSubclassOf(type) || x.GetTypeInfo().GetInterfaces().Any(i => i == type)));
                            } catch (ReflectionTypeLoadException ex) {
                                var exTypes = ex.Types != null ? ex.Types.Where(x => x != null && x.GetTypeInfo().IsSubclassOf(type)) : null;
                                if (exTypes != null)
                                    types.AddRange(exTypes);
                            }
                        }
                    }

                    var typeInfo = type.GetTypeInfo();
                    if (!types.Contains(type) && (!typeInfo.IsAbstract || typeInfo.IsInterface))
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
                var baseType = type.GetTypeInfo().BaseType;
                if (baseType == _objectType) {
                    baseType = type.GetTypeInfo().GetInterface(IEnumerableStr);
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
                if(keyType == _dateTimeType)
                {
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Call, _generatorDateToString);
                    il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                }
                else if (keyType == _dateTimeOffsetType)
                {
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Call, _generatorDateOffsetToString);
                    il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                }
                else
                {
                    if (keyType.GetTypeInfo().IsValueType)
                        il.Emit(OpCodes.Box, keyType);
                    il.Emit(OpCodes.Callvirt, _stringBuilderAppendObject);
                }
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
            var listLocal = isArray ? default(LocalBuilder) : il.DeclareLocal(type);

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

            var skipDefaultValue = il.DeclareLocal(_boolType);
            var camelCasing = il.DeclareLocal(_boolType);
            var hasValue = il.DeclareLocal(_boolType);
            var props = type.GetTypeProperties();
            var count = props.Length - 1;
            var counter = 0;
            var isClass = type.IsClass();

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, hasValue);


            //Get skip default value setting
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Callvirt, _settingsSkipDefaultValue);
            il.Emit(OpCodes.Stloc, skipDefaultValue);


            //Get camel case setting
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Callvirt, _settingsCamelCase);
            il.Emit(OpCodes.Stloc, camelCasing);

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

                il.Emit(OpCodes.Ldstr, string.Format("{0}, {1}", type.FullName, type.GetTypeInfo().Assembly.GetName().Name));
                il.Emit(OpCodes.Callvirt, _stringBuilderAppend);

                LoadQuotChar(il);
                il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                il.Emit(OpCodes.Pop);
                counter = 1;

                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Stloc, hasValue);
            }

            foreach (var mem in props)
            {
                var member = mem.Member;
                var name = member.Name;
                var prop = member.MemberType == MemberTypes.Property ? member as PropertyInfo : null;
                var field = member.MemberType == MemberTypes.Field ? member as FieldInfo : null;
                var attr = mem.Attribute;
                var isProp = prop != null;
                var getMethod = isProp ? prop.GetGetMethod() : null;
                if (!isProp || getMethod != null) {
                    if (attr != null)
                        name = attr.Name ?? name;

                    var memberType = isProp ? prop.PropertyType : field.FieldType;
                    var propType = memberType;
                    var originPropType = memberType;
                    var isPrimitive = propType.IsPrimitiveType();
                    var nullableType = propType.GetNullableType();
                    var isNullable = nullableType != null && !originPropType.IsArray;

                    propType = isNullable ? nullableType : propType;
                    var isValueType = propType.GetTypeInfo().IsValueType;
                    //var propNullLabel = _skipDefaultValue ? il.DefineLabel() : default(Label);
                    var equalityMethod = propType.GetMethod("op_Equality");
                    var propValue = il.DeclareLocal(propType);
                    var isStruct = isValueType && !isPrimitive;
                    var nullablePropValue = isNullable ? il.DeclareLocal(originPropType) : null;
                    var nameLocal = il.DeclareLocal(_stringType);
                    var camelCaseLabel = il.DefineLabel();

                    il.Emit(OpCodes.Ldstr, name);
                    il.Emit(OpCodes.Stloc, nameLocal);

                    il.Emit(OpCodes.Ldloc, camelCasing);
                    il.Emit(OpCodes.Brfalse, camelCaseLabel);

                    il.Emit(OpCodes.Ldloc, nameLocal);
                    il.Emit(OpCodes.Call, _toCamelCase);
                    il.Emit(OpCodes.Stloc, nameLocal);

                    il.MarkLabel(camelCaseLabel);

                    if (isClass) {
                        il.Emit(OpCodes.Ldarg_0);
                        if (isProp)
                            il.Emit(OpCodes.Callvirt, getMethod);
                        else
                            il.Emit(OpCodes.Ldfld, field);
                    } else {
                        il.Emit(OpCodes.Ldarga, 0);
                        if (isProp)
                            il.Emit(OpCodes.Call, getMethod);
                        else il.Emit(OpCodes.Ldfld, field);
                    }

                    if (isNullable) {
                        il.Emit(OpCodes.Stloc, nullablePropValue);

                        il.Emit(OpCodes.Ldloca, nullablePropValue);
                        il.Emit(OpCodes.Call, originPropType.GetMethod("GetValueOrDefault", Type.EmptyTypes));

                        il.Emit(OpCodes.Stloc, propValue);
                    } else
                        il.Emit(OpCodes.Stloc, propValue);


                    var propNullLabel = il.DefineLabel();
                    var skipDefaultValueTrueLabel = il.DefineLabel();
                    var skipDefaultValueFalseLabel = il.DefineLabel();
                    var skipDefaultValueTrueAndHasValueLabel = il.DefineLabel();
                    
                    var successLocal = il.DeclareLocal(_boolType);
                    var hasNullableValue = il.DeclareLocal(_boolType);

                    var hasValueMethod = isNullable ? originPropType.GetMethod("get_HasValue") : null;

                    il.Emit(OpCodes.Ldc_I4, 0);
                    il.Emit(OpCodes.Stloc, successLocal);

                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Stloc, hasNullableValue);

                    if (isNullable)
                    {
                        var hasNullableValueLabel = il.DefineLabel();
                        il.Emit(OpCodes.Ldloca, nullablePropValue);
                        il.Emit(OpCodes.Call, hasValueMethod);
                        il.Emit(OpCodes.Brfalse, hasNullableValueLabel);

                        il.Emit(OpCodes.Ldc_I4_1);
                        il.Emit(OpCodes.Stloc, hasNullableValue);

                        il.MarkLabel(hasNullableValueLabel);
                    }

                    il.Emit(OpCodes.Ldloc, skipDefaultValue);
                    il.Emit(OpCodes.Brfalse, skipDefaultValueTrueLabel);

                    if (isNullable) {
                        il.Emit(OpCodes.Ldloca, nullablePropValue);
                        il.Emit(OpCodes.Call, hasValueMethod);
                        il.Emit(OpCodes.Brfalse, propNullLabel);
                    }

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

                        } else il.Emit(OpCodes.Beq, propNullLabel);
                    }

                    WritePropertyForType(typeBuilder, il, hasValue, counter, nameLocal, propType, propValue, true, isNullable, hasNullableValue);

                    il.Emit(OpCodes.Ldc_I4, 1);
                    il.Emit(OpCodes.Stloc, successLocal);

                    il.MarkLabel(propNullLabel);

                    il.MarkLabel(skipDefaultValueTrueLabel);


                    il.Emit(OpCodes.Ldloc, skipDefaultValue);
                    il.Emit(OpCodes.Brtrue, skipDefaultValueFalseLabel);
                    il.Emit(OpCodes.Ldloc, successLocal);
                    il.Emit(OpCodes.Brtrue, skipDefaultValueFalseLabel);

                    WritePropertyForType(typeBuilder, il, hasValue, counter, nameLocal, propType, propValue, false, isNullable, hasNullableValue);

                    il.Emit(OpCodes.Ldc_I4, 1);
                    il.Emit(OpCodes.Stloc, successLocal);

                    il.MarkLabel(skipDefaultValueFalseLabel);

                    if (isNullable) {
                        il.Emit(OpCodes.Ldloc, skipDefaultValue);
                        il.Emit(OpCodes.Brfalse, skipDefaultValueTrueAndHasValueLabel);
                        il.Emit(OpCodes.Ldloca, nullablePropValue);
                        il.Emit(OpCodes.Call, hasValueMethod);
                        il.Emit(OpCodes.Brfalse, skipDefaultValueTrueAndHasValueLabel);
                        il.Emit(OpCodes.Ldloc, successLocal);
                        il.Emit(OpCodes.Brtrue, skipDefaultValueTrueAndHasValueLabel);

                        WritePropertyForType(typeBuilder, il, hasValue, counter, nameLocal, propType, propValue, true, isNullable, hasNullableValue);

                        il.MarkLabel(skipDefaultValueTrueAndHasValueLabel);
                    }
                }
                counter++;
            }

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_S, ObjectClose);
            il.Emit(OpCodes.Callvirt, _stringBuilderAppendChar);
            il.Emit(OpCodes.Pop);
        }

        private static void WritePropertyForType(TypeBuilder typeBuilder, ILGenerator il, LocalBuilder hasValue, int counter, LocalBuilder name, Type propType, LocalBuilder propValue, bool skipDefault, bool nullable, LocalBuilder hasNullableValue) {
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
            il.Emit(OpCodes.Ldloc, name);
            il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
            LoadQuotChar(il);
            il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
            il.Emit(OpCodes.Ldc_I4, ColonChr);
            il.Emit(OpCodes.Callvirt, _stringBuilderAppendChar);
            il.Emit(OpCodes.Pop);


            if ((propType == _intType || propType == _longType) && !nullable) {
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldloc, propValue);

                il.Emit(OpCodes.Call, propType == _longType ? _generatorLongToStr : _generatorIntToStr);
                il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                il.Emit(OpCodes.Pop);
            }
            else {

                var needLabel = !skipDefault && nullable;
                var writeNullForSkippedNullable = il.DefineLabel();
                var writeNormal = il.DefineLabel();
                var isSkippedValue = il.DeclareLocal(_boolType);

                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Stloc, isSkippedValue);

                il.Emit(OpCodes.Ldc_I4, skipDefault ? 1 : 0);
                il.Emit(OpCodes.Brtrue, writeNullForSkippedNullable);
                il.Emit(OpCodes.Ldc_I4, nullable ? 1 : 0);
                il.Emit(OpCodes.Brfalse, writeNullForSkippedNullable);
                il.Emit(OpCodes.Ldloc, hasNullableValue);
                il.Emit(OpCodes.Brtrue, writeNullForSkippedNullable);

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldstr, NullStr);
                il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                il.Emit(OpCodes.Pop);

                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Stloc, isSkippedValue);

                il.MarkLabel(writeNullForSkippedNullable);

                // If value is not skipped due to skip default and not having a value
                il.Emit(OpCodes.Ldloc, isSkippedValue);
                il.Emit(OpCodes.Brtrue, writeNormal);

                il.Emit(OpCodes.Ldloc, propValue);

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Call, WriteSerializeMethodFor(typeBuilder, propType));

                il.MarkLabel(writeNormal);
            }

            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Stloc, hasValue);
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
            } else if (type.GetTypeInfo().IsEnum)
                il.Emit(OpCodes.Ldc_I4_0);
        }

        internal static NetJSONSerializer<T> GetSerializer<T>() {
            return NetJSONCachedSerializer<T>.Serializer;
        }

        delegate object DeserializeWithTypeDelegate(string value);
        delegate string SerializeWithTypeDelegate(object value);

        delegate object DeserializeWithTypeSettingsDelegate(string value, NetJSONSettings settings);
        delegate string SerializeWithTypeSettingsDelegate(object value, NetJSONSettings settings);

        static ConcurrentDictionary<string, DeserializeWithTypeDelegate> _deserializeWithTypes =
            new ConcurrentDictionary<string, DeserializeWithTypeDelegate>();

        static ConcurrentDictionary<Type, SerializeWithTypeDelegate> _serializeWithTypes =
            new ConcurrentDictionary<Type, SerializeWithTypeDelegate>();

        static ConcurrentDictionary<Type, SerializeWithTypeSettingsDelegate> _serializeWithTypesSettings =
            new ConcurrentDictionary<Type, SerializeWithTypeSettingsDelegate>();

        static ConcurrentDictionary<string, DeserializeWithTypeSettingsDelegate> _deserializeWithTypesSettings =
            new ConcurrentDictionary<string, DeserializeWithTypeSettingsDelegate>();

        static MethodInfo _getSerializerMethod = _jsonType.GetMethod("GetSerializer", BindingFlags.NonPublic | BindingFlags.Static);
        static Type _netJSONSerializerType = typeof(NetJSONSerializer<>);

        /// <summary>
        /// Serialize value using the specified type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Serialize(Type type, object value) {
            if (value == null)
            {
                return NullStr;
            }
            
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
                    if (type.IsClass())
                        il.Emit(OpCodes.Isinst, type);
                    else il.Emit(OpCodes.Unbox_Any, type);

                    il.Emit(OpCodes.Callvirt, genericSerialize);

                    il.Emit(OpCodes.Ret);

                    return method.CreateDelegate(typeof(SerializeWithTypeDelegate)) as SerializeWithTypeDelegate;
                }
            })(value);
        }

        /// <summary>
        /// Serialize value using the specified type and settings
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static string Serialize(Type type, object value, NetJSONSettings settings)
        {
            if (value == null)
            {
                return NullStr;
            }
            
            return _serializeWithTypesSettings.GetOrAdd(type, _ => {
                lock (GetDictLockObject("SerializeTypeSetting", type.Name))
                {
                    var name = String.Concat(SerializeStr + "Settings", type.FullName);
                    var method = new DynamicMethod(name, _stringType, new[] { _objectType, _settingsType }, restrictedSkipVisibility: true);

                    var il = method.GetILGenerator();
                    var genericMethod = _getSerializerMethod.MakeGenericMethod(type);
                    var genericType = _netJSONSerializerType.MakeGenericType(type);

                    var genericSerialize = genericType.GetMethod(SerializeStr, new[] { type, _settingsType });

                    il.Emit(OpCodes.Call, genericMethod);

                    il.Emit(OpCodes.Ldarg_0);
                    if (type.IsClass())
                        il.Emit(OpCodes.Isinst, type);
                    else il.Emit(OpCodes.Unbox_Any, type);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Callvirt, genericSerialize);

                    il.Emit(OpCodes.Ret);

                    return method.CreateDelegate(typeof(SerializeWithTypeSettingsDelegate)) as SerializeWithTypeSettingsDelegate;
                }
            })(value, settings);
        }

        /// <summary>
        /// Serialize value using the underlying type of specified value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Serialize(object value) {
            if (value == null)
            {
                return NullStr;
            }
            
            return Serialize(value.GetType(), value);
        }

        /// <summary>
        /// Deserialize json to specified type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object Deserialize(Type type, string value) {
            if (value == null)
            {
                return null;
            }
            
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

                    if (type.IsClass())
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
        /// Deserialize json to specified type and settings
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static object Deserialize(Type type, string value, NetJSONSettings settings)
        {
            if (value == null)
            {
                return null;
            }
            
            return _deserializeWithTypesSettings.GetOrAdd(type.FullName, _ => {
                lock (GetDictLockObject("DeserializeTypeSettings", type.Name))
                {
                    var name = String.Concat(DeserializeStr + "Settings", type.FullName);
                    var method = new DynamicMethod(name, _objectType, new[] { _stringType, _settingsType }, restrictedSkipVisibility: true);

                    var il = method.GetILGenerator();
                    var genericMethod = _getSerializerMethod.MakeGenericMethod(type);
                    var genericType = _netJSONSerializerType.MakeGenericType(type);

                    var genericDeserialize = genericType.GetMethod(DeserializeStr, new[] { _stringType, _settingsType });

                    il.Emit(OpCodes.Call, genericMethod);

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Callvirt, genericDeserialize);

                    if (type.IsClass())
                        il.Emit(OpCodes.Isinst, type);
                    else
                    {
                        il.Emit(OpCodes.Box, type);
                    }

                    il.Emit(OpCodes.Ret);

                    return method.CreateDelegate(typeof(DeserializeWithTypeSettingsDelegate)) as DeserializeWithTypeSettingsDelegate;
                }
            })(value, settings);
        }

        /// <summary>
        /// Register serializer primitive method for <typeparamref name="T"/> when object type is encountered
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializeFunc"></param>
        public static void RegisterTypeSerializer<T>(Func<T, string> serializeFunc) {
            var type = typeof(T);

            if (serializeFunc == null)
                throw new InvalidOperationException("serializeFunc cannot be null");

            var method =
#if !NET_STANDARD
                serializeFunc.Method
#else
                serializeFunc.GetMethodInfo()
#endif
                ;

            if (!(method.IsPublic && method.IsStatic)) {
                throw new InvalidOperationException("serializeFun must be a public and static method");
            }

            _registeredSerializerMethods[type] = method;
        }

        /// <summary>
        /// Register serializer for any custom user defined type with exclusion to enums for <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializeFunc"></param>
        public static void RegisterCustomTypeSerializer<T>(Action<T, StringBuilder, NetJSONSettings> serializeFunc)
        {
            var type = typeof(T);

            if (serializeFunc == null)
                throw new InvalidOperationException("serializeFunc cannot be null");

            var method =
#if !NET_STANDARD
                serializeFunc.Method
#else
                serializeFunc.GetMethodInfo()
#endif
                ;

            if (!(method.IsPublic && method.IsStatic))
            {
                throw new InvalidOperationException("serializeFun must be a public and static method");
            }

            _registeredCustomSerializerMethods[type.GetName().Fix()] = method;
        }
        
        /// <summary>
        /// Register deserializer for any custom user defined type with exclusion to enums for <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="deserializeFunc"></param>
        public static void RegisterCustomTypeDeserializer<T>(DeserializeCustomTypeDelegate<T> deserializeFunc)
        {
            var type = typeof(T);

            if (deserializeFunc == null)
                throw new InvalidOperationException("serializeFunc cannot be null");

            var method =
#if !NET_STANDARD
                deserializeFunc.Method
#else
                deserializeFunc.GetMethodInfo()
#endif
                ;

            if (!(method.IsPublic && method.IsStatic))
            {
                throw new InvalidOperationException("serializeFun must be a public and static method");
            }

            _registeredCustomDeserializerMethods[type.GetName().Fix()] = method;
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
        /// Serialize value using underlying object type
        /// </summary>
        /// <param name="value">Object value</param>
        /// <param name="settings">Settings</param>
        /// <returns>String</returns>
        public static string SerializeObject(object value, NetJSONSettings settings)
        {
            if (value == null)
            {
                return NullStr;
            }
            
            return Serialize(value.GetType(), value, settings);
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

        private static MethodInfo GenerateExtractObject(TypeBuilder type) {
            MethodInfo method;
            var key = "ExtractObjectValue";
            if (_readMethodBuilders.TryGetValue(key, out method))
                return method;

            method = type.DefineMethodEx("ExtractObjectValue", StaticMethodAttribute, _objectType,
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

                //value = DecodeJSONString(json, ref index)     
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Call, _decodeJSONString);

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

        private static void ILFixedWhile(ILGenerator il, Action<ILGenerator, LocalBuilder, LocalBuilder, Label, Label> whileAction,
            bool needBreak = false, Action<ILGenerator> returnAction = null,
            Action<ILGenerator, LocalBuilder> beforeAction = null,
            Action<ILGenerator, LocalBuilder> beginIndexIf = null,
            Action<ILGenerator, LocalBuilder> endIndexIf = null) {

            var current = il.DeclareLocal(_charType);
            var ptr = il.DeclareLocal(_charPtrType);
            
            var startLoop = il.DefineLabel();
            var @break = needBreak ? il.DefineLabel() : default(Label);
            var invalidIfNull = il.DefineLabel();

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

            // Throw invalid json if needed
            /*il.Emit(OpCodes.Ldc_I4, (int)'\0');
            il.Emit(OpCodes.Ldloc, current);
            il.Emit(OpCodes.Bne_Un, invalidIfNull);

            il.Emit(OpCodes.Newobj, _invalidJSONCtor);
            il.Emit(OpCodes.Throw);

            il.MarkLabel(invalidIfNull);*/

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

        [ThreadStatic]
        static StringBuilder _decodeJSONStringBuilder;

        internal unsafe static string DecodeJSONString(char* ptr, ref int index, NetJSONSettings settings, bool fromObject = false) {
            char current = '\0', next = '\0', prev = '\0';
            bool hasQuote = false;
            bool isJustString = index == 0 && !fromObject;
            var sb = (_decodeJSONStringBuilder ?? (_decodeJSONStringBuilder = new StringBuilder())).Clear();

            // Don't process null string
            if((IntPtr)ptr == IntPtr.Zero)
            {
                return null;
            }

            while (true) {
                current = ptr[index];
                if(current == '\0')
                {
                    break;
                }

                if (isJustString || hasQuote) {
                    if (!isJustString && current == settings._quoteChar)
                    {
                        next = ptr[index + 1];
                        if (next != ',' && next != ' ' && next != ':' && next != '\n' && next != '\r' && next != '\t' && next != ']' && next != '}' && next != '\0')
                        {
                            throw new NetJSONInvalidJSONException();
                        }

                        ++index;
                        break;
                    }
                    else
                    {
                        if (current != '\\')
                        {
                            if (isJustString)
                            {
                                if(current == settings._quoteChar && !hasQuote)
                                {
                                    hasQuote = true;
                                }
                                else
                                {
                                    if (ptr[index + 1] != '\0')
                                    {
                                        sb.Append(current);
                                    }
                                    else
                                    {
                                        if(current != settings._quoteChar)
                                        {
                                            sb.Append(current);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                sb.Append(current);
                            }
                        }
                        else
                        {
                            next = ptr[++index];
                            switch (next)
                            {
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
                                    
                                    if(uu < offset)
                                    {
                                        sb.Append((char)uu);
                                    }
                                    else
                                    {
                                        sb.Append((char)(((uu - offset) >> 10) + 0xD800))
                                            .Append((char)((uu - offset) % 0x0400 + 0xDC00));
                                    }

                                    index += 4;
                                    break;
                                default:
                                    if (next == settings._quoteChar)
                                        sb.Append(next);
                                    break;
                            }
                        }
                    }
                } else {
                    if (current == settings._quoteChar) {
                        hasQuote = true;
                    } else if (current == 'n') {
                        index += 3;
                        return null;
                    }
                }
                prev = current;
                ++index;
            }

            return sb.ToString();
        }

        internal static T InvokeCustomDeserializerForReader<T>(MethodInfo methodInfo, NetJSONStringReader reader, NetJSONSettings settings)
        {
            var typeName = typeof(T).GetName().Fix();
            return (T)_customDeserializerForNetJSONStringReaders.GetOrAdd(typeName, _ => {
                lock (GetDictLockObject("InvokeCustomDeserializerForReader"))
                {
                    var meth = new DynamicMethod(Guid.NewGuid().ToString("N"), _objectType, new Type[] { _netjsonStringReaderType, _settingsType }, restrictedSkipVisibility: true);

                    var il = meth.GetILGenerator();

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Call, methodInfo);
                    il.Emit(OpCodes.Ret);

                    return meth.CreateDelegate(typeof(Func<NetJSONStringReader, NetJSONSettings, object>)) as Func<NetJSONStringReader, NetJSONSettings, object>;
                }
            })(reader, settings);
        }

        internal static T ReaderDeserializer<T>(char* ptr, ref int index, NetJSONSettings settings)
        {
            var typeName = typeof(T).GetName().Fix();
            var method = _registeredCustomDeserializerMethods[typeName];
            var reader = new NetJSONStringReader(ptr, index);
            var result = InvokeCustomDeserializerForReader<T>(method, reader, settings);
            index += reader.counter + 1;
            return result;
        }

        private static MethodInfo GenerateExtractValueFor(TypeBuilder typeBuilder, Type type) {
            MethodInfo method;
            var key = string.Concat(type.FullName, typeBuilder == null ? Dynamic : string.Empty);
            var typeName = type.GetName().Fix();
            if (_extractMethodBuilders.TryGetValue(key, out method))
                return method;

            if (_registeredCustomDeserializerMethods.ContainsKey(typeName))
            {
                return _extractMethodBuilders[key] = _readerDeserializer.MakeGenericMethod(type);
            }

            var methodName = String.Concat(ExtractStr, typeName);
            var isObjectType = type == _objectType;
            method = typeBuilder.DefineMethodEx(methodName, StaticMethodAttribute,
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

            if (type.GetTypeInfo().IsEnum) {
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
                    il.Emit(OpCodes.Ldc_I4_0);
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
            } else if (type.GetTypeInfo().IsEnum)
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
            MethodInfo method;
            var key = string.Concat(type.FullName, typeBuilder == null ? Dynamic : string.Empty);
            var typeName = type.GetName().Fix();
            if (_setValueMethodBuilders.TryGetValue(key, out method))
                return method;

            var isTypeValueType = type.GetTypeInfo().IsValueType;
            var methodName = String.Concat(SetStr, typeName);
            var isObjectType = type == _objectType;
            method = typeBuilder.DefineMethodEx(methodName, StaticMethodAttribute,
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

                        GenerateTypeSetValueFor(typeBuilder, pType, pType.GetTypeInfo().IsValueType, Optimized, il);

                        il.MarkLabel(compareLabel);
                    }
                }
            }

            il.Emit(OpCodes.Ret);

            return method;
        }

        delegate void SetterPropertyDelegate<T>(ref T instance, object value, MethodInfo methodInfo);
        delegate void SetterFieldDelegate<T>(ref T instance, object value, FieldInfo fieldInfo);
        public delegate T DeserializeCustomTypeDelegate<T>(NetJSONStringReader reader, NetJSONSettings settings);

        internal static void SetterPropertyValue<T>(ref T instance, object value, MethodInfo methodInfo) {
            (_setMemberValues.GetOrAdd(methodInfo, key => {
                lock (GetDictLockObject("SetDynamicMemberValue")) {
                    var propType = key.GetParameters()[0].ParameterType;

                    var type = key.DeclaringType;

                    var isClass = type.IsClass();
                    var name = String.Concat(type.Name, "_", key.Name);
                    var meth = new DynamicMethod(name + "_setPropertyValue", _voidType, new[] { type.MakeByRefType(), 
                    _objectType, _methodInfoType }, GetManifestModule(type), true);

                    var il = meth.GetILGenerator();

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);

                    if (propType.GetTypeInfo().IsValueType)
                        il.Emit(OpCodes.Unbox_Any, propType);
                    else
                        il.Emit(OpCodes.Isinst, propType);

                    il.Emit(isClass ? OpCodes.Callvirt : OpCodes.Call, key);

                    il.Emit(OpCodes.Ret);

                    return meth.CreateDelegate(typeof(SetterPropertyDelegate<T>));
                }
            }) as SetterPropertyDelegate<T>)(ref instance, value, methodInfo);
        }

        private static Module GetManifestModule(Type objType = null)
        {
                return
#if NET_STANDARD
    objType.GetTypeInfo().Assembly.ManifestModule
#else
    Assembly.GetExecutingAssembly().ManifestModule
#endif
                        ;
        }

        internal static void SetterFieldValue<T>(ref T instance, object value, FieldInfo fieldInfo)
        {
            (_setMemberFieldValues.GetOrAdd(fieldInfo, key => {
                lock (GetDictLockObject("SetDynamicFieldMemberValue"))
                {
                    var propType = key.FieldType;

                    var type = key.DeclaringType;

                    var isClass = type.IsClass();
                    var name = String.Concat(type.Name, "_", key.Name);
                    var meth = new DynamicMethod(name + "_setFieldValue", _voidType, new[] { type.MakeByRefType(),
                    _objectType, _fieldInfoType }, GetManifestModule(type), true);

                    var il = meth.GetILGenerator();

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);

                    if (propType.GetTypeInfo().IsValueType)
                        il.Emit(OpCodes.Unbox_Any, propType);
                    else
                        il.Emit(OpCodes.Isinst, propType);

                    il.Emit(OpCodes.Stfld, key);

                    il.Emit(OpCodes.Ret);

                    return meth.CreateDelegate(typeof(SetterFieldDelegate<T>));
                }
            }) as SetterFieldDelegate<T>)(ref instance, value, fieldInfo);
        }

        private static void GenerateTypeSetValueFor(TypeBuilder typeBuilder, Type type, bool isTypeValueType, bool Optimized, ILGenerator il) {

            var props = type.GetTypeProperties();
            var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
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
                } else
                {
                    var propValue = il.DeclareLocal(originPropType);
                    var isValueType = propType.GetTypeInfo().IsValueType;
                    var isPrimitiveType = propType.IsPrimitiveType();
                    var isStruct = isValueType && !isPrimitiveType;
                    var isBool = propType == _boolType;
                    var propNullLabel = !isNullable && !isBool ? il.DefineLabel() : default(Label);
                    var skipDefaultFalseLabel = !isNullable && !isBool ? il.DefineLabel() : default(Label);
                    var hasPropLabel = propNullLabel != default(Label);
                    var nullablePropValue = isNullable ? il.DeclareLocal(originPropType) : null;
                    var equalityMethod = propType.GetMethod("op_Equality");


                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldarg, 4);
                    il.Emit(OpCodes.Call, GenerateExtractValueFor(typeBuilder, originPropType));

                    il.Emit(OpCodes.Stloc, propValue);

                    if (hasPropLabel)
                    {
                        if (isStruct)
                            il.Emit(OpCodes.Ldloca, propValue);
                        else
                            il.Emit(OpCodes.Ldloc, propValue);

                        if (isValueType && isPrimitiveType)
                        {
                            LoadDefaultValueByType(il, propType);
                        }
                        else
                        {
                            if (!isValueType)
                                il.Emit(OpCodes.Ldnull);
                        }

                        if (equalityMethod != null)
                        {
                            il.Emit(OpCodes.Call, equalityMethod);
                            il.Emit(OpCodes.Brtrue, propNullLabel);
                            il.Emit(OpCodes.Ldarg, 4);
                            il.Emit(OpCodes.Callvirt, _settingsSkipDefaultValue);
                            il.Emit(OpCodes.Brfalse, propNullLabel);
                        }
                        else
                        {
                            if (isStruct)
                            {

                                var tempValue = il.DeclareLocal(propType);

                                il.Emit(OpCodes.Ldloca, tempValue);
                                il.Emit(OpCodes.Initobj, propType);
                                il.Emit(OpCodes.Ldloc, tempValue);
                                il.Emit(OpCodes.Box, propType);
                                il.Emit(OpCodes.Constrained, propType);

                                il.Emit(OpCodes.Callvirt, _objectEquals);

                                il.Emit(OpCodes.Brtrue, propNullLabel);
                                il.Emit(OpCodes.Ldarg, 4);
                                il.Emit(OpCodes.Callvirt, _settingsSkipDefaultValue);
                                il.Emit(OpCodes.Brfalse, propNullLabel);
                            }
                            else
                            {
                                il.Emit(OpCodes.Beq, propNullLabel);
                                il.Emit(OpCodes.Ldarg, 4);
                                il.Emit(OpCodes.Callvirt, _settingsSkipDefaultValue);
                                il.Emit(OpCodes.Brfalse, propNullLabel);
                            }
                        }
                    }

                    SetClassMemberValue(type, isTypeValueType, il, fields, prop, field, setter, isProp, propName, propType, isNullable, propValue);

                    if (hasPropLabel)
                    {
                        il.MarkLabel(propNullLabel);
                    }

                    if (hasPropLabel)
                    {
                        il.Emit(OpCodes.Ldarg, 4);
                        il.Emit(OpCodes.Callvirt, _settingsSkipDefaultValue);
                        il.Emit(OpCodes.Brtrue, skipDefaultFalseLabel);

                        SetClassMemberValue(type, isTypeValueType, il, fields, prop, field, setter, isProp, propName, propType, isNullable, propValue);

                        il.MarkLabel(skipDefaultFalseLabel);
                    }
                }

                il.Emit(OpCodes.Ret);

                il.MarkLabel(conditionLabel);
            }
            
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg, 4);
            il.Emit(OpCodes.Call, _skipProperty);
        }

        private static void SetClassMemberValue(Type type, bool isTypeValueType, ILGenerator il, FieldInfo[] fields, PropertyInfo prop, FieldInfo field, MethodInfo setter, bool isProp, string propName, Type propType, bool isNullable, LocalBuilder propValue)
        {
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldloc, propValue);

            if (isProp)
            {
                if (setter != null)
                {
                    if (!setter.IsPublic)
                    {
                        if (propType.GetTypeInfo().IsValueType)
                            il.Emit(OpCodes.Box, isNullable ? prop.PropertyType : propType);
                        il.Emit(OpCodes.Ldtoken, setter);
                        il.Emit(OpCodes.Call, _methodGetMethodFromHandle);
                        il.Emit(OpCodes.Call, _setterPropertyValueMethod.MakeGenericMethod(type));
                    }
                    else
                        il.Emit(isTypeValueType ? OpCodes.Call : OpCodes.Callvirt, setter);
                }
                else
                {
                    var setField = type.GetField($"<{propName}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (setField == null)
                    {
                        //TODO: Use IL body and field token from prop.GetGetMethod().GetMethodBody()
                        setField = fields.FirstOrDefault(x => x.Name.EndsWith(propName, StringComparison.OrdinalIgnoreCase));
                    }

                    if (setField != null)
                    {
                        if (propType.GetTypeInfo().IsValueType)
                            il.Emit(OpCodes.Box, propType);
                        il.Emit(OpCodes.Ldtoken, setField);
                        il.Emit(OpCodes.Call, _methodGetFieldFromHandle);
                        il.Emit(OpCodes.Call, _setterFieldValueMethod.MakeGenericMethod(type));
                    }
                    else
                    {
                        il.Emit(OpCodes.Pop);
                        il.Emit(OpCodes.Pop);
                    }
                }

            }
            else il.Emit(OpCodes.Stfld, field);

            il.Emit(OpCodes.Ret);
        }

        private static MethodInfo GenerateCreateListFor(TypeBuilder typeBuilder, Type type) {
            MethodInfo method;
            var key = string.Concat(type.FullName, typeBuilder == null ? Dynamic : string.Empty);
            var typeName = type.GetName().Fix();
            if (_createListMethodBuilders.TryGetValue(key, out method))
                return method;

            if (_registeredCustomDeserializerMethods.ContainsKey(typeName))
            {
                return _createListMethodBuilders[key] = _readerDeserializer.MakeGenericMethod(type);
            }

            var methodName = String.Concat(CreateListStr, typeName);
            var isObjectType = type == _objectType;
            method = typeBuilder.DefineMethodEx(methodName, StaticMethodAttribute,
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
            var isStringBased = isStringType || nullableType == _timeSpanType || isByteArray || elementType == _guidType || elementType == _charType;
            var isCollectionType = !isArray && !_listType.IsAssignableFrom(type) && !(type.Name == IEnumerableStr) && !(type.Name == IListStr) && !(type.Name == ICollectionStr) && !(type.Name == IReadOnlyCollectionStr) && !(type.Name == IReadOnlyListStr);

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

            if (nullableType.GetTypeInfo().IsEnum) {
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
                    
                    GenerateUpdateCurrent(il, current, ptr);
                    il.MarkLabel(isStringBasedLabel1);

                    il.Emit(OpCodes.Ldloc, isStringBasedLocal);
                    il.Emit(OpCodes.Brtrue, isStringBasedLabel2);
                    GenerateCreateListForNonStringBased(typeBuilder, il, elementType, settings, obj, addMethod, current);
                    
                    GenerateUpdateCurrent(il, current, ptr);
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
                il.Emit(OpCodes.Ldc_I4_0);
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

		internal static bool IsListType(this Type type) {
			Type interfaceType = null;
			//Skip type == typeof(String) since String is same as IEnumerable<Char>
			return type != _stringType && (_listType.IsAssignableFrom(type) || type.Name == IListStr ||
				(type.Name == ICollectionStr && type.GetGenericArguments()[0].Name != KeyValueStr) ||
				(type.Name == IEnumerableStr && type.GetGenericArguments()[0].Name != KeyValueStr) ||
                (type.Name == IReadOnlyCollectionStr && type.GetGenericArguments()[0].Name != KeyValueStr) ||
                (type.Name == IReadOnlyListStr && type.GetGenericArguments()[0].Name != KeyValueStr) ||
                ((interfaceType = type.GetTypeInfo().GetInterface(ICollectionStr)) != null && interfaceType.GetGenericArguments()[0].Name != KeyValueStr) ||
				((interfaceType = type.GetTypeInfo().GetInterface(IEnumerableStr)) != null && interfaceType.GetGenericArguments()[0].Name != KeyValueStr));
		}

		internal static bool IsDictionaryType(this Type type) {
			Type interfaceType = null;
			return _dictType.IsAssignableFrom(type) || type.Name == IDictStr || type.Name == IReadOnlyDictionaryStr
                || ((interfaceType = type.GetTypeInfo().GetInterface(IEnumerableStr)) != null && interfaceType.GetGenericArguments()[0].Name == KeyValueStr);
		}

		private static MethodInfo GenerateGetClassOrDictFor(TypeBuilder typeBuilder, Type type) {
            MethodInfo method;
            var key = string.Concat(type.FullName, typeBuilder == null ? Dynamic : string.Empty);
            var typeName = type.GetName().Fix();
            if (_readMethodBuilders.TryGetValue(key, out method))
                return method;

            if (_registeredCustomDeserializerMethods.ContainsKey(typeName))
            {
                return _readMethodBuilders[key] = _readerDeserializer.MakeGenericMethod(type);
            }

            var methodName = String.Concat(CreateClassOrDictStr, typeName);
            var isObjectType = type == _objectType;
            method = typeBuilder.DefineMethodEx(methodName, StaticMethodAttribute,
                type, new[] { _charPtrType, _intType.MakeByRefType(), _settingsType });
            _readMethodBuilders[key] = method;

            
            var il = method.GetILGenerator();

            var nullableType = type.GetNullableType();
            var isNullable = nullableType != null;
            type = isNullable ? nullableType : type;

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
            var isNotDictOrClass = il.DefineLabel();

            var isNotDictOrClassStartChar = il.DefineLabel();


            var isDict = type.IsDictionaryType();
            var arguments = isDict ? type.GetGenericArguments() : null;
            var hasArgument = arguments != null;
            var keyType = hasArgument ? (arguments.Length > 0 ? arguments[0] : null) : _objectType;
            var valueType = hasArgument && arguments.Length > 1 ? arguments[1] : _objectType;
            var isKeyValuePair = false;
            var isExpandoObject = type == _expandoObjectType;
            ConstructorInfo selectedCtor = null;

            if (isDict && keyType == null) {
                var baseType = type.GetTypeInfo().BaseType;
                if (baseType == _objectType) {
                    baseType = type.GetTypeInfo().GetInterface(IEnumerableStr);
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


            var isTuple = type.GetTypeInfo().IsGenericType && type.Name.StartsWith("Tuple");
            var tupleType = isTuple ? type : null;
            var tupleArguments = tupleType != null ? tupleType.GetGenericArguments() : null;
            var tupleCount = tupleType != null ? tupleArguments.Length : 0;

            if (isTuple) {
                type = _tupleContainerType;
            }

            var obj = il.DeclareLocal(type);
            var isStringType = isTuple || isDict || keyType == _stringType || keyType == _objectType || keyType == _guidType;
            var isTypeValueType = type.GetTypeInfo().IsValueType;
            var tupleCountLocal = isTuple ? il.DeclareLocal(_intType) : null;
            var isStringTypeLocal = il.DeclareLocal(_boolType);

            MethodInfo addMethod = null;

            var isNotTagLabel = il.DefineLabel();
            
            if(isDict && type.Name == IReadOnlyDictionaryStr)
            {
                // Map readonly dictionary to regular dictionary to allow operations for adding items to it
                type = _genericDictType.MakeGenericType(keyType, valueType);
            }
            
            var dictSetItem = isDict ? (isKeyValuePair ? 
                ((addMethod = type.GetMethod("Add")) != null ? addMethod :
                (addMethod = type.GetMethod("Enqueue")) != null ? addMethod :
                (addMethod = type.GetMethod("Push")) != null ? addMethod : null)
                : type.GetMethod("set_Item")) : null;

            if (isExpandoObject) 
                dictSetItem = _idictStringObject.GetMethod("Add");

            if (isDict) {
                if (type.Name == IDictStr || type.Name == IReadOnlyDictionaryStr) {
                    type = _genericDictType.MakeGenericType(keyType, valueType);
                }
            }

            il.Emit(OpCodes.Ldc_I4, isStringType ? 1 : 0);
            il.Emit(OpCodes.Stloc, isStringTypeLocal);


            if (keyType.GetTypeInfo().IsEnum) {
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Callvirt, _settingsUseEnumStringProp);
                il.Emit(OpCodes.Stloc, isStringTypeLocal);
            }

            if (tupleCountLocal != null) {
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Stloc, tupleCountLocal);
            }
            
            if (isTuple)
            {
                il.Emit(OpCodes.Ldc_I4, tupleCount);
                il.Emit(OpCodes.Newobj, type.GetConstructor(new[] { _intType }));
                il.Emit(OpCodes.Stloc, obj);
            }
            else
            {
                var typeInfo = type.GetTypeInfo();
                if (typeInfo.IsInterface || typeInfo.IsAbstract)
                {
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Stloc, obj);
                }
                else
                {
                    var ctor = type.GetConstructor(Type.EmptyTypes);
                    if (ctor == null)
                    {
                        if (typeInfo.IsInterface || typeInfo.IsAbstract)
                        {
                            il.Emit(OpCodes.Ldnull);
                            il.Emit(OpCodes.Stloc, obj);
                        }
                        else
                        {
                            selectedCtor = type.GetConstructors().OrderBy(x => x.GetParameters().Length).LastOrDefault();
                            if (isTypeValueType)
                            {
                                il.Emit(OpCodes.Ldloca, obj);
                                il.Emit(OpCodes.Initobj, type);
                            }
                            else
                            {
                                il.Emit(OpCodes.Call, _getUninitializedInstance.MakeGenericMethod(type));
                                il.Emit(OpCodes.Stloc, obj);
                            }
                        }
                    }
                    else
                    {
                        if (isTypeValueType)
                        {
                            il.Emit(OpCodes.Ldloca, obj);
                            il.Emit(OpCodes.Initobj, type);
                        }
                        else
                        {
                            il.Emit(OpCodes.Newobj, ctor);//NewObjNoctor
                            il.Emit(OpCodes.Stloc, obj);
                        }
                    }
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

                //il.Emit(OpCodes.Ldloc, current);
                //il.Emit(OpCodes.Ldc_I4, (int)' ');
                //il.Emit(OpCodes.Beq, countLabel);

                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Stloc, isTag);

                //if current != '{' throw exception
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ldloc, count);
                il.Emit(OpCodes.Bne_Un, isNotDictOrClass);

                il.Emit(OpCodes.Ldc_I4, (int)'{');
                il.Emit(OpCodes.Ldloc, current);
                il.Emit(OpCodes.Beq, isNotDictOrClass);


                // ignore whitespace and ":"
                il.Emit(OpCodes.Ldc_I4, (int)' ');
                il.Emit(OpCodes.Ldloc, current);
                il.Emit(OpCodes.Beq, isNotDictOrClassStartChar);

                il.Emit(OpCodes.Ldc_I4, (int)':');
                il.Emit(OpCodes.Ldloc, current);
                il.Emit(OpCodes.Beq, isNotDictOrClassStartChar);

                il.Emit(OpCodes.Ldc_I4, (int)'n');
                il.Emit(OpCodes.Ldloc, current);
                il.Emit(OpCodes.Beq, isNotDictOrClassStartChar);

                il.Emit(OpCodes.Newobj, _typeMismatchExceptionCtor);
                il.Emit(OpCodes.Throw);

                il.MarkLabel(isNotDictOrClassStartChar);

                il.MarkLabel(isNotDictOrClass);


                //if (count == 0 && current == 'n') {
                //    index += 4;
                //    return null;
                //}
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ldloc, count);
                il.Emit(OpCodes.Bne_Un, isNullObjectLabel);

                il.Emit(OpCodes.Ldc_I4, (int)'n');
                il.Emit(OpCodes.Ldloc, current);
                il.Emit(OpCodes.Bne_Un, isNullObjectLabel);

                IncrementIndexRef(il, count: 4);

                if (isTypeValueType) {
                    var nullLocal = il.DeclareLocal(type);

                    il.Emit(OpCodes.Ldloca, nullLocal);
                    il.Emit(OpCodes.Initobj, type);

                    il.Emit(OpCodes.Ldloc, nullLocal);
                } else {
                    il.Emit(OpCodes.Ldnull);
                }

                if (isNullable)
                {
                    il.Emit(OpCodes.Newobj, _nullableType.MakeGenericType(type).GetConstructor(new[] { type }));
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
                        var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                        var paramProps = props.Where(x => parameters.Any(y => y.Name.Equals(x.Member.Name, StringComparison.OrdinalIgnoreCase)));
                        var excludedParams = props.Where(x => !parameters.Any(y => y.Name.Equals(x.Member.Name, StringComparison.OrdinalIgnoreCase)));

                        if (paramProps.Any()) {
                            if (isTypeValueType)
                            {
                                il.Emit(OpCodes.Ldloca, sObj);
                            }

                            foreach (var parameter in paramProps)
                            {
                                il.Emit(isTypeValueType ? OpCodes.Ldloca : OpCodes.Ldloc, obj);
                                GetMemberInfoValue(il, parameter);
                            }
                            if (isTypeValueType)
                            {
                                il.Emit(OpCodes.Call, selectedCtor);
                            }
                            else
                            {
                                il.Emit(OpCodes.Newobj, selectedCtor);
                                il.Emit(OpCodes.Stloc, sObj);
                            }
                            

                            //Set field/prop not accounted for in constructor parameters
                            foreach (var param in excludedParams) {
                                il.Emit(OpCodes.Ldloc, sObj);
                                il.Emit(OpCodes.Ldloc, obj);
                                GetMemberInfoValue(il, param);
                                var prop = param.Member.MemberType == MemberTypes.Property ? param.Member as PropertyInfo : null;
                                if (prop != null) {
                                    var propName = prop.Name;
                                    var setter = prop.GetSetMethod();
                                    if (setter == null) {
                                        setter = type.GetMethod(string.Concat("set_", prop.Name), MethodBinding);
                                    }
                                    var propType = prop.PropertyType;

                                    if (setter == null)
                                    {
                                        var setField = type.GetField($"<{propName}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
                                        if (setField == null)
                                        {
                                            setField = fields.FirstOrDefault(x => x.Name.EndsWith(propName, StringComparison.OrdinalIgnoreCase));
                                        }

                                        if (setField != null)
                                        {
                                            if (propType.GetTypeInfo().IsValueType)
                                                il.Emit(OpCodes.Box, propType);
                                            il.Emit(OpCodes.Ldtoken, setField);
                                            il.Emit(OpCodes.Call, _methodGetFieldFromHandle);
                                            il.Emit(OpCodes.Call, _setterFieldValueMethod.MakeGenericMethod(type));
                                        }
                                        else
                                        {
                                            il.Emit(OpCodes.Pop);
                                            il.Emit(OpCodes.Pop);
                                        }
                                    }
                                    else
                                    {
                                        if (!setter.IsPublic)
                                        {
                                            if (propType.GetTypeInfo().IsValueType)
                                                il.Emit(OpCodes.Box, propType);
                                            il.Emit(OpCodes.Ldtoken, setter);
                                            il.Emit(OpCodes.Call, _methodGetMethodFromHandle);
                                            il.Emit(OpCodes.Call, _setterPropertyValueMethod.MakeGenericMethod(type));
                                        }
                                        else
                                            il.Emit(isTypeValueType ? OpCodes.Call : OpCodes.Callvirt, setter);
                                    }
                                } else
                                    il.Emit(OpCodes.Stfld, (FieldInfo)param.Member);
                            }

                            il.Emit(OpCodes.Ldloc, sObj);
                        } else
                            il.Emit(OpCodes.Ldloc, obj);
                    }else
                        il.Emit(OpCodes.Ldloc, obj);
                }

                if (isNullable)
                {
                    il.Emit(OpCodes.Newobj, _nullableType.MakeGenericType(type).GetConstructor(new[] { type }));
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
            {
                il.Emit(prop.DeclaringType.IsClass() ? OpCodes.Callvirt : OpCodes.Call, prop.GetGetMethod());
            }
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
            var inCompleteQuoteLabel = il.DefineLabel();
            var isCurrentQuoteLabel = il.DefineLabel();
            var currentQuotePrevNotLabel = il.DefineLabel();
            var keyLocal = il.DeclareLocal(_stringType);

            var isCurrentLocal = il.DeclareLocal(_boolType);
            var hasOverrideLabel = il.DefineLabel();
            var hasOverrideLabel2 = il.DefineLabel();
            var notHasOverrideLabel = il.DefineLabel();

            var isStringBasedLocal = il.DeclareLocal(_boolType);

            il.Emit(OpCodes.Ldc_I4, keyType.IsStringBasedType() ? 1 : 0);
            il.Emit(OpCodes.Stloc, isStringBasedLocal);

            if (keyType.GetTypeInfo().IsEnum) {
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
            il.Emit(OpCodes.Brfalse, isCurrentQuoteLabel);

            //quotes++
            il.Emit(OpCodes.Ldloc, quotes);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, quotes);

            il.MarkLabel(isCurrentQuoteLabel);

            il.Emit(OpCodes.Ldloc, isCurrentLocal);

            //if(current == _ThreadQuoteChar && quotes == 0)

            //il.Emit(OpCodes.Ldloc, current);
            //il.Emit(OpCodes.Call, _IsCurrentAQuotMethod);


            il.Emit(OpCodes.Brfalse, currentQuoteLabel);

            il.Emit(OpCodes.Ldloc, quotes);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Bne_Un, currentQuoteLabel);

            //foundQuote = true
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Stloc, foundQuote);

            ////quotes++
            //il.Emit(OpCodes.Ldloc, quotes);
            //il.Emit(OpCodes.Ldc_I4_1);
            //il.Emit(OpCodes.Add);
            //il.Emit(OpCodes.Stloc, quotes);

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

            // Check quotes count if less than 2 then missing quotes (Throw Exception for missing quote)     
            il.Emit(OpCodes.Ldloc, quotes);
            il.Emit(OpCodes.Ldc_I4_2);
            il.Emit(OpCodes.Bge, inCompleteQuoteLabel);

            il.Emit(OpCodes.Newobj, _invalidJSONCtor);
            il.Emit(OpCodes.Throw);

            il.MarkLabel(inCompleteQuoteLabel);

            //Check if end character in range is ":"
            il.Emit(OpCodes.Ldloc, ptr);
            il.Emit(OpCodes.Ldloc, startIndex);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldind_I4);
            il.Emit(OpCodes.Ldloc, startIndex);
            il.Emit(OpCodes.Sub);
            il.Emit(OpCodes.Call, _failIfInvalidCharacter);

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

            //index++
            IncrementIndexRef(il);

            if (isDict) {
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
            if (tupleItemType.GetTypeInfo().IsValueType)
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


        static bool IsStringBasedType(this Type type) {
            var nullableType = type.GetNullableType() ?? type;
            type = nullableType;
            return type == _stringType || type == _charType || type == _typeType || type == _timeSpanType || type == _byteArrayType || type == _guidType;
        }

    }
}
