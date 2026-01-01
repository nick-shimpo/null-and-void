# Install git hooks for Null and Void project
# Run this script from the project root directory

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$HooksSource = Join-Path $ScriptDir "hooks"
$HooksTarget = Join-Path $ProjectRoot ".git\hooks"

Write-Host "Installing git hooks..."

# Check if we're in a git repository
if (-not (Test-Path (Join-Path $ProjectRoot ".git"))) {
    Write-Host "ERROR: Not a git repository. Run 'git init' first." -ForegroundColor Red
    exit 1
}

# Copy hooks
$hooks = @("pre-commit", "pre-push")
foreach ($hook in $hooks) {
    $sourcePath = Join-Path $HooksSource $hook
    $targetPath = Join-Path $HooksTarget $hook

    if (Test-Path $sourcePath) {
        Copy-Item $sourcePath $targetPath -Force
        Write-Host "✓ Installed $hook hook" -ForegroundColor Green
    }
    else {
        Write-Host "WARNING: $hook hook source not found" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Git hooks installed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Hooks will run automatically on:"
Write-Host "  - pre-commit: Format check, build, and test"
Write-Host "  - pre-push: Full test suite with coverage check"
