FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY OrderService/OrderService.csproj OrderService/
RUN dotnet restore OrderService/OrderService.csproj
COPY Shared/ Shared/
COPY OrderService/ OrderService/
RUN dotnet publish OrderService/OrderService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "OrderService.dll"]
