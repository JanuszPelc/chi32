using System.Numerics;
using System.Runtime.CompilerServices;

namespace Chi32.Utl.Walkers.Generators;

/// <summary>
///     RomuDuoJr PRNG (multiply-rotate style generator).
///     By Chris Doty-Humphrey, public domain.
///     Fast and compact generator with moderate statistical quality and a 128-bit period.
///     Reference: http://pracrand.sourceforge.net/RNG_engines.txt
/// </summary>
internal class RomuDuoJrDirectionGenerator(ulong seed) : IDirectionGenerator
{
    private ulong _x = seed;
    private ulong _y = seed ^ 0xA3EC647659359ACDUL;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int NextDirection()
    {
        return (int)(NextUInt32() >> 30);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint NextUInt32()
    {
        var xp = _x;
        _x = 15241094284759029579UL * _y;
        _y = BitOperations.RotateLeft(_y - xp, 27);
        return (uint)(_x >> 32);
    }
}