using System.IO;
using API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;




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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Ticksi API",
        Version = "v1"
    });
}
);

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure the HTTP request pipeline.

app.UseCors("AllowAngular");

app.MapControllers();

app.Run();
