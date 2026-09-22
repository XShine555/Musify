# Build context: backend/
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props nuget.config ./
COPY Core/Musify.Domain/Domain.csproj                 Core/Musify.Domain/
COPY Core/Musify.Application/Application.csproj        Core/Musify.Application/
COPY Core/Musify.Infrastructure/Infrastructure.csproj Core/Musify.Infrastructure/
COPY Hosts/Musify.Worker/Worker.csproj                 Hosts/Musify.Worker/
RUN dotnet restore Hosts/Musify.Worker/Worker.csproj

COPY . .
RUN dotnet publish Hosts/Musify.Worker/Worker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# ffmpeg: the Worker transcodes uploaded audio to .m4a.
RUN apt-get update \
    && apt-get install -y --no-install-recommends ffmpeg ca-certificates \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "Musify.Worker.dll"]
