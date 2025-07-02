using BenchmarkDotNet.Attributes;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Chi32.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(CustomBenchmarkConfig))]
public class UpdateHashValueBenchmarks
{
    private int _valueToHash;

    [IterationSetup]
    public void IterationSetup()
    {
        _valueToHash = 0;
    }

    [Benchmark(Description = "Chi32.UpdateHashValue")]
    public int Core_UpdateHashValue_SingleInt()
    {
        return Chi32.UpdateHashValue(0, _valueToHash++);
    }

    [Benchmark(Description = "System.HashCode.Combine")]
    public int HashCode_Combine_SingleInt()
    {
        return HashCode.Combine(_valueToHash++);
    }
}