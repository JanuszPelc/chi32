using System.Buffers.Binary;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Chi32.Tests;

/// <summary>
///     Contains canonical references regression tests confirming the alignment with the CHI32 specification.
/// </summary>
public class CanonicalReferenceTests
{
    /// <summary>
    ///     Verifies the sequential strategy.
    /// </summary>
    [Fact]
    public void SequentialStrategy_WhenDataGenerated_MatchesReferenceData()
    {
        // Arrange
        var item = Reference.AllTestDataItems.Value.First(i => i.ParsedStrategy == Reference.StrategyKind.Sequential);

        var actualData = new List<uint>(item.Length);
        var expectedData = Reference.SequentialData.Value;
        var currentPhase = item.Phase;

        // Act
        for (var i = 0; i < item.Length; i++)
        {
            var randomValue = Chi32.DeriveValueAt(item.Seed, currentPhase);
            actualData.Add((uint)randomValue);
            currentPhase++;
        }

        // Assert
        currentPhase.Should().Be(item.Phase + item.Length);
        actualData.Should().Equal(expectedData);
    }

    /// <summary>
    ///     Verifies the swapped strategy.
    /// </summary>
    [Fact]
    public void SwappedStrategy_WhenDataGenerated_MatchesReferenceData()
    {
        // Arrange
        var item = Reference.AllTestDataItems.Value.First(i => i.ParsedStrategy == Reference.StrategyKind.Swapped);

        var actualData = new List<uint>(item.Length);
        var expectedData = Reference.SwappedData.Value;

        // NOTE: For the 'swapped' strategy, the test data intentionally inverts the roles of the input
        // parameters to validate argument independence. The CSV 'seed' is used as the fixed index,
        // and the CSV 'phase' is the initial, decrementing selector. See the porting guide for details.
        var currentSelector = item.Phase;
        var fixedIndex = item.Seed;

        // Act
        for (var i = 0; i < item.Length; i++)
        {
            var randomValue = Chi32.DeriveValueAt(currentSelector, fixedIndex);
            actualData.Add((uint)randomValue);
            currentSelector--;
        }

        // Assert
        actualData.Should().Equal(expectedData);
    }

    /// <summary>
    ///     Verifies the feedback strategy.
    /// </summary>
    [Fact]
    public void FeedbackStrategy_WhenDataGenerated_MatchesReferenceData()
    {
        // Arrange
        var item = Reference.AllTestDataItems.Value.First(i => i.ParsedStrategy == Reference.StrategyKind.Feedback);

        var actualData = new List<uint>(item.Length);
        var expectedData = Reference.FeedbackData.Value;
        var currentSeed = item.Seed;
        var currentPhase = item.Phase;

        // Act
        for (var i = 0; i < item.Length; i++)
        {
            var randomValue = Chi32.DeriveValueAt(currentSeed, currentPhase);
            actualData.Add((uint)randomValue);

            var tempSeedForFeedback = currentSeed;
            currentSeed = (long)(((ulong)tempSeedForFeedback << 32) | ((ulong)currentPhase >> 32));
            currentPhase = (long)(((ulong)currentPhase << 32) | (uint)randomValue);
        }

        // Assert
        actualData.Should().Equal(expectedData);
    }

    private static class Reference
    {
        public enum StrategyKind
        {
            Sequential,
            Swapped,
            Feedback
        }

        private static readonly Lazy<string> BasePath =
            new(() =>
            {
                var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(assemblyLocation))
                    throw new DirectoryNotFoundException("Could not determine the location of the executing assembly.");
                return Path.GetFullPath(Path.Combine(
                    assemblyLocation, "../../../../../../validation/canonical_data/"));
            });

        public static readonly Lazy<List<CanonicalTestDataItem>> AllTestDataItems =
            new(() => LoadAllTestDataItemsFromCsv(Path.Combine(BasePath.Value, "chi32_canonical_meta.csv")));

        public static readonly Lazy<List<uint>> FeedbackData =
            new(() =>
            {
                var item = AllTestDataItems.Value.First(i => i.ParsedStrategy == StrategyKind.Feedback);
                return LoadCanonicalDataFile(Path.Combine(BasePath.Value, item.BinFileName), item.Length);
            });

        public static readonly Lazy<List<uint>> SwappedData =
            new(() =>
            {
                var item = AllTestDataItems.Value.First(i => i.ParsedStrategy == StrategyKind.Swapped);
                return LoadCanonicalDataFile(Path.Combine(BasePath.Value, item.BinFileName), item.Length);
            });

        public static readonly Lazy<List<uint>> SequentialData =
            new(() =>
            {
                var item = AllTestDataItems.Value.First(i => i.ParsedStrategy == StrategyKind.Sequential);
                return LoadCanonicalDataFile(Path.Combine(BasePath.Value, item.BinFileName), item.Length);
            });

        private static List<CanonicalTestDataItem> LoadAllTestDataItemsFromCsv(string csvFilePath)
        {
            if (!File.Exists(csvFilePath))
            {
                var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var searchPathMessage = $"CSV metadata file not found at '{csvFilePath}'.";
                if (assemblyLocation != null)
                    searchPathMessage +=
                        $" Relative to assembly location: '{Path.GetRelativePath(assemblyLocation, csvFilePath)}'.";
                searchPathMessage += $" Current working directory: '{Directory.GetCurrentDirectory()}'";
                throw new FileNotFoundException(searchPathMessage);
            }

            var items = new List<CanonicalTestDataItem>();
            var lines = File.ReadAllLines(csvFilePath);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                    continue;

                var parts = trimmedLine.Split(',');
                if (parts.Length != 6)
                    throw new InvalidDataException(
                        $"Malformed CSV line in '{Path.GetFileName(csvFilePath)}': Expected 6 parts, got {parts.Length}. Line: '{trimmedLine}'");

                try
                {
                    var logicalName = parts[0].Trim();
                    var strategyCode = int.Parse(parts[1].Trim());
                    var seed = long.Parse(parts[2].Trim());
                    var phase = long.Parse(parts[3].Trim());
                    var length = int.Parse(parts[4].Trim());
                    var binFileName = parts[5].Trim();

                    var parsedStrategy = strategyCode switch
                    {
                        0 => StrategyKind.Sequential,
                        1 => StrategyKind.Swapped,
                        2 => StrategyKind.Feedback,
                        _ => throw new InvalidDataException(
                            $"Unknown strategy code '{strategyCode}' in CSV line: '{trimmedLine}'")
                    };

                    items.Add(new CanonicalTestDataItem(logicalName, parsedStrategy, seed, phase, length, binFileName));
                }
                catch (FormatException ex)
                {
                    throw new InvalidDataException(
                        $"Error parsing numeric value in CSV line in '{Path.GetFileName(csvFilePath)}': '{trimmedLine}'. Details: {ex.Message}",
                        ex);
                }
                catch (Exception ex) // Catch other potential parsing errors
                {
                    throw new InvalidDataException(
                        $"Error parsing CSV line in '{Path.GetFileName(csvFilePath)}': '{trimmedLine}'. Details: {ex.Message}",
                        ex);
                }
            }

            return items;
        }

        private static List<uint> LoadCanonicalDataFile(string filePath, int expectedLength)
        {
            if (!File.Exists(filePath))
            {
                var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var searchPathMessage = $"Data file not found at '{filePath}'.";
                if (assemblyLocation != null)
                    searchPathMessage +=
                        $" Relative to assembly location: '{Path.GetRelativePath(assemblyLocation, filePath)}'.";
                searchPathMessage += $" Current working directory: '{Directory.GetCurrentDirectory()}'";
                throw new FileNotFoundException(searchPathMessage);
            }

            var fileBytes = File.ReadAllBytes(filePath);
            if (fileBytes.Length != (long)expectedLength * 4)
                throw new InvalidDataException(
                    $"Data file '{Path.GetFileName(filePath)}' has incorrect length. " +
                    $"Expected {expectedLength * 4} bytes, got {fileBytes.Length}.");

            var data = new List<uint>(expectedLength);
            for (var i = 0; i < fileBytes.Length; i += 4)
                data.Add(BinaryPrimitives.ReadUInt32LittleEndian(fileBytes.AsSpan(i, 4)));

            return data;
        }

        public record CanonicalTestDataItem(
            // ReSharper disable once NotAccessedPositionalProperty.Local
            string LogicalName,
            StrategyKind ParsedStrategy,
            long Seed,
            long Phase,
            int Length,
            string BinFileName
        );
    }
}