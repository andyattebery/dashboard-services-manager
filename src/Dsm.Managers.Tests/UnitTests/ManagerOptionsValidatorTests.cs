using Dsm.Managers.Configuration;
using Dsm.Managers.DashboardManagers;
using Microsoft.Extensions.Options;

namespace Dsm.Managers.Tests.UnitTests;

[CancelAfter(TestTimeouts.HungThresholdMs)]
public class ManagerOptionsValidatorTests
{
    [Test]
    public void FailsOnDuplicateDashboardManagerType()
    {
        var options = new ManagerOptions
        {
            DashboardManagers = new List<DashboardManagerConfig>
            {
                new() { DashboardManagerType = DashboardManagerType.Dashy, DashboardConfigDirectoryPath = "/tmp/a" },
                new() { DashboardManagerType = DashboardManagerType.Dashy, DashboardConfigDirectoryPath = "/tmp/b" },
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
                new() { DashboardManagerType = DashboardManagerType.Dashy, DashboardConfigDirectoryPath = "/tmp/a" },
                new() { DashboardManagerType = DashboardManagerType.Homepage, DashboardConfigDirectoryPath = "/tmp/b" },
            },
        };

        var result = new ManagerOptionsValidator().Validate(null, options);

        Assert.That(result.Succeeded, Is.True);
    }

    [Test]
    public void FailsWhenWidgetsPathSetOnNonHomepageManager()
    {
        var options = new ManagerOptions
        {
            DashboardManagers = new List<DashboardManagerConfig>
            {
                new()
                {
                    DashboardManagerType = DashboardManagerType.Dashy,
                    DashboardConfigDirectoryPath = "/tmp/a",
                    SourceHomepageServiceWidgetsFilePath = "/tmp/widgets.yaml",
                },
            },
        };

        var result = new ManagerOptionsValidator().Validate(null, options);

        Assert.That(result.Failed, Is.True);
        Assert.That(result.Failures, Has.Some.Contains("only supported for Homepage"));
    }

    [Test]
    public void FailsWhenDashboardManagersIsNull()
    {
        var options = new ManagerOptions { DashboardManagers = null! };

        var result = new ManagerOptionsValidator().Validate(null, options);

        Assert.That(result.Failed, Is.True);
        Assert.That(result.Failures, Has.Some.Contains("DashboardManagers is required"));
    }

    [Test]
    public void FailsWhenDashboardManagersIsEmpty()
    {
        var options = new ManagerOptions
        {
            DashboardManagers = new List<DashboardManagerConfig>(),
        };

        var result = new ManagerOptionsValidator().Validate(null, options);

        Assert.That(result.Failed, Is.True);
        Assert.That(result.Failures, Has.Some.Contains("must contain at least one entry"));
    }

    [Test]
    public void SucceedsWithWidgetsPathOnHomepageManager()
    {
        var options = new ManagerOptions
        {
            DashboardManagers = new List<DashboardManagerConfig>
            {
                new()
                {
                    DashboardManagerType = DashboardManagerType.Homepage,
                    DashboardConfigDirectoryPath = "/tmp/a",
                    SourceHomepageServiceWidgetsFilePath = "/tmp/widgets.yaml",
                },
            },
        };

        var result = new ManagerOptionsValidator().Validate(null, options);

        Assert.That(result.Succeeded, Is.True);
    }
}
