pushd %~dp0

docker compose -f docker-compose-database.yml -f docker-compose-storage.yml -f docker-compose-fakes.yml -f docker-compose-messaging.yml -f docker-compose-prometheus.yml -f docker-compose-grafana.yml -f docker-compose-jaeger.yml down

:: Try to delete the existing images so that the spun up container has the new changes.
docker rmi "docker-cow-service"
docker rmi "docker-goat-service"
docker rmi "docker-supplier-service"
docker rmi "docker-kitchen-service"

docker compose -f docker-compose-database.yml -f docker-compose-storage.yml -f docker-compose-fakes.yml -f docker-compose-messaging.yml -f docker-compose-prometheus.yml -f docker-compose-grafana.yml -f docker-compose-jaeger.yml up --build

popd
