using System.Numerics;
using System.Runtime.CompilerServices;

namespace Chi32.Utl.Walkers.Generators;

/// <summary>
///     JSF32 (Jenkins Small Fast PRNG).
///     By Bob Jenkins, public domain / CC0.
///     Very small-state PRNG with chaotic dynamics and compact implementation.
///     Reference: http://burtleburtle.net/bob/rand/smallprng.html
/// </summary>
internal class Jsf32DirectionGenerator(uint seed) : IDirectionGenerator
{
    private uint _a = 0xf1ea5eed;
    private uint _b = seed, _c = seed, _d = seed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int NextDirection()
    {
        return (int)(NextUInt32() >> 30);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint NextUInt32()
    {
        var e = _a - BitOperations.RotateLeft(_b, 27);
        _a = _b ^ BitOperations.RotateLeft(_c, 17);
        _b = _c + _d;
        _c = _d + e;
        _d = e + _a;
        return _d;
    }
}