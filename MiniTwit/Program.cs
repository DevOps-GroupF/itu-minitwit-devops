using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using MiniTwit.Areas.Api.Metrics;
using MiniTwit.Data;
using MiniTwit.Models.DataModels;
using Prometheus;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddDbContext<MiniTwitContext>(options =>
    options.UseSqlite("Data Source=../datavol/minitwit.db")
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


builder.Services.AddOpenTelemetry().WithMetrics(builder => {
    builder.AddPrometheusExporter();
    
    builder.AddMeter(
        "Microsoft.AspNetCore.Hosting", 
        "Microsoft.AspNetCore.Server.Kestrel", 
        "Microsoft.AspNetCore.Routing", 
        "Microsoft.AspNetCore.Diagnostics"
        );
    
    builder.AddView("http.server.request.duration",
       
        new ExplicitBucketHistogramConfiguration
        {
            Boundaries = new double[] { 0, 0.005, 0.01, 0.025, 0.05,
                    0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10 }
        });
    });



var app = builder.Build();

//builder.Services.AddDbContext<MiniTwit.Data.MiniTwitContext>();

/* builder.Services.AddDbContext<MiniTwitContext>(options => */
/*     options.UseSqlite( */
/*         builder.Configuration.GetConnectionString("MiniTwitContext") */
/*             ?? throw new InvalidOperationException("Connection string 'MiniTwitContext' not found.") */
/*     ) */
/* ); */

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.MapPrometheusScrapingEndpoint();

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
