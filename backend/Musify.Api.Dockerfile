# Build context: backend/  (docker compose -f deploy/compose.yml --profile apps build)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore: copy central config + csproj files first to cache the layer.
COPY Directory.Build.props Directory.Packages.props nuget.config ./
COPY Core/Musify.Domain/Domain.csproj                 Core/Musify.Domain/
COPY Core/Musify.Application/Application.csproj        Core/Musify.Application/
COPY Core/Musify.Infrastructure/Infrastructure.csproj Core/Musify.Infrastructure/
COPY Hosts/Musify.Api/Api.csproj                        Hosts/Musify.Api/
RUN dotnet restore Hosts/Musify.Api/Api.csproj

COPY . .
RUN dotnet publish Hosts/Musify.Api/Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://+:5111
EXPOSE 5111
ENTRYPOINT ["dotnet", "Musify.Api.dll"]
