using API;
using API.Errors;
using API.Options;
using Microsoft.Extensions.Options;
using Ticksi.Application;
using Ticksi.Infrastructure;
using Ticksi.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApi(builder.Configuration);

var app = builder.Build();

app.Services.GetRequiredService<IStartupValidator>().Validate();
await app.Services.InitializeDatabaseAsync(app.Lifetime.ApplicationStopping);

app.UseApiErrorResponses();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(ClientAppOptions.CorsPolicy);
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
