using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Text.Json;

namespace NetJSON.Benchmark.Net5_0
{
    public class SimpleObject
    {
        public string Name { get; set; }
        public int ID { get; set; }
    }

    public class SimpleObjectBenchmark
    {
        private readonly SimpleObject obj;

        public SimpleObjectBenchmark()
        {
            obj = new SimpleObject { ID = 10, Name = "Performance" };
        }

        [Benchmark]
        public string NetJson() => NetJSON.Serialize(obj);

        [Benchmark]
        public string MicrosoftJson() => JsonSerializer.Serialize(obj);

        [Benchmark]
        public byte[] FastestUtf8Json() => Utf8Json.JsonSerializer.Serialize(obj);
    }

    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<SimpleObjectBenchmark>();
        }
    }
}
