# Multi-stage build for the Entra-Flow web app.
# Build:  docker build -t entra-flow .
# Run:    docker run -p 8080:8080 -v entraflow-data:/app/data \
#           -e Admin__Password=... -e Admin__ApiKey=... entra-flow

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (better layer caching).
COPY Directory.Build.props ./
COPY src/EntraFlow.Core/EntraFlow.Core.csproj src/EntraFlow.Core/
COPY src/EntraFlow.Web/EntraFlow.Web.csproj src/EntraFlow.Web/
RUN dotnet restore src/EntraFlow.Web/EntraFlow.Web.csproj

# Build and publish.
COPY . .
RUN dotnet publish src/EntraFlow.Web/EntraFlow.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# Persisted data (settings, audit, uploads, outputs, data-protection keys).
VOLUME /app/data
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    Storage__DataDirectory=/app/data

ENTRYPOINT ["dotnet", "EntraFlow.Web.dll"]
