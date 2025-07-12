#include <stdio.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <stdbool.h>

// --- TestU01 specific headers ---
#include "TestU01.h"
#include "unif01.h"
#include "bbattery.h"

// --- Include the CHI32 header file ---
#include "chi32.h"

// --- Strategy Definition ---
typedef enum {
    STRATEGY_SEQUENTIAL,
    STRATEGY_SWAPPED,
    STRATEGY_FEEDBACK,
    STRATEGY_UNKNOWN
} strategy_kind_t;

// --- Global state for our CHI32 generator ---
static int64_t g_current_selector;
static int64_t g_current_index;

static int64_t g_cli_seed_arg;
static int64_t g_cli_phase_arg;
static strategy_kind_t g_strategy = STRATEGY_SEQUENTIAL; // Default strategy

// --- Forward declarations ---
void initialize_generator_state(void);

const char* strategy_to_string(strategy_kind_t strategy) {
    switch (strategy) {
        case STRATEGY_SEQUENTIAL: return "sequential";
        case STRATEGY_SWAPPED:    return "swapped";
        case STRATEGY_FEEDBACK:   return "feedback";
        default:                  return "unknown";
    }
}

// --- Function to initialize generator state based on strategy ---
void initialize_generator_state(void) {
    switch (g_strategy) {
        case STRATEGY_SEQUENTIAL:
            g_current_selector = g_cli_seed_arg;  // Selector is fixed (from CLI seed)
            g_current_index = g_cli_phase_arg;    // Index/Phase (from CLI phase) increments
            break;
        case STRATEGY_SWAPPED:
            g_current_selector = g_cli_phase_arg; // Selector (from CLI phase) decrements
            g_current_index = g_cli_seed_arg;     // Index is fixed (from CLI seed)
            break;
        case STRATEGY_FEEDBACK:
            g_current_selector = g_cli_seed_arg;  // Initial seed for feedback
            g_current_index = g_cli_phase_arg;    // Initial phase for feedback
            break;
        default:
            fprintf(stderr, "Error: Unknown strategy in initialize_generator_state. Exiting.\n");
            exit(EXIT_FAILURE);
    }
}


// --- Generator function required by TestU01 ---
uint32_t chi32_generator_bits (void) {
    int32_t result_i32;
    uint32_t result_u32;

    switch (g_strategy) {
        case STRATEGY_SEQUENTIAL:
            result_i32 = chi32_derive_value_at(g_current_selector, g_current_index);
            g_current_index++;
            break;
        case STRATEGY_SWAPPED:
            result_i32 = chi32_derive_value_at(g_current_selector, g_current_index);
            g_current_selector--;
            break;
        case STRATEGY_FEEDBACK:
            result_u32 = (uint32_t)chi32_derive_value_at(g_current_selector, g_current_index);

            uint64_t prev_seed_u64 = (uint64_t)g_current_selector;
            uint64_t prev_phase_u64 = (uint64_t)g_current_index;

            g_current_selector = (int64_t)((prev_seed_u64 << 32) | (prev_phase_u64 >> 32));
            g_current_index = (int64_t)(((prev_phase_u64 << 32) | (uint64_t)result_u32));

            return result_u32;
        default:
            fprintf(stderr, "FATAL: Unknown strategy in chi32_generator_bits. Exiting.\n");
            exit(EXIT_FAILURE);
            result_i32 = 0;
            break;
    }
    return (uint32_t)result_i32;
}

// --- Main test harness ---
int main (int argc, char *argv[]) {
    unif01_Gen *gen;
    char *battery_name_arg;
    char *strategy_name_arg = "sequential";

    // --- Argument parsing ---
    if (argc < 4 || argc > 5) {
        fprintf(stderr, "Usage: %s <BatteryName> <hex_seed> <hex_phase> [strategy_name]\n", argv[0]);
        fprintf(stderr, "Example: %s SmallCrush 0x6A09E667F3BCC908 0 sequential\n", argv[0]);
        fprintf(stderr, "Available BatteryNames: SmallCrush, BigCrush\n");
        fprintf(stderr, "Available strategy_names: sequential (default), swapped, feedback\n");
        return 1;
    }

    battery_name_arg = argv[1];
    char *endptr_seed, *endptr_phase;
    unsigned long long ull_seed;

    ull_seed = strtoull(argv[2], &endptr_seed, 0);
    if (*endptr_seed != '\0') {
         fprintf(stderr, "Error: Invalid seed argument '%s'\n", argv[2]);
         return 1;
    }
    g_cli_seed_arg = (int64_t)ull_seed;

    g_cli_phase_arg = strtoll(argv[3], &endptr_phase, 0);
     if (*endptr_phase != '\0') {
         fprintf(stderr, "Error: Invalid phase argument '%s'\n", argv[3]);
         return 1;
    }

    if (argc == 5) {
        strategy_name_arg = argv[4];
        if (strcmp(strategy_name_arg, "sequential") == 0) {
            g_strategy = STRATEGY_SEQUENTIAL;
        } else if (strcmp(strategy_name_arg, "swapped") == 0) {
            g_strategy = STRATEGY_SWAPPED;
        } else if (strcmp(strategy_name_arg, "feedback") == 0) {
            g_strategy = STRATEGY_FEEDBACK;
        } else {
            fprintf(stderr, "Error: Invalid strategy_name '%s'. Available: sequential, swapped, feedback.\n", strategy_name_arg);
            return 1;
        }
    } else {
        g_strategy = STRATEGY_SEQUENTIAL;
    }

    initialize_generator_state();

    char generator_name_str[256];
    const char* strategy_str_for_name = strategy_to_string(g_strategy);

    if (g_strategy == STRATEGY_SWAPPED) {
        snprintf(generator_name_str, sizeof(generator_name_str),
                 "CHI32 (Strategy=%s, InitialSelector=0x%016llX, FixedIndex=0x%016llX)",
                 strategy_str_for_name, (long long)g_cli_phase_arg, (long long)g_cli_seed_arg);
    } else {
        snprintf(generator_name_str, sizeof(generator_name_str),
                 "CHI32 (Strategy=%s, Seed=0x%016llX, InitialPhase=0x%016llX)",
                 strategy_str_for_name, (long long)g_cli_seed_arg, (long long)g_cli_phase_arg);
    }


    printf("========================================\n");
    printf(" Starting TestU01 Harness for %s\n", generator_name_str);
    printf(" Battery to run: %s\n", battery_name_arg);
    printf("========================================\n\n");

    gen = unif01_CreateExternGenBits(generator_name_str, chi32_generator_bits);
    if (gen == NULL) {
        fprintf(stderr, "Error creating TestU01 generator object.\n");
        return 1;
    }

    if (strcmp(battery_name_arg, "SmallCrush") == 0) {
        printf(">>> Running bbattery_SmallCrush...\n");
        fflush(stdout);
        bbattery_SmallCrush(gen);
        printf("<<< bbattery_SmallCrush finished.\n");
    } else if (strcmp(battery_name_arg, "BigCrush") == 0) {
        printf(">>> Running bbattery_BigCrush...\n");
        fflush(stdout);
        bbattery_BigCrush(gen);
        printf("<<< bbattery_BigCrush finished.\n");
    } else {
        fprintf(stderr, "Error: Unknown battery name '%s'. Available: SmallCrush, BigCrush\n", battery_name_arg);
        unif01_DeleteExternGenBits(gen);
        return 1;
    }

    printf("----------------------------------------\n\n");

    unif01_DeleteExternGenBits(gen);

    printf("========================================\n");
    printf(" TestU01 Harness for CHI32 finished successfully for battery: %s.\n", battery_name_arg);
    printf("========================================\n");

    return 0;
}
