using Dsm.Managers.HostBuilder;
using Dsm.Shared.Configuration;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDsmManagerServices();
builder.Configuration.AddDsmManagerConfiguration();
builder.Configuration.AddEnvironmentVariables(Constants.EnvironmentVariablePrefix);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
