namespace Example
{
    using HexaEngine.Sappho;
    using System.Diagnostics;

    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            Baa baa = new() { Name = "TestBaa", ValueB = 42 };

            Test test = new() { Value1 = 3.14f, B = baa };
            baa.Test = test;


            SapphoSerializationContext context = new();
            context.RegisterType<Foo>(Foo.SapphoTypeId);
            context.RegisterType<Baa>(Baa.SapphoTypeId);
            
            
            SapphoWriter writer = new(context);
            for (int i = 0; i < 100000; i++)
            {
                writer.WriteObject(test);
                writer.Reset();
            }

            long[] samples = new long[100000];
            for (int i = 0; i < 100000; i++)
            {
                long start = Stopwatch.GetTimestamp();
                writer.WriteObject(test);
                writer.Reset();
                long end = Stopwatch.GetTimestamp();
                samples[i] = end - start;
            }

            double frequency = (double)Stopwatch.Frequency;
            double averageTicks = (double)samples.Sum() / samples.Length;
            double averageTime = averageTicks / frequency * 1000.0 * 1000.0 * 1000.0;
            Console.WriteLine($"Average serialization time: {averageTime} ns");
        }
    }

    [SapphoObject]
    public partial class Test
    {
        public float Value1 { get; set; }

        public float Valueb { get; set; }

        public float Valu1 { get; set; }

        public float Vale1 { get; set; }

        public float Vaue1 { get; set; }

        [SapphoPolymorphic]
        public Foo B { get; set; }
    }

    [SapphoObject]
    public partial class Foo
    {
        public string Name { get; set; }
    }

    [SapphoObject]
    public partial class Baa : Foo
    {
        public int ValueB { get; set; }

        public Test Test { get; set; }
    }
}