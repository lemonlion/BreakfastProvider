FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY fakes/Dependencies.Fakes.SupplierService/Dependencies.Fakes.SupplierService.csproj fakes/Dependencies.Fakes.SupplierService/
RUN dotnet restore fakes/Dependencies.Fakes.SupplierService/Dependencies.Fakes.SupplierService.csproj
COPY fakes/Dependencies.Fakes.SupplierService/ fakes/Dependencies.Fakes.SupplierService/
RUN dotnet build fakes/Dependencies.Fakes.SupplierService/Dependencies.Fakes.SupplierService.csproj -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish fakes/Dependencies.Fakes.SupplierService/Dependencies.Fakes.SupplierService.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Dependencies.Fakes.SupplierService.dll"]
