using BenchmarkDotNet.Running;
using System;
using NetJSON.Benchmark.Net5_0.Models;
using static System.Console;

namespace NetJSON.Benchmark.Net5_0
{
	static class Program
    {
        static void Main(string[] args)
        {
            WriteLine("1. Simple object");
            WriteLine("2. GUID");
            WriteLine("3. Int32 values");
            WriteLine("4. Single values");
            WriteLine("5. Double values");
            WriteLine("6. String dictionary");
            WriteLine("7. String-object dictionary");
            WriteLine("8. Int32-object");
            WriteLine("9. String-object");
            WriteLine("Type the benchmark number and press Enter key: ");
            if (Int32.TryParse(ReadLine(), out int index)) {
				switch (index) {
                    case 1:
                        BenchmarkRunner.Run<SimpleObjectBenchmark>();
                        return;
                    case 2:
                        BenchmarkRunner.Run<GuidBenchmark>();
                        return;
                    case 3:
                        BenchmarkRunner.Run<Int32ArrayBenchmark>();
                        return;
                    case 4:
                        BenchmarkRunner.Run<SingleArrayBenchmark>();
                        return;
                    case 5:
                        BenchmarkRunner.Run<DoubleArrayBenchmark>();
                        return;
                    case 6:
                        BenchmarkRunner.Run<StringDictionaryBenchmark>();
                        return;
                    case 7:
                        BenchmarkRunner.Run<StringObjectDictionaryBenchmark>();
                        return;
                    case 8:
                        BenchmarkRunner.Run<Int32Benchmark>();
                        BenchmarkRunner.Run<BoxedInt32Benchmark>();
                        return;
                    case 9:
                        BenchmarkRunner.Run<StringObjectBenchmark>();
                        return;
					default:
						break;
				}
			}
        }
    }
}
