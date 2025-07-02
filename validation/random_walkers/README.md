# Random Walker Heatmaps

Walker heatmaps are a qualitative diagnostic tool for understanding the spatial behavior of pseudo-random number generators. By simulating random walkers guided by each PRNG, the resulting images reveal patterns that may indicate directional bias, diffusion characteristics, or other long-range behaviors.

## Understanding the simulation

At each step, the walker moves one unit in one of four cardinal directions based on output from the specific PRNG. As the walker moves, it accumulates visit counts for each grid cell, which are later rendered into a heatmap image. Brighter areas indicate higher visitation frequency. Darker areas reflect less activity.

A small crosshair marks the origin, which is both the walker's starting point and the reference position for all movement. In generators with well-balanced behavior, the resulting pattern typically shows symmetric, isotropic diffusion centered on this origin, with no signs of drift or directional bias.

## Output folders

The output is grouped into three folders, each corresponding to a different simulation scale. All images share the same resolution, but differ in the number of walker steps contributing to each pixel.

* **[millions_of_steps](./millions_of_steps/):**
  Simulations in this folder use approximately 4 million steps. Each pixel corresponds to about 64 walker steps. The result is a relatively coarse, zoomed-in view, useful for quick inspection or for spotting strong directional patterns early in the walker's path.

* **[billions_of_steps](./billions_of_steps/):**
  This folder contains simulations of roughly 4 billion steps. Each pixel represents around 65 thousand steps. The resulting images provide a clearer view of the generator's overall diffusion behavior, with enough scale to smooth out short-term noise while remaining computationally practical.

* **[trillions_of_steps](./trillions_of_steps/):**
  Simulations in this folder use about 4 trillion steps. Each pixel averages over 67 million walker steps. This level produces highly detailed and stable heatmaps, making it easier to observe large-scale spatial characteristics and assess PRNG suitability for long-running, high-volume simulations.

## Included generators

| Name          | Source / Author                     | Notes                              |
| ------------- | ----------------------------------- | ---------------------------------- |
| chacha20      | D. J. Bernstein (CC0)               | Based on ChaCha20 block function   |
| chi32         | Janusz Pelc (MIT)                   | Random-access, stateless           |
| jsf32         | Bob Jenkins (public domain)         | Compact, stateful                  |
| lcg64         | Numerical Recipes (public domain)   | Simple baseline generator          |
| msws          | Bernard Widynski (public domain)    | Uses Weyl sequence                 |
| mwc           | George Marsaglia (public domain)    | Older design with known biases     |
| pcg32         | Melissa O'Neill (public domain)     | High-quality small-state generator |
| pcg_xsl_rr    | Melissa O'Neill (public domain)     | XSL-RR output variant of PCG       |
| romu_duo_jr   | Chris Doty-Humphrey (public domain) | Very fast, short state             |
| romu_trio     | Chris Doty-Humphrey (public domain) | Larger state, smoother dynamics    |
| splitmix64    | Sebastiano Vigna (public domain)    | Often used for seeding             |
| system_random | .NET built-in (MIT)                 | Standard generator in .NET         |
| xoroshiro64ss | Blackman, Vigna (CC0)               | Successor to XorShift generators   |

## Conclusion

Random walker heatmaps are a qualitative diagnostic tool, not intended to classify any generator as inherently good or bad. Instead, they highlight how different PRNGs behave across various output scales. A generator that performs well at millions of steps may begin to show structural patterns at trillions, depending on its design.

These visualizations are not a replacement for statistical tests, but a complement to them. They offer a way to observe the long-range behavior of a generator in a spatial context, helping to identify patterns, asymmetries, or other distribution characteristics that may not be captured by numeric metrics alone.

The choice of PRNG should reflect the requirements of the application: statistical quality, reproducibility, execution cost, and scale of use. Even simple generators with limited state may be appropriate in constrained or low-risk environments like embedded systems or small-scale simulations.

