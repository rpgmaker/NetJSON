using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetJSON.Benchmark.Net5_0.Models
{
	public class StringDictionaryBenchmark : Benchmark<Dictionary<string, string>>
	{
		public StringDictionaryBenchmark() : base(new Dictionary<string, string> {
			{ "name", "item 1" },
			{ "address", "Max, 12, PYM, LO" },
			{ "receiver", "Mike" },
			{ "unit", "US$" }
		}) {

		}
	}
	public class StringObjectDictionaryBenchmark : Benchmark<Dictionary<string, object>>
	{
		public StringObjectDictionaryBenchmark() : base(new Dictionary<string, object> {
			{ "name", "item 1" },
			{ "time", DateTime.Now },
			{ "address", "Max, 12, PYM, LO" },
			{ "receiver", "Mike" },
			{ "price", 5.8 },
			{ "unit", "US$" }
		}) {

		}
	}
}
