#!/bin/bash
# Script to run all performance benchmarks and generate reports

set -e

echo "======================================"
echo "Essential.OpenTelemetry Performance Tests"
echo "======================================"
echo ""

# Navigate to the performance test directory
cd "$(dirname "$0")"

# Build in Release mode
echo "Building project in Release mode..."
dotnet build -c Release > /dev/null 2>&1

# Run all benchmarks
echo "Running all performance benchmarks..."
echo "This may take several minutes..."
echo ""

dotnet run -c Release

echo ""
echo "======================================"
echo "Benchmarks complete!"
echo "Results are in: BenchmarkDotNet.Artifacts/"
echo "======================================"
