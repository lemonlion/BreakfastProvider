pushd %~dp0

docker compose -f docker-compose-database.yml -f docker-compose-storage.yml -f docker-compose-fakes.yml -f docker-compose-messaging.yml -f docker-compose-eventhub.yml -f docker-compose-prometheus.yml -f docker-compose-grafana.yml -f docker-compose-jaeger.yml -f docker-compose-sut.yml down

:: Try to delete the existing images so that the spun up container has the new changes.
docker rmi "docker-cow-service"
docker rmi "docker-goat-service"
docker rmi "docker-supplier-service"
docker rmi "docker-kitchen-service"
docker rmi "docker-notification-service"
docker rmi "docker-breakfast-provider-api"

docker compose -f docker-compose-database.yml -f docker-compose-storage.yml -f docker-compose-fakes.yml -f docker-compose-messaging.yml -f docker-compose-eventhub.yml -f docker-compose-prometheus.yml -f docker-compose-grafana.yml -f docker-compose-jaeger.yml -f docker-compose-sut.yml up --build

popd
