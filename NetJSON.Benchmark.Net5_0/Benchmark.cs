using BenchmarkDotNet.Attributes;
using System.Text.Json;
using static System.Console;

namespace NetJSON.Benchmark.Net5_0
{
	public abstract class Benchmark<TObj>
    {
        private readonly TObj obj;

        protected Benchmark(TObj instance)
        {
            obj = instance;
            WriteLine("NetJSON: " + NetJSON.Serialize(instance));
            WriteLine("MS JSON: " + JsonSerializer.Serialize(instance));
            WriteLine("UTFJSON: " + System.Text.Encoding.UTF8.GetString(Utf8Json.JsonSerializer.Serialize(instance)));
        }

        [Benchmark]
        public string NetJson() => NetJSON.Serialize(obj);

        [Benchmark]
        public string MicrosoftJson() => JsonSerializer.Serialize(obj);

        [Benchmark]
        public byte[] FastestUtf8Json() => Utf8Json.JsonSerializer.Serialize(obj);
    }
}
