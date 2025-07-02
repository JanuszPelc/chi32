# CHI32: Canonical Data

This folder contains canonical output datasets used for verifying conformance of CHI32 implementations.

Each `.bin` file contains expected outputs for a specific verification strategy (sequential, swapped, or feedback), and the accompanying `chi32_canonical_meta.csv` describes how to interpret them.

These datasets are the ground truth for validating that a ported implementation produces correct, bit-for-bit results.

For full details on the verification process and how to use these files, see the [CHI32 Porting Guide](../../docs/chi32_porting_guide.md).
