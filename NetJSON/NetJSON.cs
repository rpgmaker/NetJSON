using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            _jsonType = typeof(NetJSON),
            _textWriterType = typeof(TextWriter),
            _textReaderType = typeof(TextReader);

        static readonly MethodInfo _stringBuilderToString =
            _stringBuilderType.GetMethod("ToString", Type.EmptyTypes),
            _stringBuilderAppend = _stringBuilderType.GetMethod("Append", new[] { _stringType }),
            _stringBuilderAppendChar = _stringBuilderType.GetMethod("Append", new[] { _charType }),
            _stringBuilderClear = _stringBuilderType.GetMethod("Clear"),
            _stringOpEquality = _stringType.GetMethod("op_Equality", MethodBinding),
            _generatorGetStringBuilder = _jsonType.GetMethod("GetStringBuilder", MethodBinding),
            _generatorIntToStr = _jsonType.GetMethod("IntToStr", MethodBinding),
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
            _dateTimeParse = _dateTimeType.GetMethod("Parse", new[] { _stringType }),
            _timeSpanParse = _timeSpanType.GetMethod("Parse", new[] { _stringType }),
            _getChars = _stringType.GetMethod("get_Chars"),
            _dictSetItem = _dictType.GetMethod("set_Item"),
            _textWriterWrite = _textWriterType.GetMethod("Write", new []{ _stringType }),
            _textReaderReadToEnd = _textReaderType.GetMethod("ReadToEnd"),
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

        static readonly ConcurrentDictionary<int, StringBuilder> _stringBuilders =
            new ConcurrentDictionary<int, StringBuilder>();

        static readonly ConcurrentDictionary<int, StringBuilder> _dateStringBuilders =
            new ConcurrentDictionary<int, StringBuilder>();

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

        public static StringBuilder GetStringBuilder() {
            return _stringBuilders.GetOrAdd(Thread.CurrentThread.ManagedThreadId, key => new StringBuilder(DefaultStringBuilderCapacity));
        }

        private static bool _useTickFormat = true;

        private static bool _ignoreNullValue = true;

        public static bool UseISOFormat {
            set {
                _useTickFormat = !value;
            }
        }

        public static bool IgnoreNullValue {
            set {
                _ignoreNullValue = value;
            }
        }

        public static string DateToISOFormat(DateTime date) {
            return _dateStringBuilders.GetOrAdd(Thread.CurrentThread.ManagedThreadId, key => new StringBuilder(25))
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
            il.Emit(OpCodes.Stloc, sbLocal.LocalIndex);

            //il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldloc, sbLocal.LocalIndex);
            il.Emit(OpCodes.Call, writeMethod);

            il.Emit(OpCodes.Ldloc, sbLocal.LocalIndex);
            il.Emit(OpCodes.Callvirt, _stringBuilderToString);
            il.Emit(OpCodes.Ret);

            var wil = serializeWithTextWriterMethod.GetILGenerator();

            var wsbLocal = wil.DeclareLocal(_stringBuilderType);
            wil.Emit(OpCodes.Call, _generatorGetStringBuilder);
            wil.Emit(OpCodes.Callvirt, _stringBuilderClear);
            wil.Emit(OpCodes.Stloc, wsbLocal.LocalIndex);

            //il.Emit(OpCodes.Ldarg_0);
            wil.Emit(OpCodes.Ldarg_1);
            wil.Emit(OpCodes.Ldloc, wsbLocal.LocalIndex);
            wil.Emit(OpCodes.Call, writeMethod);

            wil.Emit(OpCodes.Ldarg_2);
            wil.Emit(OpCodes.Ldloc, wsbLocal.LocalIndex);
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
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldloca, index);
                il.Emit(OpCodes.Call, GenerateCreateListFor(typeBuilder, typeof(List<object>)));
                il.Emit(OpCodes.Ret);

                il.MarkLabel(startsWithLabel);

                il.Emit(OpCodes.Ldloc, startsWith);
                il.Emit(OpCodes.Brtrue, notStartsWithLabel);

                //IsDictionary
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldloca, index);
                il.Emit(OpCodes.Call, GenerateGetClassOrDictFor(typeBuilder, 
                    typeof(Dictionary<string, object>)));
                il.Emit(OpCodes.Ret);

                il.MarkLabel(notStartsWithLabel);

                il.Emit(OpCodes.Ldnull);
            } else {
                var isArray = type.IsListType() || type.IsArray;

                il.Emit(OpCodes.Ldarg_0);
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
                    if (type == _objectType)
                        il.Emit(OpCodes.Callvirt, _objectToString);
                    il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                    //il.Emit(OpCodes.Pop);

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
                        il.Emit(OpCodes.Ldloc, boolLocal.LocalIndex);
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
            il.Emit(OpCodes.Stloc_S, enumeratorLocal.LocalIndex);
            il.BeginExceptionBlock();
            il.Emit(OpCodes.Br, startEnumeratorLabel);
            il.MarkLabel(moveNextLabel);
            il.Emit(OpCodes.Ldloca_S, enumeratorLocal.LocalIndex);
            il.Emit(OpCodes.Call,
                enumeratorLocal.LocalType.GetProperty("Current")
                .GetGetMethod());
            il.Emit(OpCodes.Stloc, entryLocal.LocalIndex);

            il.Emit(OpCodes.Ldloc, hasItem);
            il.Emit(OpCodes.Brfalse, hasItemLabel);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_S, Delimeter);
            il.Emit(OpCodes.Callvirt, _stringBuilderAppendChar);
            il.Emit(OpCodes.Pop);

            il.MarkLabel(hasItemLabel);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldstr, QuotChar);

            il.Emit(OpCodes.Ldloca, entryLocal.LocalIndex);
            il.Emit(OpCodes.Call, keyValuePairType.GetProperty("Key").GetGetMethod());
            if (keyType.IsValueType)
                il.Emit(OpCodes.Box, keyType);


            il.Emit(OpCodes.Ldstr, QuotChar);
            il.Emit(OpCodes.Ldstr, Colon);

            il.Emit(OpCodes.Call, _stringConcat);

            il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
            il.Emit(OpCodes.Pop);

            //il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloca, entryLocal.LocalIndex);
            il.Emit(OpCodes.Call, keyValuePairType.GetProperty("Value").GetGetMethod());
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, WriteSerializeMethodFor(typeBuilder, valueType));

            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Stloc, hasItem);

            il.MarkLabel(startEnumeratorLabel);
            il.Emit(OpCodes.Ldloca_S, enumeratorLocal.LocalIndex);
            il.Emit(OpCodes.Call, enumeratorType.GetMethod("MoveNext", MethodBinding));
            il.Emit(OpCodes.Brtrue, moveNextLabel);
            il.Emit(OpCodes.Leave, endEnumeratorLabel);
            il.BeginFinallyBlock();
            il.Emit(OpCodes.Ldloca_S, enumeratorLocal.LocalIndex);
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
                il.Emit(OpCodes.Stloc, countLocal.LocalIndex);
            } else {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Callvirt, type.GetMethod("get_Count"));
                il.Emit(OpCodes.Stloc, countLocal.LocalIndex);
            }

            il.Emit(OpCodes.Ldloc, countLocal.LocalIndex);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Sub);
            il.Emit(OpCodes.Stloc, diffLocal.LocalIndex);


            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, indexLocal.LocalIndex);
            il.Emit(OpCodes.Br, startLabel);
            il.MarkLabel(endLabel);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, indexLocal.LocalIndex);
            if (isArray)
                il.Emit(OpCodes.Ldelem, itemType);
            else
                il.Emit(OpCodes.Callvirt, type.GetMethod("get_Item"));
            il.Emit(OpCodes.Stloc, itemLocal.LocalIndex);


            //il.Emit(OpCodes.Ldarg_0);

            if (itemLocal.LocalType == _intType) {
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldloc, itemLocal.LocalIndex);
                il.Emit(OpCodes.Call, _generatorIntToStr);
                il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                il.Emit(OpCodes.Pop);
            } else {
                il.Emit(OpCodes.Ldloc, itemLocal.LocalIndex);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Call, WriteSerializeMethodFor(typeBuilder, itemType));
            }


            il.Emit(OpCodes.Ldloc, indexLocal.LocalIndex);
            il.Emit(OpCodes.Ldloc, diffLocal.LocalIndex);
            il.Emit(OpCodes.Beq, checkCountLabel);

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_S, Delimeter);
            il.Emit(OpCodes.Callvirt, _stringBuilderAppendChar);
            il.Emit(OpCodes.Pop);


            il.MarkLabel(checkCountLabel);


            il.Emit(OpCodes.Ldloc, indexLocal.LocalIndex);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, indexLocal.LocalIndex);
            il.MarkLabel(startLabel);
            il.Emit(OpCodes.Ldloc, indexLocal.LocalIndex);
            il.Emit(OpCodes.Ldloc, countLocal.LocalIndex);
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
                var isPrimitive = propType.IsPrimitiveType();

                var isValueType =_ignoreNullValue && propType.IsValueType;
                var propNullLabel = _ignoreNullValue ? il.DefineLabel() : default(Label);
                var equalityMethod = _ignoreNullValue ? propType.GetMethod("op_Equality") : null;
                var propValue = _ignoreNullValue ? il.DeclareLocal(propType) : null;

                if (_ignoreNullValue) {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Callvirt, prop.GetGetMethod());
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


                if (propType == _intType) {
                    il.Emit(OpCodes.Ldarg_1);
                    if (_ignoreNullValue)
                        il.Emit(OpCodes.Ldloc, propValue);
                    else {
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Callvirt, prop.GetGetMethod());
                    }
                    //il.Emit(OpCodes.Box, propType);
                    il.Emit(OpCodes.Call, _generatorIntToStr);
                    il.Emit(OpCodes.Callvirt, _stringBuilderAppend);
                    il.Emit(OpCodes.Pop);
                } else {
                    //il.Emit(OpCodes.Ldarg_0);
                    if (_ignoreNullValue)
                        il.Emit(OpCodes.Ldloc, propValue);
                    else {
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Callvirt, prop.GetGetMethod());
                    }
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Call, WriteSerializeMethodFor(typeBuilder, propType));
                }


                if (_ignoreNullValue)
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
            var type = typeof(T);
            return (NetJSONSerializer<T>)GetSerializer(type);
        }

        internal static object GetSerializer(Type type) {
            var serializer = default(object);
            if (!_serializers.TryGetValue(type, out serializer)) {
                serializer = _serializers[type] = Activator.CreateInstance(Generate(type));
            }
            return serializer;
        }

        public static string Serialize<T>(T value) {
            //lock (_lockObject)
                return GetSerializer<T>().Serialize(value);
        }

        public static void Serialize<T>(T value, TextWriter writer) {
            //lock (_lockObject)
            GetSerializer<T>().Serialize(value, writer);
        }

        public static T Deserialize<T>(string json) {
            //lock (_lockObject)
                return GetSerializer<T>().Deserialize(json);
        }

        public static T Deserialize<T>(TextReader reader) {
            //lock (_lockObject)
            return GetSerializer<T>().Deserialize(reader);
        }

        public static object DeserializeObject(string json) {
            lock (_lockObject)
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
                    new[] { _stringType, _intType.MakeByRefType() });

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
            
            var ptr = il.DeclareLocal(typeof(char*));
            var pinned = il.DeclareLocal(typeof(string), true);

            var @fixed = il.DefineLabel();
            var startLoop = il.DefineLabel();
            var @break = needBreak ? il.DefineLabel() : default(Label);


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

            //Logic before loop

            //current = '\0';
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, current);

            
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
                        return val;
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
                        return val;
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

        private static MethodInfo GenerateExtractValueFor(TypeBuilder typeBuilder, Type type) {
            MethodBuilder method;
            var key = type.FullName;
            var typeName = type.GetName().Fix();
            if (_extractMethodBuilders.TryGetValue(key, out method))
                return method;
            var methodName = String.Concat(ExtractStr, typeName);
            var isObjectType = type == _objectType;
            method = typeBuilder.DefineMethod(methodName, StaticMethodAttribute,
                type, new[] { _stringType, _intType.MakeByRefType() });
            _extractMethodBuilders[key] = method;

            var il = method.GetILGenerator();
            var value = il.DeclareLocal(_stringType);
            
            if (type.IsPrimitiveType()) {
                
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                if (type.IsStringBasedType()) 
                    il.Emit(OpCodes.Call, _getStringBasedValue);    
                else
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
            else if (value.StartsWith("\\/Date(")) {
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
                _voidType, new[] { _stringType, _intType.MakeByRefType(), type, _stringType });
            _setValueMethodBuilders[key] = method;

            const bool Optimized = true;

            var props = type.GetProperties();
            var il = method.GetILGenerator();

            for (var i = 0; i < props.Length; i++) {
                var prop = props[i];
                var propName = prop.Name;
                var conditionLabel = il.DefineLabel();
                var propType = prop.PropertyType;
                
                
                il.Emit(OpCodes.Ldarg_3);
                il.Emit(OpCodes.Ldstr, propName);
                il.Emit(OpCodes.Call, _stringOpEquality);
                il.Emit(OpCodes.Brfalse, conditionLabel);


                if (!Optimized) {
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Call, GenerateExtractValueFor(typeBuilder, propType));
                    il.Emit(OpCodes.Callvirt, prop.SetMethod);
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
                    il.Emit(OpCodes.Callvirt, prop.SetMethod);

                    il.Emit(OpCodes.Ret);

                    il.MarkLabel(propNullLabel);
                }
                
                il.Emit(OpCodes.Ret);

                il.MarkLabel(conditionLabel);
            }

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
                type, new[] { _stringType, _intType.MakeByRefType() });
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
                        var currentQuoteAndPrevSlashLabel = il.DefineLabel();
                        var startIndexEqualZeroLabel = il.DefineLabel();
                        var startIndexNotEqualZeroLabel = il.DefineLabel();
                        var currentEqual2Label = il.DefineLabel();
                        var text = il.DeclareLocal(_stringType);

                        //if(current == '"' && prev != '\\')
                        il.Emit(OpCodes.Ldloc, current);
                        il.Emit(OpCodes.Ldc_I4, (int)'"');
                        il.Emit(OpCodes.Bne_Un, currentQuoteAndPrevSlashLabel);
                        il.Emit(OpCodes.Ldloc, prev);
                        il.Emit(OpCodes.Ldc_I4, (int)'\\');
                        il.Emit(OpCodes.Beq, currentQuoteAndPrevSlashLabel);

                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Ldloc, startIndex);
                        il.Emit(OpCodes.Bne_Un, startIndexEqualZeroLabel);

                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Ldind_I4);
                        il.Emit(OpCodes.Stloc, startIndex);

                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Ldloc, startIndex);
                        il.Emit(OpCodes.Beq, startIndexNotEqualZeroLabel);

                        il.MarkLabel(startIndexEqualZeroLabel);

                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Ldind_I4);
                        il.Emit(OpCodes.Ldc_I4_1);
                        il.Emit(OpCodes.Sub);
                        il.Emit(OpCodes.Stloc, endIndex);

                        il.MarkLabel(startIndexNotEqualZeroLabel);

                        il.Emit(OpCodes.Ldloc, count);
                        il.Emit(OpCodes.Ldc_I4_1);
                        il.Emit(OpCodes.Add);
                        il.Emit(OpCodes.Stloc, count);

                        il.MarkLabel(currentQuoteAndPrevSlashLabel);

                        il.Emit(OpCodes.Ldloc, count);
                        il.Emit(OpCodes.Ldc_I4_2);
                        il.Emit(OpCodes.Bne_Un, currentEqual2Label);

                        il.Emit(OpCodes.Ldloc, ptr);
                        il.Emit(OpCodes.Ldloc, startIndex);
                        il.Emit(OpCodes.Ldc_I4_1);
                        il.Emit(OpCodes.Add);
                        il.Emit(OpCodes.Ldloc, endIndex);
                        il.Emit(OpCodes.Ldloc, startIndex);
                        il.Emit(OpCodes.Sub);
                        il.Emit(OpCodes.Newobj, _strCtorWithPtr);
                        //il.Emit(OpCodes.Call, _createString);
                        il.Emit(OpCodes.Stloc, text);

                        il.Emit(OpCodes.Ldloc, obj);
                        GenerateChangeTypeFor(typeBuilder, elementType, il, text);
                        il.Emit(OpCodes.Callvirt, addMethod);

                        il.MarkLabel(currentEqual2Label);
                    } else {
                        var isEndChar = il.DeclareLocal(_boolType);
                        var startIndexEndCharLabel = il.DefineLabel();
                        var isEndCharLabel = il.DefineLabel();
                        var text = il.DeclareLocal(_stringType);
                        var currentEndCharLabel = il.DefineLabel();
                        var currentEndCharLabel2 = il.DefineLabel();

                        //current == ',' || current == ']' || current == ' ';

                        il.Emit(OpCodes.Ldloc, current);
                        il.Emit(OpCodes.Ldc_I4, (int)',');
                        il.Emit(OpCodes.Beq, currentEndCharLabel);

                        il.Emit(OpCodes.Ldloc, current);
                        il.Emit(OpCodes.Ldc_I4, (int)']');
                        il.Emit(OpCodes.Beq, currentEndCharLabel);

                        il.Emit(OpCodes.Ldloc, current);
                        il.Emit(OpCodes.Ldc_I4, (int)' ');
                        il.Emit(OpCodes.Ceq);
                        il.Emit(OpCodes.Br, currentEndCharLabel2);

                        il.MarkLabel(currentEndCharLabel);
                        il.Emit(OpCodes.Ldc_I4_1);
                        il.MarkLabel(currentEndCharLabel2);
 
                        il.Emit(OpCodes.Stloc, isEndChar);

                        il.Emit(OpCodes.Ldloc, startIndex);
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Bne_Un, startIndexEndCharLabel);
                        il.Emit(OpCodes.Ldloc, isEndChar);
                        il.Emit(OpCodes.Brtrue, startIndexEndCharLabel);

                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Ldind_I4);
                        il.Emit(OpCodes.Stloc, startIndex);

                        il.MarkLabel(startIndexEndCharLabel);

                        il.Emit(OpCodes.Ldloc, isEndChar);
                        il.Emit(OpCodes.Brfalse, isEndCharLabel);

                        il.Emit(OpCodes.Ldloc, ptr);
                        il.Emit(OpCodes.Ldloc, startIndex);
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Ldind_I4);
                        il.Emit(OpCodes.Ldloc, startIndex);
                        il.Emit(OpCodes.Sub);
                        //il.Emit(OpCodes.Call, _createString);
                        il.Emit(OpCodes.Newobj, _strCtorWithPtr);
                        il.Emit(OpCodes.Stloc, text);

                        il.Emit(OpCodes.Ldloc, obj);
                        GenerateChangeTypeFor(typeBuilder, elementType, il, text);
                        il.Emit(OpCodes.Callvirt, addMethod);

                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Stloc, startIndex);
                        il.Emit(OpCodes.Stloc, endIndex);

                        il.MarkLabel(isEndCharLabel);
                    }
                } else {
                    var currentBlank = il.DefineLabel();
                    var currentBlockEnd = il.DefineLabel();

                    il.Emit(OpCodes.Ldloc, current);
                    il.Emit(OpCodes.Ldc_I4, (int)' ');
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
                type, new[] { _stringType, _intType.MakeByRefType() });
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEndChar(char current) {
            return current == ':' || current == '{' || current == ' ';
        }

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsArrayEndChar(char current) {
            return current == ',' || current == ']' || current == ' ';
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCharTag(char current) {
            return current == '{' || current == '}';
        }


        public unsafe static string GetStringBasedValue(string json, ref int index) {
            char current = '\0', prev = '\0';
            int count = 0, startIndex = 0;
            string value = string.Empty;
            
            fixed (char* ptr = json) {
                while (true) {
                    current = *(ptr + index);
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
            }

            return value;
        }

        public unsafe static string GetNonStringValue(string json, ref int index) {
            char current = '\0';
            int startIndex = -1;
            string value = string.Empty;

            fixed (char* ptr = json) {
                while (true) {
                    current = *(ptr + index);
                    if (current != ' ' && current != ':') {
                        if (startIndex == -1)
                            startIndex = index;
                        if (current == ',' || current == ']' || current == '}') {
                            value = new string(ptr, startIndex, index - startIndex);
                            --index;
                            break;
                        }
                    }
                    ++index;
                }
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
