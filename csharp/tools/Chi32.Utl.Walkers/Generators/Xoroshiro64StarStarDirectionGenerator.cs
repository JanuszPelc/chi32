using System.Numerics;
using System.Runtime.CompilerServices;

namespace Chi32.Utl.Walkers.Generators;

/// <summary>
///     Xoroshiro64** (XOR/rotation-based PRNG).
///     By David Blackman and Sebastiano Vigna, public domain / CC0.
///     Fast 64-bit generator with good statistical performance and small state.
///     Reference: http://prng.di.unimi.it/xoroshiro64starstar.c
/// </summary>
internal class Xoroshiro64StarStarDirectionGenerator(uint seed) : IDirectionGenerator
{
    private uint _s0 = seed;
    private uint _s1 = seed ^ 0x9E3779B9;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int NextDirection()
    {
        return (int)(NextUInt32() >> 30);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint NextUInt32()
    {
        var result = BitOperations.RotateLeft(_s0 * 0x9E3779BBu, 5) * 5;

        var t = _s0 ^ _s1;
        _s0 = BitOperations.RotateLeft(_s0, 26) ^ t ^ (t << 9);
        _s1 = BitOperations.RotateLeft(t, 13);

        return result;
    }
}