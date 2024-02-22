# Learn about building .NET container images:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
WORKDIR /source

# copy csproj and restore as distinct layers
# COPY MiniTwit/*.csproj .
COPY ./itu-minitwit-dotnet.sln .
COPY ./MiniTwit/*.csproj ./MiniTwit/
COPY ./MiniTwitAPI/*.csproj ./MiniTwitAPI/
COPY ./MiniTwitInfra/*.csproj ./MiniTwitInfra/
COPY ./MiniTwitTests/*.csproj ./MiniTwitTests/
COPY ./MinitwitAzure/*.csproj ./MinitwitAzure/

RUN dotnet restore -a $TARGETARCH

# copy and publish app and libraries
COPY . .
RUN dotnet publish -a $TARGETARCH --no-restore -o /app

COPY MiniTwit/minitwit.db /datavol/minitwit.db

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 8080
WORKDIR /app

USER $APP_UID

COPY --from=build /app .
COPY --chown=$APP_UID --from=build /datavol /datavol

ENTRYPOINT ["./MiniTwitAPI"]

