using Dsm.Managers.Configuration;
using Dsm.Managers.DashboardManagers;
using Dsm.Shared.Models;
using Dsm.Shared.Tests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dsm.Managers.Tests.UnitTests;

public class DashboardCommandProcessorDuplicateTypeTests : BaseTest
{
    private DashboardCommandProcessor _processor = null!;
    private string _dashyPathA = null!;
    private string _dashyPathB = null!;

    [OneTimeSetUp]
    public override void OneTimeSetUp()
    {
    }

    [SetUp]
    public void Setup()
    {
        _dashyPathA = Path.Combine(Path.GetTempPath(), $"dsm_dup_a_{Guid.NewGuid():N}.yml");
        _dashyPathB = Path.Combine(Path.GetTempPath(), $"dsm_dup_b_{Guid.NewGuid():N}.yml");

        foreach (var path in new[] { _dashyPathA, _dashyPathB })
        {
            File.WriteAllText(path, """
                ---
                sections: []
                """);
        }

        ServiceProvider = ServiceProviderFactory.Create(ConfigureConfiguration, AddServices);
        _processor = ServiceProvider.GetRequiredService<DashboardCommandProcessor>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var path in new[] { _dashyPathA, _dashyPathB })
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Test]
    public void ThrowsOnDuplicateManagerType()
    {
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _processor.UpdateWithServicesFromProvider(new List<Service>()));
    }

    protected override void AddServices(IConfiguration configuration, IServiceCollection services)
    {
        base.AddServices(configuration, services);

        services.Configure<ManagerOptions>(opts =>
        {
            opts.DashboardManagers = new List<DashboardManagerConfig>
            {
                new DashboardManagerConfig
                {
                    DashboardManagerType = DashboardManagerType.Dashy,
                    DashboardConfigFilePath = _dashyPathA,
                },
                new DashboardManagerConfig
                {
                    DashboardManagerType = DashboardManagerType.Dashy,
                    DashboardConfigFilePath = _dashyPathB,
                },
            };
        });

        services.AddTransient<IOptions<ServiceDefaultOptions>>(_ =>
            Options.Create(new ServiceDefaultOptions { UseWalkxcodeDashboardIcons = false }));
    }
}
