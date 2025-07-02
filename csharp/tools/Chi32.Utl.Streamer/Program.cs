using System.Buffers.Binary;
using System.CommandLine;
using System.Diagnostics;
using System.Globalization;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Chi32.Utl.Streamer;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var strategyOption = new Option<string>(
            "--strategy",
            "The generation strategy: 'sequential', 'swapped', or 'feedback'.")
        {
            IsRequired = false
        };
        strategyOption.SetDefaultValue("sequential");
        strategyOption.AddCompletions("sequential", "swapped", "feedback");
        strategyOption.AddValidator(result =>
        {
            var val = result.GetValueForOption(strategyOption);
            if (val?.ToLowerInvariant() is not ("sequential" or "swapped" or "feedback"))
                result.ErrorMessage = "Strategy must be 'sequential', 'swapped', or 'feedback'.";
        });

        var seedOption = new Option<long>(
            "--seed",
            description: "The 64-bit seed (decimal or 0x prefixed hex). This option is required.",
            parseArgument: argumentResult =>
            {
                var token = argumentResult.Tokens.FirstOrDefault()?.Value;
                if (string.IsNullOrEmpty(token))
                    return 0;

                try
                {
                    return ParseLongFlexible(token);
                }
                catch (Exception ex)
                {
                    argumentResult.ErrorMessage = $"Invalid seed '{token}': {ex.Message}";
                    return 0;
                }
            })
        {
            IsRequired = true
        };

        var phaseOption = new Option<long>(
            "--phase",
            description: "The starting 64-bit phase (decimal or 0x prefixed hex). Defaults to 0.",
            parseArgument: argumentResult =>
            {
                var token = argumentResult.Tokens.FirstOrDefault()?.Value;
                if (string.IsNullOrEmpty(token))
                    return 0L;

                try
                {
                    return ParseLongFlexible(token);
                }
                catch (Exception ex)
                {
                    argumentResult.ErrorMessage = $"Invalid phase '{token}': {ex.Message}";
                    return 0;
                }
            });
        phaseOption.SetDefaultValue(0L);

        var rootCommand = new RootCommand(
            "CHI32 Streamer (chi32stream): Generates a continuous stream of CHI32 pseudo-random numbers.\n" +
            "Redirect standard output to a file or pipe to a consumer (e.g., PractRand).\n" +
            "Example: chi32stream --seed 0xFEDCBA9876543210 --strategy swapped | RNG_test stdin32")
        {
            strategyOption,
            seedOption,
            phaseOption
        };

        rootCommand.SetHandler(context =>
        {
            var strategyValue = context.ParseResult.GetValueForOption(strategyOption) ?? "sequential";
            var seedValue = context.ParseResult.GetValueForOption(seedOption);
            var phaseValue = context.ParseResult.GetValueForOption(phaseOption);

            var strategy = strategyValue.ToLowerInvariant();

            try
            {
                using var outputStream = Console.OpenStandardOutput();
                StreamerCore.GenerateStream(strategy, seedValue, phaseValue, outputStream);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[{DateTime.Now:HH:mm:ss}] Critical error setting up stream: {ex.Message}");
                context.ExitCode = 2;
            }
        });

        return await rootCommand.InvokeAsync(args);
    }

    private static long ParseLongFlexible(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            throw new ArgumentException("Input string for parsing cannot be null or empty.");

        var trimmedInput = s.Trim();

        if (trimmedInput.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            var hexPart = trimmedInput.Length >= 3 ? trimmedInput[2..] : "";
            if (hexPart.Length == 0 || hexPart.Length > 16)
                throw new FormatException(
                    $"Hex string '{trimmedInput}' is empty or too long after '0x'. Max 16 hex digits allowed.");
            if (ulong.TryParse(hexPart, NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                    out var ulongHexResult)) return unchecked((long)ulongHexResult);
            throw new FormatException($"Invalid hexadecimal format for input: '{trimmedInput}'.");
        }

        if (long.TryParse(trimmedInput, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longDecResult))
            return longDecResult;
        if (trimmedInput.All(char.IsDigit) &&
            ulong.TryParse(trimmedInput, NumberStyles.None, CultureInfo.InvariantCulture,
                out var ulongDecResultPositive) &&
            ulongDecResultPositive > long.MaxValue)
            throw new OverflowException(
                $"Decimal value '{trimmedInput}' is positive but too large to fit in a signed 64-bit integer. " +
                $"If this is intended as a hex value, prefix with '0x'.");
        throw new FormatException($"Invalid decimal long format for input: '{trimmedInput}'.");
    }
}

public static class StreamerCore
{
    private const int BufferSize = 65536;

    public static void GenerateStream(
        string strategy,
        long initialSeed,
        long initialPhase,
        Stream outputStream)
    {
        Console.Error.WriteLine(
            $"[{DateTime.Now:HH:mm:ss}] Streamer: Starting with Strategy='{strategy}', Seed=0x{initialSeed:X16} ({initialSeed}), Phase=0x{initialPhase:X16} ({initialPhase})");

        var stopwatch = Stopwatch.StartNew();
        ulong generatedCount = 0;

        try
        {
            switch (strategy.ToLowerInvariant())
            {
                case "sequential":
                    generatedCount = StreamSequential(initialSeed, initialPhase, outputStream);
                    break;
                case "swapped":
                    generatedCount = StreamSwapped(initialSeed, initialPhase, outputStream);
                    break;
                case "feedback":
                    generatedCount = StreamFeedback(initialSeed, initialPhase, outputStream);
                    break;
                default:
                    Console.Error.WriteLine($"Internal Error: Unknown strategy '{strategy}'.");
                    Environment.ExitCode = 1;
                    return;
            }

            stopwatch.Stop();
            Console.Error.WriteLine(
                $"[{DateTime.Now:HH:mm:ss}] Streamer: Normal exit. Total uints: {generatedCount}. Time: {stopwatch.Elapsed}.");
        }
        catch (IOException ex) when (IsPipeBrokenException(ex))
        {
            stopwatch.Stop();
            Console.Error.WriteLine(
                $"[{DateTime.Now:HH:mm:ss}] Streamer: Pipe broken or stream closed (consumer likely exited).");
            Console.Error.WriteLine($"  Total uints during this run: {generatedCount}. Time: {stopwatch.Elapsed}.");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Console.Error.WriteLine($"[{DateTime.Now:HH:mm:ss}] Streamer: Unexpected error: {ex.Message}");
            Console.Error.WriteLine($"  Total uints during this run: {generatedCount}. Time: {stopwatch.Elapsed}.");
            Environment.ExitCode = 1;
        }
    }

    private static ulong StreamSequential(long seed, long initialPhase, Stream outputStream)
    {
        var buffer = new byte[BufferSize];
        var bufferIndex = 0;
        ulong generatedCount = 0;
        var currentPhase = initialPhase;

        try
        {
            while (true)
            {
                var value = (uint)Chi32.DeriveValueAt(seed, currentPhase);

                currentPhase++;

                BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(bufferIndex), value);
                bufferIndex += 4;
                generatedCount++;

                if (bufferIndex < buffer.Length) continue;

                outputStream.Write(buffer, 0, bufferIndex);
                bufferIndex = 0;
            }
        }
        catch (IOException ex) when (IsPipeBrokenException(ex))
        {
            FlushRemainingBuffer(outputStream, buffer, bufferIndex, generatedCount, "Sequential");
            throw;
        }
    }

    private static ulong StreamSwapped(long seed, long initialPhase, Stream outputStream)
    {
        var buffer = new byte[BufferSize];
        var bufferIndex = 0;
        ulong generatedCount = 0;
        var currentPhase = initialPhase;

        try
        {
            while (true)
            {
                var value = (uint)Chi32.DeriveValueAt(currentPhase, seed);

                currentPhase--;

                BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(bufferIndex), value);
                bufferIndex += 4;
                generatedCount++;

                if (bufferIndex < buffer.Length) continue;

                outputStream.Write(buffer, 0, bufferIndex);
                bufferIndex = 0;
            }
        }
        catch (IOException ex) when (IsPipeBrokenException(ex))
        {
            FlushRemainingBuffer(outputStream, buffer, bufferIndex, generatedCount, "Swapped");
            throw;
        }
    }

    private static ulong StreamFeedback(long initialSeed, long initialPhase, Stream outputStream)
    {
        var buffer = new byte[BufferSize];
        var bufferIndex = 0;
        ulong generatedCount = 0;
        var currentSeed = initialSeed;
        var currentPhase = initialPhase;

        try
        {
            while (true)
            {
                var value = (uint)Chi32.DeriveValueAt(currentSeed, currentPhase);

                var tempSeedForFeedback = currentSeed;
                currentSeed = (long)(((ulong)tempSeedForFeedback << 32) | ((ulong)currentPhase >> 32));
                currentPhase = (long)(((ulong)currentPhase << 32) | value);

                BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(bufferIndex), value);
                bufferIndex += 4;
                generatedCount++;

                if (bufferIndex < buffer.Length) continue;

                outputStream.Write(buffer, 0, bufferIndex);
                bufferIndex = 0;
            }
        }
        catch (IOException ex) when (IsPipeBrokenException(ex))
        {
            FlushRemainingBuffer(outputStream, buffer, bufferIndex, generatedCount, "Feedback");
            throw;
        }
    }

    private static void FlushRemainingBuffer(Stream outputStream, byte[] buffer, int bufferIndex,
        ulong totalGeneratedPrior, string strategyName)
    {
        if (bufferIndex <= 0)
            return;

        try
        {
            Console.Error.WriteLine(
                $"[{DateTime.Now:HH:mm:ss}] Streamer ({strategyName}): Flushing remaining {bufferIndex} bytes (total generated up to this point: {totalGeneratedPrior})...");
            outputStream.Write(buffer, 0, bufferIndex);
            outputStream.Flush();
        }
        catch (Exception flushEx)
        {
            Console.Error.WriteLine(
                $"[{DateTime.Now:HH:mm:ss}] Streamer ({strategyName}): Error flushing remaining buffer: {flushEx.Message}");
        }
    }

    private static bool IsPipeBrokenException(IOException ex)
    {
        if (ex.InnerException is ObjectDisposedException) return true;

        var msg = ex.Message;
        if (msg.Contains("Pipe", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("Stream closed", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("handle is invalid", StringComparison.OrdinalIgnoreCase))
            return true;

        const int epipeHresultFromPipes = unchecked((int)0x800700E8);
        return ex.HResult == epipeHresultFromPipes;
    }
}