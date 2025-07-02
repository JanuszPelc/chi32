using System.Runtime.CompilerServices;

namespace Chi32.Utl.Walkers.Generators;

/// <summary>
///     CHI32 wrapper (Cascading Hash Interleave 32-bit PRNG).
///     By Janusz Pelc, licensed under MIT.
///     Random-access, stateless, deterministic, strong-quality PRNG.
///     Reference: https://github.com/JanuszPelc/chi32
/// </summary>
internal class Chi32DirectionGenerator(long seed) : IDirectionGenerator
{
    private long _phase;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int NextDirection()
    {
        return (int)(NextUInt32(seed, ref _phase) >> 30);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint NextUInt32(long seed, ref long phase)
    {
        var sample = (uint)Chi32.DeriveValueAt(seed, phase);
        phase++;
        return sample;
    }
}