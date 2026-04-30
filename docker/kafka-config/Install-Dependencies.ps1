$ErrorActionPreference = "Stop"
function Test-IsGitHubRunner {
    if ($env:GITHUB_ACTIONS -eq "true") {
        Write-Host "This script is running in a GitHub Actions runner."
        return $true;
      } else {
        Write-Host "This script is not running in a GitHub Actions runner."
        return $false
      }
}

# Function to check if PowerShell is running as Administrator
function Test-IsAdmin {

    if (Test-IsGitHubRunner) {
        Write-Host "This script is running in a GitHub Actions runner. Skipping check for Administrator privileges."
        return $true
    }

    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $currentPrincipal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    $adminRole = [Security.Principal.WindowsBuiltInRole]::Administrator

    return $currentPrincipal.IsInRole($adminRole)
}

# Check if running as Administrator
if (-not (Test-IsAdmin)) {
    Write-Host "PowerShell is NOT running as Administrator. Restart with elevated privileges."
    exit
}

# Install chocolatey if not already installed
Write-Host "Checking if Chocolatey is installed..."
if (!(Get-Command choco -ErrorAction SilentlyContinue)) {
    Write-Host "Chocolatey is not installed..."
    Write-Host "Installing Chocolatey..."

    if (-not (Test-IsGitHubRunner)) {
        Set-ExecutionPolicy Bypass -Scope Process -Force; iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
    } else {
        iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
    }
    Write-Host "Successfully installed Chocolatey"
}
else {
    Write-Host "Chocolatey is already installed..."
}

# Install openssl
Write-Host "Installing openssl..."
if (!(Get-Command openssl -ErrorAction SilentlyContinue)) {
    choco install openssl -y
    Write-Host "Successfully installed openssl"
}
else {
    Write-Host "openssl is already installed..."
}

# Install keytool
Write-Host "Installing keytool (via OpenJDK)..."
if (!(Get-Command keytool -ErrorAction SilentlyContinue)) {
    choco install openjdk -y
    Write-Host "Successfully installed keytool"
}
else {
    Write-Host "keytool is already installed..."
}
