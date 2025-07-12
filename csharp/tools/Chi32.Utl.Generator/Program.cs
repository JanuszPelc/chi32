using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;

namespace Chi32.Utl.Generator;

internal record DataDefinition(
    string LogicalName,
    long Seed,
    long Phase,
    int Length,
    string Strategy,
    string BinFileName
);

internal static class Program
{
    private const string OutputDirectoryName = "generated_canonical_data";
    private const string CanonicalMetaCsvFileName = "chi32_canonical_meta.csv";

    private static readonly List<DataDefinition> CanonicalData =
    [
        new(
            "chi32_sequential",
            42L,
            int.MaxValue - short.MaxValue,
            ushort.MaxValue,
            "sequential",
            "chi32_sequential.bin"
        ),
        new(
            "chi32_swapped",
            -42L,
            short.MaxValue,
            ushort.MaxValue,
            "swapped",
            "chi32_swapped.bin"
        ),
        new(
            "chi32_feedback",
            0,
            0,
            ushort.MaxValue,
            "feedback",
            "chi32_feedback.bin"
        )
    ];

    public static void Main(string[] _)
    {
        Console.WriteLine("CHI32 Canonical Data Generator");
        Console.WriteLine($"Output will be placed in ./{OutputDirectoryName}/");
        Console.WriteLine("---");

        var stopwatch = Stopwatch.StartNew();

        var baseOutputDirectory = Path.GetDirectoryName(AppContext.BaseDirectory);
        if (string.IsNullOrEmpty(baseOutputDirectory))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("Error: Could not determine executable directory.");
            Console.ResetColor();
            Environment.ExitCode = 1;
            return;
        }

        var outputFullPath = Path.Combine(baseOutputDirectory, OutputDirectoryName);
        Directory.CreateDirectory(outputFullPath);

        var csvBuilder = new StringBuilder();
        csvBuilder.AppendLine("# MetaData for CHI32 Canonical Tests");
        csvBuilder.AppendLine("# Fields: logical_name,strategy_code,seed,phase,length,bin_filename");
        csvBuilder.AppendLine("# strategy_code: 0=sequential, 1=swapped, 2=feedback");

        foreach (var definition in CanonicalData)
        {
            Console.WriteLine($"Processing definition: {definition.LogicalName}...");
            Console.WriteLine(
                $"  Seed: {definition.Seed}, Phase: {definition.Phase}, Length: {definition.Length}, Strategy: {definition.Strategy}");

            var data = GenerateCanonicalData(definition);
            WriteDataToFile(data, Path.Combine(outputFullPath, definition.BinFileName),
                definition.LogicalName);

            var strategyCode = definition.Strategy.ToLowerInvariant() switch
            {
                "sequential" => 0,
                "swapped" => 1,
                "feedback" => 2,
                _ => throw new InvalidOperationException($"Unknown strategy: {definition.Strategy}")
            };

            csvBuilder.AppendLine(
                $"{definition.LogicalName},{strategyCode},{definition.Seed},{definition.Phase},{definition.Length},{definition.BinFileName}"
            );

            Console.WriteLine($"  Generated: {definition.BinFileName}, Appended to CSV: {definition.LogicalName}");
            Console.WriteLine("---");
        }

        var csvFilePath = Path.Combine(outputFullPath, CanonicalMetaCsvFileName);
        try
        {
            File.WriteAllText(csvFilePath, csvBuilder.ToString());
            Console.WriteLine($"Successfully wrote CSV metadata to: {Path.GetFileName(csvFilePath)}");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Error writing CSV metadata file '{Path.GetFileName(csvFilePath)}': {ex.Message}");
            Console.ResetColor();
        }

        stopwatch.Stop();
        Console.WriteLine(
            $"Canonical data files and CSV metadata generated successfully in {stopwatch.ElapsedMilliseconds} ms.");
        Console.WriteLine($"Files are in: {Path.GetFullPath(outputFullPath)}");
    }

    private static List<uint> GenerateCanonicalData(DataDefinition definition)
    {
        var stopwatch = Stopwatch.StartNew();
        var data = definition.Strategy.ToLower() switch
        {
            "sequential" => GenerateSequentialData(definition),
            "swapped" => GenerateSwappedData(definition),
            "feedback" => GenerateFeedbackData(definition),
            _ => throw new InvalidDataException($"Invalid '{nameof(definition.Strategy)}' {definition.Strategy}")
        };
        stopwatch.Stop();

        Console.WriteLine($"    Data generation for {definition.LogicalName} took {stopwatch.ElapsedMilliseconds} ms.");
        return data;
    }

    private static List<uint> GenerateSequentialData(DataDefinition definition)
    {
        var data = new List<uint>(definition.Length);
        var currentPhase = definition.Phase;

        for (var i = 0; i < definition.Length; i++)
        {
            var value = Chi32.DeriveValueAt(definition.Seed, currentPhase);
            data.Add((uint)value);

            currentPhase++;
        }

        return data;
    }

    private static List<uint> GenerateSwappedData(DataDefinition definition)
    {
        var data = new List<uint>(definition.Length);
        var currentPhase = definition.Phase;

        for (var i = 0; i < definition.Length; i++)
        {
            var value = Chi32.DeriveValueAt(currentPhase, definition.Seed);
            data.Add((uint)value);

            currentPhase--;
        }

        return data;
    }

    private static List<uint> GenerateFeedbackData(DataDefinition definition)
    {
        var data = new List<uint>(definition.Length);
        var currentSeed = definition.Seed;
        var currentPhase = definition.Phase;

        for (var i = 0; i < definition.Length; i++)
        {
            var value = Chi32.DeriveValueAt(currentSeed, currentPhase);
            data.Add((uint)value);

            currentSeed = (long)(((ulong)currentSeed << 32) | ((ulong)currentPhase >> 32));
            currentPhase = (long)(((ulong)currentPhase << 32) | (uint)value);
        }

        return data;
    }

    private static void WriteDataToFile(List<uint> data, string outputFilePath, string logicalName)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var fileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[4];
            foreach (var uValue in data)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(buffer, uValue);
                fileStream.Write(buffer, 0, 4);
            }

            fileStream.Flush();

            var fileSize = new FileInfo(outputFilePath).Length;
            sw.Stop();
            Console.WriteLine(
                $"    Wrote {Path.GetFileName(outputFilePath)} ({fileSize} bytes) in {sw.ElapsedMilliseconds} ms.");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(
                $"Error writing data file '{Path.GetFileName(outputFilePath)}' for {logicalName}: {ex.Message}");
            Console.ResetColor();
        }
    }
}