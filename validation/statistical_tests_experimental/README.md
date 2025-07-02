# CHI32: Experimental Statistical Tests

This folder contains statistical test results for CHI32 under alternative input strategies that depart from the standard sequential pattern.

Many real-world applications—such as procedural generation or spatial simulations—naturally involve varied combinations of selector and index. These patterns are common in practice, even if they differ from the basic linear model.

The purpose of this testing is to evaluate CHI32's behavior in such contexts, particularly where input structure deviates from the simple incrementing-index approach. All results were produced using the TestU01 BigCrush suite, one of the most rigorous tools available for evaluating pseudo-random number generators.

## Swapped argument strategy

Logs are located in [./testu01_swapped_logs/](./testu01_swapped_logs/).

In this setup, the roles of `selector` and `index` are reversed. The index remains fixed while the selector is decremented with each step. This tests resilience to inverted access patterns and altered input emphasis.

All six seeds passed the full BigCrush battery without any failures.

## Feedback strategy

Logs are located in [./testu01_feedback_logs/](./testu01_feedback_logs/).

In this configuration, each output value influences the selection of the next `(selector, index)` pair. This introduces stateful feedback into the otherwise stateless core logic. The approach is described in the CHI32 porting guide.

Of the six seeds tested:
- Four passed the entire BigCrush battery
- Two showed a single minor deviation:
  - `p = 7.9e-4` in `snpair_ClosePairs`
  - `p = 8.7e-4` in `sstring_HammingCorr`

These p-values are on the edge of the standard significance level but are not considered failures, suggesting that CHI32 retains robust performance even under this highly chaotic feedback model.

## How to reproduce

For building instructions and reproduction steps, see the relevant section of the [C reference implementation documentation](../../c/).

## Summary

These tests explore CHI32's behavior under input strategies that emphasize variation over strict sequentiality. The results indicate that CHI32 maintains strong statistical performance even outside conventional access patterns and supports use cases involving non-linear or application-specific generation schemes.

All logs are included for full transparency.
