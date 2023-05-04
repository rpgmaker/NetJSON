using System.Linq;
using Bogus;

namespace NetJSON.Net7_0.Tests;

public class SimpleObject
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string Value { get; set; }
}
    
public struct SimpleObjectStruct
{
    public int ID;
    public string Name;
    public string Value;
}

public class BuiltInClass
    {
        public bool Bool { get; set; }
        public byte Byte { get; set; }
        public sbyte Sbyte { get; set; }
        public char Char { get; set; }
        public decimal Decimal { get; set; }
        public double Double { get; set; }
        public float Float { get; set; }
        public int Int { get; set; }
        public uint Uint { get; set; }
        public long Long { get; set; }
        public ulong Ulong { get; set; }
        public short Short { get; set; }
        public ushort Ushort { get; set; }

        public bool[]? BoolArray { get; set; }
        public byte[]? ByteArray { get; set; }

        public sbyte[]? SbyteArray { get; set; }

        public char[]? CharArray { get; set; }
        public decimal[]? DecimalArray { get; set; }
        public double[]? DoubleArray { get; set; }
        public float[]? FloatArray { get; set; }
        public int[]? IntArray { get; set; }
        public uint[]? UintArray { get; set; }
        public long[]? LongArray { get; set; }
        public ulong[]? UlongArray { get; set; }
        public short[]? ShortArray { get; set; }
        public ushort[]? UshortArray { get; set; }

        public string? String { get; set; }
        public string[]? StringArray { get; set; }
    }

// https://stackoverflow.com/questions/71786891/create-a-list-of-numbers-in-bogus
    class BuiltInClassFaker : Faker<BuiltInClass>
    {
        public BuiltInClassFaker(int count)
        {
            RuleFor(o => o.Bool, f => f.Random.Bool());
            RuleFor(o => o.Byte, f => f.Random.Byte());
            RuleFor(o => o.Char, f => f.Random.Char());
            RuleFor(o => o.Decimal, f => f.Random.Decimal());
            RuleFor(o => o.Double, f => f.Random.Double());
            RuleFor(o => o.Float, f => f.Random.Float());
            RuleFor(o => o.Int, f => f.Random.Int());
            RuleFor(o => o.Long, f => f.Random.Long());
            RuleFor(o => o.Sbyte, f => f.Random.SByte());
            RuleFor(o => o.Short, f => f.Random.Short());
            RuleFor(o => o.Uint, f => f.Random.UInt());
            RuleFor(o => o.Ulong, f => f.Random.ULong());
            RuleFor(o => o.Ushort, f => f.Random.UShort());

            // RuleFor(o => o.BoolArray, f => Enumerable.Range(1, count).Select(_ => f.Random.Bool()).ToArray());
            RuleFor(o => o.ByteArray, f => Enumerable.Range(1, count).Select(_ => f.Random.Byte()).ToArray());
            RuleFor(o => o.SbyteArray, f => Enumerable.Range(1, count).Select(_ => f.Random.SByte()).ToArray());
            // RuleFor(o => o.CharArray, f => Enumerable.Range(1, count).Select(_ => f.Random.Char()).ToArray());
            RuleFor(o => o.DecimalArray, f => Enumerable.Range(1, count).Select(_ => f.Random.Decimal()).ToArray());
            RuleFor(o => o.DoubleArray, f => Enumerable.Range(1, count).Select(_ => f.Random.Double()).ToArray());
            RuleFor(o => o.FloatArray, f => Enumerable.Range(1, count).Select(_ => f.Random.Float()).ToArray());
            RuleFor(o => o.IntArray, f => Enumerable.Range(1, count).Select(_ => f.Random.Int()).ToArray());
            RuleFor(o => o.UintArray, f => Enumerable.Range(1, count).Select(_ => f.Random.UInt()).ToArray());
            RuleFor(o => o.LongArray, f => Enumerable.Range(1, count).Select(_ => f.Random.Long()).ToArray());
            RuleFor(o => o.UlongArray, f => Enumerable.Range(1, count).Select(_ => f.Random.ULong()).ToArray());
            RuleFor(o => o.ShortArray, f => Enumerable.Range(1, count).Select(_ => f.Random.Short()).ToArray());
            RuleFor(o => o.UshortArray, f => Enumerable.Range(1, count).Select(_ => f.Random.UShort()).ToArray());


            RuleFor(o => o.String, f => f.Lorem.Word());
            RuleFor(o => o.StringArray, f => f.Lorem.Words());
        }
    }