#!/bin/bash
# Switches appsettings.componenttests.json to Manual External SUT mode.
# Use this when you have already started Docker containers manually via
# docker-compose-external-sut-up.bat and want to run tests against them
# WITHOUT the test framework managing the Docker lifecycle.
# Containers stay alive after tests — ideal for inspecting Grafana, Prometheus, etc.

FILE="../appsettings.componenttests.json"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
FILE="$SCRIPT_DIR/$FILE"

sed -i \
  -e 's/"RunWithAnInMemoryCowService": true/"RunWithAnInMemoryCowService": false/' \
  -e 's/"RunWithAnInMemoryGoatService": true/"RunWithAnInMemoryGoatService": false/' \
  -e 's/"RunWithAnInMemorySupplierService": true/"RunWithAnInMemorySupplierService": false/' \
  -e 's/"RunWithAnInMemoryKitchenService": true/"RunWithAnInMemoryKitchenService": false/' \
  -e 's/"RunWithAnInMemoryDatabase": true/"RunWithAnInMemoryDatabase": false/' \
  -e 's/"RunWithAnInMemoryEventGrid": true/"RunWithAnInMemoryEventGrid": false/' \
  -e 's/"RunWithAnInMemoryKafkaBroker": true/"RunWithAnInMemoryKafkaBroker": false/' \
  -e 's/"RunWithAnInMemoryReportingDatabase": true/"RunWithAnInMemoryReportingDatabase": false/' \
  -e 's/"RunWithAnInMemoryBreakfastDatabase": true/"RunWithAnInMemoryBreakfastDatabase": false/' \
  -e 's/"RunWithAnInMemorySpannerDatabase": true/"RunWithAnInMemorySpannerDatabase": false/' \
  -e 's/"RunWithAnInMemoryNotificationService": true/"RunWithAnInMemoryNotificationService": false/' \
  -e 's/"RunWithAnInMemoryEventHub": true/"RunWithAnInMemoryEventHub": false/' \
  -e 's/"RunWithAnInMemoryPubSub": true/"RunWithAnInMemoryPubSub": false/' \
  -e 's/"RunAgainstExternalServiceUnderTest": false/"RunAgainstExternalServiceUnderTest": true/' \
  -e 's/"EnableDockerInSetupAndTearDown": true/"EnableDockerInSetupAndTearDown": false/' \
  -e 's/"SkipDockerTearDown": true/"SkipDockerTearDown": false/' \
  "$FILE"

XUNIT="$SCRIPT_DIR/../xunit.runner.json"
sed -i \
  -e 's/"maxParallelThreads": [0-9]*/"maxParallelThreads": 6/' \
  "$XUNIT"

echo "Switched to Manual External SUT mode."
echo "  - All RunWithAnInMemory* = false"
echo "  - EnableDockerInSetupAndTearDown = false"
echo "  - SkipDockerTearDown = false"
echo "  - RunAgainstExternalServiceUnderTest = true"
echo ""
echo "Start Docker manually first:  docker/docker-compose-external-sut-up.bat"
echo "Then run tests:               dotnet test tests/BreakfastProvider.Tests.Component.xUnit"
echo "Containers remain running after tests for Grafana/Prometheus inspection."
