using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using MiniTwit.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddDbContext<MiniTwitContext>(options =>
    options.UseSqlite("Data Source=./minitwit.db")
);

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(3); // Set session timeout
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

/* builder.Services.AddIdentityCore<MiniTwit.Models.User>(); */
builder.Services.AddScoped<
    IPasswordHasher<MiniTwit.Models.User>,
    PasswordHasher<MiniTwit.Models.User>
>();

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

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
