ARG DOTNET_VERSION=7.0

FROM mcr.microsoft.com/dotnet/sdk:$DOTNET_VERSION AS build

ENV PROVIDER_PROJECT_NAME=Dsm.Provider.App

WORKDIR /source

COPY . .

RUN dotnet restore $PROVIDER_PROJECT_NAME/$PROVIDER_PROJECT_NAME.csproj

# publish app and libraries
RUN dotnet publish $PROVIDER_PROJECT_NAME/$PROVIDER_PROJECT_NAME.csproj --configuration Release --output /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/runtime:$DOTNET_VERSION

WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Dsm.Provider.App.dll"]