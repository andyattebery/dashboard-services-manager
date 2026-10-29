ARG VERSION=0.0.0

# The SDK must be exactly src/global.json's version (rollForward: disable). Dependabot bumps
# both in one PR, and CI's test job fails when they differ.
FROM mcr.microsoft.com/dotnet/sdk:10.0.403 AS build
ARG VERSION

ENV PROVIDER_PROJECT_NAME=Dsm.Manager.Api

WORKDIR /source

COPY src/ .

RUN dotnet restore $PROVIDER_PROJECT_NAME/$PROVIDER_PROJECT_NAME.csproj --locked-mode

# publish app and libraries — VERSION is computed by CI (GitVersion) and passed via --build-arg.
# DisableGitVersionTask=true skips GitVersion.MsBuild's git read since .git isn't in this build context.
RUN dotnet publish $PROVIDER_PROJECT_NAME/$PROVIDER_PROJECT_NAME.csproj \
    --configuration Release \
    --output /app \
    --no-restore \
    /p:Version=$VERSION \
    /p:DisableGitVersionTask=true

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS app

RUN apt-get update \
 && apt-get install -y --no-install-recommends gosu curl \
 && rm -rf /var/lib/apt/lists/* \
 && groupadd -o -g 1000 dsm \
 && useradd -o -u 1000 -g dsm -d /home/dsm -m -s /bin/sh dsm

WORKDIR /app
COPY --from=build /app .
COPY docker/docker-entrypoint.sh /usr/local/bin/docker-entrypoint.sh
RUN chmod +x /usr/local/bin/docker-entrypoint.sh \
 && mkdir -p /data

VOLUME [ "/config" ]
# /data/state.json holds every provider's latest services, including ones the dashboards never
# show (ignored, or without a URL). Without a mount it lives in an anonymous volume.
VOLUME [ "/data" ]

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["/usr/local/bin/docker-entrypoint.sh", "dotnet", "Dsm.Manager.Api.dll"]
