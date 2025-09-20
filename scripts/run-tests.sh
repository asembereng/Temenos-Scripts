#!/bin/bash

# Temenos Alert Manager - Test Runner Script
# This script runs the comprehensive Playwright test suite with coverage reporting

set -e

echo "🚀 Starting Temenos Alert Manager Test Suite"
echo "=============================================="

# Navigate to the repository root
cd "$(dirname "$0")/../.."

# Build the solution
echo "📦 Building solution..."
dotnet build --configuration Release

# Install Playwright browsers (attempt, but don't fail if not available)
echo "🌐 Installing Playwright browsers..."
if command -v npx &> /dev/null; then
    npx playwright install || echo "⚠️  Browser installation failed (expected in CI environments)"
else
    echo "⚠️  NPX not available, skipping browser installation"
fi

# Run basic functionality tests first
echo "🧪 Running basic functionality tests..."
dotnet test tests/TemenosAlertManager.Tests.Playwright/ \
    --filter "FullyQualifiedName~Basic" \
    --configuration Release \
    --verbosity normal \
    --logger "console;verbosity=normal"

if [ $? -eq 0 ]; then
    echo "✅ Basic tests passed!"
else
    echo "❌ Basic tests failed!"
    exit 1
fi

# Run API tests with coverage
echo "🔍 Running API tests with coverage..."
dotnet test tests/TemenosAlertManager.Tests.Playwright/ \
    --filter "FullyQualifiedName~Api" \
    --configuration Release \
    --collect:"XPlat Code Coverage" \
    --verbosity normal \
    --logger "console;verbosity=normal"

if [ $? -eq 0 ]; then
    echo "✅ API tests passed!"
else
    echo "❌ API tests failed!"
    exit 1
fi

# Run web interface tests (may skip in environments without browsers)
echo "🌐 Running web interface tests..."
dotnet test tests/TemenosAlertManager.Tests.Playwright/ \
    --filter "FullyQualifiedName~Web" \
    --configuration Release \
    --verbosity normal \
    --logger "console;verbosity=normal" || echo "⚠️  Web tests may have been skipped due to browser unavailability"

# Run all tests for final coverage report
echo "📊 Generating comprehensive test coverage report..."
dotnet test tests/TemenosAlertManager.Tests.Playwright/ \
    --configuration Release \
    --collect:"XPlat Code Coverage" \
    --verbosity minimal \
    --logger "console;verbosity=minimal"

# Check for coverage files
COVERAGE_DIR="tests/TemenosAlertManager.Tests.Playwright/TestResults"
if [ -d "$COVERAGE_DIR" ]; then
    echo "📈 Coverage reports generated in $COVERAGE_DIR"
    
    # Find the latest coverage file
    LATEST_COVERAGE=$(find "$COVERAGE_DIR" -name "coverage.cobertura.xml" -type f -printf '%T@ %p\n' | sort -k 1nr | head -1 | cut -d' ' -f2-)
    
    if [ -n "$LATEST_COVERAGE" ]; then
        echo "📄 Latest coverage report: $LATEST_COVERAGE"
        
        # Extract coverage percentage if reportgenerator is available
        if command -v dotnet &> /dev/null; then
            echo "📊 Installing ReportGenerator..."
            dotnet tool install -g dotnet-reportgenerator-globaltool || echo "ReportGenerator already installed"
            
            echo "📊 Generating HTML coverage report..."
            mkdir -p coverage-report
            reportgenerator \
                -reports:"$LATEST_COVERAGE" \
                -targetdir:coverage-report \
                -reporttypes:Html \
                || echo "⚠️  HTML report generation failed"
                
            if [ -f "coverage-report/index.html" ]; then
                echo "✅ HTML coverage report generated: coverage-report/index.html"
            fi
        fi
    fi
else
    echo "⚠️  No coverage reports found"
fi

echo ""
echo "🎉 Test execution completed!"
echo "=============================="
echo ""
echo "📋 Test Summary:"
echo "  ✅ Basic functionality tests"
echo "  ✅ API endpoint tests"
echo "  🌐 Web interface tests (browser-dependent)"
echo "  📊 Code coverage reporting"
echo ""
echo "📁 Output locations:"
echo "  📄 Test results: tests/TemenosAlertManager.Tests.Playwright/TestResults/"
echo "  📊 Coverage report: coverage-report/ (if generated)"
echo ""
echo "🚀 All tests completed successfully!"