using System.Numerics;
using System.Runtime.CompilerServices;

namespace Chi32.Utl.Walkers.Generators;

/// <summary>
///     RomuTrio PRNG (multiply-rotate trio variant).
///     By Chris Doty-Humphrey, public domain.
///     Strong quality PRNG with 192-bit state and fast operations.
///     Reference: http://pracrand.sourceforge.net/RNG_engines.txt
/// </summary>
internal class RomuTrioDirectionGenerator(ulong seed) : IDirectionGenerator
{
    private ulong _x = seed;
    private ulong _y = seed ^ 0xD3833E804F4C574BUL;
    private ulong _z = seed ^ 0x9E3779B97F4A7C15UL;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int NextDirection()
    {
        return (int)(NextUInt32() >> 30);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint NextUInt32()
    {
        ulong xp = _x, yp = _y, zp = _z;
        _x = 15241094284759029579UL * zp;
        _y = BitOperations.RotateLeft(yp - xp, 12);
        _z = BitOperations.RotateLeft(zp - yp, 44);
        return (uint)(_x >> 32);
    }
}