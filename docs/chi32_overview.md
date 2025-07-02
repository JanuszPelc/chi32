# CHI32: Overview

Cascading Hash Interleave 32-bit (CHI32) is a stateless, deterministic random number generator that produces high-quality 32-bit values by applying a sequence of well-mixed hashing operations to a pair of 64-bit integers.

## Input naming conventions

CHI32 documentation and tools use two naming conventions for its 64-bit inputs, depending on context:

- **Core primitive (`selector`, `index`)**:
  Used when describing the algorithm's internal logic. CHI32 acts as a pure function: `value = f(selector, index)`, enabling stateless, random-access generation. These names are conceptually similar to `key` and `counter` in counter-based generators (CBRNGs).

- **Sequential usage (`seed`, `phase`)**:
  These terms are used in idiomatic wrappers that emulate stateful PRNGs, and appear in tools, test harnesses, and canonical datasets. In this context, one input (the seed) stays fixed while the other (the phase) increments.

Both pairs describe the same inputs: the core terms emphasize functional purity, while the sequential terms align with PRNG-like usage.

## Key characteristics

### Robust statistical quality

CHI32 is designed to produce outputs with strong statistical quality. Its suitability for demanding non-cryptographic applications has been demonstrated through extensive statistical-suite testing.

### Stateless computation

CHI32 generates a 32-bit pseudo-random value directly from its 64-bit `selector` and 64-bit `index` inputs. This stateless design allows O(1) random access to any value, supporting straightforward parallel generation without shared-state management.

### Cross-platform determinism

Given any specific `selector`/`index` pair, CHI32 reliably generates the same 32-bit output, ensuring consistency and repeatability across platforms.

## Non-goals

### Cryptographic security

CHI32 is not a cryptographically secure PRNG. It does not resist prediction or malicious attack and should not be used for tasks such as key generation, encryption, or digital signatures.

### Maximal sequential throughput

Sequential access with CHI32 can be a few times slower than with common non-cryptographic PRNGs tuned for maximum throughput. This trade-off prioritizes direct random access and enables stateless parallel execution, while still being fast enough for most use cases.

## Characteristics and usage

### Statistical quality

Reference implementations tested with six diverse seeds passed the full TestU01 BigCrush battery. In the PractRand suite, each seed processed more than 256 terabytes of data, triggering only two “unusual” anomalies, which is expected even for near-perfect generators. This confirms CHI32'’'s reliability for demanding non-cryptographic work.

### Random access in O(1) time

CHI32 transforms a 64-bit `selector` and 64-bit `index` into a deterministic 32-bit output using stateless functions. Each unique pair yields a consistent value, enabling direct access without maintaining or advancing internal state.

### Concurrency and parallel use

CHI32’s stateless, purely functional nature removes shared-state concerns, allowing parallel generation without locks. O(1) access enables:

- Sequential generation without internal state progression
- Procedural content generation (textures, meshes, levels) via direct addressing
- Replayable simulations and snapshotting through deterministic state restoration
- Monte Carlo task partitioning and distributed simulations with independent streams
- Stateless noise synthesis for Perlin noise, fractals, dithering, and more

### Beyond sequential access

Conceptually, the `selector` chooses one of 2<sup>64</sup> streams, while the `index` selects a position within that stream. Validation with BigCrush confirms robustness across varied patterns:

- Sequential strategy: full pass for all seeds
- Swapped argument strategy: full pass for all seeds
- Feedback strategy: four of six seeds passed cleanly; the other two showed only one low-severity deviation each

This flexibility makes CHI32 well-suited for application-specific generation techniques that depart from conventional sequential access, particularly when using non-degenerate patterns.

Full logs and further validation details are in the [experimental statistical tests](../validation/statistical_tests_experimental) folder.

## Versioning policy

CHI32 specification is frozen once released. All conformant ports produce identical output for identical inputs. This policy guarantees stable, reproducible results even as new languages and platforms are added.

## Further information

- [Usage guide](../docs/chi32_usage_guide.md): Practical instructions for real-world use
- [Porting guide](../docs/chi32_porting_guide.md): Implementing CHI32 in new languages or platforms
- [Design rationale](../docs/chi32_design_rationale.md): Background on design choices and development journey
- [Validation artifacts](../validation/): Complete statistical results and benchmarks
