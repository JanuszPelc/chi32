using System.Runtime.CompilerServices;

namespace Chi32.Utl.Walkers.Generators;

/// <summary>
///     MSWS (Middle-Square Weyl Sequence PRNG).
///     By Bernard Widynski, public domain.
///     Chaotic, nonlinear, and minimal-state PRNG—suitable for visualization and experimentation.
///     Reference: https://arxiv.org/abs/1704.00358
/// </summary>
internal class MswsDirectionGenerator(ulong seed) : IDirectionGenerator
{
    private const ulong S = 0xb5ad4eceda1ce2a9UL; // Weyl increment constant
    private ulong _w;
    private ulong _x = seed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int NextDirection()
    {
        return (int)(NextUInt32() >> 30);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint NextUInt32()
    {
        _x *= _x;
        _x += _w += S;
        _x = (_x >> 32) | (_x << 32);
        return (uint)(_x >> 32);
    }
}