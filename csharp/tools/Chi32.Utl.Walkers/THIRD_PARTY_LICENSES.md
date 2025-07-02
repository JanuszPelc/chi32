## PRNG Implementations for Comparison

The random walker simulation tool includes implementations of several other pseudo-random number generators for comparative analysis. These are used for diagnostic purposes only and are not part of the core CHI32 library.

### ChaCha20
- **Author/Source:** Daniel J. Bernstein
- **License:** Public Domain / CC0-compatible
- **Reference:** https://cr.yp.to/chacha.html

### JSF32 (Jenkins Small Fast)
- **Author/Source:** Bob Jenkins
- **License:** Public Domain
- **Reference:** http://burtleburtle.net/bob/rand/smallprng.html

### LCG64 (Linear Congruential Generator)
- **Author/Source:** Based on constants from Numerical Recipes
- **License:** Public Domain
- **Reference:** https://en.wikipedia.org/wiki/Linear_congruential_generator

### MSWS (Middle-Square Weyl Sequence)
- **Author/Source:** Bernard Widynski
- **License:** Public Domain
- **Reference:** https://arxiv.org/abs/1704.00358

### MWC (Multiply-with-Carry)
- **Author/Source:** George Marsaglia
- **License:** Public Domain
- **Reference:** https://en.wikipedia.org/wiki/Multiply-with-carry

### PCG (Permuted Congruential Generator) Family
- **Author/Source:** Melissa E. O'Neill
- **License:** Public Domain (based on reference implementation) / Apache 2.0 (for the library)
- **Reference:** https://www.pcg-random.org/
- **Note:** The implementations for `Pcg32` and `PcgXslRr` are based on the simple, public domain reference code.

### Romu Family (RomuDuoJr, RomuTrio)
- **Author/Source:** Chris Doty-Humphrey
- **License:** Public Domain
- **Reference:** http://pracrand.sourceforge.net/RNG_engines.txt

### SplitMix64
- **Author/Source:** Sebastiano Vigna
- **License:** Public Domain / CC0
- **Reference:** http://prng.di.unimi.it/splitmix64.c

### Xoroshiro64**
- **Author/Source:** David Blackman and Sebastiano Vigna
- **License:** Public Domain / CC0
- **Reference:** http://prng.di.unimi.it/xoroshiro64starstar.c
