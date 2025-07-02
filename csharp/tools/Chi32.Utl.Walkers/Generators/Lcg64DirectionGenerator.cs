using System.Runtime.CompilerServices;

namespace Chi32.Utl.Walkers.Generators;

/// <summary>
///     LCG64 (64-bit Linear Congruential Generator).
///     Based on Numerical Recipes constants, public domain.
///     Simple and fast PRNG with moderate statistical quality for upper bits.
///     Reference: https://en.wikipedia.org/wiki/Linear_congruential_generator
/// </summary>
internal class Lcg64DirectionGenerator(ulong seed) : IDirectionGenerator
{
    private const ulong Multiplier = 6364136223846793005UL;
    private const ulong Increment = 1442695040888963407UL;
    private ulong _state = seed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int NextDirection()
    {
        return (int)(NextUInt32() >> 30);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint NextUInt32()
    {
        _state = unchecked(_state * Multiplier + Increment);
        return (uint)(_state >> 32);
    }
}