ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:$DOTNET_VERSION AS build

ENV PROVIDER_PROJECT_NAME=Dsm.Manager.Api

WORKDIR /source

COPY src/ .

RUN dotnet restore $PROVIDER_PROJECT_NAME/$PROVIDER_PROJECT_NAME.csproj

# publish app and libraries
RUN dotnet publish $PROVIDER_PROJECT_NAME/$PROVIDER_PROJECT_NAME.csproj --configuration Release --output /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:$DOTNET_VERSION AS app

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

EXPOSE 8080

ENTRYPOINT ["/usr/local/bin/docker-entrypoint.sh", "dotnet", "Dsm.Manager.Api.dll"]
