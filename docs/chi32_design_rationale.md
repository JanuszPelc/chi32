# CHI32: Finding the Fit

This is a behind-the-scenes look at the motivations, challenges, discoveries, and technical tradeoffs that shaped the Cascading Hash Interleave 32-bit (CHI32).

## The question

The initial spark was a simple question: could a 32-bit non-cryptographic hash function be designed with statistical quality strong enough to serve as the foundation for a freely addressable pseudo-random number generator?

The motivation came from practical needs in game development. The aim was to use such a hypothetical hashing function in a PRNG utility class with an immutable seed, an incrementally advancing phase, and the ability to inspect any arbitrary phase immutably.

This conceptual model was intended for tasks like procedural generation, deterministic serialization, undo/redo systems, and other workflows that benefit from stateless, not necessarily sequential access to random values.

## Defining the boundaries

The question led to a more focused effort to design a 32-bit hashing function with statistical quality robust enough to support that concept, all within a strict performance budget. The target was practical and measurable: about one nanosecond per call on an Apple M1 processor.

While functions like [FNV](https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function), [xxHash](https://github.com/Cyan4973/xxHash), and [MurmurHash](https://en.wikipedia.org/wiki/MurmurHash) were well-known benchmarks, the chosen direction favored a fresh design tailored to the project’s specific goals and constraints. To pursue that direction, a small, selected set of building blocks balanced effectiveness with the CPU budget: prime multiplications, bitwise XOR operations, and bit rotations.

Although the building blocks were conventional, their statistical effectiveness depended heavily on specific values, particularly the shift offsets and the primes used for multiplication. Properly tuned parameters would improve bit diffusion, reduce repeating patterns, and help avoid structural bias in the output. Selecting and refining these constants was expected to be a key focus in the next phases of tuning.

## Building the fitness model

To evaluate candidate constants, a custom fitness function combined multiple quality metrics into a single score. The function assigned weighted importance to several dimensions, including diffusion strength, bit distribution, statistical deviation, and collision rates, using both ordered and randomized input sequences.

The scoring process began with simple brute-force methods to search for [Xoshiro256+](https://prng.di.unimi.it) seeds that produced input sets yielding the highest observed fitness scores among well-established reference hash functions. Each selected seed defined a set containing millions of 32-bit integers. In parallel, the relative weights within the fitness function were adjusted to leave about 20 percent headroom, ensuring sensitivity to further improvements.

To standardize comparison of the hashing function under development, a normalized fitness score was introduced. For each dataset, the highest score achieved by any reference hash function served as the baseline. The candidate’s score was then divided by this baseline to produce a normalized score expressed as a percentage.

In this setup, the initial version of the hashing function performed modestly. Averaged across datasets, its normalized fitness score was just below 30 percent. This result clearly indicated that significant parameter tuning remained necessary.

## Searching the parameter space

To identify optimal parameter sets, a brute-force exploration tool ranked each configuration by its normalized fitness score. Individual candidate primes were first filtered to avoid repetitive bit patterns, maintain balanced bit distribution, and remain safely distant from power-of-two values. Viable primes were then grouped into sets and tested across a broad range of shift-offset combinations.

As parameter tuning progressed, the fitness function weights were gradually refined to emphasize traits most strongly correlated with high-quality output. This led to a final distribution in which diffusion strength, chi-square uniformity, and output deviation were each assigned an equal 33 percent share, forming the core of the fitness score.

Collision-related tests were given a combined weight of just 1 percent, and critically, a strict filter was introduced: any parameter set that produced collisions in the isolated-input test was immediately discarded, regardless of its performance elsewhere. This enforced basic injectivity while allowing chained tests to expose deeper structural flaws.

With this configuration settled, [CRC32](https://en.wikipedia.org/wiki/Cyclic_redundancy_check) stood out as a very strong performer. While not a general-purpose hash in the usual sense, its performance happened to align well with the chi-square and deviation metrics emphasized at this stage, giving it a clear advantage in the fitness model despite its suboptimal diffusion strength.

Due to the size of the datasets and the level of detail required in each evaluation, a single testing round typically took about an hour to complete. To speed up the process, a dedicated machine was configured to run eight instances of the search tool in parallel. It ran continuously in the background, evaluating new candidate sets in pursuit of the highest-performing parameter combinations.

## The overfitting pitfall

Standard benchmarking suites such as [SMHasher](https://github.com/rurban/smhasher) were intentionally excluded during the main development phase. The concern was that brute-force-evaluated parameter sets could begin to adapt too closely to the structure and expectations of a fixed test suite. This kind of implicit overfitting risked producing strong scores without guaranteeing broader statistical reliability.

Rather than optimize directly against such benchmarks, the focus remained on tuning the hashing function’s parameters under defined constraints while actively mitigating overfitting risk. The plan was to evaluate its effectiveness later through a pseudo-random number generator prototype. If that prototype could pass demanding statistical test suites such as [PractRand](https://pracrand.sourceforge.net) and [TestU01](https://simul.iro.umontreal.ca/testu01/tu01.html) BigCrush, it would offer indirect evidence of the underlying hash function’s quality.

Having full control over the scoring model allowed for the periodic introduction of new datasets, specifically to raise the scores of well-established hash functions, thereby lowering the relative standing of the current design. These disruptions were used to challenge emerging biases, expose weaknesses, and keep evaluation pressure aligned with evolving priorities.

In addition, an important rule in fitness calculation was to always use the worst-case result when a metric had multiple outcomes. Regardless of input order, alignment, or test conditions, only the weakest result was counted. This conservative policy prevented optimistic bias and favored consistently reliable configurations.

A key change introduced to further mitigate overfitting was the use of a composite fitness score. Rather than relying solely on the average normalized fitness score, the scoring process began to penalize candidates based on performance variance across all datasets. This shift helped steer the selection process toward general-purpose reliability rather than isolated statistical highs.

## Finding the fit

By this point, the search for a top-performing configuration had already been running in the background for roughly two months. During that time, the composite fitness score gradually improved, eventually reaching around 85 percent.

It was then that the decision was made to trust the setup that had emerged through earlier refinement. No further changes were made to the fitness-function weights or the dataset pool. The goal was simple but demanding: discover a configuration with both normalized and composite fitness scores greater than 100 percent.

Such a result would mean that even the lowest relative score across all datasets still exceeded the best score from any competitor. Whether this was achievable remained unclear, but based on the project’s original framing, anything below that threshold was not considered strong enough.

Two more months of unattended search later, the configuration that surpassed the intended baseline finally emerged. Altogether, the search spanned more than four months and explored more than a third of a billion parameter combinations. This marked the conclusion of the hash function’s tuning process and set the stage for the PRNG’s final design.

## Breaking through

The task ahead was clear: generate a high-quality 32-bit pseudo-random value from a pair of 64-bit inputs, within an arbitrarily chosen 15-nanosecond CPU budget. The prototype started simple. Each input was split into halves, then independently hashed. The intermediate hashes were combined through a final hashing step.

That early arrangement had a kind of elegant symmetry, but PractRand tests consistently revealed a sharp drop-off in output quality after just 32 gigabytes. Multiple variations were explored, including different forms of pre- and post-processing, reordering of operations, and lightweight mixing additions. None of them, however, resolved the underlying issue.

A more deliberate strategy was needed. This led to the cascading sequence of four interleaved hash updates, designed to simulate the evolution of a 64-bit intermediate state. It was not modeled on any particular PRNG, but it echoed the layered state transitions found in some of the best ones.

This structure alone consumed more than half of the CPU budget, but it broke through the earlier statistical limitations. Output integrity was maintained across hundreds of gigabytes, with degradation appearing later and varying by seed. With this new core in place, the design felt like it had a foundation worth building on.

## Deciding to share

With the intermediate hashing structure in place, two missing pieces remained. Both were designed with close attention to the CPU budget, where every nanosecond still mattered.

The first was input pre-processing. Instead of feeding raw arguments directly into the core logic, the inputs were transformed into two conceptual pointers: one advancing, the other retreating. They evolved in an uncorrelated manner to reduce structural bias and improve diffusion early in the pipeline without adding significant cost.

The second was a final slicing step. From the 64-bit intermediate result, a 32-bit output was extracted using a state-dependent offset. This technique was loosely inspired by the “Dropping Bits Using a Random Shift” section of [The PCG Paper](https://www.pcg-random.org/paper.html). It introduced lightweight irregularity at the boundary and helped suppress subtle artifacts.

Together, these refinements noticeably improved the algorithm’s statistical resilience. PractRand runs extended beyond the terabyte mark with only rare “unusual” anomalies. At that point, the decision to open-source the work came naturally. Not as a milestone, but as a quiet recognition that a practically useful, statistically robust, and conceptually distinct design deserved to be shared with others.

## The final twist

During an ongoing PractRand run, one seed exhibited a mildly suspicious anomaly around the 8-terabyte mark. Meanwhile, in the TestU01 BigCrush battery, two other seeds triggered single minor deviations. These were not catastrophic failures, but they were not clean passes either.

This led to a decision to pause and run a focused series of experiments targeting the input pre-mixing phase. The hypothesis was that subtle improvements at this early stage might reinforce output quality, particularly under stress.

Initial attempts leaned toward increasing the influence of the index, based on the assumption that stronger mixing on that side would enhance statistical diffusion. Surprisingly, every variation along those lines degraded output quality. More counterintuitively, reducing mixing improved stability for seeds close to zero.

After a series of trials, one variant stood out. It preserved the structure of the original logic, but the influence on diffusion strength shifted from the index to the selector. A selector-derived mask now gated every index bit, producing progressively denser coupling as the selector moves away from all-zero or all-one patterns in either direction.

This change led to notable improvements. The previously mildly suspicious anomaly disappeared. The total count of unusual anomalies in PractRand dropped from over a dozen to just two. And in BigCrush, all seeds now passed cleanly without exception.

That it also reduced CPU usage by over a nanosecond ultimately sealed the decision to adopt this less intuitive but clearly better design.

## The answer

It was a rocky road, but in the end, reference implementations tested with six diverse seeds passed the full TestU01 BigCrush battery. In the PractRand suite, each of those seeds processed over 256 terabytes of data without any sign of statistical weakness.

That is the answer CHI32 gives.
