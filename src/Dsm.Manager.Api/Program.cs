using Dsm.Managers.HostBuilder;
using Dsm.Shared.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

const string OutputTemplate =
    "{Timestamp:o} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

// Bootstrap logger captures startup logs before host config is built; replaced by AddSerilog below.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: OutputTemplate)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .WriteTo.Console(outputTemplate: OutputTemplate));

    builder.Services.AddControllers();
    builder.Services
        .AddOpenApi()
        .AddDsmManagerServices();

    builder.Configuration
        .AddDsmManagerConfiguration()
        .AddEnvironmentVariables(Constants.EnvironmentVariablePrefix);

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
