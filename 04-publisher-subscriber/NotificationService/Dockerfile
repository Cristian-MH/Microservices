FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY NotificationService/NotificationService.csproj NotificationService/
RUN dotnet restore NotificationService/NotificationService.csproj
COPY Shared/ Shared/
COPY NotificationService/ NotificationService/
RUN dotnet publish NotificationService/NotificationService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "NotificationService.dll"]
