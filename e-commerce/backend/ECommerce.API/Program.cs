using ECommerce.API.Extensions;
using ECommerce.API.Middlewares;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddApplicationServices(builder.Configuration, builder.Environment);
builder.Services.AddScoped<DataSeeder>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    if (app.Environment.IsDevelopment())
    {
        var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
        await seeder.SeedAsync();
    }
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseRouting();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();


if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.MapControllers();
app.Run();
