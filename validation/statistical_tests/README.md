# CHI32: Statistical Test Results

This folder contains results from statistical testing of CHI32 using its standard access pattern: a fixed selector and a linearly incrementing index. This is the basic usage model and the one most representative of real-world integration.

The purpose of these tests is to validate CHI32's statistical reliability using established, rigorous tools: PractRand and TestU01 (BigCrush).

## PractRand

Logs are located in [./practrand_logs/](./practrand_logs/). Each stream was generated using the C# `chi32stream` utility. A diverse set of six seeds was tested, with each run processing over 256 terabytes of data.

CHI32 performed robustly throughout, with only two minor “unusual” anomalies across all runs. Isolated results like these are expected even for high-quality generators and are not considered signs of statistical weakness.

## TestU01 BigCrush

Logs for this suite are located in [./testu01_logs/](./testu01_logs/). Tests were run using the `chi32_testu01_harness`, compiled from the C reference implementation.

All six seeds passed the full BigCrush battery without a single failure.

## How to reproduce

For building instructions and reproduction steps, see the relevant sections of the [C reference implementation documentation](../../c/) and the [C# reference implementation documentation](../../csharp/).

## Summary

These results indicate that CHI32 meets high statistical standards under its primary usage pattern. Both PractRand and TestU01 BigCrush reported no significant concerns, supporting CHI32's reliability in non-cryptographic applications.

All logs are included for full transparency.