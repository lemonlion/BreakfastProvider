#!/usr/bin/env bash
# Sets all RunWithAnInMemory* flags to the given value (true or false).
# Usage: ./set-inmemory-flags.sh <true|false>

set -euo pipefail

VALUE="${1:?Usage: set-inmemory-flags.sh <true|false>}"

if [[ "$VALUE" != "true" && "$VALUE" != "false" ]]; then
  echo "Error: argument must be 'true' or 'false', got '$VALUE'"
  exit 1
fi

echo "RunWithAnInMemoryCowService=$VALUE" >> "$GITHUB_ENV"
echo "RunWithAnInMemoryGoatService=$VALUE" >> "$GITHUB_ENV"
echo "RunWithAnInMemorySupplierService=$VALUE" >> "$GITHUB_ENV"
echo "RunWithAnInMemoryKitchenService=$VALUE" >> "$GITHUB_ENV"
echo "RunWithAnInMemoryDatabase=$VALUE" >> "$GITHUB_ENV"
echo "RunWithAnInMemoryEventGrid=$VALUE" >> "$GITHUB_ENV"
echo "RunWithAnInMemoryKafkaBroker=$VALUE" >> "$GITHUB_ENV"
echo "RunWithAnInMemoryReportingDatabase=$VALUE" >> "$GITHUB_ENV"
echo "RunWithAnInMemoryBreakfastDatabase=$VALUE" >> "$GITHUB_ENV"
echo "RunWithAnInMemorySpannerDatabase=$VALUE" >> "$GITHUB_ENV"
echo "RunWithAnInMemoryNotificationService=$VALUE" >> "$GITHUB_ENV"
echo "RunWithAnInMemoryEventHub=$VALUE" >> "$GITHUB_ENV"
echo "RunWithAnInMemoryPubSub=$VALUE" >> "$GITHUB_ENV"
