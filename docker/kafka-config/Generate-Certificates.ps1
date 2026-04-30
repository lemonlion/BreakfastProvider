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

$VALIDITY_IN_DAYS = 365
$WORKING_DIRECTORY = "certificates"
$SECRET = Get-Content "..\.env" | Select-String -Pattern "KAFKA_CERTIFICATES_PASSWORD" | ForEach-Object { $_ -replace "KAFKA_CERTIFICATES_PASSWORD=", "" }

if(Test-Path $WORKING_DIRECTORY){
    Write-Host "Path $WORKING_DIRECTORY exists"
} else{
    Write-Host "Creating path $WORKING_DIRECTORY"
    New-Item -ItemType Directory -Path $WORKING_DIRECTORY
}

Write-Host "Create the CA key"
& openssl genpkey -algorithm RSA -out "$WORKING_DIRECTORY\ca.key"

Write-Host "Create the CA certificate"
& openssl req -new -x509 -key "$WORKING_DIRECTORY\ca.key" -out "$WORKING_DIRECTORY\ca.crt" -days $VALIDITY_IN_DAYS -subj "/CN=ca.local"

Write-Host "Create a keystore and generate a key pair for Kafka"
& keytool -genkeypair -alias localhost -keyalg RSA -dname "CN=kafka.local" -keystore "$WORKING_DIRECTORY\kafka.keystore.jks" -storepass $SECRET -keypass $SECRET

Write-Host "Generate a Certificate Signing Request (CSR) from the keystore"
& keytool -certreq -alias localhost -file "$WORKING_DIRECTORY\kafka.csr" -keystore "$WORKING_DIRECTORY\kafka.keystore.jks" -storepass $SECRET

Write-Host "Sign the CSR with the CA certificate"
& openssl x509 -req -in "$WORKING_DIRECTORY\kafka.csr" -CA "$WORKING_DIRECTORY\ca.crt" -CAkey "$WORKING_DIRECTORY\ca.key" -out "$WORKING_DIRECTORY\kafka.crt" -days $VALIDITY_IN_DAYS -CAcreateserial

Write-Host "Delete Certificate Signing Request"
Remove-Item "$WORKING_DIRECTORY\kafka.csr"

Write-Host "Delete the CA key"
Remove-Item "$WORKING_DIRECTORY\ca.key"