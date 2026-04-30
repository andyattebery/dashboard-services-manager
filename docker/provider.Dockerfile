ARG DOTNET_VERSION=10.0
ARG VERSION=0.0.0

FROM mcr.microsoft.com/dotnet/sdk:$DOTNET_VERSION AS build
ARG VERSION

ENV PROVIDER_PROJECT_NAME=Dsm.Provider.App

WORKDIR /source

COPY src/ .

RUN dotnet restore $PROVIDER_PROJECT_NAME/$PROVIDER_PROJECT_NAME.csproj

# publish app and libraries — VERSION is computed by CI (GitVersion) and passed via --build-arg.
# DisableGitVersionTask=true skips GitVersion.MsBuild's git read since .git isn't in this build context.
RUN dotnet publish $PROVIDER_PROJECT_NAME/$PROVIDER_PROJECT_NAME.csproj \
    --configuration Release \
    --output /app \
    --no-restore \
    /p:Version=$VERSION \
    /p:DisableGitVersionTask=true

# final stage/image
FROM mcr.microsoft.com/dotnet/runtime:$DOTNET_VERSION

RUN apt-get update \
 && apt-get install -y --no-install-recommends gosu \
 && rm -rf /var/lib/apt/lists/* \
 && groupadd -o -g 1000 dsm \
 && useradd -o -u 1000 -g dsm -d /home/dsm -m -s /bin/sh dsm

WORKDIR /app
COPY --from=build /app .
COPY docker/docker-entrypoint.sh /usr/local/bin/docker-entrypoint.sh
RUN chmod +x /usr/local/bin/docker-entrypoint.sh

VOLUME [ "/config" ]

ENTRYPOINT ["/usr/local/bin/docker-entrypoint.sh", "dotnet", "Dsm.Provider.App.dll"]
