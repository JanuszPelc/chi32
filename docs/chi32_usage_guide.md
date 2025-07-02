# CHI32: Usage Guide

This guide explains how to use Cascading Hash Interleave 32-bit (CHI32) for various pseudo-random-number tasks.

It is organized to support both direct interaction with the core CHI32 primitives for custom use cases and the use of idiomatic wrapper classes for seamless, language-specific integration.

For a deeper understanding of CHI32’s design principles and key characteristics, see the [overview](../README.md).

## Direct usage

Direct usage means embedding the reference source of the CHI32 primitives directly in your codebase. Choose this path if you need granular control or must integrate CHI32 where no wrapper exists.

### When to choose direct usage

- You need minimal dependencies and maximum control
- You are writing custom logic that relies on CHI32 primitives
- Your environment lacks an idiomatic CHI32 wrapper

### Steps for direct usage

1. **Copy the core algorithm source**:
   Either use one of the implementations linked below or go to `/<language>/src/` and copy the core file.

2. **Integrate into your project**:
   Add the file(s) to your codebase unchanged.

3. **Comply with licensing**:
   Keep the full MIT header and any copyright notices.

4. **Use the primitives**:
   Call `DeriveValueAt(selector, index)` (and other primitives if needed) directly.

### Available reference implementations

- C#: [Chi32.cs](../csharp/src/Chi32/Chi32.cs)
- C: [chi32.h](../c/src/chi32.h)

## Building stateful PRNG wrappers

Many projects prefer a stateful PRNG interface. CHI32 primitives make it easy to build one.

A wrapper usually manages a `seed` (CHI32’s `selector`) and a `phase` (CHI32’s `index`). The wrapper feels stateful, while `DeriveValueAt` remains pure.

### Conceptual design of a stateful wrapper

- Initialize with a `seed`; start `phase` at 0
- NextValue()
  1. Call `DeriveValueAt(seed, phase)`
  2. Increment `phase`
  3. Return the value
- PeekAtValue(phase) returns a value at any phase without changing the wrapper’s current state
- Optional getters/setters for `phase` enable snapshot, restore, or jumping

### Pseudocode example

```pseudo
CLASS Chi32Prng:

    PUBLIC IMMUTABLE Seed : Int64
    PUBLIC MUTABLE   Phase : Int64

    METHOD Initialize(initial_seed : Int64):
        Seed  = initial_seed
        Phase = 0
    END METHOD

    METHOD NextValue() -> Int32:
        result = Chi32Algorithm.DeriveValueAt(Seed, Phase)
        Phase  = Phase + 1
        RETURN result
    END METHOD

    METHOD PeekAtValue(phase_to_peek : Int64) -> Int32:
        result = Chi32Algorithm.DeriveValueAt(Seed, phase_to_peek)
        RETURN result
    END METHOD

END CLASS
```

### Benefits of this approach

* Idiomatic API feels familiar to anyone using stateful PRNGs
* Sequential generation is straightforward
* Random access via `PeekAtValue` is free
* State management allows save, restore, and replay
* Parallelism is easy: multiple wrappers with different seeds or phase ranges for a shared seed can run concurrently without locks, thanks to CHI32’s stateless core
