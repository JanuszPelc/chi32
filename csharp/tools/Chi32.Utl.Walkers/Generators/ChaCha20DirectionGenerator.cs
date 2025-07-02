using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Chi32.Utl.Walkers.Generators;

/// <summary>
///     ChaCha20 (stream cipher–based PRNG).
///     Based on design by Daniel J. Bernstein, public domain / CC0-compatible.
///     Deterministic, seedable variant adapted for simulation use.
///     Reference: https://cr.yp.to/chacha.html
/// </summary>
internal class ChaCha20DirectionGenerator : IDirectionGenerator
{
    private readonly uint[] _buffer = new uint[16];
    private readonly uint[] _state = new uint[16];
    private int _index;

    public ChaCha20DirectionGenerator(ulong seed) : this(seed, 0)
    {
    }

    private ChaCha20DirectionGenerator(ulong seed, ulong nonce)
    {
        Span<byte> key = stackalloc byte[32];

        BinaryPrimitives.WriteUInt64LittleEndian(key[..8], seed);
        BinaryPrimitives.WriteUInt64LittleEndian(key.Slice(8, 8), ~seed);

        Span<byte> pad = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];
        pad.CopyTo(key[16..]);

        _state[0] = 0x61707865;
        _state[1] = 0x3320646e;
        _state[2] = 0x79622d32;
        _state[3] = 0x6b206574;

        for (var i = 0; i < 8; i++)
            _state[4 + i] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(i * 4, 4));

        _state[12] = 0;
        _state[13] = 0;

        _state[14] = (uint)nonce;
        _state[15] = (uint)(nonce >> 32);

        _index = 16;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int NextDirection()
    {
        if (_index >= 16)
            Refill();

        return (int)(_buffer[_index++] >> 30);
    }

    private void Refill()
    {
        var x = new uint[16];
        Array.Copy(_state, x, 16);

        for (var i = 0; i < 10; i++)
        {
            QuarterRound(ref x[0], ref x[4], ref x[8], ref x[12]);
            QuarterRound(ref x[1], ref x[5], ref x[9], ref x[13]);
            QuarterRound(ref x[2], ref x[6], ref x[10], ref x[14]);
            QuarterRound(ref x[3], ref x[7], ref x[11], ref x[15]);

            QuarterRound(ref x[0], ref x[5], ref x[10], ref x[15]);
            QuarterRound(ref x[1], ref x[6], ref x[11], ref x[12]);
            QuarterRound(ref x[2], ref x[7], ref x[8], ref x[13]);
            QuarterRound(ref x[3], ref x[4], ref x[9], ref x[14]);
        }

        for (var i = 0; i < 16; i++)
            _buffer[i] = unchecked(x[i] + _state[i]);

        unchecked
        {
            _state[12]++;
            if (_state[12] == 0)
                _state[13]++;
        }

        _index = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void QuarterRound(ref uint a, ref uint b, ref uint c, ref uint d)
    {
        a += b;
        d ^= a;
        d = RotateLeft(d, 16);
        c += d;
        b ^= c;
        b = RotateLeft(b, 12);
        a += b;
        d ^= a;
        d = RotateLeft(d, 8);
        c += d;
        b ^= c;
        b = RotateLeft(b, 7);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint RotateLeft(uint value, int count)
    {
        return (value << count) | (value >> (32 - count));
    }
}