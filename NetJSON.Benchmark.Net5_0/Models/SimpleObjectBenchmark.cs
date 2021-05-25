namespace NetJSON.Benchmark.Net5_0
{
	public class SimpleObject
    {
        public string Name { get; set; }
        public int ID { get; set; }
    }

    public class SimpleObjectBenchmark : Benchmark<SimpleObject>
	{
		public SimpleObjectBenchmark() : base (new SimpleObject { ID = 10, Name = "Performance" }) {
		}
	}

	public class Int32Benchmark : Benchmark<int>
	{
		public Int32Benchmark() : base(3) {
		}
	}

	public class BoxedInt32Benchmark : Benchmark<object>
	{
		public BoxedInt32Benchmark() : base(3) {
		}
	}

	public class StringObjectBenchmark : Benchmark<string>
	{
		public StringObjectBenchmark() : base("Hello world") {
		}
	}
}
