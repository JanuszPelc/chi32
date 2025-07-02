using System.Runtime.CompilerServices;

namespace Chi32.Utl.Walkers.Generators;

/// <summary>
///     System.Random wrapper (LCG-based standard PRNG).
///     Built-in .NET generator, licensed under MIT.
///     Implements a legacy linear congruential algorithm with limited randomness quality.
///     Reference: https://learn.microsoft.com/en-us/dotnet/api/system.random
/// </summary>
internal class SystemRandomDirectionGenerator(int seed) : IDirectionGenerator
{
    private readonly Random _rng = new(seed);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int NextDirection()
    {
        return _rng.Next(4);
    }
}