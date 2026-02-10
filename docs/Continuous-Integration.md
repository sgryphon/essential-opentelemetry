# Continuous Integration and Testing

This document describes the automated CI/CD pipeline for Essential OpenTelemetry.

## Overview

The project uses **GitHub Actions** for continuous integration and automated testing. The CI pipeline runs automatically on:

- **Pull Requests** to main/master branches
- **Direct pushes** to main/master branches
- **Manual dispatch** (via GitHub UI)

## What the CI Pipeline Does

The automated pipeline performs the following checks:

### 1. Code Formatting Check

Verifies that all C# code is properly formatted using **CSharpier**.

```bash
dotnet csharpier check .
```

If formatting issues are found, the build will fail. To fix locally:

```bash
dotnet csharpier format .
```

### 2. Build Verification

Builds the entire solution for all target frameworks:

- .NET 10.0
- .NET 9.0
- .NET 8.0

```bash
dotnet build Essential.OpenTelemetry.slnx --configuration Release
```

### 3. Unit Tests

Runs all xUnit tests across all target frameworks:

```bash
dotnet test Essential.OpenTelemetry.slnx --configuration Release
```

### 4. Code Coverage

Collects code coverage data using **Coverlet** and generates reports:

- Coverage data is collected in Cobertura XML format
- Reports are generated using **ReportGenerator** in HTML and Markdown formats
- Coverage summary is posted as a comment on pull requests
- Reports are uploaded to **Codecov** (if configured)

## Local Development

### Prerequisites

- .NET SDK 8.0, 9.0, and 10.0
- Git

### Running Checks Locally

Before submitting a pull request, run these commands:

```bash
# Restore tools
dotnet tool restore

# Check formatting
dotnet csharpier check .

# Or auto-format code
dotnet csharpier format .

# Build
dotnet build Essential.OpenTelemetry.slnx

# Run tests
dotnet test Essential.OpenTelemetry.slnx

# Run tests with coverage
dotnet test Essential.OpenTelemetry.slnx --collect:"XPlat Code Coverage"
```

## Code Coverage Setup

### Coverlet Collector

The test project includes the `coverlet.collector` package, which integrates with the test SDK to collect coverage data without additional configuration.

### Viewing Coverage Reports

Coverage reports are automatically generated for each CI run:

1. **On Pull Requests**: A coverage summary is posted as a comment
2. **Artifacts**: Full HTML reports are available as workflow artifacts
3. **Codecov** (optional): If configured, coverage trends are tracked at codecov.io

#### Accessing HTML Reports

1. Go to the GitHub Actions tab
2. Click on the workflow run
3. Download the `coverage-report` artifact
4. Extract and open `index.html` in a browser

### Local Coverage Reports

To generate coverage reports locally:

```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Install ReportGenerator (if not already installed)
dotnet tool install --global dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator -reports:"./coverage/**/coverage.cobertura.xml" -targetdir:"./coverage-report" -reporttypes:"Html"

# Open the report
open ./coverage-report/index.html  # macOS
# or
start ./coverage-report/index.html  # Windows
# or
xdg-open ./coverage-report/index.html  # Linux
```

## Codecov Integration (Optional)

To enable Codecov integration:

1. Sign up at [codecov.io](https://codecov.io) with your GitHub account
2. Add the repository to Codecov
3. Copy the upload token
4. Add it as a repository secret named `CODECOV_TOKEN`:
   - Go to repository Settings → Secrets and variables → Actions
   - Click "New repository secret"
   - Name: `CODECOV_TOKEN`
   - Value: Your Codecov token

Once configured, coverage trends and detailed reports will be available at codecov.io.

## Workflow Configuration

The CI workflow is defined in `.github/workflows/ci.yml`.

### Key Features

- **Multi-framework Support**: Tests run on .NET 8, 9, and 10
- **Fast Feedback**: Parallel job execution where possible
- **Comprehensive Checks**: Formatting, build, tests, and coverage in one workflow
- **PR Integration**: Coverage summaries posted directly to pull requests
- **Artifact Storage**: Coverage reports saved for 90 days

### Customization

To modify the workflow:

1. Edit `.github/workflows/ci.yml`
2. Common modifications:
   - Add/remove .NET versions in the `Setup .NET` step
   - Adjust coverage thresholds
   - Add additional quality checks (linting, security scanning)
   - Configure different triggers

## Troubleshooting

### Build Fails on Formatting

```bash
# Fix locally
dotnet csharpier format .
git add .
git commit -m "Fix code formatting"
```

### Tests Pass Locally but Fail in CI

- Ensure you're testing against all target frameworks: `dotnet test`
- Check for environment-specific issues (paths, line endings)
- Review the CI logs for specific error messages

### Coverage Report Not Generated

- Verify coverlet.collector is installed in test projects
- Check that tests are actually running (not skipped)
- Ensure the coverage results directory exists and is writable

## Best Practices

1. **Always run formatting** before committing: `dotnet csharpier format .`
2. **Run tests locally** before pushing: `dotnet test`
3. **Check CI status** before merging pull requests
4. **Review coverage reports** to identify untested code
5. **Keep dependencies updated** for security and compatibility

## Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
- [ReportGenerator Documentation](https://github.com/danielpalme/ReportGenerator)
- [CSharpier Documentation](https://csharpier.com/)
- [xUnit Documentation](https://xunit.net/)

## Support

If you encounter issues with the CI pipeline:

1. Check the workflow run logs in the Actions tab
2. Review this documentation
3. Open an issue with details about the problem
