#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <string.h>
#include <stdbool.h>
#include <errno.h>
#include <ctype.h>

#include "../src/chi32.h"

// --- Constants ---

#define MAX_LOGICAL_NAME_LEN 128
#define MAX_FILENAME_LEN 128
#define MAX_LINE_LEN 512
#define MAX_TEST_CASES 3

const char* REFERENCE_DATA_ROOT_PATH = "../../validation/canonical_data";

// --- Type Definitions ---

typedef enum {
    STRATEGY_SEQUENTIAL = 0,
    STRATEGY_SWAPPED = 1,
    STRATEGY_FEEDBACK = 2,
    STRATEGY_UNKNOWN = -1
} strategy_kind_t;

typedef struct {
    char logical_name[MAX_LOGICAL_NAME_LEN];
    strategy_kind_t strategy;
    int64_t seed;
    int64_t phase;
    int32_t length;
    char bin_filename[MAX_FILENAME_LEN];
    uint32_t* data_buffer;
} canonical_test_case_t;


// --- Forward Declarations of Helper Functions ---

int parse_canonical_meta_csv(const char* csv_filepath, canonical_test_case_t test_cases[], int max_cases);
bool load_binary_data_for_test_case(canonical_test_case_t* test_case);
bool run_test_sequential(const canonical_test_case_t* test_case);
bool run_test_swapped(const canonical_test_case_t* test_case);
bool run_test_feedback(const canonical_test_case_t* test_case);


// --- Main Function ---

int main(void) {
    printf("CHI32 C Implementation - Canonical Reference Tests\n");
    printf("=================================================\n");

    canonical_test_case_t test_cases[MAX_TEST_CASES];

    for (int i = 0; i < MAX_TEST_CASES; ++i) {
        test_cases[i].data_buffer = NULL;
    }

    char csv_file_path[MAX_FILENAME_LEN * 2];
    snprintf(csv_file_path, sizeof(csv_file_path), "%s/chi32_canonical_meta.csv", REFERENCE_DATA_ROOT_PATH);

    int num_test_cases_parsed = parse_canonical_meta_csv(csv_file_path, test_cases, MAX_TEST_CASES);

    if (num_test_cases_parsed <= 0) {
        fprintf(stderr, "CRITICAL: Failed to parse any test cases from metadata CSV. Exiting.\n");
        return EXIT_FAILURE;
    }

    printf("Parsed %d test case definitions from %s.\n", num_test_cases_parsed, csv_file_path);

    bool all_overall_tests_passed = true;

    for (int i = 0; i < num_test_cases_parsed; ++i) {
        printf("\n--- Processing Test Case: %s ---\n", test_cases[i].logical_name);

        if (!load_binary_data_for_test_case(&test_cases[i])) {
            fprintf(stderr, "  ERROR: Failed to load binary data for %s. Skipping test.\n", test_cases[i].logical_name);
            all_overall_tests_passed = false;
            continue;
        }
        printf("  Successfully loaded %d values from %s.\n", test_cases[i].length, test_cases[i].bin_filename);

        bool current_test_passed = false;
        switch (test_cases[i].strategy) {
            case STRATEGY_SEQUENTIAL:
                current_test_passed = run_test_sequential(&test_cases[i]);
                break;
            case STRATEGY_SWAPPED:
                current_test_passed = run_test_swapped(&test_cases[i]);
                break;
            case STRATEGY_FEEDBACK:
                current_test_passed = run_test_feedback(&test_cases[i]);
                break;
            default:
                fprintf(stderr, "  ERROR: Unknown strategy for test case %s. Skipping.\n", test_cases[i].logical_name);
                current_test_passed = false;
                break;
        }

        if (current_test_passed) {
            printf("  PASS: Test case '%s' verified.\n", test_cases[i].logical_name);
        } else {
            fprintf(stderr, "  FAIL: Test case '%s' failed.\n", test_cases[i].logical_name);
            all_overall_tests_passed = false;
        }
    }

    for (int i = 0; i < num_test_cases_parsed; ++i) {
        if (test_cases[i].data_buffer != NULL) {
            free(test_cases[i].data_buffer);
            test_cases[i].data_buffer = NULL;
        }
    }

    printf("\n=================================================\n");
    if (all_overall_tests_passed) {
        printf("All CHI32 canonical tests PASSED.\n");
        return EXIT_SUCCESS;
    } else {
        printf("One or more CHI32 canonical tests FAILED.\n");
        return EXIT_FAILURE;
    }
}


// --- Implementations of Helper Functions ---

static char* trim_leading_whitespace(char* str) {
    if (str == NULL) return NULL;
    while (*str != '\0' && isspace((unsigned char)*str)) {
        str++;
    }
    return str;
}

int parse_canonical_meta_csv(const char* csv_filepath, canonical_test_case_t test_cases[], int max_cases) {
    FILE* fp = fopen(csv_filepath, "r");
    if (fp == NULL) {
        fprintf(stderr, "ERROR: Could not open CSV metadata file: %s\n", csv_filepath);
        perror("fopen details");
        return -1;
    }

    char line_buffer[MAX_LINE_LEN];
    int cases_parsed = 0;
    int line_number = 0;

    while (fgets(line_buffer, sizeof(line_buffer), fp) != NULL && cases_parsed < max_cases) {
        line_number++;
        char* current_line_ptr = line_buffer;

        current_line_ptr[strcspn(current_line_ptr, "\r\n")] = 0;
        current_line_ptr = trim_leading_whitespace(current_line_ptr);

        if (current_line_ptr[0] == '\0' || current_line_ptr[0] == '#') {
            continue;
        }

        char temp_logical_name[MAX_LOGICAL_NAME_LEN];
        int temp_strategy_code;
        long long temp_seed;
        long long temp_phase;
        int temp_length;
        char temp_bin_filename[MAX_FILENAME_LEN];

        int items_scanned = sscanf(current_line_ptr,
                                   "%127[^,],%d,%lld,%lld,%d,%127s",
                                   temp_logical_name,
                                   &temp_strategy_code,
                                   &temp_seed,
                                   &temp_phase,
                                   &temp_length,
                                   temp_bin_filename);

        if (items_scanned == 6) {
            strncpy(test_cases[cases_parsed].logical_name, temp_logical_name, MAX_LOGICAL_NAME_LEN -1);
            test_cases[cases_parsed].logical_name[MAX_LOGICAL_NAME_LEN -1] = '\0';

            if (temp_strategy_code < STRATEGY_SEQUENTIAL || temp_strategy_code > STRATEGY_FEEDBACK) {
                fprintf(stderr, "WARNING: Invalid strategy code %d on line %d of %s. Skipping line.\n", temp_strategy_code, line_number, csv_filepath);
                continue;
            }
            test_cases[cases_parsed].strategy = (strategy_kind_t)temp_strategy_code;

            test_cases[cases_parsed].seed = (int64_t)temp_seed;
            test_cases[cases_parsed].phase = (int64_t)temp_phase;
            test_cases[cases_parsed].length = (int32_t)temp_length;

            strncpy(test_cases[cases_parsed].bin_filename, temp_bin_filename, MAX_FILENAME_LEN -1);
            test_cases[cases_parsed].bin_filename[MAX_FILENAME_LEN -1] = '\0';

            test_cases[cases_parsed].data_buffer = NULL;

            cases_parsed++;
        } else {
            fprintf(stderr, "WARNING: Malformed line %d in CSV %s (expected 6 fields, got %d). Line: '%s'. Skipping.\n",
                    line_number, csv_filepath, items_scanned, current_line_ptr);
        }
    }

    if (ferror(fp)) {
        fprintf(stderr, "ERROR: Error reading from CSV file: %s\n", csv_filepath);
        perror("fgets details");
        fclose(fp);
        return -1;
    }

    fclose(fp);
    return cases_parsed;
}

bool load_binary_data_for_test_case(canonical_test_case_t* test_case) {
    if (test_case == NULL || test_case->bin_filename[0] == '\0' || test_case->length <= 0) {
        fprintf(stderr, "ERROR (load_binary_data): Invalid test case parameters.\n");
        return false;
    }

    char full_bin_filepath[MAX_FILENAME_LEN * 2];
    snprintf(full_bin_filepath, sizeof(full_bin_filepath), "%s/%s",
             REFERENCE_DATA_ROOT_PATH, test_case->bin_filename);

    test_case->data_buffer = (uint32_t*)malloc(test_case->length * sizeof(uint32_t));
    if (test_case->data_buffer == NULL) {
        fprintf(stderr, "ERROR (load_binary_data): Failed to allocate memory for %d uint32_t values for %s.\n",
                test_case->length, test_case->logical_name);
        perror("malloc details");
        return false;
    }

    FILE* fp = fopen(full_bin_filepath, "rb");
    if (fp == NULL) {
        fprintf(stderr, "ERROR (load_binary_data): Could not open binary data file: %s\n", full_bin_filepath);
        perror("fopen details");
        free(test_case->data_buffer);
        test_case->data_buffer = NULL;
        return false;
    }

    for (int32_t i = 0; i < test_case->length; ++i) {
        unsigned char bytes[4];
        if (fread(bytes, 1, 4, fp) != 4) {
            fprintf(stderr, "ERROR (load_binary_data): Failed to read uint32_t at index %d from %s.\n",
                    i, full_bin_filepath);
            if (feof(fp)) fprintf(stderr, "  (Reached end of file prematurely - expected %d values, read %d)\n", test_case->length, i);
            if (ferror(fp)) perror("  fread details");
            fclose(fp);
            free(test_case->data_buffer);
            test_case->data_buffer = NULL;
            return false;
        }
        test_case->data_buffer[i] = ((uint32_t)bytes[0])       |
                                    ((uint32_t)bytes[1] << 8)  |
                                    ((uint32_t)bytes[2] << 16) |
                                    ((uint32_t)bytes[3] << 24);
    }

    unsigned char temp_byte;
    if (fread(&temp_byte, 1, 1, fp) == 1) {
        fprintf(stderr, "WARNING (load_binary_data): File %s contains more data than expected length %d.\n",
                full_bin_filepath, test_case->length);
    }

    if (ferror(fp)) {
        fprintf(stderr, "ERROR (load_binary_data): An error occurred while reading from %s after successfully reading all expected values.\n", full_bin_filepath);
        perror("  fread details");
    }

    fclose(fp);
    return true;
}

bool run_test_sequential(const canonical_test_case_t* test_case) {
    if (test_case == NULL || test_case->data_buffer == NULL || test_case->strategy != STRATEGY_SEQUENTIAL) {
        fprintf(stderr, "ERROR (run_test_sequential): Invalid test case or data buffer for sequential test.\n");
        return false;
    }

    printf("  Running Sequential Test: Seed=0x%016llX, Initial Phase=0x%016llX, Length=%d\n",
           (long long)test_case->seed, (long long)test_case->phase, test_case->length);

    int64_t current_phase = test_case->phase;
    int32_t errors_found = 0;
    const int max_errors_to_print = 5;

    for (int32_t i = 0; i < test_case->length; ++i) {
        uint32_t actual_value_u32 = (uint32_t)chi32_derive_value_at(test_case->seed, current_phase);

        if (actual_value_u32 != test_case->data_buffer[i]) {
            if (errors_found < max_errors_to_print) {
                fprintf(stderr, "    MISMATCH (Sequential) at index %d (Phase: 0x%016llX):\n", i, (long long)current_phase);
                fprintf(stderr, "      Expected: 0x%08X (%u)\n", test_case->data_buffer[i], test_case->data_buffer[i]);
                fprintf(stderr, "      Actual:   0x%08X (%u)\n", actual_value_u32, actual_value_u32);
            } else if (errors_found == max_errors_to_print) {
                fprintf(stderr, "    (Further sequential mismatches suppressed...)\n");
            }
            errors_found++;
        }
        current_phase++;
    }

    if (current_phase != (test_case->phase + test_case->length)) {
        fprintf(stderr, "    INTERNAL WARNING (Sequential): Phase counter mismatch after loop. Expected: 0x%016llX, Actual: 0x%016llX\n",
                (long long)(test_case->phase + test_case->length), (long long)current_phase);
    }

    if (errors_found > 0) {
        fprintf(stderr, "  Sequential Test FAILED with %d mismatche(s).\n", errors_found);
        return false;
    }

    return true;
}

bool run_test_swapped(const canonical_test_case_t* test_case) {
    if (test_case == NULL || test_case->data_buffer == NULL || test_case->strategy != STRATEGY_SWAPPED) {
        fprintf(stderr, "ERROR (run_test_swapped): Invalid test case or data buffer for swapped test.\n");
        return false;
    }

    // NOTE: For the 'swapped' strategy, the test data intentionally inverts the roles of the input
    // parameters to validate argument independence. The CSV 'seed' is used as the fixed index,
    // and the CSV 'phase' is the initial, decrementing selector. See the porting guide for details.
    int64_t fixed_index = test_case->seed;
    int64_t current_selector = test_case->phase;

    printf("  Running Swapped Test: Initial Selector=0x%016llX, Fixed Index=0x%016llX, Length=%d\n",
           (long long)current_selector, (long long)fixed_index, test_case->length);

    int32_t errors_found = 0;
    const int max_errors_to_print = 5;

    for (int32_t i = 0; i < test_case->length; ++i) {
        uint32_t actual_value_u32 = (uint32_t)chi32_derive_value_at(current_selector, fixed_index);

        if (actual_value_u32 != test_case->data_buffer[i]) {
            if (errors_found < max_errors_to_print) {
                fprintf(stderr, "    MISMATCH (Swapped) at index %d (Selector: 0x%016llX):\n", i, (long long)current_selector);
                fprintf(stderr, "      Expected: 0x%08X (%u)\n", test_case->data_buffer[i], test_case->data_buffer[i]);
                fprintf(stderr, "      Actual:   0x%08X (%u)\n", actual_value_u32, actual_value_u32);
            } else if (errors_found == max_errors_to_print) {
                fprintf(stderr, "    (Further swapped mismatches suppressed...)\n");
            }
            errors_found++;
        }
        current_selector--;
    }

    int64_t expected_final_selector = test_case->phase - test_case->length;
    if (current_selector != expected_final_selector) {
         fprintf(stderr, "    INTERNAL WARNING (Swapped): Selector counter mismatch after loop. Expected: 0x%016llX, Actual: 0x%016llX\n",
                (long long)expected_final_selector, (long long)current_selector);
    }

    if (errors_found > 0) {
        fprintf(stderr, "  Swapped Test FAILED with %d mismatche(s).\n", errors_found);
        return false;
    }

    return true;
}

bool run_test_feedback(const canonical_test_case_t* test_case) {
    if (test_case == NULL || test_case->data_buffer == NULL || test_case->strategy != STRATEGY_FEEDBACK) {
        fprintf(stderr, "ERROR (run_test_feedback): Invalid test case or data buffer for feedback test.\n");
        return false;
    }

    int64_t current_seed_i64 = test_case->seed;
    int64_t current_phase_i64 = test_case->phase;

    printf("  Running Feedback Test: Initial Seed=0x%016llX, Initial Phase=0x%016llX, Length=%d\n",
           (long long)current_seed_i64, (long long)current_phase_i64, test_case->length);

    int32_t errors_found = 0;
    const int max_errors_to_print = 5;

    for (int32_t i = 0; i < test_case->length; ++i) {
        uint32_t actual_value_u32 = (uint32_t)chi32_derive_value_at(current_seed_i64, current_phase_i64);

        if (actual_value_u32 != test_case->data_buffer[i]) {
            if (errors_found < max_errors_to_print) {
                fprintf(stderr, "    MISMATCH (Feedback) at index %d:\n", i);
                fprintf(stderr, "      Input Seed:  0x%016llX, Input Phase: 0x%016llX\n", (long long)current_seed_i64, (long long)current_phase_i64);
                fprintf(stderr, "      Expected:    0x%08X (%u)\n", test_case->data_buffer[i], test_case->data_buffer[i]);
                fprintf(stderr, "      Actual:      0x%08X (%u)\n", actual_value_u32, actual_value_u32);
            } else if (errors_found == max_errors_to_print) {
                fprintf(stderr, "    (Further feedback mismatches suppressed...)\n");
            }
            errors_found++;
        }

        uint64_t prev_seed_u64 = (uint64_t)current_seed_i64;
        uint64_t prev_phase_u64 = (uint64_t)current_phase_i64;
        uint64_t actual_val_as_u64_for_phase = (uint64_t)actual_value_u32; // actual_value_u32 is already uint32_t

        current_seed_i64 = (int64_t)((prev_seed_u64 << 32) | (prev_phase_u64 >> 32));
        current_phase_i64 = (int64_t)((prev_phase_u64 << 32) | actual_val_as_u64_for_phase);
    }

    if (errors_found > 0) {
        fprintf(stderr, "  Feedback Test FAILED with %d mismatche(s).\n", errors_found);
        return false;
    }

    return true;
}
