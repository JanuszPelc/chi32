using System.Runtime.CompilerServices;

namespace Chi32.Utl.Walkers.Generators;

/// <summary>
///     MWC (Multiply-with-Carry PRNG).
///     By George Marsaglia, public domain.
///     Simple carry-based generator with short period and correlated output.
///     Reference: https://en.wikipedia.org/wiki/Multiply-with-carry
/// </summary>
internal class MwcDirectionGenerator(uint seed1, uint seed2) : IDirectionGenerator
{
    private uint _w = seed2 == 0 ? 0x46A3FFFFu : seed2;
    private uint _z = seed1 == 0 ? 0x9068FFFFu : seed1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int NextDirection()
    {
        return (int)(NextUInt32() >> 30);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint NextUInt32()
    {
        _z = 36969 * (_z & 65535) + (_z >> 16);
        _w = 18000 * (_w & 65535) + (_w >> 16);
        return (_z << 16) + _w;
    }
}