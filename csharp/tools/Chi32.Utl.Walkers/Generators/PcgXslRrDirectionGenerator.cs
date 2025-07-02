using System.Runtime.CompilerServices;

namespace Chi32.Utl.Walkers.Generators;

/// <summary>
///     PCG-XSL-RR (XOR-Shift-Low with Rotate-Right output function).
///     By Melissa O'Neill, public domain reference implementation.
///     Multiplier-only variant of PCG with XSL-RR output function (no increment).
///     Reference: https://www.pcg-random.org/
/// </summary>
internal class PcgXslRrDirectionGenerator(ulong seed) : IDirectionGenerator
{
    // Multiplier constant from PCG paper (specific to 64-bit MCG)
    private const ulong Multiplier = 6364136223846793005UL;

    private ulong _state = (seed << 1) | 1; // Ensure odd non-zero seed

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int NextDirection()
    {
        return (int)(NextUInt32() >> 30);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint NextUInt32()
    {
        _state *= Multiplier;

        // PCG-XSL-RR output transform
        var xorshifted = (uint)((_state >> 18) ^ _state);
        var rot = (int)(_state >> 59);
        return (xorshifted >> rot) | (xorshifted << (-rot & 31));
    }
}