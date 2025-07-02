# Compiler and Flags
CC ?= gcc
CFLAGS = -std=c99 -Wall -Wextra -pedantic -I$(SRC_DIR)
CFLAGS += -g -O2

# Directories
SRC_DIR = src
TESTS_DIR = tests
BUILD_DIR = build

# Files
TARGET_EXEC = $(BUILD_DIR)/test_chi32
TEST_C_FILE = $(TESTS_DIR)/test_chi32_canonical.c
TEST_OBJ_FILE = $(BUILD_DIR)/test_chi32_canonical.o
HEADER_FILE = $(SRC_DIR)/chi32.h

# Default target: build the test executable
all: $(TARGET_EXEC)

# Rule to link the executable from its object file
$(TARGET_EXEC): $(TEST_OBJ_FILE) | $(BUILD_DIR)
	$(CC) $(CFLAGS) -o $@ $(TEST_OBJ_FILE)

# Rule to compile the test .c file into an object file
# This rule depends on the .c file AND the header file.
$(TEST_OBJ_FILE): $(TEST_C_FILE) $(HEADER_FILE) | $(BUILD_DIR)
	$(CC) $(CFLAGS) -c -o $@ $(TEST_C_FILE)

# Rule to create the build directory if it doesn't exist
$(BUILD_DIR):
	mkdir -p $(BUILD_DIR)

# Target to run the tests
test: $(TARGET_EXEC)
	@echo "Running tests..."
	@cd $(BUILD_DIR) && ./$(notdir $(TARGET_EXEC))
	@echo "Tests finished."

# Target to clean build artifacts
clean:
	@echo "Cleaning up..."
	rm -rf $(BUILD_DIR)
	@echo "Cleanup complete."

.PHONY: all test clean