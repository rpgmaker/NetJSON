namespace NetJSON {
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using global::NetJSON.Extensions;

    internal static class AutomaticTypeConverter {
        public static object ToExpectedType(string value) {
            
            if (value.IsNullOrWhiteSpace()) return value;

            var typeRegExs = TypeRegExs;
            var typeRuleFuncs = TypeRuleFuncs;
            foreach (var regex in typeRegExs)
                if (Regex.IsMatch(value, regex.Value))
                    return typeRuleFuncs[regex.Key](value);
            return value;
        }

        private static Dictionary<string, string> TypeRegExs {
            get {
                var regexs = new Dictionary<string, string>
                {
                    ["bool"] = @"^(false)$|^(true)$",
                    ["date"] = @"^\d{1,2}/\d{1,2}/\d{4}",
                    ["date2"] = @"\\/Date\((?<ticks>-?\d+)\)\\/",
                    ["date3"] = @"^(\d){4}-(\d){2}-(\d){2}T(\d){2}:(\d){2}:(\d){2}.(\d){2,3}Z$",
                    ["int"] = @"^-?\d{1,10}$",
                    ["long"] = @"^-?\d{19}$",
                    ["double"] = @"^-?[0-9]{0,15}(\.[0-9]{1,15})?$|^-?(100)(\.[0]{1,15})?$"
                };
                return regexs;
            }
        }

        private static readonly long Epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).Ticks;
        private static readonly Regex DateRegex = new Regex(@"\\/Date\((?<ticks>-?\d+)\)\\/", RegexOptions.Compiled);

        private static Dictionary<string, Func<string, object>> TypeRuleFuncs {
            get {
                var rules = new Dictionary<string, Func<string, object>>
                {
                    ["bool"] = str => CastTo<bool>(str),
                    ["date"] = str => CastTo<DateTime>(str),
                    ["date2"] = str =>
                    {
                        var ticks = long.Parse(DateRegex.Match(str).Groups["ticks"].Value);
                        return new DateTime(ticks + Epoch).ToLocalTime();
                    },
                    ["date3"] = str => DateTime.Parse(str),
                    ["int"] = str => Internals.SerializerUtilities.FastStringToInt(str),
                    ["long"] = str => Internals.SerializerUtilities.FastStringToLong(str),
                    ["double"] = str => Internals.SerializerUtilities.FastStringToDouble(str)
                };
                return rules;
            }
        }

        private static T CastTo<T>(string str) {
            return (T)Convert.ChangeType(str, typeof(T));
        }
    }
}
