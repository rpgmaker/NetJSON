using System;
using System.IO;

namespace NetJSON
{
	/// <summary>
	/// Attribute for renaming field/property name to use for serialization and deserialization
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Enum)]
	public sealed class NetJSONPropertyAttribute : Attribute
	{
		/// <summary>
		/// Name of property/field
		/// </summary>
		public string Name { get; private set; }
		/// <summary>
		/// Default constructor
		/// </summary>
		/// <param name="name"></param>
		public NetJSONPropertyAttribute(string name) {
			Name = name;
		}
	}

	public sealed class NetJSONSettings
	{
        internal bool _hasDateStringFormat = false;
		/// <summary>
		/// Determine date format: Default: Default
		/// </summary>
		public NetJSONDateFormat DateFormat { get; set; }
        internal string _dateStringFormat;
        /// <summary>
        /// String Format to use for formatting date when provided
        /// </summary>
        public string DateStringFormat {
            get
            {
                return _dateStringFormat;
            }
            set
            {
                _dateStringFormat = value;
                _hasDateStringFormat = !string.IsNullOrEmpty(value);
            }
        }
        /// <summary>
        /// Determine time zone format: Default : Unspecified
        /// </summary>
        public NetJSONTimeZoneFormat TimeZoneFormat { get; set; }
		/// <summary>
		/// Determine formatting for output json: Default: Default
		/// </summary>
		public NetJSONFormat Format { get; set; }
		/// <summary>
		/// Determine if Enum should be serialized as string or int value. Default: True
		/// </summary>
		public bool UseEnumString { get; set; }
		/// <summary>
		/// Determine if default value should be skipped: Default: True
		/// </summary>
		public bool SkipDefaultValue { get; set; }
		public StringComparison _caseComparison = StringComparison.Ordinal;
		private bool _caseSensitive;

		/// <summary>
		/// Determine case sensitive for property/field name: Default: True
		/// </summary>
		public bool CaseSensitive {
			get {
				return _caseSensitive;
			}
			set {
				_caseSensitive = value;
				_caseComparison = _caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
			}
		}

		private NetJSONQuote _quoteType;
		/// <summary>
		/// Quote Type: Default: Double Quote
		/// </summary>
		public NetJSONQuote QuoteType {
			get {
				return _quoteType;
			}
			set {
				_quoteType = value;
				_quoteChar = _quoteType == NetJSONQuote.Single ? '\'' : '"';
				_quoteCharString = _quoteType == NetJSONQuote.Single ? "'" : "\"";
			}
		}

		public char _quoteChar;
		public string _quoteCharString;

		public bool HasOverrideQuoteChar { get; internal set; }

		public bool UseStringOptimization { get; set; }

		/// <summary>
		/// Enable including type information for serialization and deserialization
		/// </summary>
		public bool IncludeTypeInformation { get; set; }

		private StringComparison CaseComparison {
			get {
				return CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
			}
		}

		/// <summary>
		/// Enable camelCasing for property/field names
		/// </summary>
		public bool CamelCase { get; set; }

		/// <summary>
		/// Default constructor
		/// </summary>
		public NetJSONSettings() {
			IncludeTypeInformation = NetJSON.IncludeTypeInformation;
			DateFormat = NetJSON.DateFormat;
			TimeZoneFormat = NetJSON.TimeZoneFormat;
			UseEnumString = NetJSON.UseEnumString;
			SkipDefaultValue = NetJSON.SkipDefaultValue;
			CaseSensitive = NetJSON.CaseSensitive;
			QuoteType = NetJSON.QuoteType;
			UseStringOptimization = NetJSON.UseStringOptimization;
			Format = NetJSONFormat.Default;
			CamelCase = false;
		}

		/// <summary>
		/// Clone settings
		/// </summary>
		/// <returns></returns>
		public NetJSONSettings Clone() {
			return new NetJSONSettings {
				IncludeTypeInformation = IncludeTypeInformation,
				DateFormat = DateFormat,
				TimeZoneFormat = TimeZoneFormat,
				UseEnumString = UseEnumString,
				SkipDefaultValue = SkipDefaultValue,
				CaseSensitive = CaseSensitive,
				QuoteType = QuoteType,
				UseStringOptimization = UseStringOptimization,
				Format = Format
			};
		}

		[ThreadStatic]
		private static NetJSONSettings _currentSettings;
		/// <summary>
		/// Returns current NetJSONSettings that correspond to old use of settings
		/// </summary>
		public static NetJSONSettings CurrentSettings {
			get {
				return _currentSettings ?? (_currentSettings = new NetJSONSettings());
			}
		}
	}

	/// <summary>
	/// Attribute for configuration of Class that requires type information for serialization and deserialization
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, AllowMultiple = true)]
	public sealed class NetJSONKnownTypeAttribute : Attribute
	{
		/// <summary>
		/// Type
		/// </summary>
		public Type Type { private set; get; }
		/// <summary>
		/// Default constructor
		/// </summary>
		/// <param name="type"></param>
		public NetJSONKnownTypeAttribute(Type type) {
			Type = type;
		}
	}

	/// <summary>
	/// Exception thrown for invalid json string
	/// </summary>
	public sealed class NetJSONInvalidJSONException : Exception
	{
		public NetJSONInvalidJSONException()
			: base("Input is not a valid JSON.") {
		}
	}

	/// <summary>
	/// Exception thrown for invalid json property attribute
	/// </summary>
	public sealed class NetJSONInvalidJSONPropertyException : Exception
	{
		/// <summary>
		/// Default constructor
		/// </summary>
		public NetJSONInvalidJSONPropertyException()
			: base("Class cannot contain any NetJSONProperty with null or blank space character") {
		}
	}

	/// <summary>
	/// Exception thrown for invalid assembly generation when adding all assembly into a specified assembly file
	/// </summary>
	public sealed class NetJSONInvalidAssemblyGeneration : Exception
	{
		/// <summary>
		/// Default constructor
		/// </summary>
		/// <param name="asmName"></param>
		public NetJSONInvalidAssemblyGeneration(string asmName) : base(String.Format("Could not generate assembly with name [{0}] due to empty list of types to include", asmName)) { }
	}

	internal abstract class NetJSONSerializer<T>
	{

		public abstract string Serialize(T value);
		public abstract T Deserialize(string value);

		public abstract void Serialize(T value, TextWriter writer);
		public abstract T Deserialize(TextReader reader);

		//With Settings
		public abstract string Serialize(T value, NetJSONSettings settings);
		public abstract T Deserialize(string value, NetJSONSettings settings);
		public abstract void Serialize(T value, TextWriter writer, NetJSONSettings settings);
		public abstract T Deserialize(TextReader reader, NetJSONSettings settings);
	}

	/// <summary>
	/// Option for determining date formatting
	/// </summary>
	public enum NetJSONDateFormat
	{
		/// <summary>
		/// Default /Date(...)/
		/// </summary>
		Default = 0,
		/// <summary>
		/// ISO Format
		/// </summary>
		ISO = 2,
		/// <summary>
		/// Unix Epoch Milliseconds
		/// </summary>
		EpochTime = 4,
		/// <summary>
		/// JSON.NET Format for backward compatibility
		/// </summary>
		JsonNetISO = 6,
        /// <summary>
        /// .NET System.Web.Script.Serialization.JavaScriptSerializer backward compatibility
        /// </summary>
        JavascriptSerializer = 8
	}


	/// <summary>
	/// Option for determining timezone formatting
	/// </summary>
	public enum NetJSONTimeZoneFormat
	{
		/// <summary>
		/// Default unspecified
		/// </summary>
		Unspecified = 0,
		/// <summary>
		/// Utc
		/// </summary>
		Utc = 2,
		/// <summary>
		/// Local time
		/// </summary>
		Local = 4
	}

	/// <summary>
	/// Option for determine what type of quote to use for serialization and deserialization
	/// </summary>
	public enum NetJSONQuote
	{
		/// <summary>
		/// Default: double quote
		/// </summary>
		Default = 0,
		/// <summary>
		/// Use double quote
		/// </summary>
		Double = Default,
		/// <summary>
		/// Use single quote
		/// </summary>
		Single = 2
	}

	/// <summary>
	/// Options for controlling serialize json format
	/// </summary>
	public enum NetJSONFormat
	{
		/// <summary>
		/// Default
		/// </summary>
		Default = 0,
		/// <summary>
		/// Prettify string
		/// </summary>
		Prettify = 2
	}

}
