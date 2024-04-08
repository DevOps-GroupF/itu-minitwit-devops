# Learn about building .NET container images:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# BUILD APP
# copy csproj and restore as distinct layers
COPY MiniTwit/*.csproj /MiniTwit/
RUN dotnet restore /MiniTwit/

# copy and publish app and libraries
COPY MiniTwit/. /MiniTwit/
RUN dotnet publish --no-restore -o /app /MiniTwit/

# COPY AND RUN TESTS
COPY MiniTwitTests/. /MiniTwitTests/
RUN dotnet test /MiniTwitTests/

# GENERATE IMAGE
FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 8080
WORKDIR /app

USER $APP_UID

COPY --from=build /app .

ENTRYPOINT ["./MiniTwit"]

