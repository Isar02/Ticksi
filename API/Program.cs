using API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddCors(options => 
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


var baseDirectory = AppContext.BaseDirectory;

var solutionRoot = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", ".."));
var dbPath = Path.Combine(solutionRoot, "db-backups", "TicksiDb.mdf");
// Get the full absolute path - SQL Server connection strings need absolute paths
var dbPathAbsolute = Path.GetFullPath(dbPath);

// Get the connection string and replace the AttachDbFilename value
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString))
{
    
    if (connectionString.Contains("|DataDirectory|"))
    {
        connectionString = connectionString.Replace("|DataDirectory|\\..\\..\\..\\..\\db-backups\\TicksiDb.mdf", dbPathAbsolute);
    }
    
    else if (connectionString.Contains("..\\..\\..\\..\\db-backups\\TicksiDb.mdf"))
    {
        connectionString = connectionString.Replace("..\\..\\..\\..\\db-backups\\TicksiDb.mdf", dbPathAbsolute);
    }
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));


var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseCors("AllowAngular");

app.MapControllers();

app.Run();
