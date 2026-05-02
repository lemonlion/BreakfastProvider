@echo off
REM Switches appsettings.componenttests.json to InMemory mode.
REM All fakes run in-process — no Docker containers required.

set "FILE=%~dp0..\appsettings.componenttests.json"

powershell -NoProfile -Command ^
  "$f = Get-Content '%FILE%' -Raw;" ^
  "$f = $f -replace '\"RunWithAnInMemoryCowService\": false',      '\"RunWithAnInMemoryCowService\": true';" ^
  "$f = $f -replace '\"RunWithAnInMemoryGoatService\": false',     '\"RunWithAnInMemoryGoatService\": true';" ^
  "$f = $f -replace '\"RunWithAnInMemorySupplierService\": false', '\"RunWithAnInMemorySupplierService\": true';" ^
  "$f = $f -replace '\"RunWithAnInMemoryKitchenService\": false',  '\"RunWithAnInMemoryKitchenService\": true';" ^
  "$f = $f -replace '\"RunWithAnInMemoryDatabase\": false',        '\"RunWithAnInMemoryDatabase\": true';" ^
  "$f = $f -replace '\"RunWithAnInMemoryEventGrid\": false',       '\"RunWithAnInMemoryEventGrid\": true';" ^
  "$f = $f -replace '\"RunWithAnInMemoryKafkaBroker\": false',     '\"RunWithAnInMemoryKafkaBroker\": true';" ^
  "$f = $f -replace '\"RunWithAnInMemoryReportingDatabase\": false', '\"RunWithAnInMemoryReportingDatabase\": true';" ^
  "$f = $f -replace '\"RunWithAnInMemoryBreakfastDatabase\": false', '\"RunWithAnInMemoryBreakfastDatabase\": true';" ^
  "$f = $f -replace '\"RunWithAnInMemorySpannerDatabase\": false', '\"RunWithAnInMemorySpannerDatabase\": true';" ^
  "$f = $f -replace '\"RunWithAnInMemoryNotificationService\": false', '\"RunWithAnInMemoryNotificationService\": true';" ^
  "$f = $f -replace '\"RunAgainstExternalServiceUnderTest\": true', '\"RunAgainstExternalServiceUnderTest\": false';" ^
  "$f = $f -replace '\"EnableDockerInSetupAndTearDown\": true',    '\"EnableDockerInSetupAndTearDown\": false';" ^
  "$f = $f -replace '\"SkipDockerTearDown\": true',                '\"SkipDockerTearDown\": false';" ^
  "[IO.File]::WriteAllText('%FILE%', $f)"

set "XUNIT=%~dp0..\xunit.runner.json"
powershell -NoProfile -Command ^
  "$f = Get-Content '%XUNIT%' -Raw;" ^
  "$f = $f -replace '\"maxParallelThreads\": \d+', '\"maxParallelThreads\": 0';" ^
  "[IO.File]::WriteAllText('%XUNIT%', $f)"

echo Switched to InMemory mode.
echo   - All RunWithAnInMemory* = true
echo   - EnableDockerInSetupAndTearDown = false
echo   - RunAgainstExternalServiceUnderTest = false
