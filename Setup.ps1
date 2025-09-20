# Noxon Executor Setup Script
Write-Host "Noxon Executor Setup" -ForegroundColor Green
Write-Host "====================" -ForegroundColor Green

# Check prerequisites
$prerequisites = @(
    @{ Name = "Visual Studio 2022"; Check = { Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\VisualStudio\17.0" -ErrorAction SilentlyContinue } },
    @{ Name = ".NET Framework 4.8"; Check = { Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" -ErrorAction SilentlyContinue } },
    @{ Name = "Windows SDK"; Check = { Get-Command "makeappx.exe" -ErrorAction SilentlyContinue } }
)

foreach ($prereq in $prerequisites) {
    if (& $prereq.Check) {
        Write-Host "✓ $($prereq.Name)" -ForegroundColor Green
    } else {
        Write-Host "✗ $($prereq.Name) - MISSING" -ForegroundColor Red
    }
}

# Download Lua library
Write-Host "`nDownloading Lua library..." -ForegroundColor Yellow
$luaUrl = "https://www.lua.org/ftp/lua-5.4.4.tar.gz"
$luaPath = "lua-5.4.4"

if (-not (Test-Path $luaPath)) {
    Invoke-WebRequest -Uri $luaUrl -OutFile "lua.tar.gz"
    tar -xzf lua.tar.gz
    Remove-Item lua.tar.gz
    Write-Host "✓ Lua library downloaded" -ForegroundColor Green
} else {
    Write-Host "✓ Lua library already exists" -ForegroundColor Green
}

# Check for Detours
Write-Host "`nChecking for Microsoft Detours..." -ForegroundColor Yellow
$detoursPath = "C:\Program Files (x86)\Microsoft Research\Detours"

if (Test-Path $detoursPath) {
    Write-Host "✓ Detours found at $detoursPath" -ForegroundColor Green
} else {
    Write-Host "✗ Detours not found - download from:" -ForegroundColor Red
    Write-Host "  https://www.microsoft.com/en-us/research/project/detours/" -ForegroundColor Yellow
}

# Create lib directory for dependencies
if (-not (Test-Path "lib")) {
    New-Item -ItemType Directory -Path "lib" | Out-Null
}

Write-Host "`nSetup complete. Build instructions:" -ForegroundColor Green
Write-Host "1. Install Microsoft Detours if missing" -ForegroundColor Yellow
Write-Host "2. Run Build.bat to compile" -ForegroundColor Yellow
Write-Host "3. Check bin directory for output files" -ForegroundColor Yellow

Read-Host "`nPress Enter to continue"
