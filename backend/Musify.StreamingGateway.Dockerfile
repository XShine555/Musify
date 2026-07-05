# Build context: backend/
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props nuget.config ./
COPY Musify.StreamingGateway/Musify.StreamingGateway.csproj Musify.StreamingGateway/
RUN dotnet restore Musify.StreamingGateway/Musify.StreamingGateway.csproj

COPY . .
RUN dotnet publish Musify.StreamingGateway/Musify.StreamingGateway.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://+:8081
EXPOSE 8081
ENTRYPOINT ["dotnet", "Musify.StreamingGateway.dll"]
