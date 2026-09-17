FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SkyHelp.csproj ./
RUN dotnet restore SkyHelp.csproj

COPY . .
RUN dotnet publish SkyHelp.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

USER $APP_UID

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

EXPOSE 8080

# Render inyecta PORT; si no existe, se usa 8080 (local/docker compose).
ENTRYPOINT ["sh", "-c", "dotnet SkyHelp.dll --urls http://0.0.0.0:${PORT:-8080}"]
