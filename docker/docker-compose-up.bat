pushd %~dp0

docker compose -f docker-compose-database.yml -f docker-compose-storage.yml -f docker-compose-fakes.yml -f docker-compose-messaging.yml -f docker-compose-eventhub.yml -f docker-compose-prometheus.yml -f docker-compose-grafana.yml -f docker-compose-jaeger.yml down

:: Try to delete the existing images so that the spun up container has the new changes.
:: Note: docker-notification-service is excluded because it must be pre-built locally
:: (the Dockerfile fails behind corporate proxies). Build it with:
::   dotnet publish fakes/Dependencies.Fakes.NotificationService -c Release -o publish/notification-service
::   docker build -t docker-notification-service -f - . < (echo FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview & echo WORKDIR /app & echo COPY publish/notification-service/ . & echo ENTRYPOINT ["dotnet","Dependencies.Fakes.NotificationService.dll"])
docker rmi "docker-cow-service"
docker rmi "docker-goat-service"
docker rmi "docker-supplier-service"
docker rmi "docker-kitchen-service"

docker compose -f docker-compose-database.yml -f docker-compose-storage.yml -f docker-compose-fakes.yml -f docker-compose-messaging.yml -f docker-compose-eventhub.yml -f docker-compose-prometheus.yml -f docker-compose-grafana.yml -f docker-compose-jaeger.yml up -d

popd
