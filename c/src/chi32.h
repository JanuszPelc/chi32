#ifndef CHI32_H
#define CHI32_H

// MIT License
//
// Copyright (c) 2025 Janusz Pelc
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

// Implementation of Cascading Hash Interleave 32-bit (CHI32)
// Documentation and specification: https://github.com/JanuszPelc/chi32

#include <stdint.h>

// === Internal helper functions (Static Inline) ===

/**
 * @brief Rotates the bits of a 32-bit unsigned integer to the left.
 * @param x The value to rotate.
 * @param k The number of positions to rotate by (caller ensures k is masked, e.g., k & 31).
 * @return The rotated value.
 */
static inline uint32_t chi32_internal_rotate_left_u32(uint32_t x, int k) {
    return (x << k) | (x >> (32 - k));
}

/**
 * @brief Rotates the bits of a 64-bit unsigned integer to the left.
 * @param x The value to rotate.
 * @param k The number of positions to rotate by (caller ensures k is masked, e.g., k & 63).
 * @return The rotated value.
 */
static inline uint64_t chi32_internal_rotate_left_u64(uint64_t x, int k) {
    return (x << k) | (x >> (64 - k));
}

// === CHI32 algorithm implementation (Static Inline) ===

/**
 * @brief Updates a 32-bit hash value based on the previous value and new input.
 *
 * The 'previous_hash' should be initialized to 0 for the first call.
 *
 * @param previous_hash Prior hash value in the sequence.
 * @param value Input contributing to the updated hash.
 * @return Updated hash value.
 */
static inline int32_t chi32_update_hash_value(int32_t previous_hash, int32_t value) {
    const uint32_t prime_number_1 = 0x8addb2d1U;
    const uint32_t prime_number_2 = 0x8c723b45U;
    const uint32_t prime_number_3 = 0xfd923173U;
    const uint32_t prime_number_4 = 0x89a6aa0bU;
    const uint32_t prime_number_5 = 0x1f844cb7U;
    const uint32_t prime_number_6 = 0xfd2c1e9dU;

    const int shift_offset_1 = 15;
    const int shift_offset_2 = 7;
    const int shift_offset_3 = 29;
    const int shift_offset_4 = 16;

    uint32_t hash_u32 = (uint32_t)previous_hash;
    uint32_t value_u32 = (uint32_t)value;

    hash_u32 ^= prime_number_1;

    int rotate_amount = (int)(hash_u32 & 0x1FU);
    hash_u32 += prime_number_2 ^ chi32_internal_rotate_left_u32(value_u32, rotate_amount);
    hash_u32 *= prime_number_3;

    hash_u32 ^= hash_u32 >> shift_offset_1;
    hash_u32 *= prime_number_4;

    hash_u32 ^= hash_u32 >> shift_offset_2;
    hash_u32 += hash_u32 >> shift_offset_3;
    hash_u32 *= prime_number_5;

    hash_u32 ^= hash_u32 >> shift_offset_4;
    hash_u32 *= prime_number_6;

    return (int32_t)hash_u32;
}

/**
 * @brief Calculates a 32-bit pseudo-random value based on a specified selector and index.
 *
 * @param selector Sequence selector.
 * @param index Position within the sequence.
 * @return Mixed 64-bit pseudo-random value.
 */
static inline int64_t chi32_apply_cascading_hash_interleave(int64_t selector, int64_t index) {
    const uint64_t golden_ratio_prime_multiplier = 0x9E3779B97F4A7C55ULL;

    uint64_t primary_anchor_u64 = (uint64_t)selector;
    uint64_t alternate_anchor_u64 = ((uint64_t)(~selector)) * golden_ratio_prime_multiplier;
    uint64_t anchor_coupling_mask_u64 = primary_anchor_u64 & alternate_anchor_u64;

    uint64_t primary_offset_u64 = (uint64_t)index;
    uint64_t alternate_offset_u64 = ((uint64_t)(~index)) ^ anchor_coupling_mask_u64;

    uint64_t primary_pointer_u64 = primary_anchor_u64 + primary_offset_u64;
    uint64_t alternate_pointer_u64 = alternate_anchor_u64 - alternate_offset_u64;

    int32_t primary_pointer_low_i32 = (int32_t)primary_pointer_u64;
    int32_t primary_pointer_high_i32 = (int32_t)(primary_pointer_u64 >> 32);
    int32_t alternate_pointer_low_i32 = (int32_t)alternate_pointer_u64;
    int32_t alternate_pointer_high_i32 = (int32_t)(alternate_pointer_u64 >> 32);

    const int interleave_bit_offset = 16;
    const int wrap_around_bit_offset = interleave_bit_offset * 3;

    uint64_t hash_accumulator_u64;

    hash_accumulator_u64 = (uint32_t)chi32_update_hash_value(0, alternate_pointer_low_i32);
    hash_accumulator_u64 = (uint32_t)chi32_update_hash_value((int32_t)hash_accumulator_u64, alternate_pointer_high_i32)
                           ^ (hash_accumulator_u64 << interleave_bit_offset);
    hash_accumulator_u64 = (uint32_t)chi32_update_hash_value((int32_t)hash_accumulator_u64, primary_pointer_high_i32)
                           ^ (hash_accumulator_u64 << interleave_bit_offset);
    hash_accumulator_u64 = (uint32_t)chi32_update_hash_value((int32_t)hash_accumulator_u64, primary_pointer_low_i32)
                           ^ (hash_accumulator_u64 << interleave_bit_offset)
                           ^ (hash_accumulator_u64 >> wrap_around_bit_offset);

    const uint64_t final_step_prime_multiplier = 0x72A4EB92D796ED93ULL;

    return (int64_t)(hash_accumulator_u64 * final_step_prime_multiplier);
}

/**
 * @brief Calculates a pseudo-random value based on a specified selector and index.
 *
 * Uses the CHI32 algorithm to produce a deterministic pseudo-random 64-bit intermediate state,
 * which is then truncated through a 32-bit extraction window with a state-dependent offset.
 *
 * @param selector The int64_t value serving as a pseudo-random sequence selector.
 * @param index    The int64_t value used as an index within this sequence.
 * @return An int32_t representing the pseudo-random value.
 */
static inline int32_t chi32_derive_value_at(int64_t selector, int64_t index) {
    uint64_t state_u64 = (uint64_t)chi32_apply_cascading_hash_interleave(selector, index);

    uint32_t low_bits_for_xor = (uint32_t)state_u64;
    uint32_t mid_bits_for_xor = (uint32_t)(state_u64 >> 29);
    uint32_t high_bits_for_xor = (uint32_t)(state_u64 >> 58);

    int offset = (int)((low_bits_for_xor ^ mid_bits_for_xor ^ high_bits_for_xor) & 0x3FU); // 0x3F is 63

    uint64_t rotated_state_u64 = chi32_internal_rotate_left_u64(state_u64, offset);

    return (int32_t)rotated_state_u64;
}

#endif // CHI32_H
