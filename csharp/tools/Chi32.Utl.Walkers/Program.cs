using Chi32.Utl.Walkers;
using Chi32.Utl.Walkers.Generators;

const ulong seed = 0x88B66D918A3B2AD9;

var generators = new Dictionary<string, Func<IDirectionGenerator>>
{
    { "chi32", () => new Chi32DirectionGenerator(unchecked((long)seed)) },
    { "chacha20", () => new ChaCha20DirectionGenerator(seed) },
    { "jsf32", () => new Jsf32DirectionGenerator(unchecked((uint)seed)) },
    { "lcg64", () => new Lcg64DirectionGenerator(seed) },
    { "msws", () => new MswsDirectionGenerator(seed) },
    { "mwc", () => new MwcDirectionGenerator(unchecked((uint)seed), unchecked((uint)(seed >> 32))) },
    { "pcg32", () => new Pcg32DirectionGenerator(seed) },
    { "pcg_xsl_rr", () => new PcgXslRrDirectionGenerator(seed) },
    { "romu_duo_jr", () => new RomuDuoJrDirectionGenerator(seed) },
    { "romu_trio", () => new RomuTrioDirectionGenerator(seed) },
    { "splitmix64", () => new SplitMix64DirectionGenerator(seed) },
    { "system_random", () => new SystemRandomDirectionGenerator(unchecked((int)seed)) },
    { "xoroshiro64ss", () => new Xoroshiro64StarStarDirectionGenerator(unchecked((uint)seed)) }
};

(string Description, string FolderName, ulong WalkerSteps, int ScaleShift)[] configurations =
[
    ("Millions of steps (fast)", "millions_of_steps", 4ul * 1_000_000, 3),
    ("Billions of steps (thousand times slower)", "billions_of_steps", 4ul * 1_000_000_000, 8),
    ("Trillions of steps (million times slower)", "trillions_of_steps", 4ul * 1_000_000_000_000, 13)
];

Console.WriteLine("Select simulation configuration:");
for (var i = 0; i < configurations.Length; i++)
    Console.WriteLine($"  {i}: {configurations[i].Description}");

Console.Write("Enter index [0]: ");
var input = Console.ReadLine();
var index = int.TryParse(input, out var parsed) && parsed >= 0 && parsed < configurations.Length
    ? parsed
    : throw new Exception("Invalid input");
var config = configurations[index];

var outputDir = config.FolderName;
Directory.CreateDirectory(outputDir);

var simulations = generators
    .Select(kv => new WalkerSimulation(config.WalkerSteps, config.ScaleShift, kv.Key, kv.Value))
    .ToList();

Console.CancelKeyPress += (_, _) => Console.CursorVisible = true;
AppDomain.CurrentDomain.ProcessExit += (_, _) => Console.CursorVisible = true;
Console.CursorVisible = false;

Console.Clear();
var baseTop = 0;
try
{
    baseTop = Console.CursorTop;
    ClearConsoleLinesLocal(baseTop, simulations.Count + 2);
}
catch (IOException)
{
}

var simulationStates = simulations.Select(sim => new SimulationState
{
    Simulation = sim,
    StatusMessage = ""
}).ToList();

var threadCount = Math.Max(1, (int)(2f / 3 * Math.Min(simulations.Count, Environment.ProcessorCount)));
var scheduler = new ChunkScheduler(threadCount);

foreach (var state in simulationStates)
    // ReSharper disable once AccessToDisposedClosure
    scheduler.Enqueue(() => RunSimulationInChunks(state, scheduler));

RenderProgress(simulationStates, baseTop);

while (simulationStates.Any(s => string.IsNullOrEmpty(s.StatusMessage)))
{
    RenderProgress(simulationStates, baseTop);
    await Task.Delay(TimeSpan.FromSeconds(1));
}

RenderProgress(simulationStates, baseTop);
scheduler.Dispose();

try
{
    Console.CursorVisible = true;
    if (Console.WindowHeight > 0)
        Console.SetCursorPosition(0, Math.Min(baseTop + simulations.Count, Console.BufferHeight - 1));
}
catch (IOException)
{
}

Console.WriteLine();
Console.WriteLine($"All images saved to {Path.GetFullPath(outputDir)}");

return 0;

void RunSimulationInChunks(SimulationState state, ChunkScheduler chunkScheduler)
{
    try
    {
        if (state.Simulation.IsComplete)
        {
            var fileName = $"{state.Simulation.Name}_walker.bmp";
            var path = Path.Combine(outputDir, fileName);

            state.Simulation.SaveBmp(path);
            state.StatusMessage = "(saved)";

            return;
        }

        state.Simulation.RunChunk();
        chunkScheduler.Enqueue(() => RunSimulationInChunks(state, chunkScheduler));
    }
    catch (Exception ex)
    {
        var msg = ex.GetBaseException().Message;
        var shortError = msg.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Unknown";
        state.StatusMessage = $"(error! {shortError[..Math.Min(shortError.Length, 30)]}...)";
    }
}

void RenderProgress(List<SimulationState> currentStates, int currentBaseTop)
{
    const int width = 50;
    var canUseConsolePosition = Console.WindowHeight > 0 && Console.WindowWidth > 0;

    for (var i = 0; i < currentStates.Count; i++)
    {
        var state = currentStates[i];
        var sim = state.Simulation;
        var progressValue = sim.IsComplete
            ? state.Simulation.StepsPerWalker
            : sim.Progress;

        var ratio = progressValue / (float)state.Simulation.StepsPerWalker;
        ratio = Math.Clamp(ratio, 0f, 1f);
        var filled = (int)(ratio * width);

        var label = sim.Name.PadRight(24);
        if (canUseConsolePosition)
            try
            {
                Console.SetCursorPosition(0, currentBaseTop + i);
            }
            catch (IOException)
            {
                canUseConsolePosition = false;
            }

        var filledBar = new string('#', filled);
        var remainingBar = new string('-', width - filled);
        var completionRate = $"{ratio:P2}".PadLeft(7);
        var suffix = string.IsNullOrEmpty(state.StatusMessage) ? "" : $" {state.StatusMessage}";

        var steps = state.Simulation.Progress;
        var seconds = state.Simulation.CpuTime.TotalSeconds;
        var throughput = seconds > 0 ? steps / seconds : 0;
        var throughputInfo = $"{throughput / 1_000_000.0,6:F1} M/s";

        var line = $"[{filledBar}{remainingBar}] {completionRate} {label} {throughputInfo}{suffix}";

        if (canUseConsolePosition)
            Console.Write(line.PadRight(Console.WindowWidth > 0 ? Console.WindowWidth - 1 : 80));
        else
            Console.WriteLine(line);
    }
}

void ClearConsoleLinesLocal(int top, int count)
{
    if (Console.WindowHeight == 0 || Console.WindowWidth == 0) return;

    try
    {
        Console.SetCursorPosition(0, top);
        for (var i = 0; i < count; i++)
        {
            if (top + i >= Console.BufferHeight) break;
            Console.Write(new string(' ', Console.WindowWidth - 1));
        }

        Console.SetCursorPosition(0, top);
    }
    catch (IOException)
    {
    }
}