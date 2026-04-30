#!/bin/sh
set -e

# Install any certificates mounted into /certs/ (e.g. Kafka CA, EventGrid self-signed)
if [ -d /certs ] && ls /certs/*.crt 1>/dev/null 2>&1; then
  cp /certs/*.crt /usr/local/share/ca-certificates/
  update-ca-certificates
fi

exec dotnet BreakfastProvider.Api.dll "$@"
