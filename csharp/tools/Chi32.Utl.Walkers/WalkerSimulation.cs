using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Chi32.Utl.Walkers;

/// <summary>
///     A qualitative diagnostic visualization technique for exposing PRNG bias using spatial simulation heatmaps.
/// </summary>
internal class WalkerSimulation(
    ulong stepsPerWalker,
    int scaleShift,
    string name,
    Func<IDirectionGenerator> createFunc)
{
    private const int ImageSizeBits = 9;
    private const int ImageSize = 1 << ImageSizeBits;
    private const double ImageMarginPercent = 0.025;
    private const int GridSizeBits = ImageSizeBits + 1;
    private const int GridSize = 1 << GridSizeBits;
    private const int GridHalfSize = GridSize / 2;
    private const double ChunkBudgetSeconds = 5;
    private const int TotalCells = GridSize * GridSize;

    private readonly IDirectionGenerator _generator = createFunc();
    private readonly Stopwatch _stopwatch = new();
    private readonly long[] _visitCounts = new long[TotalCells];
    private Walker _agent = new(0, 0);
    private ulong _progress;
    private ulong _step;
    private int ScaleShift { get; } = scaleShift;
    public ulong StepsPerWalker { get; } = stepsPerWalker;

    public ulong Progress => Volatile.Read(ref _progress);
    public bool IsComplete => Progress >= StepsPerWalker;
    public TimeSpan CpuTime { get; private set; }

    public string Name => name;

    public void RunChunk()
    {
        if (IsComplete) return;

        _stopwatch.Restart();
        var updateInterval = Math.Max(BitOperations.RoundUpToPowerOf2(StepsPerWalker / 1_000_000), 256) - 1;

        for (; _step < StepsPerWalker; _step++)
        {
            ref var agent = ref _agent;
            var dir = _generator.NextDirection();

            MoveAgent(ref agent, dir);

            var x = (agent.X >> ScaleShift) + GridHalfSize;
            var y = (agent.Y >> ScaleShift) + GridHalfSize;
            if (x is >= 0 and < GridSize && y is >= 0 and < GridSize)
            {
                var index = (y << GridSizeBits) | x;
                _visitCounts[index]++;
            }

            if ((_step & updateInterval) != 0)
                continue;

            if (_stopwatch.Elapsed < TimeSpan.FromSeconds(ChunkBudgetSeconds))
                continue;

            break;
        }

        _stopwatch.Stop();
        CpuTime += _stopwatch.Elapsed;

        Interlocked.Exchange(ref _progress, _step);

        return;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void MoveAgent(ref Walker walker, int dir)
        {
            var isOdd = dir & 1;
            var delta = 1 - (isOdd << 1);

            var affectsX = ((dir ^ 2) >> 1) & 1;
            var affectsY = (dir >> 1) & 1;

            var xApplicationMask = -affectsX;
            var yApplicationMask = -affectsY;

            walker.X += delta & xApplicationMask;
            walker.Y += delta & yApplicationMask;
        }
    }

    public void SaveBmp(string path)
    {
        var normalizedImage1 = CropAndNormalizeImage();
        var renderedImage = RenderImage(normalizedImage1);

        const int rowSize = (ImageSize * 3 + 3) & ~3;
        const int pixelDataSize = rowSize * ImageSize;
        const int fileSize = 54 + pixelDataSize;

        using var fs = new FileStream(path, FileMode.Create);
        using var bw = new BinaryWriter(fs);

        bw.Write((ushort)0x4D42);
        bw.Write(fileSize);
        bw.Write((ushort)0);
        bw.Write((ushort)0);
        bw.Write(54);

        bw.Write(40);
        bw.Write(renderedImage.Width);
        bw.Write(renderedImage.Height);
        bw.Write((ushort)1);
        bw.Write((ushort)24);
        bw.Write(0);
        bw.Write(pixelDataSize);
        bw.Write(2835);
        bw.Write(2835);
        bw.Write(0);
        bw.Write(0);

        var padding = new byte[rowSize - renderedImage.Width * 3];

        for (var y = renderedImage.Height - 1; y >= 0; y--)
        {
            for (var x = 0; x < renderedImage.Width; x++)
            {
                var color = renderedImage.Colors[y][x];
                bw.Write((byte)color);
                bw.Write((byte)(color >> 8));
                bw.Write((byte)(color >> 16));
            }

            bw.Write(padding);
        }
    }

    private NormalizedImage CropAndNormalizeImage()
    {
        const int margin = (int)(ImageMarginPercent * ImageSize);

        var raw = new float[GridSize][];
        var histogram = new Dictionary<float, long>();

        for (var y = 0; y < GridSize; y++)
        {
            raw[y] = new float[GridSize];
            for (var x = 0; x < GridSize; x++)
            {
                var count = _visitCounts[(y << GridSizeBits) | x];
                raw[y][x] = count;

                if (count <= 0)
                    continue;

                if (!histogram.TryAdd(count, 1))
                    histogram[count]++;
            }
        }

        if (histogram.Count == 0)
            return new NormalizedImage(ImageSize, ImageSize, GridHalfSize, GridHalfSize,
                Enumerable.Repeat(new float[ImageSize], ImageSize).ToArray());

        var sortedHistogram = histogram.OrderBy(kvp => kvp.Key).ToList();
        var totalMass = sortedHistogram.Sum(kvp => kvp.Key * kvp.Value);
        var valueToCdf = new Dictionary<float, float>();
        long cumulativeDistribution = 0;

        foreach (var (value, count) in sortedHistogram)
        {
            cumulativeDistribution += (long)(value * count);
            valueToCdf[value] = cumulativeDistribution / totalMass;
        }

        var normalized = new float[GridSize][];
        for (var y = 0; y < GridSize; y++)
        {
            normalized[y] = new float[GridSize];
            for (var x = 0; x < GridSize; x++)
            {
                var rawVal = raw[y][x];
                normalized[y][x] = rawVal > 0 && valueToCdf.TryGetValue(rawVal, out var p) ? p : 0f;
            }
        }

        var cropX0 = FindOccupiedCenter(normalized, GridSize, ImageSize, margin, true);
        var cropY0 = FindOccupiedCenter(normalized, GridSize, ImageSize, margin, false);

        var cropped = new float[ImageSize][];
        for (var y = 0; y < ImageSize; y++)
        {
            cropped[y] = new float[ImageSize];
            for (var x = 0; x < ImageSize; x++)
                cropped[y][x] = normalized[cropY0 + y][cropX0 + x];
        }

        var originX = GridHalfSize - cropX0;
        var originY = GridHalfSize - cropY0;

        return new NormalizedImage(ImageSize, ImageSize, originX, originY, cropped);

        static int FindOccupiedCenter(float[][] normalized, int axisLength, int windowSize, int margin, bool horizontal)
        {
            var min = int.MaxValue;
            var max = int.MinValue;

            for (var a = 0; a < axisLength; a++)
            for (var b = 0; b < axisLength; b++)
            {
                var value = horizontal ? normalized[b][a] : normalized[a][b];
                if (value == 0f)
                    continue;

                min = Math.Min(min, a);
                max = Math.Max(max, a);
            }

            var isEmpty = min >= max;
            if (isEmpty)
                return (axisLength - windowSize) / 2;

            var center = (min + max) / 2;
            var start = center - windowSize / 2;
            var maxStart = axisLength - margin - windowSize;

            return Math.Clamp(start, margin, maxStart);
        }
    }

    private RenderedImage RenderImage(NormalizedImage normalizedImage)
    {
        const uint backgroundColor = 0x051f39u;
        var palette = new[]
        {
            0x2B2567u, 0x3D2473u, 0x4A2480u, 0x5B2B8Fu, 0x762F97u, 0x933197u, 0xAF3395u,
            0xC53A9Du, 0xD14B9Eu, 0xE25A91u, 0xEF6B81u, 0xF87D78u, 0xFF8E80u, 0xFFA792u
        };

        const double crosshairPercent = 0.07;
        const int halfLength = (int)(crosshairPercent * ImageSize / 2);

        var width = normalizedImage.Width;
        var height = normalizedImage.Height;
        var originalValues = normalizedImage.Values;

        var values = new float[height][];
        for (var y = 0; y < height; y++)
        {
            values[y] = new float[width];
            Array.Copy(originalValues[y], values[y], width);
        }

        DrawCrosshair();

        var colors = new uint[height][];
        for (var y = 0; y < height; y++)
        {
            colors[y] = new uint[width];
            for (var x = 0; x < width; x++)
            {
                var value = values[y][x];
                var index = value == 0f
                    ? -1
                    : Math.Clamp((int)MathF.Round(value * (palette.Length - 1)), 0, palette.Length - 1);
                var color = index < 0 ? backgroundColor : palette[index];

                colors[y][x] = color;
            }
        }

        return new RenderedImage(width, height, colors);

        void DrawCrosshair()
        {
            var ox = normalizedImage.OriginX;
            var oy = normalizedImage.OriginY;

            for (var offset = 0; offset <= halfLength; offset++)
            {
                var t = Math.Abs(offset) / (float)(halfLength + 1);
                var intensity = (1f - t * t) / 2;

                DecreasePixelIntensity(ox + offset, oy + 1, intensity);
                DecreasePixelIntensity(ox + offset, oy - 1, intensity);
                DecreasePixelIntensity(ox + 1, oy + offset, intensity);
                DecreasePixelIntensity(ox - 1, oy + offset, intensity);
                DecreasePixelIntensity(ox - offset, oy + 1, intensity);
                DecreasePixelIntensity(ox - offset, oy - 1, intensity);
                DecreasePixelIntensity(ox + 1, oy - offset, intensity);
                DecreasePixelIntensity(ox - 1, oy - offset, intensity);
            }

            for (var offset = 0; offset <= halfLength; offset++)
            {
                var t = Math.Abs(offset) / (float)(halfLength + 1);
                var intensity = 1f - t * t;

                IncreasePixelIntensity(ox + offset, oy, intensity);
                IncreasePixelIntensity(ox, oy + offset, intensity);
                IncreasePixelIntensity(ox - offset, oy, intensity);
                IncreasePixelIntensity(ox, oy - offset, intensity);
            }

            return;

            void IncreasePixelIntensity(int x, int y, float intensity)
            {
                if (x < 0 || x >= width || y < 0 || y >= height)
                    return;

                var value = values[y][x];
                values[y][x] = Math.Max(Math.Min(value + intensity, 1), 0);
            }

            void DecreasePixelIntensity(int x, int y, float intensity)
            {
                if (x < 0 || x >= width || y < 0 || y >= height || x == ox || y == oy)
                    return;

                var value = values[y][x];
                values[y][x] = Math.Max(Math.Min(value - intensity, 1), 0);
            }
        }
    }

    private record NormalizedImage(int Width, int Height, int OriginX, int OriginY, float[][] Values);

    private record RenderedImage(int Width, int Height, uint[][] Colors);

    private record struct Walker(int X, int Y);
}