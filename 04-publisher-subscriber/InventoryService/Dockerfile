FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY InventoryService/InventoryService.csproj InventoryService/
RUN dotnet restore InventoryService/InventoryService.csproj
COPY Shared/ Shared/
COPY InventoryService/ InventoryService/
RUN dotnet publish InventoryService/InventoryService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "InventoryService.dll"]
