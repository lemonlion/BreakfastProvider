@echo off
REM Switches appsettings.componenttests.json to External SUT (Docker) mode.
REM The BreakfastProvider API runs inside a Docker container alongside all its
REM dependencies. Tests target the external API via HTTP. The Docker Compose
REM lifecycle (including the SUT container) is managed automatically.

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
  "$f = $f -replace '\"RunWithAnInMemoryPubSub\": true', '\"RunWithAnInMemoryPubSub\": false';" ^
  "$f = $f -replace '\"RunAgainstExternalServiceUnderTest\": false', '\"RunAgainstExternalServiceUnderTest\": true';" ^
  "$f = $f -replace '\"EnableDockerInSetupAndTearDown\": false',   '\"EnableDockerInSetupAndTearDown\": true';" ^
  "$f = $f -replace '\"SkipDockerTearDown\": false',               '\"SkipDockerTearDown\": true';" ^
  "[IO.File]::WriteAllText('%FILE%', $f)"

set "XUNIT=%~dp0..\xunit.runner.json"
powershell -NoProfile -Command ^
  "$f = Get-Content '%XUNIT%' -Raw;" ^
  "$f = $f -replace '\"maxParallelThreads\": \d+', '\"maxParallelThreads\": 6';" ^
  "[IO.File]::WriteAllText('%XUNIT%', $f)"

echo Switched to External SUT (Docker) mode.
echo   - All RunWithAnInMemory* = false
echo   - EnableDockerInSetupAndTearDown = true
echo   - SkipDockerTearDown = true
echo   - RunAgainstExternalServiceUnderTest = true
