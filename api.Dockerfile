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

WORKDIR /app
COPY --from=build /app .

VOLUME [ "/config" ]

ENTRYPOINT ["dotnet", "Dsm.Manager.Api.dll"]