using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OpenCBS.Api.Data;
using OpenCBS.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "OpenCBS Modern Core Banking API",
        Version = "v1",
        Description = "Cloud-native modernization of OpenCBS desktop core banking system into ASP.NET Core 8 WebAPI & EF Core 8."
    });
});

// Configure EF Core In-Memory Database
builder.Services.AddDbContext<OpenCbsDbContext>(options =>
    options.UseInMemoryDatabase("OpenCbsDb"));

builder.Services.AddScoped<IBankingService, BankingService>();

var app = builder.Build();

// Ensure Database Seeded
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OpenCbsDbContext>();
    db.Database.EnsureCreated();
}

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "OpenCBS Modern v1"));
}

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "OpenCBS Modern Core Banking",
    timestamp = DateTime.UtcNow
}));

app.Run();

public partial class Program { }
