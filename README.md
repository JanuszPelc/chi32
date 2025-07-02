# CHI32

> **⚠️ Pre-release version. Installation may not work properly. Official release coming very soon.**

Cascading Hash Interleave 32-bit (CHI32) is a stateless, deterministic random number generator that produces high-quality 32-bit values by applying a sequence of well-mixed hashing operations to a pair of 64-bit integers.

It is built around a custom, non-cryptographic mixing function and is designed to support two complementary roles:

- As a freely addressable random source for parallel workloads, procedural generation, replayable simulations, and any system requiring deterministic, cross-platform behavior
- As a statistically robust foundation for counter-based generators (CBRNGs) that evolve their state by incrementing a 64-bit counter

## Who is CHI32 for

CHI32 is designed for developers, toolmakers, and researchers who need a deterministic, cross-platform generator suitable for both freely-addressable and sequential pseudo-random generation.

As a stateless generator, it computes each 32-bit output directly from a given `selector` and `index`, with no internal state to manage or advance. This makes it well suited for:

- Parallel generation in multithreaded or distributed environments
- Indexed access in procedural content generation, noise synthesis, and spatial simulations
- Replayable or snapshot-driven systems that benefit from deterministic value retrieval

CHI32 can also be used to build sequential-style PRNGs. By fixing a `selector` as a seed and incrementing the `index` as a phase, it effectively behaves like a traditional PRNG.

This usage pattern has been validated through extensive testing: six diverse seeds passed the full TestU01 BigCrush battery, and each processed over 256 terabytes in the PractRand suite without any sign of statistical weakness.

Finally, CHI32 may serve as a point of interest for algorithm enthusiasts and researchers studying the design and statistical behavior of non-cryptographic PRNGs. The [design rationale](./docs/chi32_design_rationale.md) documents the experiments, constraints, and tradeoffs that shaped its structure.

## Notes

CHI32 is not suitable for cryptographic use. It does not provide protection against prediction or malicious analysis and should not be used in security-sensitive contexts.

CHI32 uses two naming conventions for its 64-bit inputs: `selector` and `index` for the core primitive, and `seed` and `phase` for sequential usage. See the [overview](./docs/chi32_overview.md) for details.

## Documentation

- [Overview](./docs/chi32_overview.md): Technical summary of the CHI32 algorithm, including its structure, key characteristics, and versioning policy
- [Usage guide](./docs/chi32_usage_guide.md): Practical instructions for using CHI32 in real-world applications, including generation patterns and edge cases
- [Design rationale](./docs/chi32_design_rationale.md): A behind-the-scenes look at the motivations, challenges, discoveries, and technical tradeoffs that shaped CHI32

## Reference implementations

- [C](./c/): Header-only C99 implementation using `static inline` functions
- [C#](./csharp/): Self-contained implementation with aggressive inlining

## Validation artifacts

- [Statistical test results](./validation/statistical_tests/): Comprehensive logs from industry-standard test suites like PractRand and TestU01 (BigCrush), covering various input strategies to demonstrate robustness
- [Performance benchmarks](./validation/benchmarks/): Detailed performance metrics for core algorithm primitives, measured on representative hardware using the C# reference implementation
- [Random walker heatmaps](./validation/random_walkers/): Diagnostic heatmaps comparing spatial characteristics of various PRNGs through deterministic random walker simulations
- [Canonical data](./validation/canonical_data/): Ground truth datasets used to verify implementation correctness and conformance

## Project status

CHI32 is a passion project maintained in spare time. The primary focus is on stability and correctness above all else. This includes fixing bugs, improving documentation, and ensuring reliable behavior across platforms and use cases.

New features are considered carefully but take lower priority than maintaining the existing functionality at high quality.

## Contributing

- [Contributing](./CONTRIBUTING.md): See detailed guidelines for contributions.
- [Porting guide](./docs/chi32_porting_guide.md): Detailed instructions for implementing CHI32 in new languages or platforms, including required behaviors and conformance tests.

Language ports are especially welcome and receive priority support.

## License

CHI32 is distributed under the [MIT license](./LICENSE).

## Related projects

[ChiVariate](https://github.com/JanuszPelc/ChiVariate) is a deterministic, data-oriented random engine seamlessly bridging expressive everyday randomness and statistically rigorous, domain-specific simulations. Built on CHI32 for reproducible, allocation-free entropy with strong statistical guarantees.
