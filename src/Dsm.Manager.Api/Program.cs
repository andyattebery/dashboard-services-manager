using Dsm.Managers.Hosting;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

HostBuilderConfiguration.AddServices(builder.Configuration, builder.Services);
HostBuilderConfiguration.ConfigureConfiguration(builder.Configuration);
builder.Configuration.AddEnvironmentVariables(prefix: "DSM_");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
