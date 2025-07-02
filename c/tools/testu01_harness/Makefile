CC = gcc
CFLAGS_COMMON = -std=c99 -Wall -Wextra -pedantic -O2 -g

# --- Project Paths ---
# Path to the CHI32 'src' directory, relative to this Makefile
CHI32_SRC_DIR = ../../src

# Path to TestU01 installation directory.
# !!! USER ACTION MAY BE REQUIRED HERE !!!
# This path is set to a default relative to the main CHI32 project folder,
# assuming TestU01-1.2.3 is a sibling directory.
# If your TestU01 is installed elsewhere, or your directory structure differs,
# you MUST override this variable when running make.
# Example: make TESTU01_INSTALL_DIR=/path/to/your/testu01_installation
TESTU01_INSTALL_DIR_DEFAULT_RELATIVE = ../../../../TestU01-1.2.3/_install
TESTU01_INSTALL_DIR ?= $(TESTU01_INSTALL_DIR_DEFAULT_RELATIVE)

# --- Include Paths ---
# Check if TestU01 include directory exists, provide guidance if not.
ifeq ($(wildcard $(TESTU01_INSTALL_DIR)/include/TestU01.h),)
    $(warning !!! TestU01 header 'TestU01.h' not found at '$(TESTU01_INSTALL_DIR)/include'. !!!)
    $(warning Default relative path assumes 'TestU01-1.2.3' is a sibling to the main CHI32 project folder.)
    $(warning If TestU01 is installed elsewhere, or your top-level project folder has a different name,)
    $(warning please specify its location via: make TESTU01_INSTALL_DIR=/path/to/your/testu01_installation)
    $(warning Or edit the TESTU01_INSTALL_DIR variable in this Makefile.)
    # $(error Halting due to missing TestU01.h. Please set TESTU01_INSTALL_DIR correctly.)
endif

CFLAGS = $(CFLAGS_COMMON) -I$(CHI32_SRC_DIR) -I$(TESTU01_INSTALL_DIR)/include

# --- Linker Flags and Libraries ---
LDFLAGS = -L$(TESTU01_INSTALL_DIR)/lib
LIBS = -ltestu01 -lprobdist -lmylib -lm # TestU01 libraries

# --- Target Executable ---
TARGET = chi32_testu01_harness
SRC = main.c
LOG_DIR = testu01_logs

.PHONY: all clean run_test check_testu01_path $(LOG_DIR)

all: $(TARGET)

check_testu01_path:
	@echo "Attempting to locate TestU01 using path: $(TESTU01_INSTALL_DIR)"
	@echo "This path is derived from TESTU01_INSTALL_DIR_DEFAULT_RELATIVE or your override."
	@if [ -f "$(TESTU01_INSTALL_DIR)/include/TestU01.h" ]; then \
		echo "  [OK] Found: $(TESTU01_INSTALL_DIR)/include/TestU01.h"; \
	else \
		echo "  [MISSING] TestU01 header 'TestU01.h' not found at '$(TESTU01_INSTALL_DIR)/include'."; \
		echo "  Ensure 'TestU01-1.2.3' (containing an '_install' subdirectory with TestU01) is a sibling to your main project repository folder."; \
		echo "  Or, provide the correct path: make TESTU01_INSTALL_DIR=/path/to/testu01_installation"; \
		exit 1; \
	fi
	@if [ -f "$(TESTU01_INSTALL_DIR)/lib/libtestu01.a" ] || [ -f "$(TESTU01_INSTALL_DIR)/lib/libtestu01.so" ] || [ -f "$(TESTU01_INSTALL_DIR)/lib/libtestu01.dylib" ]; then \
		echo "  [OK] Found a TestU01 library file in $(TESTU01_INSTALL_DIR)/lib (e.g., libtestu01.a, .so, or .dylib)"; \
	else \
		echo "  [MISSING] TestU01 library (libtestu01.a, .so, or .dylib) not found in '$(TESTU01_INSTALL_DIR)/lib'."; \
		echo "  Ensure TestU01 libraries are correctly installed there."; \
		exit 1; \
	fi
	@echo "TestU01 path check appears successful for the specified location."


$(TARGET): $(SRC) $(CHI32_SRC_DIR)/chi32.h | $(LOG_DIR) check_testu01_path
	@echo "Compiling and Linking $(TARGET)..."
	@echo "Using TestU01 from: $(TESTU01_INSTALL_DIR)"
	@echo "Using CHI32 headers from: $(CHI32_SRC_DIR)"
	$(CC) $(CFLAGS) $(LDFLAGS) -o $@ $< $(LIBS)
	@echo "$(TARGET) created successfully."

$(LOG_DIR):
	mkdir -p $(LOG_DIR)

clean:
	@echo "Cleaning up $(TARGET) and logs..."
	rm -f $(TARGET)
	rm -rf $(LOG_DIR)
	@echo "Cleanup complete."

# --- MODIFIED: run_test target ---
# Defines default strategy if not provided
STRATEGY_ARG ?= sequential

run_test: $(TARGET)
ifndef BATTERY
	$(error BATTERY variable is not set. Usage: make run_test BATTERY=SmallCrush ...)
endif
ifndef SEED
	$(error SEED variable is not set. Usage: make run_test SEED=0x... or SEED=0 ...)
endif
ifndef PHASE
	$(error PHASE variable is not set. Usage: make run_test PHASE=0x... or PHASE=0 ...)
endif
# The STRATEGY variable can be overridden from the command line, e.g., make run_test STRATEGY=swapped ...
# If STRATEGY is passed, it will override STRATEGY_ARG. If not, STRATEGY_ARG (sequential) is used.
	@echo "Running $(TARGET) with BATTERY=$(BATTERY) SEED=$(SEED) PHASE=$(PHASE) STRATEGY=$(or $(STRATEGY),$(STRATEGY_ARG))..."
	./$(TARGET) $(BATTERY) $(SEED) $(PHASE) $(or $(STRATEGY),$(STRATEGY_ARG)) 2>&1 | tee "$(LOG_DIR)/$(TARGET)_$(BATTERY)_$(or $(STRATEGY),$(STRATEGY_ARG))_$(SEED)_$(PHASE).log"
	@echo "---"
	@echo "TestU01 output was displayed and saved to $(LOG_DIR)/$(TARGET)_$(BATTERY)_$(or $(STRATEGY),$(STRATEGY_ARG))_$(SEED)_$(PHASE).log"
