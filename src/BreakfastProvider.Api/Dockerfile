FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY docker/ /tmp/certs/
RUN if [ -f /tmp/certs/Root_CA.crt ]; then \
      cp /tmp/certs/Root_CA.crt /usr/local/share/ca-certificates/ && \
      update-ca-certificates; \
    fi
COPY src/BreakfastProvider.Api/BreakfastProvider.Api.csproj src/BreakfastProvider.Api/
RUN dotnet restore src/BreakfastProvider.Api/BreakfastProvider.Api.csproj
COPY src/BreakfastProvider.Api/ src/BreakfastProvider.Api/
RUN dotnet build src/BreakfastProvider.Api/BreakfastProvider.Api.csproj -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish src/BreakfastProvider.Api/BreakfastProvider.Api.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl libssl3 && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BreakfastProvider.Api.dll"]
