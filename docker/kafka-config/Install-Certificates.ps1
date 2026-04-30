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

$WORKING_DIRECTORY = "certificates"
$SECRET = Get-Content "..\.env" | Select-String -Pattern "KAFKA_CERTIFICATES_PASSWORD" | ForEach-Object { $_ -replace "KAFKA_CERTIFICATES_PASSWORD=", "" }

if(Test-Path $WORKING_DIRECTORY){
    Write-Host "Path $WORKING_DIRECTORY exists"
} else{
    Write-Host "Creating path $WORKING_DIRECTORY"
    New-Item -ItemType Directory -Path $WORKING_DIRECTORY
}

Write-Host "Import the CA certificate into the keystore"
& keytool -import -file "$WORKING_DIRECTORY\ca.crt" -keystore "$WORKING_DIRECTORY\kafka.keystore.jks" -alias CARoot -storepass $SECRET -noprompt

Write-Host "Import the signed Kafka certificate into the keystore"
& keytool -import -file "$WORKING_DIRECTORY\kafka.crt" -keystore "$WORKING_DIRECTORY\kafka.keystore.jks" -alias localhost -storepass $SECRET -noprompt

Write-Host "Create a truststore and import the CA certificate"
& keytool -import -file "$WORKING_DIRECTORY\ca.crt" -keystore "$WORKING_DIRECTORY\kafka.truststore.jks" -alias CARoot -storepass $SECRET -noprompt

Write-Host "Insert root CA to local Trusted Root Certificates"
Import-Certificate -FilePath ".\$WORKING_DIRECTORY\ca.crt" -CertStoreLocation "Cert:\LocalMachine\Root"