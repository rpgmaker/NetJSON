using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetJSON.Benchmark.Net5_0.Models
{
	public class Int32ArrayBenchmark : Benchmark<int[]>
	{
		public Int32ArrayBenchmark() : base(new int[] { 0, 1, 2, Int32.MinValue, Int32.MaxValue, -1 }) {
		}
	}

	public class SingleArrayBenchmark : Benchmark<float[]>
	{
		public SingleArrayBenchmark() : base(new float[] { 0, 1, 2, Single.MinValue, Single.MaxValue, (float)Math.PI, (float)Math.E }) {
		}
	}

	public class DoubleArrayBenchmark : Benchmark<double[]>
	{
		public DoubleArrayBenchmark() : base(new double[] { 0, 1, 2, Double.MinValue, Double.MaxValue, Math.PI, Math.E }) {
		}
	}
}
