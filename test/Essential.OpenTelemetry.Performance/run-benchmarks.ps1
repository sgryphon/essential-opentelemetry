# Script to run all performance benchmarks and generate reports

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Essential.OpenTelemetry Performance Tests" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Navigate to the performance test directory
Set-Location $PSScriptRoot

# Build in Release mode
Write-Host "Building project in Release mode..." -ForegroundColor Yellow
dotnet build -c Release > $null 2>&1

# Run all benchmarks
Write-Host "Running all performance benchmarks..." -ForegroundColor Yellow
Write-Host "This may take several minutes..." -ForegroundColor Yellow
Write-Host ""

dotnet run -c Release

Write-Host ""
Write-Host "======================================" -ForegroundColor Green
Write-Host "Benchmarks complete!" -ForegroundColor Green
Write-Host "Results are in: BenchmarkDotNet.Artifacts/" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green
