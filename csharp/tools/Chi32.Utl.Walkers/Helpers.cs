using System.Collections.Concurrent;

namespace Chi32.Utl.Walkers;

internal interface IDirectionGenerator
{
    int NextDirection();
}

internal class SimulationState
{
    public required WalkerSimulation Simulation { get; init; }
    public string StatusMessage { get; set; } = "";
}

internal sealed class ChunkScheduler : IDisposable
{
    private readonly Thread[] _threads;
    private readonly BlockingCollection<Action> _work = new();

    public ChunkScheduler(int threadCount)
    {
        _threads = new Thread[threadCount];
        for (var i = 0; i < threadCount; i++)
        {
            _threads[i] = new Thread(Worker) { IsBackground = true };
            _threads[i].Start();
        }
    }

    public void Dispose()
    {
        _work.CompleteAdding();
        foreach (var thread in _threads)
            thread.Join();
    }

    public void Enqueue(Action chunk)
    {
        if (Random.Shared.Next(2) == 0)
            Thread.Sleep(Random.Shared.Next(1, 250));
        _work.Add(chunk);
    }

    private void Worker()
    {
        foreach (var action in _work.GetConsumingEnumerable())
            try
            {
                action();
            }
            catch
            {
                // Ignore individual chunk failures to continue processing
            }
    }
}

internal static class SeedHelper
{
}