# CHI32: Performance Benchmarks

This section details the performance benchmarks for the Cascading Hash Interleave 32-bit (CHI32) algorithm.

The primary goals of this benchmarking effort are to:

- Offer empirical evidence of CHI32's performance characteristics
- Provide transparency by making benchmark results and methodologies available

Benchmarks were run using BenchmarkDotNet on an Apple M1 Pro with the C# reference implementation (`csharp/`). The results showcase the execution speed of CHI32's core primitives.

## Benchmark results

The following benchmarks measure different aspects of the CHI32 algorithm:

### `ApplyCascadingHashInterleaveBenchmarks`

- Description: measures the performance of the core mixing function responsible for generating the 64-bit intermediate state from the `selector` and `index`
- Results: [`./benchmark_apply_cascading_hash_interleave.txt`](./benchmark_apply_cascading_hash_interleave.txt)

### `DeriveValueAtBenchmarks`

- Description: evaluates the performance of the main PRNG function when accessing values with non-sequential (randomized jumps) `index` values. This highlights its O(1) random access capability
- Results: [`./benchmark_derive_value_at.txt`](./benchmark_derive_value_at.txt)

### `RandomNextBenchmarks`

- Description: measures the throughput of the main PRNG function when used in a sequential mode. This is compared against two internal `SystemRandom` implementations for baseline reference.
- Results: [`./benchmark_random_next_throughput.txt`](./benchmark_random_next_throughput.txt)

### `UpdateHashValueBenchmarks`

- Description: evaluates the performance of the 32-bit hash primitive, which is used internally by `ApplyCascadingHashInterleave` and can also be used as a standalone general-purpose non-cryptographic hash function. This is compared with `System.HashCode.Combine(int)` as a baseline
- Results: [`./benchmark_update_hash_value.txt`](./benchmark_update_hash_value.txt)

## Summary of performance

The benchmark data indicates that CHI32 offers competitive performance, especially considering its stateless, direct random-access design, which typically has different optimization priorities than stateful, sequential-only PRNGs.

All benchmark logs are included for full transparency.
