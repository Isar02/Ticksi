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
var dbPathAbsolute = Path.GetFullPath(dbPath);

Directory.CreateDirectory(Path.GetDirectoryName(dbPathAbsolute)!);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")?
    .Replace("{DbPath}", dbPathAbsolute);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));



var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseCors("AllowAngular");

app.MapControllers();

app.Run();
