using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NetJSON {

    public abstract class NetJSONSerializer<T> {

        public abstract string Serialize(T value);
        public abstract T Deserialize(string value);

        public abstract void Serialize(T value, TextWriter writer);
        public abstract T Deserialize(TextReader reader);

    }

    public static class NetJSON {

        private static class NetJSONCachedSerializer<T> {
            public static readonly NetJSONSerializer<T> Serializer = (NetJSONSerializer<T>)Activator.CreateInstance(Generate(typeof(T)));
        }
        
        const string QuotChar = "\"";

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
            _stringType = typeof(String),
            _byteArrayType = typeof(byte[]),
            _charType = typeof(char),
            _charPtrType = typeof(char*),
            _guidType = typeof(Guid),
            _boolType = typeof(bool),
            _timeSpanType = typeof(TimeSpan),
            _stringBuilderType = typeof(StringBuilder),
            _listType = typeof(IList),
            _dictType = typeof(IDictionary),
            _genericDictType = typeof(Dictionary<,>),
            _genericListType = typeof(List<>),
            _objectType = typeof(Object),
            _nullableType = typeof(Nullable<>),
            _decimalType = typeof(decimal),
            _genericKeyValuePairType = typeof(KeyValuePair<,>),
            _serializerType = typeof(NetJSONSerializer<>),
            _genericDictionaryEnumerator =
                Type.GetType("System.Collections.Generic.Dictionary`2+Enumerator"),
            _genericListEnumerator =
                Type.GetType("System.Collections.Generic.List`1+Enumerator"),
            _typeType = typeof(Type),
            _voidType = typeof(void),
            _intType = typeof(int),
            _longType = typeof(long),
            _jsonType = typeof(NetJSON),
            _textWriterType = typeof(TextWriter),
            _textReaderType = typeof(TextReader);

        static readonly MethodInfo _stringBuilderToString =
            _stringBuilderType.GetMethod("ToString", Type.EmptyTypes),
            _stringBuilderAppend = _stringBuilderType.GetMethod("Append", new[] { _stringType }),
            _stringBuilderAppendObject = _stringBuilderType.GetMethod("Append", new[] { _objectType }),
            _stringBuilderAppendChar = _stringBuilderType.GetMethod("Append", new[] { _charType }),
            _stringBuilderClear = _stringBuilderType.GetMethod("Clear"),
            _stringOpEquality = _stringType.GetMethod("op_Equality", MethodBinding),
            _generatorGetStringBuilder = _jsonType.GetMethod("GetStringBuilder", MethodBinding),
            _generatorIntToStr = _jsonType.GetMethod("IntToStr", MethodBinding),
            _generatorLongToStr = _jsonType.GetMethod("LongToStr", MethodBinding),
            _generatorDateToString = _jsonType.GetMethod("DateToString", MethodBinding),
            _generatorDateToISOFormat = _jsonType.GetMethod("DateToISOFormat", MethodBinding),
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
            _fastStringToLong = _jsonType.GetMethod("FastStringToLong", MethodBinding),
            _fastStringToULong = _jsonType.GetMethod("FastStringToULong", MethodBinding),
            _fastStringToDecimal = _jsonType.GetMethod("FastStringToDecimal", MethodBinding),
            _fastStringToFloat = _jsonType.GetMethod("FastStringToFloat", MethodBinding),
            _fastStringToDate = _jsonType.GetMethod("FastStringToDate", MethodBinding),
            _fastStringToDouble = _jsonType.GetMethod("FastStringToDouble", MethodBinding),
            _fastStringToBool = _jsonType.GetMethod("FastStringToBool", MethodBinding),
            _moveToArrayBlock = _jsonType.GetMethod("MoveToArrayBlock", MethodBinding),
            _fastStringToByteArray = _jsonType.GetMethod("FastStringToByteArray", MethodBinding),
            _stringLength = _stringType.GetMethod("get_Length"),
            _createString = _jsonType.GetMethod("CreateString"),
            _isCharTag = _jsonType.GetMethod("IsCharTag"),
            _isEndChar = _jsonType.GetMethod("IsEndChar", MethodBinding),
            _isArrayEndChar = _jsonType.GetMethod("IsArrayEndChar", MethodBinding),
            _encodedJSONString = _jsonType.GetMethod("EncodedJSONString", MethodBinding),
            _decodeJSONString = _jsonType.GetMethod("DecodeJSONString", MethodBinding),
            _skipProperty = _jsonType.GetMethod("SkipProperty", MethodBinding),
            _dateTimeParse = _dateTimeType.GetMethod("Parse", new[] { _stringType }),
            _timeSpanParse = _timeSpanType.GetMethod("Parse", new[] { _stringType }),
            _getChars = _stringType.GetMethod("get_Chars"),
            _dictSetItem = _dictType.GetMethod("set_Item"),
            _textWriterWrite = _textWriterType.GetMethod("Write", new []{ _stringType }),
            _textReaderReadToEnd = _textReaderType.GetMethod("ReadToEnd"),
            _stringEqualCompare = _stringType.GetMethod("Equals", new []{_stringType, _stringType, typeof(StringComparison)}),
            _stringConcat = _stringType.GetMethod("Concat", new[] { _objectType, _objectType, _objectType, _objectType });

        const int Delimeter = (int)',',
            ArrayOpen = (int)'[', ArrayClose = (int)']', ObjectOpen = (int)'{', ObjectClose = (int)'}';

        const string IsoFormat = "{0:yyyy-MM-ddTHH:mm:ss.fffZ}",
             ClassStr = "Class", _dllStr = ".dll",
             NullStr = "null",
              IListStr = "IList`1",
              IDictStr = "IDictionary`2",
              CreateListStr = "CreateList",
              CreateClassOrDictStr = "CreateClassOrDict",
              ExtractStr = "Extract",
              SetStr = "Set",
              WriteStr = "Write", ReadStr = "Read", ReadEnumStr = "ReadEnum",
              QuoteChar = "`",
              ArrayStr = "Array", AnonymousBracketStr = "<>",
              ArrayLiteral = "[]",
              Colon = ":",
              SerializeStr = "Serialize", DeserializeStr = "Deserialize";

        static ConstructorInfo _strCtorWithPtr = _stringType.GetConstructor(new[] { typeof(char*), _intType, _intType });

        static readonly ConcurrentDictionary<Type, Type> _types =
            new ConcurrentDictionary<Type, Type>();
        static readonly ConcurrentDictionary<string, MethodBuilder> _writeMethodBuilders =
            new ConcurrentDictionary<string, MethodBuilder>();

        static readonly ConcurrentDictionary<string, MethodBuilder> _setValueMethodBuilders =
            new ConcurrentDictionary<string, MethodBuilder>();

        static readonly ConcurrentDictionary<string, MethodBuilder> _readMethodBuilders =
            new ConcurrentDictionary<string, MethodBuilder>();

        static readonly ConcurrentDictionary<string, MethodBuilder> _createListMethodBuilders =
            new ConcurrentDictionary<string, MethodBuilder>();

        static readonly ConcurrentDictionary<string, MethodBuilder> _extractMethodBuilders =
            new ConcurrentDictionary<string, MethodBuilder>();

        static readonly ConcurrentDictionary<string, MethodBuilder> _readDeserializeMethodBuilders =
            new ConcurrentDictionary<string, MethodBuilder>();

        static readonly ConcurrentDictionary<string, MethodBuilder> _writeEnumToStringMethodBuilders =
            new ConcurrentDictionary<string, MethodBuilder>();

        static readonly ConcurrentDictionary<string, MethodBuilder> _readEnumToStringMethodBuilders =
            new ConcurrentDictionary<string, MethodBuilder>();

        static readonly ConcurrentDictionary<Type, bool> _primitiveTypes =
            new ConcurrentDictionary<Type, bool>();

        static readonly ConcurrentDictionary<Type, Type> _nullableTypes =
            new ConcurrentDictionary<Type, Type>();


        static readonly ConcurrentDictionary<Type, object> _serializers = new ConcurrentDictionary<Type, object>();

        static readonly ConcurrentDictionary<Type, IEnumerable<PropertyInfo>> _typeProperties =
            new ConcurrentDictionary<Type, IEnumerable<PropertyInfo>>();

        static readonly ConcurrentDictionary<string, string> _fixes =
            new ConcurrentDictionary<string, string>();

        const int DefaultStringBuilderCapacity = 1024 * 2;

        private readonly static object _lockObject = new object();

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

        private static bool IsPrimitiveType(this Type type) {
            return _primitiveTypes.GetOrAdd(type, key => {
                if (key.IsGenericType &&
                    key.GetGenericTypeDefinition() == _nullableType)
                    key = key.GetGenericArguments()[0];

                return key == _stringType ||
                    key.IsPrimitive || key == _dateTimeType ||
                    key == _decimalType || key == _timeSpanType ||
                    key == _guidType ||
                    key.IsEnum || key == _byteArrayType;
            });
        }

        private static Type GetNullableType(this Type type) {
            return _nullableTypes.GetOrAdd(type, key => {
                return key.Name.StartsWith("Nullable`") ? key.GetGenericArguments()[0] : null;
            });
        }

        internal static IEnumerable<PropertyInfo> GetTypeProperties(this Type type) {
            return _typeProperties.GetOrAdd(type, key => key.GetProperties(PropertyBinding));
        }

        internal static bool IsListType(this Type type) {
            return _listType.IsAssignableFrom(type) || type.Name == IListStr;
        }

        internal static bool IsDictionaryType(this Type type) {
            return _dictType.IsAssignableFrom(type) || type.Name == IDictStr;
        }

        public static bool IsCollectionType(this Type type) {
            return type.IsListType() || type.IsDictionaryType();
        }

        public static bool IsClassType(this Type type) {
            return !type.IsCollectionType() && !type.IsPrimitiveType();
        }

        [ThreadStatic]
        private static StringBuilder _cachedStringBuilder;
        public static StringBuilder GetStringBuilder() {
            return _cachedStringBuilder ?? (_cachedStringBuilder = new StringBuilder(DefaultStringBuilderCapacity));
        }

        private static bool _useTickFormat = true;

        public static bool UseISOFormat {
            set {
                _useTickFormat = !value;
            }
        }

        private static bool _caseSensitive = true;

        public static bool CaseSensitive {
            set {
                _caseSensitive = value;
            }
        }

        [ThreadStatic]
        private static StringBuilder _cachedDateStringBuilder;

        public static string DateToISOFormat(DateTime date) {
            return (_cachedDateStringBuilder ?? (_cachedDateStringBuilder = new StringBuilder(25)))
                .Clear().Append(IntToStr(date.Year)).Append('-').Append(IntToStr(date.Month))
            .Append('-').Append(IntToStr(date.Day)).Append('T').Append(IntToStr(date.Hour)).Append(':').Append(IntToStr(date.Minute)).Append(':')
            .Append(IntToStr(date.Second)).Append('.').Append(IntToStr(date.Millisecond)).Append('Z').ToString();
        }

        private static DateTime Epoch = new DateTime(1970, 1, 1);
        
        public static string DateToString(DateTime date) {
            if (date == DateTime.MinValue)
                return "\\/Date(-62135596800)\\/";
            else if (date == DateTime.MaxValue)
                return "\\/Date(253402300800)\\/";
            return String.Concat("\\/Date(", IntUtility.ltoa((long)(date - Epoch).TotalSeconds), ")\\/");
        }

        internal static Type Generate(Type objType) {

            var returnType = default(Type);
            if (_types.TryGetValue(objType, out returnType))
                return returnType;

            var isPrimitive = objType.IsPrimitiveType();
            var genericType = _serializerType.MakeGenericType(objType);
            var typeName = String.Concat(objType.Name, ClassStr);//objType.Name;
            var asmName = typeName;
            var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName(asmName) {
                    Version = new Version(1, 0, 0, 0)
                },
                AssemblyBuilderAccess.RunAndSave);


            //[assembly: CompilationRelaxations(8)]
            assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(CompilationRelaxationsAttribute).GetConstructor(new [] { _intType }), new object[] { 8 }));

            //[assembly: RuntimeCompatibility(WrapNonExceptionThrows=true)]
            assembly.SetCustomAttribute(new CustomAttributeBuilder(
                typeof(RuntimeCompatibilityAttribute).GetConstructor(Type.EmptyTypes),
                new object[] {  },
                new[] {  typeof(RuntimeCompatibilityAttribute).GetProperty("WrapNonExceptionThrows")
                },
                new object[] { true }));

            //[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification=true)]
            assembly.SetCustomAttribute(new CustomAttributeBuilder(
                typeof(SecurityPermissionAttribute).GetConstructor(new []{ typeof(SecurityAction)}),
                new object[] { SecurityAction.RequestMinimum },
                new[] {  typeof(SecurityPermissionAttribute).GetProperty("SkipVerification")
                },
                new object[] { true }));

            
            var module = assembly.DefineDynamicModule(String.Concat(typeName, _dllStr));

            var type = module.DefineType(typeName, TypeAttribute, genericType);

            var writeMethod = WriteSerializeMethodFor(type, objType, needQuote: !isPrimitive);

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
            il.Emit(OpCodes.Callvirt, _stringBuilderClear);
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
            wil.Emit(OpCodes.Callvirt, _stringBuilderClear);
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

            dil.Emit(OpCodes.Ldarg_1);
            dil.Emit(OpCodes.Call, readMethod);
            dil.Emit(OpCodes.Ret);

            var rdil = deserializeWithReaderMethod.GetILGenerator();

            rdil.Emit(OpCodes.Ldarg_1);
            rdil.Emit(OpCodes.Callvirt, _textReaderReadToEnd);
            rdil.Emit(OpCodes.Call, readMethod);
            rdil.Emit(OpCodes.Ret);

            type.DefineMethodOverride(serializeMethod,
                genericType.GetMethod(SerializeStr, new []{ objType }));

            type.DefineMethodOverride(serializeWithTextWriterMethod,
                genericType.GetMethod(SerializeStr, new []{ objType, _textWriterType }));

            type.DefineMethodOverride(deserializeMethod,
                genericType.GetMethod(DeserializeStr, new []{ _stringType }));

            type.DefineMethodOverride(deserializeWithReaderMethod,
                genericType.GetMethod(DeserializeStr, new [] { _textReaderType }));

            returnType = type.CreateType();
            _types[objType] = returnType;
            
            //assembly.Save(String.Concat(typeName, _dllStr));

            return returnType;
        }


        public static unsafe void SkipProperty(char* ptr, ref int index) {
            char current = '\0', schar = '\0', echar = '\0', prev = '\0';
            int count = 0, charCount = 0;
            bool isBeginEnd = false, isTag = false, isQuote = false;

            while (true) {
                current = *(ptr + index);
                var hasChar = schar != '\0';
                if (!hasChar) {
                    if (current != ' ' && current != ':' && current != '\n' && current != '\r') {
                        echar = current == '"' ? '"' :
                                current == '{' ? '}' :
                                current == '[' ? ']' : '\0';
                        isQuote = echar == '"';
                        if (echar == '\0') {
                            index--;
                            GetNonStringValue(ptr, ref index);
                            return;
                        }
                        schar = current;
                        count = 1;
                        charCount = 1;
                    }
                    ++index;
                    continue;
                }
            endLabel:
                isBeginEnd = (current == schar || current == echar);
                isTag = isBeginEnd && charCount == 0;
                if (isTag) count++;
                if (count == 2) {
                    ++index;
                    break;
                }
                if (!isTag) {
                    if (isQuote) {
                        if (current == '"' && charCount > 0 && prev != '\\') {
                            //++index;
                            charCount = 0;
                            goto endLabel;
                            //continue;
                        }
                    } else {
                        if (isBeginEnd) {
                            if (current == schar) charCount++;
                            else if (current == echar) charCount--;
                            if (charCount == 0 && prev == echar) index++;
                        }
                    }
                }
                prev = current;
                if (current != echar)
                    ++index;
            }
        }

        public static unsafe void EncodedJSONString(StringBuilder sb, string str) {
            char c;
            fixed (char* chr = str) {
                char* ptr = chr;
                while ((c = *(ptr++)) != '\0') {
                    switch (c) {
                        case '"': sb.Append("\\\""); break;
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
                        default: sb.Append(c); break;
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
                var index = key.IndexOf(QuoteChar, StringComparison.OrdinalIgnoreCase);
                var quoteText = index > -1 ? key.Substring(index, 2) : QuoteChar;
                var value = key.Replace(quoteText, string.Empty).Replace(ArrayLiteral, ArrayStr).Replace(AnonymousBracketStr, string.Empty);
                if (value.Contains(QuoteChar))
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
            var il = method.GetILGenerator();

            var values = Enum.GetValues(type).Cast<int>().ToArray();
            var keys = Enum.GetNames(type);

            var count = values.Length;

            for (var i = 0; i < count; i++) {

                var value = values[i];
                var k = keys[i];
                var @int = value;

                var label = il.DefineLabel();
                var label2 = il.DefineLabel();

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldstr, k);
                il.Emit(OpCodes.Call, _stringOpEquality);
                il.Emit(OpCodes.Brfalse, label);

                il.Emit(OpCodes.Ldc_I4, @int);
                il.Emit(OpCodes.Ret);

                il.MarkLabel(label);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldstr, IntToStr(@int));
                il.Emit(OpCodes.Call, _stringOpEquality);
                il.Emit(OpCodes.Brfalse, label2);

                il.Emit(OpCodes.Ldc_I4, @int);
                il.Emit(OpCodes.Ret);

                il.MarkLabel(label2);
            }

            //Return default enum if no match is found
            il.Emit(OpCodes.Ldc_I4_0);
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
            var il = method.GetILGenerator();

            var values = Enum.GetValues(type).Cast<int>().ToArray();

            var count = values.Length;

            for (var i = 0; i < count; i++) {

                var value = values[i];
                var @int = (int)value;

                var label = il.DefineLabel();

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, @int);
                il.Emit(OpCodes.Bne_Un, label);

                il.Emit(OpCodes.Ldstr, IntToStr(@int));
                il.Emit(OpCodes.Ret);

                il.MarkLabel(label);
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

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldstr, "[");
                il.Emit(OpCodes.Callvirt, _stringType.GetMethod("StartsWith", new []{ _stringType }));
                il.Emit(OpCodes.Stloc, startsWith);

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
                if(isArray)
                    il.Emit(OpCodes.Call, GenerateCreateListFor(typeBuilder, type));
                else
                    il.Emit(OpCodes.Call, GenerateGetClassOrDictFor(typeBuilder, type));
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

            if (type.IsPrimitiveType()) {
                var nullLabel = il.DefineLabel();

                needQuote = needQuote && (type == _stringType || type == _guidType || type == _timeSpanType || type == _dateTimeType || type == _byteArrayType);

                if (type == _stringType || type == _objectType) {

                    il.Emit(OpCodes.Ldarg_0);
                    if (type == _stringType) {
                        il.Emit(OpCodes.Ldnull);
                        il.Emit(OpCodes.Call, _stringOpEquality);
                    }
                    il.Emit(OpCodes.Brfalse, nullLabel);

                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldstr, NullStr);
                    il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                    il.Emit(OpCodes.Pop);

                    il.Emit(OpCodes.Ret);

                    il.MarkLabel(nullLabel);

                    if (needQuote) {
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Ldstr, QuotChar);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        //il.Emit(OpCodes.Pop);
                    } else il.Emit(OpCodes.Ldarg_1);

                    //il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Ldarg_0);
                    if (type == _objectType){
                        il.Emit(OpCodes.Callvirt, _objectToString);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        //il.Emit(OpCodes.Pop);
                    }
                    else if (type == _stringType) {
                        //il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Call, _encodedJSONString);
                        il.Emit(OpCodes.Ldarg_1);
                    }

                    if (needQuote) {
                        //il.Emit(OpCodes.Ldarg_2);
                        il.Emit(OpCodes.Ldstr, QuotChar);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);
                    } else il.Emit(OpCodes.Pop);
                } else {

                    if (type == _dateTimeType) {
                        if (needQuote) {
                            il.Emit(OpCodes.Ldarg_1);
                            il.Emit(OpCodes.Ldstr, QuotChar);
                            il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                            il.Emit(OpCodes.Pop);
                        }
                        il.Emit(OpCodes.Ldarg_1);
                        //il.Emit(OpCodes.Ldstr, IsoFormat);
                        il.Emit(OpCodes.Ldarg_0);
                        //il.Emit(OpCodes.Box, _dateTimeType);
                        //il.Emit(OpCodes.Call, _stringFormat);
                        il.Emit(OpCodes.Call, _useTickFormat ? _generatorDateToString : _generatorDateToISOFormat);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);
                        if (needQuote) {
                            il.Emit(OpCodes.Ldarg_1);
                            il.Emit(OpCodes.Ldstr, QuotChar);
                            il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                            il.Emit(OpCodes.Pop);
                        }
                    } else if (type == _byteArrayType) {

                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Brtrue, nullLabel);

                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Ldstr, NullStr);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);

                        il.Emit(OpCodes.Ret);
                        il.MarkLabel(nullLabel);

                        if (needQuote) {
                            il.Emit(OpCodes.Ldarg_1);
                            il.Emit(OpCodes.Ldstr, QuotChar);
                            il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                            il.Emit(OpCodes.Pop);
                        }

                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Call, _convertBase64);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);
                        if (needQuote) {
                            il.Emit(OpCodes.Ldarg_1);
                            il.Emit(OpCodes.Ldstr, QuotChar);
                            il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                            il.Emit(OpCodes.Pop);
                        }
                    } else if (type == _boolType) {
                        var boolLocal = il.DeclareLocal(_stringType);
                        var boolLabel = il.DefineLabel();
                        il.Emit(OpCodes.Ldstr, "true");
                        il.Emit(OpCodes.Stloc, boolLocal);

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
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Call, WriteEnumToStringFor(typeBuilder, type));
                        //il.Emit(OpCodes.Conv_I4);
                        //il.Emit(OpCodes.Box, _intType);
                        //il.Emit(OpCodes.Call, _generatorIntToStr);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);
                    } else {
                        if (needQuote) {
                            il.Emit(OpCodes.Ldarg_1);
                            il.Emit(OpCodes.Ldstr, QuotChar);
                            il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                            il.Emit(OpCodes.Pop);
                        }
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Box, type);
                        il.Emit(OpCodes.Callvirt, _objectToString);
                        il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                        il.Emit(OpCodes.Pop);

                        if (needQuote) {
                            il.Emit(OpCodes.Ldarg_1);
                            il.Emit(OpCodes.Ldstr, QuotChar);
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

            methodIL.Emit(OpCodes.Ldarg_0);
            methodIL.Emit(OpCodes.Brtrue, conditionLabel);
            methodIL.Emit(OpCodes.Ldarg_1);
            methodIL.Emit(OpCodes.Ldstr, NullStr);
            methodIL.Emit(OpCodes.Callvirt, _stringBuilderAppend);
            methodIL.Emit(OpCodes.Pop);
            methodIL.Emit(OpCodes.Ret);
            methodIL.MarkLabel(conditionLabel);

            if (type.IsClassType()) WritePropertiesFor(typeBuilder, type, methodIL);
            else WriteCollection(typeBuilder, type, methodIL);
        }

        internal static void WriteCollection(TypeBuilder typeBuilder, Type type, ILGenerator il) {

            var isDict = type.IsDictionaryType();

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_S, isDict ? ObjectOpen : ArrayOpen);
            il.Emit(OpCodes.Callvirt, _stringBuilderAppendChar);
            il.Emit(OpCodes.Pop);

            if (type.IsDictionaryType())
                WriteDictionary(typeBuilder, type, il);
            else WriteListArray(typeBuilder, type, il);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_S, isDict ? ObjectClose : ArrayClose);
            il.Emit(OpCodes.Callvirt, _stringBuilderAppendChar);
            il.Emit(OpCodes.Pop);
        }

        internal static void WriteDictionary(TypeBuilder typeBuilder, Type type, ILGenerator il) {
            var arguments = type.GetGenericArguments();
            var keyType = arguments[0];
            var valueType = arguments[1];
            var isKeyPrimitive = keyType.IsPrimitiveType();
            var isValuePrimitive = valueType.IsPrimitiveType();
            var keyValuePairType = _genericKeyValuePairType.MakeGenericType(keyType, valueType);
            var enumeratorType = _genericDictionaryEnumerator.MakeGenericType(keyType, valueType);
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
                _genericDictType.MakeGenericType(keyType, valueType).GetMethod("GetEnumerator"));
            il.Emit(OpCodes.Stloc_S, enumeratorLocal);
            il.BeginExceptionBlock();
            il.Emit(OpCodes.Br, startEnumeratorLabel);
            il.MarkLabel(moveNextLabel);
            il.Emit(OpCodes.Ldloca_S, enumeratorLocal);
            il.Emit(OpCodes.Call,
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

            il.Emit(OpCodes.Ldstr, QuotChar);
            il.Emit(OpCodes.Callvirt, _stringBuilderAppend);

            il.Emit(OpCodes.Ldloca, entryLocal);
            il.Emit(OpCodes.Call, keyValuePairType.GetProperty("Key").GetGetMethod());
            if (keyType.IsValueType)
                il.Emit(OpCodes.Box, keyType);
            il.Emit(OpCodes.Callvirt, _stringBuilderAppendObject);

            il.Emit(OpCodes.Ldstr, QuotChar);
            il.Emit(OpCodes.Callvirt, _stringBuilderAppend);

            il.Emit(OpCodes.Ldstr, Colon);
            il.Emit(OpCodes.Callvirt, _stringBuilderAppend);

            il.Emit(OpCodes.Pop);

            //il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloca, entryLocal);
            il.Emit(OpCodes.Call, keyValuePairType.GetProperty("Value").GetGetMethod());
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, WriteSerializeMethodFor(typeBuilder, valueType));

            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Stloc, hasItem);

            il.MarkLabel(startEnumeratorLabel);
            il.Emit(OpCodes.Ldloca_S, enumeratorLocal);
            il.Emit(OpCodes.Call, enumeratorType.GetMethod("MoveNext", MethodBinding));
            il.Emit(OpCodes.Brtrue, moveNextLabel);
            il.Emit(OpCodes.Leave, endEnumeratorLabel);
            il.BeginFinallyBlock();
            il.Emit(OpCodes.Ldloca_S, enumeratorLocal);
            il.Emit(OpCodes.Constrained, enumeratorLocal.LocalType);
            il.Emit(OpCodes.Callvirt, _iDisposableDispose);
            il.EndExceptionBlock();
            il.MarkLabel(endEnumeratorLabel);
        }

        internal static void WriteListArray(TypeBuilder typeBuilder, Type type, ILGenerator il) {

            var isArray = type.IsArray;
            var itemType = isArray ? type.GetElementType() : type.GetGenericArguments()[0];
            var isPrimitive = itemType.IsPrimitiveType();
            var itemLocal = il.DeclareLocal(itemType);
            var indexLocal = il.DeclareLocal(_intType);
            var startLabel = il.DefineLabel();
            var endLabel = il.DefineLabel();
            var countLocal = il.DeclareLocal(typeof(int));
            var diffLocal = il.DeclareLocal(typeof(int));
            var checkCountLabel = il.DefineLabel();

            if (isArray) {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldlen);
                il.Emit(OpCodes.Conv_I4);
                il.Emit(OpCodes.Stloc, countLocal);
            } else {
                il.Emit(OpCodes.Ldarg_0);
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
            il.Emit(OpCodes.Ldarg_0);
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

            var props = type.GetTypeProperties();
            var count = props.Count() - 1;
            var counter = 0;
            foreach (var prop in props) {
                var name = prop.Name;
                var propType = prop.PropertyType;
                var originPropType = prop.PropertyType;
                var isPrimitive = propType.IsPrimitiveType();
                var nullableType = propType.GetNullableType();
                var isNullable = nullableType != null;

                propType = isNullable ? nullableType : propType;
                var isValueType = propType.IsValueType;
                var propNullLabel = il.DefineLabel();
                var equalityMethod = propType.GetMethod("op_Equality");
                var propValue = il.DeclareLocal(propType);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Callvirt, prop.GetGetMethod());
                if (isNullable) {
                    var nullablePropValue = il.DeclareLocal(originPropType);
                    il.Emit(OpCodes.Stloc, nullablePropValue);
                    il.Emit(OpCodes.Ldloca, nullablePropValue);
                    il.Emit(OpCodes.Call, originPropType.GetMethod("GetValueOrDefault", Type.EmptyTypes));
                }

                il.Emit(OpCodes.Stloc, propValue);

                il.Emit(OpCodes.Ldloc, propValue);
                if (isValueType && isPrimitive) {
                    LoadDefaultValueByType(il, propType);
                } else {
                    il.Emit(OpCodes.Ldnull);
                }

                if (equalityMethod != null) {
                    il.Emit(OpCodes.Call, equalityMethod);
                    il.Emit(OpCodes.Brtrue, propNullLabel);
                } else {
                    il.Emit(OpCodes.Beq, propNullLabel);
                }


                if (counter > 0) {
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldc_I4, Delimeter);
                    il.Emit(OpCodes.Callvirt, _stringBuilderAppendChar);
                    il.Emit(OpCodes.Pop);
                }

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldstr, String.Concat(QuotChar, name, QuotChar, Colon));
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

                il.MarkLabel(propNullLabel);

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
            else if (type == typeof(uint))
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
            else if (type == _decimalType) {
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Newobj, _decimalType.GetConstructor(new[] { _intType }));
            } else if (type.IsEnum)
                il.Emit(OpCodes.Ldc_I4_0);
        }

        internal static NetJSONSerializer<T> GetSerializer<T>() {
            return NetJSONCachedSerializer<T>.Serializer;
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

            return _readMethodBuilders.GetOrAdd("ExtractObjectValue", _ => {

                var method = type.DefineMethod("ExtractObjectValue", StaticMethodAttribute, _objectType,
                    new[] { _charPtrType, _intType.MakeByRefType() });

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

                    il.Emit(OpCodes.Ldc_I4, (int)'\r');
                    il.Emit(OpCodes.Ldloc, current);
                    il.Emit(OpCodes.Beq, tokenLabel);


                    //if(current == '"') {
                    il.Emit(OpCodes.Ldc_I4, (int)'"');
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
            });
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
                while (*p != '\0') {
                    if (*p == '.') {
                        double rem = 0.0;
                        double div = 1;
                        ++p;
                        while (*p != '\0') {
                            rem = (rem * 10.0) + (*p - '0');
                            div *= 10.0;
                            ++p;
                        }
                        val += rem / div;
                        return val * neg;
                    }
                    val = (val * 10.0) + (*p - '0');
                    ++p;
                }
            }
            return val * neg;
        }
        
        public static unsafe float FastStringToFloat(string numStr) {
            float val = 0.0f;
            float neg = 1;
            fixed (char* ptr = numStr) {
                char* p = ptr;
                if (*p == '-') {
                    neg = -1;
                    ++p;
                }
                while (*p != '\0') {
                    if (*p == '.') {
                        float rem = 0.0f;
                        float div = 1;
                        ++p;
                        while (*p != '\0') {
                            rem = (rem * 10.0f) + (*p - '0');
                            div *= 10.0f;
                            ++p;
                        }
                        val += rem / div;
                        return val * neg;
                    }
                    val = (val * 10.0f) + (*p - '0');
                    ++p;
                }
            }
            return val * neg;
        }

        public static decimal FastStringToDecimal(string numStr) {
            return new Decimal(FastStringToDouble(numStr));
        }

        [ThreadStatic]
        static StringBuilder _decodeJSONStringBuilder;

        public unsafe static string DecodeJSONString(char* ptr, ref int index) {
            char current = '\0', next = '\0';
            bool hasQuote = false;
            var sb = (_decodeJSONStringBuilder ?? (_decodeJSONStringBuilder = new StringBuilder())).Clear();

            while (true) {
                current = ptr[index];

                if (hasQuote) {
                    //if (current == '\0') break;

                    if (current == '"') {
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
                                case '"': sb.Append('"'); break;
                                case '\\': sb.Append('\\'); break;
                                case '/': sb.Append('/'); break;
                                case 'b': sb.Append('\b'); break;
                                case 'u':
                                    const int offset = 0x10000;
                                    var uu = Int32.Parse(new string(ptr, index, 4), NumberStyles.HexNumber);
                                    var u = uu < offset ? new string((char)uu, 1) :
                                        new string(
                                            new char[]{
                                                (char)(((uu - offset) >> 10) + 0xD800),
                                                (char)((uu - offset) % 0x0400 + 0xDC00)
                                            }
                                        );
                                    sb.Append(u);
                                    break;
                            }

                        }
                    }
                } else {
                    if (current == '"') {
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
            return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        public static byte[] FastStringToByteArray(string value) {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return Convert.FromBase64String(value);
        }

        public static DateTime FastStringToDate(string value) {

            if (value == "\\/Date(-62135596800)\\/")
                return DateTime.MinValue;
            else if (value == "\\/Date(253402300800)\\/")
                return DateTime.MaxValue;
            else if (value[0] == '\\') {
                var ticks = FastStringToLong(value.Substring(7, value.IndexOf(')',7) - 7));
                return new DateTime(1970, 1, 1).AddSeconds(ticks);
            }

            return DateTime.Parse(value);
        }

        private static void GenerateChangeTypeFor(TypeBuilder typeBuilder, Type type, ILGenerator il, LocalBuilder value) {
            il.Emit(OpCodes.Ldloc, value);

            if (type == _intType)
                il.Emit(OpCodes.Call, _fastStringToInt);
            else if (type == typeof(uint))
                il.Emit(OpCodes.Call, _fastStringToUInt);
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
            else if (type == _timeSpanType)
                il.Emit(OpCodes.Call, _timeSpanParse);
            else if (type == _byteArrayType)
                il.Emit(OpCodes.Call, _fastStringToByteArray);
            else if (type == _boolType) 
                il.Emit(OpCodes.Call, _fastStringToBool);
            else if (type.IsEnum)
                il.Emit(OpCodes.Call, ReadStringToEnumFor(typeBuilder, type));
        }

        private static MethodInfo GenerateSetValueFor(TypeBuilder typeBuilder, Type type) {
            MethodBuilder method;
            var key = type.FullName;
            var typeName = type.GetName().Fix();
            if (_setValueMethodBuilders.TryGetValue(key, out method))
                return method;
            var methodName = String.Concat(SetStr, typeName);
            var isObjectType = type == _objectType;
            method = typeBuilder.DefineMethod(methodName, StaticMethodAttribute,
                _voidType, new[] { _charPtrType, _intType.MakeByRefType(), type, _stringType });
            _setValueMethodBuilders[key] = method;

            const bool Optimized = true;

            var props = type.GetProperties();
            var il = method.GetILGenerator();

            for (var i = 0; i < props.Length; i++) {
                var prop = props[i];
                var propName = prop.Name;
                var conditionLabel = il.DefineLabel();
                var propType = prop.PropertyType;
                var nullableType = propType.GetNullableType();
                var isNullable = nullableType != null;
                propType = isNullable ? nullableType : propType;

                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Ldstr, propName);
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
                    il.Emit(OpCodes.Callvirt, prop.GetSetMethod());
                } else {
                    var propValue = il.DeclareLocal(propType);
                    var isValueType = propType.IsValueType;
                    var propNullLabel = il.DefineLabel();
                    var equalityMethod = propType.GetMethod("op_Equality");

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Call, GenerateExtractValueFor(typeBuilder, propType));
                    il.Emit(OpCodes.Stloc, propValue);

                    il.Emit(OpCodes.Ldloc, propValue);
                    if (isValueType) {
                        LoadDefaultValueByType(il, propType);
                    } else {
                        il.Emit(OpCodes.Ldnull);
                    }

                    if (equalityMethod != null) {
                        il.Emit(OpCodes.Call, equalityMethod);
                        il.Emit(OpCodes.Brtrue, propNullLabel);
                    }
                    else {
                        il.Emit(OpCodes.Beq, propNullLabel);
                    }

                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Ldloc, propValue);
                    if (isNullable) {
                        il.Emit(OpCodes.Newobj, _nullableType.MakeGenericType(propType).GetConstructor(new[] { propType }));
                    }
                    il.Emit(OpCodes.Callvirt, prop.GetSetMethod());

                    il.Emit(OpCodes.Ret);

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
            var isPrimitive = elementType.IsPrimitiveType();
            var isStringType = elementType == _stringType;
            var isByteArray = elementType == _byteArrayType;
            var isStringBased = isStringType || elementType == _dateTimeType || elementType == _timeSpanType || isByteArray;


            var obj = il.DeclareLocal(typeof(List<>).MakeGenericType(elementType));
            var objArray = isArray ? il.DeclareLocal(elementType.MakeArrayType()) : null;
            var count = il.DeclareLocal(_intType);
            var startIndex = il.DeclareLocal(_intType);
            var endIndex = il.DeclareLocal(_intType);
            var prev = il.DeclareLocal(_charType);
            var addMethod = obj.LocalType.GetMethod("Add");

            var prevLabel = il.DefineLabel();
            

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

            var dict = il.DeclareLocal(_dictType);
            var prev = il.DeclareLocal(_charType);
            var count = il.DeclareLocal(_intType);
            var startIndex = il.DeclareLocal(_intType);
            var quotes = il.DeclareLocal(_intType);
            var isTag = il.DeclareLocal(_boolType);
            var obj = il.DeclareLocal(type);

            var incLabel = il.DefineLabel();
            var openCloseBraceLabel = il.DefineLabel();
            var isTagLabel = il.DefineLabel();
            var isNotTagLabel = il.DefineLabel();
            var countLabel = il.DefineLabel();
            var isNullObjectLabel = il.DefineLabel();


            var isDict = _dictType.IsAssignableFrom(type);
            var arguments = isDict ? type.GetGenericArguments() : null;
            var hasArgument = arguments != null;
            var keytype = hasArgument ? arguments[0] : null;
            var valueType = hasArgument ? arguments[1] : null;
            var isStringType = !isDict || keytype == _stringType || keytype == _objectType;
            


            if (type.IsValueType) {
                il.Emit(OpCodes.Ldloca, obj);
                il.Emit(OpCodes.Initobj, type);
            } else {
                il.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
                il.Emit(OpCodes.Stloc, obj);
            }

            if (isDict) {
                il.Emit(OpCodes.Ldloc, obj);
                il.Emit(OpCodes.Isinst, _dictType);
                il.Emit(OpCodes.Stloc, dict);
            }

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, count);

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
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ret);

                il.MarkLabel(isNullObjectLabel);


                //current == '{' || current == '}'
                //il.Emit(OpCodes.Ldloc, current);
                //il.Emit(OpCodes.Call, _isCharTag);
                //il.Emit(OpCodes.Brfalse, openCloseBraceLabel);

                
                var currentisCharTagLabel = il.DefineLabel();

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

                    //if(current == '"' && quotes == 0)
                    il.Emit(OpCodes.Ldloc, current);
                    il.Emit(OpCodes.Ldc_I4, (int)'"');
                    il.Emit(OpCodes.Bne_Un, currentQuoteLabel);
                    il.Emit(OpCodes.Ldloc, quotes);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Bne_Un, currentQuoteLabel);

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

                    il.Emit(OpCodes.Br, currentQuotePrevNotLabel);
                    il.MarkLabel(currentQuoteLabel);
                    //else if(current == '"' && quotes > 0 && prev != '\\')
                    il.Emit(OpCodes.Ldloc, current);
                    il.Emit(OpCodes.Ldc_I4, (int)'"');
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

                    //index++
                    IncrementIndexRef(il);

                    if (isDict) {
                        //dict[key] = ExtractValue(json, ref index)
                        il.Emit(OpCodes.Ldloc, dict);
                        il.Emit(OpCodes.Ldloc, keyLocal);
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Call, GenerateExtractValueFor(typeBuilder, valueType));
                        il.Emit(OpCodes.Callvirt, _dictSetItem);
                    } else {
                        //Set property based on key
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Ldloc, obj);
                        il.Emit(OpCodes.Ldloc, keyLocal);
                        il.Emit(OpCodes.Call, GenerateSetValueFor(typeBuilder, type));
                    }

                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Stloc, quotes);

                    il.Emit(OpCodes.Br, startLoop);

                    il.MarkLabel(currentQuotePrevNotLabel);

                } else {
                    var isEndOfChar = il.DeclareLocal(_boolType);
                    var text = il.DeclareLocal(_stringType);
                    var keyLocal = il.DeclareLocal(_objectType);
                    var startIndexIsEndCharLabel = il.DefineLabel();
                    var startIndexGreaterIsEndOfCharLabel = il.DefineLabel();

                    
                    var currentEndCharLabel = il.DefineLabel();
                    var currentEndCharLabel2 = il.DefineLabel();

                    //current == ':' || current == '{' || current == ' ';

                    il.Emit(OpCodes.Ldloc, current);
                    il.Emit(OpCodes.Ldc_I4, (int)':');
                    il.Emit(OpCodes.Beq, currentEndCharLabel);

                    il.Emit(OpCodes.Ldloc, current);
                    il.Emit(OpCodes.Ldc_I4, (int)'\n');
                    il.Emit(OpCodes.Beq, currentEndCharLabel);

                    il.Emit(OpCodes.Ldloc, current);
                    il.Emit(OpCodes.Ldc_I4, (int)'\r');
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
                    //il.Emit(OpCodes.Call, _createString);
                    il.Emit(OpCodes.Stloc, text);

                    GenerateChangeTypeFor(typeBuilder, keytype, il, text);
                    if (keytype.IsPrimitiveType())
                        il.Emit(OpCodes.Box, keytype);

                    il.Emit(OpCodes.Stloc, keyLocal);

                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Stloc, startIndex);

                    IncrementIndexRef(il);

                    il.Emit(OpCodes.Ldloc, dict);
                    il.Emit(OpCodes.Ldloc, keyLocal);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Call, GenerateExtractValueFor(typeBuilder, valueType));
                    il.Emit(OpCodes.Callvirt, _dictSetItem);

                    GenerateUpdateCurrent(il, current, ptr);

                    il.MarkLabel(startIndexGreaterIsEndOfCharLabel);
                }

                il.MarkLabel(isNotTagLabel);

                il.Emit(OpCodes.Ldloc, current);
                il.Emit(OpCodes.Stloc, prev);

            }, needBreak: true,
            returnAction: msil => {
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


            while (true) {
                current = ptr[index];
                if (count == 0 && current == '"') {
                    startIndex = index + 1;
                    ++count;
                } else if (count > 0 && current == '"' && prev != '\\') {
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
                        case ' ':
                            ++indexDiff;
                            break;
                    }
                }
                if (current != ' ' && current != ':') {
                    if (startIndex == -1)
                        startIndex = index;
                    if (current == ',' || current == ']' || current == '}') {
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
            return type == _stringType || type == _dateTimeType || type == _timeSpanType || type == _byteArrayType;
        }
    }
}
