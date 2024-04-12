using System;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.KeyPerFile;
using Microsoft.Extensions.Hosting;
using MiniTwit.Areas.Api.Metrics;
using MiniTwit.Data;
using MiniTwit.Models.DataModels;
using OpenTelemetry.Metrics;
using Prometheus;
using Serilog;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();

string? connectionString;

if (!builder.Environment.IsDevelopment())
{
    string? ConnectionStringEnvVar = builder.Configuration["DbConnectionStringEnvVar"];
    connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvVar);
}
else
{
    connectionString = builder.Configuration.GetConnectionString("MinitwitSqlServer");
}

builder.Services.AddDbContext<MiniTwitContext>(options =>
    options.UseNpgsql(connectionString, providerOptions => providerOptions.EnableRetryOnFailure())
);

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(3); // Set session timeout
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

/* builder.Services.AddIdentityCore<MiniTwit.Models.User>(); */
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddMetrics();

// builder
//     .Services.AddOpenTelemetry()
//     .WithMetrics(builder =>
//     {
//         builder.AddPrometheusExporter();

//         builder.AddMeter(
//             "Microsoft.AspNetCore.Hosting",
//             "Microsoft.AspNetCore.Server.Kestrel",
//             "Microsoft.AspNetCore.Routing",
//             "Microsoft.AspNetCore.Diagnostics"
//         );

//         builder.AddView(
//             "http.server.request.duration",
//             new ExplicitBucketHistogramConfiguration
//             {
//                 Boundaries = new double[]
//                 {
//                     0,
//                     0.005,
//                     0.01,
//                     0.025,
//                     0.05,
//                     0.075,
//                     0.1,
//                     0.25,
//                     0.5,
//                     0.75,
//                     1,
//                     2.5,
//                     5,
//                     7.5,
//                     10
//                 }
//             }
//         );
//     });

try
{
    configureLogging();
    builder.Host.UseSerilog();
}
catch (Exception e) { }

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MiniTwitContext>();

    if (db.Database.CanConnect())
    {
        db.Database.Migrate();
    }
}

// app.MapPrometheusScrapingEndpoint();

app.UseMetricServer();
app.UseHttpMetrics();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapAreaControllerRoute(
    name: "default",
    areaName: "FrontEnd",
    pattern: "{controller=Home}/{action=Index}"
);

app.MapAreaControllerRoute(
    name: "UserTimeline",
    areaName: "FrontEnd",
    pattern: "{username}/{action}",
    defaults: new { controller = "UserTimeline", action = "Index" }
);

app.MapAreaControllerRoute(
    name: "Api",
    areaName: "Api",
    pattern: "api/{controller}/{action=Index}"
);
app.UseMiddleware<CounterMetricMiddleware>();
app.UseMiddleware<RequestInFlightMiddleware>();
app.UseMiddleware<ResponseTimeMiddleware>();

app.Run();

void configureLogging()
{
    var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{environment}.json", optional: true)
        .Build();

    Log.Logger = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .Enrich.WithExceptionDetails()
        .WriteTo.Debug()
        .WriteTo.Console()
        .WriteTo.Elasticsearch(ConfigureElasticSink(configuration, environment))
        .Enrich.WithProperty("Environment", environment)
        .ReadFrom.Configuration(configuration)
        .CreateLogger();
}

static ElasticsearchSinkOptions ConfigureElasticSink(
    IConfigurationRoot configuration,
    string environment
)
{
    return new ElasticsearchSinkOptions(new Uri(configuration["ElasticConfiguration:Uri"]))
    {
        AutoRegisterTemplate = true,
        IndexFormat =
            $"{Assembly.GetExecutingAssembly().GetName().Name.ToLower().Replace(".", "-")}-{environment.ToLower()}-{DateTime.UtcNow:yyyy-MM}",
        NumberOfReplicas = 1,
        NumberOfShards = 2
    };
}

