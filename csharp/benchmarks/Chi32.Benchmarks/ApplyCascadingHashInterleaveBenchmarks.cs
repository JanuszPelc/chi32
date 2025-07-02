using BenchmarkDotNet.Attributes;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Chi32.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(CustomBenchmarkConfig))]
public class ApplyCascadingHashInterleaveBenchmarks
{
    private const long DefaultSelector = 0x6A09E667F3BCC908L;
    private long _index;
    private long _selector;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _selector = DefaultSelector;
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _index = 0;
    }

    [Benchmark(Description = "Chi32.ApplyCascadingHashInterleave")]
    public long Core_ApplyCascadingHashInterleave()
    {
        return Chi32.ApplyCascadingHashInterleave(_selector, _index++);
    }
}