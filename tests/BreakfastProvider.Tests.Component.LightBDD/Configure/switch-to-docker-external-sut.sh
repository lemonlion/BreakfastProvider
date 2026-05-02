#!/bin/bash
# Switches appsettings.componenttests.json to External SUT (Docker) mode.
# The BreakfastProvider API runs inside a Docker container alongside all its
# dependencies. Tests target the external API via HTTP. The Docker Compose
# lifecycle (including the SUT container) is managed automatically.

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
  -e 's/"RunAgainstExternalServiceUnderTest": false/"RunAgainstExternalServiceUnderTest": true/' \
  -e 's/"EnableDockerInSetupAndTearDown": false/"EnableDockerInSetupAndTearDown": true/' \
  -e 's/"SkipDockerTearDown": false/"SkipDockerTearDown": true/' \
  "$FILE"

XUNIT="$SCRIPT_DIR/../xunit.runner.json"
sed -i \
  -e 's/"maxParallelThreads": [0-9]*/"maxParallelThreads": 6/' \
  "$XUNIT"

echo "Switched to External SUT (Docker) mode."
echo "  - All RunWithAnInMemory* = false"
echo "  - EnableDockerInSetupAndTearDown = true"
echo "  - SkipDockerTearDown = true"
echo "  - RunAgainstExternalServiceUnderTest = true"
