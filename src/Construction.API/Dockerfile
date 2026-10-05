FROM mcr.microsoft.com/dotnet/sdk:9.0@sha256:01fabc4758d1d74e39eda700c8463dae6241a61481f973683692ddcb59a5eeb7 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src/Construction.Domain/Construction.Domain.csproj src/Construction.Domain/
COPY src/Construction.Application/Construction.Application.csproj src/Construction.Application/
COPY src/Construction.Infrastructure/Construction.Infrastructure.csproj src/Construction.Infrastructure/
COPY src/Construction.API/Construction.API.csproj src/Construction.API/
RUN dotnet restore src/Construction.API/Construction.API.csproj

COPY src/ src/
RUN dotnet publish src/Construction.API/Construction.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0@sha256:2680706c9656fafe34876446d694fd25077fd0b5da478f38d56073ac6c21d37e AS runtime
WORKDIR /app

RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

COPY --from=build /app/publish .

RUN mkdir -p /app/logs && chown -R appuser:appgroup /app

# Pre-create the local-disk attachment store, owned by appuser, so a fresh
# named volume mounted here (docker-compose.yml's attachment-data) inherits
# this ownership when Docker seeds it from the image on first mount — rather
# than the root:root the volume would otherwise start with, which the app
# (running as appuser below) cannot write into.
RUN mkdir -p /var/lib/construction/storage && chown -R appuser:appgroup /var/lib/construction/storage

USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Construction.API.dll"]
