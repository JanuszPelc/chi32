# CHI32: C Reference Implementation

This directory contains the C reference implementation of the Cascading Hash Interleave 32-bit (CHI32) pseudo-random number generator.

The core algorithm is implemented as a single header-only file: `src/chi32.h`. For basic use, you only need to copy this file into your project.

This implementation includes:

- Core CHI32 algorithm primitives
- Canonical reference tests to validate conformance
- A harness for statistical testing with the TestU01 library

## Directory structure

- `Makefile`: Builds the canonical test binary
- `src/chi32.h`: Header-only CHI32 implementation
- `tests/test_chi32_canonical.c`: Canonical reference test cases
- `tools/testu01_harness/`: TestU01 integration
  - `main.c`: Entry point for statistical testing
  - `Makefile`: Builds the harness

## Prerequisites

- C99-compatible compiler (e.g. GCC, Clang)
- `make` utility
- For statistical testing:
  - A compiled and installed copy of [TestU01](http://simul.iro.umontreal.ca/testu01/tu01.html)

### Licensing note

The source code for the harness itself is provided under the MIT License. However, this tool is designed to be linked against the TestU01 library, which is licensed under the GNU General Public License v2 (GPL-v2). Consequently, the compiled binary executable (chi32_testu01_harness) is a derivative work and is subject to the terms of the GPL-v2.

## Canonical reference tests

These tests verify that the C implementation produces the exact output defined by the CHI32 specification.

To build and run:

1. Navigate to the `c/` directory.
2. Build the test binary:

   ```bash
   make
   ```

   This creates `build/test_chi32`.

3. Run the test:

   ```bash
   make test
   ```

   Output should confirm all tests have passed.

4. To clean:

   ```bash
   make clean
   ```

## Statistical testing with TestU01

The `tools/testu01_harness/` directory contains a harness for running CHI32 through TestU01's SmallCrush and BigCrush batteries.

### Building the harness

1. Navigate to the harness directory:

   ```bash
   cd tools/testu01_harness/
   ```

2. Build using the default path (relative to the CHI32 repo):

   ```bash
   make
   ```

   If TestU01 is installed elsewhere, specify the path explicitly:

   ```bash
   make TESTU01_INSTALL_DIR=/path/to/your/testu01_installation
   ```

   You can also check path validity with:

   ```bash
   make check_testu01_path [TESTU01_INSTALL_DIR=...]
   ```

### Running tests

Once built, use the `run_test` target to launch a test. Specify:

- `BATTERY` - `SmallCrush` or `BigCrush`
- `SEED` - 64-bit selector (decimal or `0x`-prefixed hex)
- `PHASE` - 64-bit index (decimal or hex)
- `STRATEGY` (optional) - Generation strategy (see below). Default: `sequential`

Log files are saved in `tools/testu01_harness/testu01_logs/`.

### Experimental: testing non-sequential strategies

The TestU01 harness supports additional generation strategies beyond the default sequential pattern. These are useful for exploring how CHI32 performs under alternate access modes.

- `sequential` (default) - Standard generation: selector is fixed (`SEED`), index increments from `PHASE`
- `swapped` - Index is fixed (`SEED`), selector decrements from `PHASE`
- `feedback` - Output of each step influences the next `(selector, index)` pair

#### Example: swapped strategy

```bash
make run_test BATTERY=BigCrush SEED=0x0 PHASE=0x6A09E667F3BCC908 STRATEGY=swapped
```

Here, `SEED` is the fixed index, and `PHASE` is the initial, decrementing selector.

#### Example: feedback strategy

```bash
make run_test BATTERY=BigCrush SEED=0x12345 PHASE=0x67890 STRATEGY=feedback
```

This mode evolves both selector and index based on prior outputs, as described in the CHI32 porting guide.

### Cleaning

To remove all harness build artifacts:

```bash
make clean
```

---

For algorithm documentation, usage guidance, and porting details, see the main [`/docs/`](../docs/) directory.
