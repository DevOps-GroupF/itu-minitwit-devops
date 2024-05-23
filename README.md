# Group F - MiniTwit on ASP.NET Core Razor Pages
**Group members**: nicje, mlsc, dmon, gelu, piro.

## Auxiliary Repository: Deployment

We have a secondary repository: <https://github.com/DevOps-GroupF/server-files>

This repository contains the logic for deploying and updating MiniTwit to production.

We don't want clone the whole repository to each production server, as we only want the image to be present in production.
However, we want version control of the deployment logic, hence the secondary repository.


## Decision Log
Our decisions and other information are recorded in the [repository wiki](https://github.com/DevOps-GroupF/itu-minitwit-devops/wiki).

You can also read our [report](https://github.com/DevOps-GroupF/itu-minitwit-devops/blob/main/report/build/MSc_group_f.pdf), which contains even more information.

## Running the application
The easiest way to run the application is using the pre-configured docker compose, which contains both the application and all dependencies needed to run it locally:
```bash
docker-compose up
```

## Building and running the application manually
The application can also be run manually. Bear in mind that the application has certain dependencies, like a DB, which will need to be set up manually in this case.
The application can be built executing:
```bash
cd MiniTwit
dotnet build
```

Or a development server can be run by using:
```bash
cd MiniTwit
dotnet run
```

