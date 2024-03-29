# Learn about building .NET container images:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# COPY TESTS
COPY . /MiniTwitOldTests/

# Set command to run tests when container is run
CMD ["sleep", "20"]
CMD ["dotnet", "test", "/MiniTwitUITests/"]