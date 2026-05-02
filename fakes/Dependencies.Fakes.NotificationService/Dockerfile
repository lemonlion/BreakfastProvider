FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY fakes/Dependencies.Fakes.NotificationService/Dependencies.Fakes.NotificationService.csproj fakes/Dependencies.Fakes.NotificationService/
RUN dotnet restore fakes/Dependencies.Fakes.NotificationService/Dependencies.Fakes.NotificationService.csproj
COPY fakes/Dependencies.Fakes.NotificationService/ fakes/Dependencies.Fakes.NotificationService/
RUN dotnet build fakes/Dependencies.Fakes.NotificationService/Dependencies.Fakes.NotificationService.csproj -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish fakes/Dependencies.Fakes.NotificationService/Dependencies.Fakes.NotificationService.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Dependencies.Fakes.NotificationService.dll"]
