using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NetJSON
{
	public enum BindingFlags
	{
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

	public static partial class Type35Extension
	{
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
	}
}
