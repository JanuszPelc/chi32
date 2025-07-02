using BenchmarkDotNet.Attributes;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Chi32.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(CustomBenchmarkConfig))]
public class DeriveValueAtBenchmarks
{
    private const long DefaultSelector = 0x6A09E667F3BCC908L;
    private const long SmallStep = 1;
    private const long HugeStep = unchecked((long)0x9E3779B97F4A7C55);

    private long _hugeStepIndex;
    private long _smallStepIndex;

    [Benchmark(Description = "Chi32.DeriveValueAt(smallStep)")]
    public int Core_ComputeAt_IncrementalPhase()
    {
        var result = Chi32.DeriveValueAt(DefaultSelector, _smallStepIndex);
        _smallStepIndex += SmallStep;
        return result;
    }

    [Benchmark(Description = "Chi32.DeriveValueAt(hugeStep)")]
    public int Core_ComputeAt_RandomJumps()
    {
        var result = Chi32.DeriveValueAt(DefaultSelector, _hugeStepIndex);
        _hugeStepIndex += HugeStep;
        return result;
    }
}