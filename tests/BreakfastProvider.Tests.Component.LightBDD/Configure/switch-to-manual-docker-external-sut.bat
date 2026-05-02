@echo off
REM Switches appsettings.componenttests.json to Manual External SUT mode.
REM Use this when you have already started Docker containers manually via
REM docker-compose-external-sut-up.bat and want to run tests against them
REM WITHOUT the test framework managing the Docker lifecycle.
REM Containers stay alive after tests ? ideal for inspecting Grafana, Prometheus, etc.

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
  "$f = $f -replace '\"RunAgainstExternalServiceUnderTest\": false', '\"RunAgainstExternalServiceUnderTest\": true';" ^
  "$f = $f -replace '\"EnableDockerInSetupAndTearDown\": true',    '\"EnableDockerInSetupAndTearDown\": false';" ^
  "$f = $f -replace '\"SkipDockerTearDown\": true',               '\"SkipDockerTearDown\": false';" ^
  "[IO.File]::WriteAllText('%FILE%', $f)"

set "XUNIT=%~dp0..\xunit.runner.json"
powershell -NoProfile -Command ^
  "$f = Get-Content '%XUNIT%' -Raw;" ^
  "$f = $f -replace '\"maxParallelThreads\": \d+', '\"maxParallelThreads\": 6';" ^
  "[IO.File]::WriteAllText('%XUNIT%', $f)"

echo Switched to Manual External SUT mode.
echo   - All RunWithAnInMemory* = false
echo   - EnableDockerInSetupAndTearDown = false
echo   - SkipDockerTearDown = false
echo   - RunAgainstExternalServiceUnderTest = true
echo.
echo Start Docker manually first:  docker\docker-compose-external-sut-up.bat
