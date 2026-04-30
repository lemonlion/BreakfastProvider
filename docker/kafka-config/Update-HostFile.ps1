$ErrorActionPreference = "Stop"

# Function to check if PowerShell is running as Administrator
function Test-IsAdmin {
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

Write-Host "PowerShell is running as Administrator."

# Path to the hosts file
$hostsFilePath = "$env:SystemRoot\System32\drivers\etc\hosts"

# Read the current content of the hosts file
$hostsContent = Get-Content -Path $hostsFilePath

# Define the new entry
$newEntry = "127.0.0.1 kafka.local"

# Define the string to search for
$searchString = "kafka.local"

# Check if the string appears in the hosts file
$entryExists = $hostsContent | Select-String -Pattern $searchString

# Add the new entry if it doesn't already exist
if ($entryExists) {
    Write-Host "Entry already exists in hosts file."
} else {
    $hostsContent += $newEntry
    Set-Content -Path $hostsFilePath -Value $hostsContent -Force
    Write-Host "Entry added to hosts file."
}