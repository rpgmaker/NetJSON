using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace NetJSON
{
	public class ConcurrentDictionary<K, V> : Dictionary<K, V>
	{
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

	public static class String35Extension
	{
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

	public static class StringBuilder35Extension
	{
		public static StringBuilder Clear(this StringBuilder value) {
			value.Length = 0;
			return value;
		}
	}

	public static partial class Type35Extension
	{
		public static Type GetEnumUnderlyingType(this Type type) {
			FieldInfo[] fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			if ((fields == null) || (fields.Length != 1)) {
				throw new ArgumentException("Invalid enum");
			}
			return fields[0].FieldType;
		}
	}
}
