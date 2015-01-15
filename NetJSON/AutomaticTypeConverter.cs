using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NetJSON {
    public static class AutomaticTypeConverter {
        public static object ToExpectedType(string value) {
            if (string.IsNullOrWhiteSpace(value)) return value;
            foreach (var regex in TypeRegExs.Where(regex => Regex.IsMatch(value, regex.Value)))
                return TypeRuleFuncs[regex.Key](value);
            return value;
        }

        private static Dictionary<string, string> TypeRegExs {
            get {
                var regexs = new Dictionary<string, string>();
                regexs["bool"] = @"^(false)$|^(true)$";
                regexs["date"] = @"^\d{1,2}/\d{1,2}/\d{4}";
                regexs["date2"] = @"\\/Date\((?<ticks>-?\d+)\)\\/";
                regexs["date3"] = @"(\d){4}-(\d){2}-(\d){2}T(\d){2}:(\d){2}:(\d){2}.(\d){3}Z";
                regexs["int"] = @"^-?\d{1,10}$";
                regexs["long"] = @"^-?\d{19}$";
                regexs["double"] = @"^-?[0-9]{0,15}(\.[0-9]{1,15})?$|^-?(100)(\.[0]{1,15})?$";
                return regexs;
            }
        }

        private static readonly long _epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).Ticks;
        private static readonly Regex _dateRegex = new Regex(@"\\/Date\((?<ticks>-?\d+)\)\\/", RegexOptions.Compiled);

        private static Dictionary<string, Func<string, object>> TypeRuleFuncs {
            get {
                var rules = new Dictionary<string, Func<string, object>>();
                rules["bool"] = str => CastTo<bool>(str);
                rules["date"] = str => CastTo<DateTime>(str);
                rules["date2"] = str => {
                                            var ticks = long.Parse(_dateRegex.Match(str).Groups["ticks"].Value);
                                            return new DateTime(ticks + _epoch).ToLocalTime();
                };
                rules["date3"] = str => DateTime.Parse(str);
                rules["int"] = str => NetJSON.FastStringToInt(str);
                rules["long"] = str => NetJSON.FastStringToLong(str);
                rules["double"] = str => NetJSON.FastStringToDouble(str);
                return rules;
            }
        }

        private static T CastTo<T>(string str) => (T)Convert.ChangeType(str, typeof(T));
    }
}
