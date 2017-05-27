namespace NetJSON.Extensions {

    internal static class StringExtensions {
        internal static bool IsNullOrWhiteSpace(this string value) {
            if (value == null) return true;
 
            for(int i = 0; i < value.Length; i++) {
                if(!char.IsWhiteSpace(value[i])) return false;
            }
 
            return true;
        }
    }
}