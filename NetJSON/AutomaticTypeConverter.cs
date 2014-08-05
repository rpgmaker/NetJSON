using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetJSON {
    public static class AutomaticTypeConverter {
        public static object ToExpectedType(string value) {
            if (String.IsNullOrWhiteSpace(value)) return value;
            var typeRegExs = TypeRegExs;
            var typeRuleFuncs = TypeRuleFuncs;
            foreach (var regex in typeRegExs)
                if (Regex.IsMatch(value, regex.Value))
                    return typeRuleFuncs[regex.Key](value);
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
        private static Regex _dateRegex = new Regex(@"\\/Date\((?<ticks>-?\d+)\)\\/", RegexOptions.Compiled);

        private static Dictionary<string, Func<string, object>> TypeRuleFuncs {
            get {
                var rules = new Dictionary<string, Func<string, object>>();
                rules["bool"] = new Func<string, object>(str => { return CastTo<bool>(str); });
                rules["date"] = new Func<string, object>(str => { return CastTo<DateTime>(str); });
                rules["date2"] = new Func<string, object>(str => {
                    var ticks = long.Parse(_dateRegex.Match(str).Groups["ticks"].Value);
                    return new DateTime(ticks + _epoch).ToLocalTime();
                });
                rules["date3"] = new Func<string, object>(str => { return DateTime.Parse(str); });
                rules["int"] = new Func<string, object>(str => { return NetJSON.FastStringToInt(str); });
                rules["long"] = new Func<string, object>(str => { return NetJSON.FastStringToLong(str); });
                rules["double"] = new Func<string, object>(str => { return NetJSON.FastStringToDouble(str); });
                return rules;
            }
        }

        private static T CastTo<T>(string str) {
            return (T)Convert.ChangeType(str, typeof(T));
        }
    }
}
