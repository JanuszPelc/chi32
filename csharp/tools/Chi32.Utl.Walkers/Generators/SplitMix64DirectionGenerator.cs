using System.Runtime.CompilerServices;

namespace Chi32.Utl.Walkers.Generators;

/// <summary>
///     SplitMix64 (64-bit mixing function PRNG).
///     By Sebastiano Vigna, public domain / CC0.
///     Fast, stateless mixing generator used for seeding other PRNGs.
///     Reference: http://prng.di.unimi.it/splitmix64.c
/// </summary>
internal class SplitMix64DirectionGenerator(ulong seed) : IDirectionGenerator
{
    private ulong _state = seed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int NextDirection()
    {
        return (int)(NextUInt32() >> 30);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint NextUInt32()
    {
        var z = _state += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return (uint)(z ^ (z >> 31));
    }
}