# Build from the repo root:  docker build -f src/SustainMonitor.Web/Dockerfile -t sustainmonitor-web .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/SustainMonitor.Domain/SustainMonitor.Domain.csproj src/SustainMonitor.Domain/
COPY src/SustainMonitor.Web/SustainMonitor.Web.csproj src/SustainMonitor.Web/
RUN dotnet restore src/SustainMonitor.Web/SustainMonitor.Web.csproj
COPY src/SustainMonitor.Domain/ src/SustainMonitor.Domain/
COPY src/SustainMonitor.Web/ src/SustainMonitor.Web/
RUN dotnet publish src/SustainMonitor.Web/SustainMonitor.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN mkdir -p /keys && chown -R $APP_UID:$APP_UID /keys
COPY --from=build /app .
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    UseHttpsRedirection=false \
    Api__BaseUrl=http://api:8080/ \
    DataProtection__KeysFolder=/keys
EXPOSE 8080
VOLUME ["/keys"]
ENTRYPOINT ["dotnet", "SustainMonitor.Web.dll"]
