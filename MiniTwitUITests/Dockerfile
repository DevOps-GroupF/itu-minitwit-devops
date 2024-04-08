# Learn about building .NET container images:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM mcr.microsoft.com/playwright/dotnet:v1.42.0-jammy AS build
WORKDIR /source

# COPY TESTS
COPY . /MiniTwitUITests/

# Set command to run tests when container is run
CMD ["dotnet", "test", "/MiniTwitUITests/"]