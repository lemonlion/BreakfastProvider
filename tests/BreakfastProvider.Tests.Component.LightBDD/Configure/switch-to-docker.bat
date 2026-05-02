@echo off
REM Switches appsettings.componenttests.json to Docker mode.
REM Tests run against real Docker containers (Cosmos DB, Kafka, fakes, etc.).
REM The Docker Compose lifecycle is managed automatically by the test framework.

set "FILE=%~dp0..\appsettings.componenttests.json"

powershell -NoProfile -Command ^
  "$f = Get-Content '%FILE%' -Raw;" ^
  "$f = $f -replace '\"RunWithAnInMemoryCowService\": true',      '\"RunWithAnInMemoryCowService\": false';" ^
  "$f = $f -replace '\"RunWithAnInMemoryGoatService\": true',     '\"RunWithAnInMemoryGoatService\": false';" ^
  "$f = $f -replace '\"RunWithAnInMemorySupplierService\": true', '\"RunWithAnInMemorySupplierService\": false';" ^
  "$f = $f -replace '\"RunWithAnInMemoryKitchenService\": true',  '\"RunWithAnInMemoryKitchenService\": false';" ^
  "$f = $f -replace '\"RunWithAnInMemoryDatabase\": true',        '\"RunWithAnInMemoryDatabase\": false';" ^
  "$f = $f -replace '\"RunWithAnInMemoryEventGrid\": true',       '\"RunWithAnInMemoryEventGrid\": false';" ^
  "$f = $f -replace '\"RunWithAnInMemoryKafkaBroker\": true',     '\"RunWithAnInMemoryKafkaBroker\": false';" ^
  "$f = $f -replace '\"RunWithAnInMemoryReportingDatabase\": true', '\"RunWithAnInMemoryReportingDatabase\": false';" ^
  "$f = $f -replace '\"RunWithAnInMemoryBreakfastDatabase\": true', '\"RunWithAnInMemoryBreakfastDatabase\": false';" ^
  "$f = $f -replace '\"RunWithAnInMemorySpannerDatabase\": true', '\"RunWithAnInMemorySpannerDatabase\": false';" ^
  "$f = $f -replace '\"RunWithAnInMemoryNotificationService\": true', '\"RunWithAnInMemoryNotificationService\": false';" ^
  "$f = $f -replace '\"RunWithAnInMemoryEventHub\": true', '\"RunWithAnInMemoryEventHub\": false';" ^
  "$f = $f -replace '\"RunAgainstExternalServiceUnderTest\": true', '\"RunAgainstExternalServiceUnderTest\": false';" ^
  "$f = $f -replace '\"EnableDockerInSetupAndTearDown\": false',   '\"EnableDockerInSetupAndTearDown\": true';" ^
  "$f = $f -replace '\"SkipDockerTearDown\": false',               '\"SkipDockerTearDown\": true';" ^
  "[IO.File]::WriteAllText('%FILE%', $f)"

set "XUNIT=%~dp0..\xunit.runner.json"
powershell -NoProfile -Command ^
  "$f = Get-Content '%XUNIT%' -Raw;" ^
  "$f = $f -replace '\"maxParallelThreads\": \d+', '\"maxParallelThreads\": 6';" ^
  "[IO.File]::WriteAllText('%XUNIT%', $f)"

echo Switched to Docker mode.
echo   - All RunWithAnInMemory* = false
echo   - EnableDockerInSetupAndTearDown = true
echo   - SkipDockerTearDown = true
echo   - RunAgainstExternalServiceUnderTest = false
