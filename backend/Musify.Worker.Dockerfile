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
# AppSettings*.json (PascalCase) -> lowercase copies for case-sensitive Linux.
RUN cp /app/AppSettings.json /app/appsettings.json \
    && cp /app/AppSettings.Development.json /app/appsettings.Development.json

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# ffmpeg: the Worker transcodes audio to .m4a. yt-dlp: downloads YouTube audio.
# yt-dlp is installed via pip in a venv rather than the standalone PyInstaller
# binary: the binary adds a ~1s unpack cost per invocation and extracts slower,
# so pip roughly halves resolution latency. deno solves nsig challenges.
RUN apt-get update \
    && apt-get install -y --no-install-recommends ffmpeg python3 python3-venv ca-certificates curl unzip \
    && python3 -m venv /opt/ytdlp \
    && /opt/ytdlp/bin/pip install --no-cache-dir -U yt-dlp \
    && ln -s /opt/ytdlp/bin/yt-dlp /usr/local/bin/yt-dlp \
    && curl -fsSL https://deno.land/install.sh | sh -s -- -y \
    && mv /root/.deno/bin/deno /usr/local/bin/deno \
    && apt-get purge -y curl unzip \
    && rm -rf /var/lib/apt/lists/* /root/.deno
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "Musify.Worker.dll"]
