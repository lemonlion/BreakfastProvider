#!/bin/bash
cd "$(dirname "$0")"

docker compose -f docker-compose-database.yml -f docker-compose-storage.yml -f docker-compose-fakes.yml -f docker-compose-messaging.yml -f docker-compose-eventhub.yml -f docker-compose-prometheus.yml -f docker-compose-grafana.yml -f docker-compose-jaeger.yml -f docker-compose-sut.yml down

# Try to delete the existing images so that the spun up container has the new changes.
# Note: docker-notification-service is excluded — see docker-compose-up.bat for build instructions.
docker rmi "docker-cow-service" 2>/dev/null
docker rmi "docker-goat-service" 2>/dev/null
docker rmi "docker-supplier-service" 2>/dev/null
docker rmi "docker-kitchen-service" 2>/dev/null
docker rmi "docker-breakfast-provider-api" 2>/dev/null

docker compose -f docker-compose-database.yml -f docker-compose-storage.yml -f docker-compose-fakes.yml -f docker-compose-messaging.yml -f docker-compose-eventhub.yml -f docker-compose-prometheus.yml -f docker-compose-grafana.yml -f docker-compose-jaeger.yml -f docker-compose-sut.yml up -d
