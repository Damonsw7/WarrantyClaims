using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Serilog;
using WarrantyClaims.Data;
using WarrantyClaims.Models;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(builder.Environment.ContentRootPath, "Logs", "warrantyclaims-.log"),
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddDbContext<WarrantyClaimsContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("WarrantyClaimsContext")));

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WarrantyClaimsContext>();
    if (!db.Agents.Any())
    {
        db.Agents.AddRange(
            new Agent { Name = "Damon", Pin = "1111", IsAdmin = true },
            new Agent { Name = "Sarah", Pin = "2222", IsAdmin = false },
            new Agent { Name = "Tom", Pin = "3333", IsAdmin = false });
        db.SaveChanges();
    }
}

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

app.MapGet("/uploads/{fileName}", (string fileName, IWebHostEnvironment env) =>
{
    var safeFileName = Path.GetFileName(fileName);
    var filePath = Path.Combine(env.ContentRootPath, "Uploads", safeFileName);

    if (!File.Exists(filePath))
    {
        return Results.NotFound();
    }

    var contentTypeProvider = new FileExtensionContentTypeProvider();
    if (!contentTypeProvider.TryGetContentType(filePath, out var contentType))
    {
        contentType = "application/octet-stream";
    }

    return Results.File(filePath, contentType);
});

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}
