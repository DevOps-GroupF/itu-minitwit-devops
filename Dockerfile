# Learn about building .NET container images:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY MiniTwit/*.csproj .
RUN dotnet restore

# copy and publish app and libraries
COPY MiniTwit/. .
RUN dotnet publish --no-restore -o /app

COPY MiniTwit/minitwit.db /datavol/minitwit.db

# copy csproj and restore as distinct layers
COPY MiniTwitTests/*.csproj /tests/
RUN dotnet restore /tests

# copy and publish app and libraries
COPY MiniTwitTests/. /tests/
RUN dotnet publish /tests --no-restore -o /app/tests

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 8080
WORKDIR /app

USER $APP_UID

COPY --from=build /app .
COPY --chown=$APP_UID --from=build /datavol /datavol

ENTRYPOINT ["./MiniTwit"]

