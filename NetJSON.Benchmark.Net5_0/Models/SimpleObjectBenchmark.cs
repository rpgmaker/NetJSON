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
}
