using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Perfolizer.Horology;

namespace Chi32.Benchmarks;

internal static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}

internal class CustomBenchmarkConfig : ManualConfig
{
    public CustomBenchmarkConfig()
    {
        AddJob(Job.MediumRun
            .WithMinIterationTime(TimeInterval.FromMilliseconds(100))
            .WithUnrollFactor(1)
            .WithId("VLR_Min100ms_AutoInvoke")
        );
    }
}