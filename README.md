# Group F - MiniTwit on ASP.NET Core Razor Pages

## Decision Log

Our decisions are recorded in the [repository wiki](https://github.com/DevOps-GroupF/itu-minitwit-devops/wiki).

## Building
```bash
cd MiniTwit
dotnet build
```

## Running

```bash
dotnet run
```

### With Docker

```bash
docker build -t minitwit-image
docker run -p 8080:8080 minitwit-image
```
