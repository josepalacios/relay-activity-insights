FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY seed.sql ./seed.sql
COPY backend/Relay.Api/Relay.Api.csproj backend/Relay.Api/
RUN dotnet restore backend/Relay.Api/Relay.Api.csproj
COPY backend/Relay.Api/ backend/Relay.Api/
RUN dotnet publish backend/Relay.Api/Relay.Api.csproj -c Release -o /app/publish --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Relay.Api.dll"]
