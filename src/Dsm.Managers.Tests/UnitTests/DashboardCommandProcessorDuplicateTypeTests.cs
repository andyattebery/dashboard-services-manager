using Dsm.Managers.Configuration;
using Dsm.Managers.DashboardManagers;
using Microsoft.Extensions.Options;

namespace Dsm.Managers.Tests.UnitTests;

public class ManagerOptionsValidatorTests
{
    [Test]
    public void FailsOnDuplicateDashboardManagerType()
    {
        var options = new ManagerOptions
        {
            DashboardManagers = new List<DashboardManagerConfig>
            {
                new() { DashboardManagerType = DashboardManagerType.Dashy, DashboardConfigFilePath = "/tmp/a" },
                new() { DashboardManagerType = DashboardManagerType.Dashy, DashboardConfigFilePath = "/tmp/b" },
            },
        };

        var result = new ManagerOptionsValidator().Validate(null, options);

        Assert.That(result.Failed, Is.True);
        Assert.That(result.Failures, Has.Some.Contains("duplicate"));
    }

    [Test]
    public void SucceedsOnDistinctDashboardManagerTypes()
    {
        var options = new ManagerOptions
        {
            DashboardManagers = new List<DashboardManagerConfig>
            {
                new() { DashboardManagerType = DashboardManagerType.Dashy, DashboardConfigFilePath = "/tmp/a" },
                new() { DashboardManagerType = DashboardManagerType.Homepage, DashboardConfigFilePath = "/tmp/b" },
            },
        };

        var result = new ManagerOptionsValidator().Validate(null, options);

        Assert.That(result.Succeeded, Is.True);
    }
}
