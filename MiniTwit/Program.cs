using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.KeyPerFile;
using MiniTwit.Data;
using MiniTwit.Models.DataModels;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();

if (!builder.Environment.IsDevelopment())
{
    string? ConnectionStringKey = builder.Configuration["DbConnectionStringSecretName"];
    string? connectionString = builder.Configuration.GetConnectionString(ConnectionStringKey);

    builder.Configuration.AddKeyPerFile(directoryPath: builder.Configuration["SecretsDir"]);
    builder.Services.AddDbContext<MiniTwitContext>(options =>
        options.UseSqlServer(connectionString)
    );
}
else
{
    builder.Services.AddDbContext<MiniTwitContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("MinitwitSqlServer"))
    );
}

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(3); // Set session timeout
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

/* builder.Services.AddIdentityCore<MiniTwit.Models.User>(); */
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

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
    db.Database.Migrate();
}

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

app.Run();
