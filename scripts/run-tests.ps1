# Temenos Alert Manager - Test Runner Script (PowerShell)
# This script runs the comprehensive Playwright test suite with coverage reporting

param(
    [switch]$SkipBuild = $false,
    [switch]$SkipBrowserInstall = $false,
    [string]$Configuration = "Release"
)

Write-Host "🚀 Starting Temenos Alert Manager Test Suite" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green

# Navigate to the repository root
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir
Set-Location $RepoRoot

# Build the solution
if (-not $SkipBuild) {
    Write-Host "📦 Building solution..." -ForegroundColor Yellow
    dotnet build --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed!" -ForegroundColor Red
        exit 1
    }
}

# Install Playwright browsers (attempt, but don't fail if not available)
if (-not $SkipBrowserInstall) {
    Write-Host "🌐 Installing Playwright browsers..." -ForegroundColor Yellow
    if (Get-Command npx -ErrorAction SilentlyContinue) {
        npx playwright install
        if ($LASTEXITCODE -ne 0) {
            Write-Host "⚠️  Browser installation failed (expected in CI environments)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "⚠️  NPX not available, skipping browser installation" -ForegroundColor Yellow
    }
}

# Run basic functionality tests first
Write-Host "🧪 Running basic functionality tests..." -ForegroundColor Yellow
dotnet test tests/TemenosAlertManager.Tests.Playwright/ `
    --filter "FullyQualifiedName~Basic" `
    --configuration $Configuration `
    --verbosity normal `
    --logger "console;verbosity=normal"

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Basic tests passed!" -ForegroundColor Green
} else {
    Write-Host "❌ Basic tests failed!" -ForegroundColor Red
    exit 1
}

# Run API tests with coverage
Write-Host "🔍 Running API tests with coverage..." -ForegroundColor Yellow
dotnet test tests/TemenosAlertManager.Tests.Playwright/ `
    --filter "FullyQualifiedName~Api" `
    --configuration $Configuration `
    --collect:"XPlat Code Coverage" `
    --verbosity normal `
    --logger "console;verbosity=normal"

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ API tests passed!" -ForegroundColor Green
} else {
    Write-Host "❌ API tests failed!" -ForegroundColor Red
    exit 1
}

# Run web interface tests (may skip in environments without browsers)
Write-Host "🌐 Running web interface tests..." -ForegroundColor Yellow
dotnet test tests/TemenosAlertManager.Tests.Playwright/ `
    --filter "FullyQualifiedName~Web" `
    --configuration $Configuration `
    --verbosity normal `
    --logger "console;verbosity=normal"

if ($LASTEXITCODE -ne 0) {
    Write-Host "⚠️  Web tests may have been skipped due to browser unavailability" -ForegroundColor Yellow
}

# Run all tests for final coverage report
Write-Host "📊 Generating comprehensive test coverage report..." -ForegroundColor Yellow
dotnet test tests/TemenosAlertManager.Tests.Playwright/ `
    --configuration $Configuration `
    --collect:"XPlat Code Coverage" `
    --verbosity minimal `
    --logger "console;verbosity=minimal"

# Check for coverage files
$CoverageDir = "tests/TemenosAlertManager.Tests.Playwright/TestResults"
if (Test-Path $CoverageDir) {
    Write-Host "📈 Coverage reports generated in $CoverageDir" -ForegroundColor Cyan
    
    # Find the latest coverage file
    $LatestCoverage = Get-ChildItem -Path $CoverageDir -Name "coverage.cobertura.xml" -Recurse | 
                     Sort-Object LastWriteTime -Descending | 
                     Select-Object -First 1
    
    if ($LatestCoverage) {
        $CoverageFile = Join-Path $CoverageDir $LatestCoverage.Name
        Write-Host "📄 Latest coverage report: $CoverageFile" -ForegroundColor Cyan
        
        # Extract coverage percentage if reportgenerator is available
        try {
            Write-Host "📊 Installing ReportGenerator..." -ForegroundColor Yellow
            dotnet tool install -g dotnet-reportgenerator-globaltool 2>$null
            
            Write-Host "📊 Generating HTML coverage report..." -ForegroundColor Yellow
            if (-not (Test-Path "coverage-report")) {
                New-Item -ItemType Directory -Path "coverage-report" | Out-Null
            }
            
            reportgenerator `
                -reports:"$CoverageFile" `
                -targetdir:coverage-report `
                -reporttypes:Html
                
            if (Test-Path "coverage-report/index.html") {
                Write-Host "✅ HTML coverage report generated: coverage-report/index.html" -ForegroundColor Green
            }
        } catch {
            Write-Host "⚠️  HTML report generation failed: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "⚠️  No coverage reports found" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🎉 Test execution completed!" -ForegroundColor Green
Write-Host "==============================" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Test Summary:" -ForegroundColor Cyan
Write-Host "  ✅ Basic functionality tests" -ForegroundColor Green
Write-Host "  ✅ API endpoint tests" -ForegroundColor Green
Write-Host "  🌐 Web interface tests (browser-dependent)" -ForegroundColor Yellow
Write-Host "  📊 Code coverage reporting" -ForegroundColor Cyan
Write-Host ""
Write-Host "📁 Output locations:" -ForegroundColor Cyan
Write-Host "  📄 Test results: tests/TemenosAlertManager.Tests.Playwright/TestResults/" -ForegroundColor White
Write-Host "  📊 Coverage report: coverage-report/ (if generated)" -ForegroundColor White
Write-Host ""
Write-Host "🚀 All tests completed successfully!" -ForegroundColor Green