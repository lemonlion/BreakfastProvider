#!/bin/bash
# Switches appsettings.componenttests.json to InMemory mode.
# All fakes run in-process — no Docker containers required.

FILE="../appsettings.componenttests.json"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
FILE="$SCRIPT_DIR/$FILE"

sed -i \
  -e 's/"RunWithAnInMemoryCowService": false/"RunWithAnInMemoryCowService": true/' \
  -e 's/"RunWithAnInMemoryGoatService": false/"RunWithAnInMemoryGoatService": true/' \
  -e 's/"RunWithAnInMemorySupplierService": false/"RunWithAnInMemorySupplierService": true/' \
  -e 's/"RunWithAnInMemoryKitchenService": false/"RunWithAnInMemoryKitchenService": true/' \
  -e 's/"RunWithAnInMemoryDatabase": false/"RunWithAnInMemoryDatabase": true/' \
  -e 's/"RunWithAnInMemoryEventGrid": false/"RunWithAnInMemoryEventGrid": true/' \
  -e 's/"RunWithAnInMemoryKafkaBroker": false/"RunWithAnInMemoryKafkaBroker": true/' \
  -e 's/"RunWithAnInMemoryReportingDatabase": false/"RunWithAnInMemoryReportingDatabase": true/' \
  -e 's/"RunAgainstExternalServiceUnderTest": true/"RunAgainstExternalServiceUnderTest": false/' \
  -e 's/"EnableDockerInSetupAndTearDown": true/"EnableDockerInSetupAndTearDown": false/' \
  -e 's/"SkipDockerTearDown": true/"SkipDockerTearDown": false/' \
  "$FILE"

XUNIT="$SCRIPT_DIR/../xunit.runner.json"
sed -i \
  -e 's/"maxParallelThreads": [0-9]*/"maxParallelThreads": 0/' \
  "$XUNIT"

echo "Switched to InMemory mode."
echo "  - All RunWithAnInMemory* = true"
echo "  - EnableDockerInSetupAndTearDown = false"
echo "  - RunAgainstExternalServiceUnderTest = false"
