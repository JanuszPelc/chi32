# CHI32: C# Reference Implementation

This directory contains the C# reference implementation of the Cascading Hash Interleave 32-bit (CHI32) pseudo-random number generator.

The core algorithm is implemented as a self-contained file: `src/Chi32/Chi32.cs`. For basic use, you only need to copy this file into your project.

Included components:

- Core `Chi32` static class with the algorithm’s logic
- Canonical reference tests to ensure conformance
- BenchmarkDotNet projects for performance evaluation
- Utility tools for generating reference data, streaming output, and producing spatial walker heatmaps

## Directory structure

- `Chi32.sln`: Main solution file
- `Directory.Build.props`: Shared build configuration
- `src/Chi32/`: Core library
- `tests/Chi32.Tests/`: xUnit-based reference tests
- `benchmarks/Chi32.Benchmarks/`: BenchmarkDotNet project
- `tools/Chi32.Utl.Generator/`: Generates canonical `.bin` and `.csv` files
- `tools/Chi32.Utl.Streamer/`: Streams CHI32 output to `stdout`
- `tools/Chi32.Utl.Walkers/`: Runs spatial simulations to produce heatmap visualizations

## Prerequisites

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) or later

## Building the solution

From the `csharp/` directory:

- For a **Debug** build (used for development and testing):

  ```bash
  dotnet build Chi32.sln -c Debug
  ```

- For a **Release** build (recommended for utilities and benchmarks):

  ```bash
  dotnet build Chi32.sln -c Release
  ```

Alternatively, open `Chi32.sln` in Visual Studio or Rider.

## Running reference tests

To verify output correctness:

1. Ensure a Debug build exists.
2. From the `csharp/` directory, run:

   ```bash
   dotnet test Chi32.sln
   ```

   Or target the test project directly:

   ```bash
   dotnet test tests/Chi32.Tests/Chi32.Tests.csproj -c Debug
   ```

*Note: The Fluent Assertions library may print a license-related message. It is free for non-commercial use.*

## Using the utilities

These tools are intended for Release builds.

### Canonical data generator

- Output: `tools/Chi32.Utl.Generator/bin/Release/net8.0/chi32gen`
- Use only if regenerating `.bin` or `.csv` reference files (not required for typical use)

### PRNG streamer (`chi32stream`)

- Output: `tools/Chi32.Utl.Streamer/bin/Release/net8.0/chi32stream`
- Streams CHI32 output to `stdout`, suitable for input to tools like PractRand

A helper script is provided:

**`run_csharp_pracrand.sh`**

1. Ensure `chi32stream` is built in Release mode.
2. Navigate to `tools/`
3. Open the script and update `PATH_TO_RNG_TEST` to point to your local `RNG_test` binary.
4. Make the script executable:

   ```bash
   chmod +x run_csharp_pracrand.sh
   ```

5. Run it:

   ```bash
   ./run_csharp_pracrand.sh --seed 0xYOUR_SEED
   ```

   Optional arguments include phase, strategy, and stream length:

   ```bash
   ./run_csharp_pracrand.sh --seed 0x6A09E667F3BCC908 --phase 0x100 --strategy feedback --tlmax 32TB
   ```

Output logs will appear in `tools/practrand_logs/`.

For manual usage:

```bash
./tools/Chi32.Utl.Streamer/bin/Release/net8.0/chi32stream --help
```

### Walker simulator

- Output: `tools/Chi32.Utl.Walkers/bin/Release/net8.0/`
- Performs deterministic random walker simulations using CHI32 and other PRNGs
- Generates visual heatmaps grouped by step scale:

  - `millions_of_steps/`
  - `billions_of_steps/`
  - `trillions_of_steps/`

To run:

```bash
dotnet run --project tools/Chi32.Utl.Walkers/Chi32.Utl.Walkers.csproj -c Release
```

Images will be saved to subfolders based on scale. Each file is named after the PRNG used.

For simulation details, see the [heatmap README](../validation/random_walkers/README.md).

### Benchmarks

Run with Release builds:

```bash
dotnet run --project benchmarks/Chi32.Benchmarks/Chi32.Benchmarks.csproj -c Release --filter "*"
```

---

For details on the CHI32 algorithm, porting, and design rationale, see the main documentation in the [`/docs/`](../docs/) directory.
