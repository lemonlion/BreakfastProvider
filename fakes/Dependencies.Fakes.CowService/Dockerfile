FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY fakes/Dependencies.Fakes.CowService/Dependencies.Fakes.CowService.csproj fakes/Dependencies.Fakes.CowService/
RUN dotnet restore fakes/Dependencies.Fakes.CowService/Dependencies.Fakes.CowService.csproj
COPY fakes/Dependencies.Fakes.CowService/ fakes/Dependencies.Fakes.CowService/
RUN dotnet build fakes/Dependencies.Fakes.CowService/Dependencies.Fakes.CowService.csproj -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish fakes/Dependencies.Fakes.CowService/Dependencies.Fakes.CowService.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Dependencies.Fakes.CowService.dll"]
