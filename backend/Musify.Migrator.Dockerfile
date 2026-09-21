# Build context: backend/  (docker compose -f deploy/compose.yml up migrate)
#
# Applies EF Core migrations, then exits. Musify.Infrastructure is both the
# migrations and the design-time startup project: it owns the
# IDesignTimeDbContextFactory and the EFCore.Design package. The connection
# string comes from the Database__ConnectionString env var (compose sets it),
# read via DatabaseDesignTimeFactory's AddEnvironmentVariables().
FROM mcr.microsoft.com/dotnet/sdk:10.0
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props nuget.config ./
COPY Core/Musify.Domain/Domain.csproj                 Core/Musify.Domain/
COPY Core/Musify.Application/Application.csproj        Core/Musify.Application/
COPY Core/Musify.Infrastructure/Infrastructure.csproj Core/Musify.Infrastructure/
RUN dotnet restore Core/Musify.Infrastructure/Infrastructure.csproj

COPY Core/Musify.Domain         Core/Musify.Domain/
COPY Core/Musify.Application    Core/Musify.Application/
COPY Core/Musify.Infrastructure Core/Musify.Infrastructure/

RUN dotnet tool install --global dotnet-ef --version 10.0.8
ENV PATH="$PATH:/root/.dotnet/tools"

WORKDIR /src/Core/Musify.Infrastructure
ENTRYPOINT ["dotnet", "ef", "database", "update", "--context", "Database"]
