using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.KeyPerFile;
using MiniTwit.Areas.Api.Metrics;
using MiniTwit.Data;
using MiniTwit.Models.DataModels;
using OpenTelemetry.Metrics;
using Prometheus;

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
    options.UseSqlServer(
        connectionString,
        providerOptions => providerOptions.EnableRetryOnFailure()
    )
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
