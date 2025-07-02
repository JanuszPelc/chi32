using System.Runtime.CompilerServices;

namespace Chi32.Utl.Walkers.Generators;

/// <summary>
///     PCG32 (Permuted Congruential Generator).
///     By Melissa O'Neill, public domain reference implementation.
///     High-quality PRNG with small state and statistically excellent output.
///     Reference: https://www.pcg-random.org/
/// </summary>
internal class Pcg32DirectionGenerator(ulong seed) : IDirectionGenerator
{
    private readonly ulong _inc = (seed << 1) | 1;
    private ulong _state = seed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int NextDirection()
    {
        return (int)(NextUInt32() >> 30);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint NextUInt32()
    {
        var oldState = _state;
        _state = unchecked(oldState * 6364136223846793005UL + _inc);
        var xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
        var rot = (int)(oldState >> 59);
        return (xorShifted >> rot) | (xorShifted << (-rot & 31));
    }
}