# Build context: backend/
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props nuget.config ./
COPY Musify.Domain/Musify.Domain.csproj                 Musify.Domain/
COPY Musify.Application/Musify.Application.csproj        Musify.Application/
COPY Musify.Infrastructure/Musify.Infrastructure.csproj Musify.Infrastructure/
COPY Musify.Worker/Musify.Worker.csproj                 Musify.Worker/
RUN dotnet restore Musify.Worker/Musify.Worker.csproj

COPY . .
RUN dotnet publish Musify.Worker/Musify.Worker.csproj -c Release -o /app --no-restore
# AppSettings*.json (PascalCase) -> lowercase copies for case-sensitive Linux.
RUN cp /app/AppSettings.json /app/appsettings.json \
    && cp /app/AppSettings.Development.json /app/appsettings.Development.json

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# ffmpeg: the Worker transcodes audio to .m4a.
RUN apt-get update \
    && apt-get install -y --no-install-recommends ffmpeg \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "Musify.Worker.dll"]
