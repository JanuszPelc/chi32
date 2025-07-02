using BenchmarkDotNet.Attributes;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Chi32.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(CustomBenchmarkConfig))]
public class RandomNextBenchmarks
{
    private const long Seed = 0x6A09E667F3BCC908L;
    private const int IterationCount = 1_000_000;
    private long _index;
    private long _selector;

    private Random _systemRandom = null!;
    private Random _systemRandomXoshiro = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _selector = Seed;
        _index = 0;
        _systemRandom = new Random(unchecked((int)Seed)); // Net5CompatDerivedImpl is used
        _systemRandomXoshiro = new Random(); // With no seed specified XoshiroImpl is used
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _index = 0;
    }

    [Benchmark(Description = "Chi32.MinimalNext")]
    public long Chi32_MinimalNext()
    {
        var result = 0L;
        for (var i = 0; i < IterationCount; i++)
            result += Chi32.DeriveValueAt(_selector, _index++);
        return result;
    }

    [Benchmark(Description = "SystemRandom.Next")]
    public long SystemRandom_Next()
    {
        var result = 0L;
        for (var i = 0; i < IterationCount; i++)
            result += _systemRandom.Next();
        return result;
    }

    [Benchmark(Description = "SystemRandomXoshiro.Next")]
    public long SystemRandomXoshiro_Next()
    {
        var result = 0L;
        for (var i = 0; i < IterationCount; i++)
            result += _systemRandomXoshiro.Next();
        return result;
    }
}