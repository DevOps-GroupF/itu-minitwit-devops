# Group F - MiniTwit on ASP.NET Core Razor Pages

## Decision Log

### Framework Choice

We chose to base or refactoring on ASP.NET Core Razor pages for the following reasons:

- Multiple group members are familiar with C# and ASP.NET 
- All group members are familiar with OOP
- Razor Pages are relatively simple to implement and have a good separation of concerns
- Since we are in the .NET world, we can widely expand our system later in the course; we are not constrained to a small framework, and as such, we won't have to change much to scale up the system.
- ASP.NET comes with EF Core, which allowed us to use the database mostly without writing SQL-code, which made implementing the data layer a quick affair.

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


