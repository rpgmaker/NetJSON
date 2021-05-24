using System;

namespace NetJSON.Benchmark.Net5_0.Models
{
	public class GuidBenchmark : Benchmark<Guid>
	{
		public GuidBenchmark() : base(Guid.NewGuid()) {
		}
	}
}
