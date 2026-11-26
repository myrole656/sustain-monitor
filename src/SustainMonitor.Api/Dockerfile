# Build from the repo root:  docker build -f src/SustainMonitor.Api/Dockerfile -t sustainmonitor-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/SustainMonitor.Domain/SustainMonitor.Domain.csproj src/SustainMonitor.Domain/
COPY src/SustainMonitor.Infrastructure/SustainMonitor.Infrastructure.csproj src/SustainMonitor.Infrastructure/
COPY src/SustainMonitor.Api/SustainMonitor.Api.csproj src/SustainMonitor.Api/
RUN dotnet restore src/SustainMonitor.Api/SustainMonitor.Api.csproj
COPY src/ src/
RUN dotnet publish src/SustainMonitor.Api/SustainMonitor.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# /data holds the SQLite database, evidence files and token keys (mount a volume here).
RUN mkdir -p /data/evidence /data/keys && chown -R $APP_UID:$APP_UID /data
COPY --from=build /app .
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    UseHttpsRedirection=false \
    ConnectionStrings__Default="Data Source=/data/sustainmonitor.db" \
    Storage__EvidenceFolder=/data/evidence \
    DataProtection__KeysFolder=/data/keys
EXPOSE 8080
VOLUME ["/data"]
ENTRYPOINT ["dotnet", "SustainMonitor.Api.dll"]
