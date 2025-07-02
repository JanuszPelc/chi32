#!/bin/bash
#
# run_csharp_pracrand.sh
#
# Helper script to run the CHI32 C# streamer utility (`chi32stream`)
# piped to PractRand's RNG_test for statistical analysis.
#
# Make sure chi32stream has been built in Release configuration:
# (from csharp/) dotnet build Chi32.sln -c Release

# --- Configuration: REVIEW AND UPDATE THESE PATHS ---
# Relative path to the chi32stream executable from this script's location
# Assumes this script is in /csharp/tools/
PATH_TO_CHI32STREAM="./Chi32.Utl.Streamer/bin/Release/net8.0/chi32stream"

# !!! IMPORTANT: Update this path to your PractRand RNG_test executable !!!
PATH_TO_RNG_TEST="../../../PractRand/RNG_test"
# Example: PATH_TO_RNG_TEST="$HOME/PractRand/RNG_test"

# Default output directory for logs (relative to this script's location)
LOG_DIR="./practrand_logs"
# ----------------------------------------------------

# --- Default Test Parameters (can be overridden by command-line arguments) ---
DEFAULT_PHASE="0x0"
DEFAULT_STRATEGY="sequential"
DEFAULT_PRACTRAND_TLMAX="256TB"

# --- Function to display usage ---
usage() {
    echo "Usage: $0 --seed <HEX_SEED> [--phase <HEX_PHASE>] [--strategy <STRATEGY>] [--tlmax <TB_LIMIT>] [--practrand-path <PATH>]"
    echo ""
    echo "Arguments:"
    echo "  --seed <HEX_SEED>          : (Required) The 64-bit seed for chi32stream (e.g., 0xFEDCBA9876543210)."
    echo "  --phase <HEX_PHASE>        : (Optional) The starting 64-bit phase for chi32stream. Defaults to ${DEFAULT_PHASE}."
    echo "  --strategy <STRATEGY>      : (Optional) Generation strategy for chi32stream (sequential, swapped, feedback)."
    echo "                               Defaults to ${DEFAULT_STRATEGY}."
    echo "  --tlmax <TB_LIMIT>         : (Optional) Terabyte limit for PractRand's -tlmax. Defaults to ${DEFAULT_PRACTRAND_TLMAX}."
    echo "  --practrand-path <PATH>    : (Optional) Override the PATH_TO_RNG_TEST variable."
    echo ""
    echo "Example: $0 --seed 0x6A09E667F3BCC908 --tlmax 1TB"
    echo "Example: $0 --seed 0xBB67AE8584CAA73B --phase 0x1000 --strategy swapped"
    exit 1
}

# --- Parse Command-Line Arguments ---
SEED_HEX=""
PHASE_HEX="${DEFAULT_PHASE}"
STRATEGY="${DEFAULT_STRATEGY}"
PRACTRAND_TLMAX="${DEFAULT_PRACTRAND_TLMAX}"

while [[ "$#" -gt 0 ]]; do
    case $1 in
        --seed) SEED_HEX="$2"; shift ;;
        --phase) PHASE_HEX="$2"; shift ;;
        --strategy) STRATEGY="$2"; shift ;;
        --tlmax) PRACTRAND_TLMAX="$2"; shift ;;
        --practrand-path) PATH_TO_RNG_TEST="$2"; shift ;;
        -h|--help) usage ;;
        *) echo "Unknown parameter passed: $1"; usage ;;
    esac
    shift
done

if [ -z "${SEED_HEX}" ]; then
    echo "Error: --seed argument is required."
    usage
fi

# --- Validate Paths ---
if [ ! -f "${PATH_TO_CHI32STREAM}" ]; then
    echo "Error: chi32stream not found at '${PATH_TO_CHI32STREAM}'"
    echo "Please ensure it's built in Release configuration and the path in this script is correct."
    exit 1
fi

if [ ! -x "${PATH_TO_RNG_TEST}" ]; then # Check if executable
    echo "Error: PractRand RNG_test not found or not executable at '${PATH_TO_RNG_TEST}'"
    echo "Please update the PATH_TO_RNG_TEST variable in this script or use --practrand-path."
    exit 1
fi

# --- Prepare Log File and Directory ---
mkdir -p "${LOG_DIR}"
# Use SEED_HEX and PHASE_HEX directly in the filename to preserve the 0x prefix if present in the input
OUTPUT_LOG_FILE="${LOG_DIR}/chi32stream_${STRATEGY}_seed_${SEED_HEX}_phase_${PHASE_HEX}.log"

# --- Construct PractRand Options ---
# PractRand's -seed option here is mainly for its own logging/record-keeping.
# The actual random stream comes from stdin32.
PRACTRAND_FULL_OPTIONS="stdin32 -seed ${SEED_HEX} -multithreaded -tlmax ${PRACTRAND_TLMAX}"

# --- Execute the Test ---
echo "================================================================================"
echo "Starting CHI32 C# Streamer Test with PractRand"
echo "--------------------------------------------------------------------------------"
echo "CHI32 Streamer   : ${PATH_TO_CHI32STREAM}"
echo "  Seed           : ${SEED_HEX}"
echo "  Phase          : ${PHASE_HEX}"
echo "  Strategy       : ${STRATEGY}"
echo "PractRand        : ${PATH_TO_RNG_TEST}"
echo "  Options        : ${PRACTRAND_FULL_OPTIONS}"
echo "Log File         : ${OUTPUT_LOG_FILE}"
echo "================================================================================"
echo # Newline for readability before PractRand output

# Run the command, pipe output to tee (for console and file)
"${PATH_TO_CHI32STREAM}" --seed "${SEED_HEX}" --phase "${PHASE_HEX}" --strategy "${STRATEGY}" | \
    "${PATH_TO_RNG_TEST}" ${PRACTRAND_FULL_OPTIONS} 2>&1 | \
    tee "${OUTPUT_LOG_FILE}"

echo "================================================================================"
echo "PractRand test finished. Output saved to: ${OUTPUT_LOG_FILE}"
echo "================================================================================"
