# Group F - MiniTwit on ASP.NET Core Razor Pages

## Decision Log

### CI/CD setup
For our CI/CD setup, we wanted to use the tools we are already using to the furthest extent possible, to concentrate the number of platforms where our project is present, in the name of simplicity.
Therefore, since we already are using GitHub extensively, we picked GitHub Actions as our CI/CD pipeline provider.
Here, we have plenty of compute time, and the configuration is dead-simple.
Furthermore, by having our distributed version control and CI/CD pipeline on the same platform, we reap the benefits of having a close association between each change in code and the outcome of building and deploying the system with that change.
For example, when a PR is submitted, the proposed new version is automatically built and tested, and the results are clearly present in the overview of the PR.
As such, the code review process is enhanced.

Another major impact of using GitHub Actions is that now the task of building our application is carried within GitHub, and not on our VM.
In theory, this should free up some processing power on the VM, because the only purpose of the VM is now to execute the application (besides keeping up to date with the latest version of the software).

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


