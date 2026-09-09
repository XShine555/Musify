# Build context: backend/  (docker compose -f deploy/compose.yml --profile apps build)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore: copy central config + csproj files first to cache the layer.
COPY Directory.Build.props Directory.Packages.props nuget.config ./
COPY Core/Musify.Domain/Musify.Domain.csproj                 Core/Musify.Domain/
COPY Core/Musify.Application/Musify.Application.csproj        Core/Musify.Application/
COPY Core/Musify.Infrastructure/Musify.Infrastructure.csproj Core/Musify.Infrastructure/
COPY Hosts/Musify.Api/Musify.Api.csproj                        Hosts/Musify.Api/
RUN dotnet restore Hosts/Musify.Api/Musify.Api.csproj

COPY . .
RUN dotnet publish Hosts/Musify.Api/Musify.Api.csproj -c Release -o /app --no-restore
# Config files are AppSettings*.json (PascalCase); on case-sensitive Linux the host
# looks for appsettings*.json. Add lowercase copies so they get loaded.
RUN cp /app/AppSettings.json /app/appsettings.json \
    && cp /app/AppSettings.Development.json /app/appsettings.Development.json

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://+:5111
EXPOSE 5111
ENTRYPOINT ["dotnet", "Musify.Api.dll"]
