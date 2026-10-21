using Dsm.Managers.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Dsm.Managers.Tests.UnitTests;

[CancelAfter(TestTimeouts.HungThresholdMs)]
public class StaleProviderTrackerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(10);

    // Shorter than Timeout, so regular POSTs never look like a manager-side outage.
    private static readonly TimeSpan PostInterval = TimeSpan.FromMinutes(6);

    [Test]
    public void FirstPost_UntrackedProviderId_IsNotStale()
    {
        var time = new ManualTimeProvider();
        var tracker = CreateTracker(time);

        tracker.MarkSeen(["live"]);

        Assert.That(tracker.FindStale(["gone"]), Is.Empty);
    }

    [Test]
    public void ProviderIdUnseen_PastTimeout_IsStale()
    {
        var time = new ManualTimeProvider();
        var tracker = CreateTracker(time);

        tracker.MarkSeen(["live", "gone"]);
        time.Advance(PostInterval);
        tracker.MarkSeen(["live"]);
        time.Advance(PostInterval);
        tracker.MarkSeen(["live"]);

        var stale = tracker.FindStale(["live", "gone"]);

        Assert.That(stale.Keys, Is.EquivalentTo(new[] { "gone" }));
        Assert.That(stale["gone"], Is.EqualTo(TimeSpan.FromMinutes(12)));
    }

    [Test]
    public void ProviderIdUnseen_ExactlyTimeout_IsNotStale()
    {
        var time = new ManualTimeProvider();
        var tracker = CreateTracker(time);

        tracker.MarkSeen(["live", "gone"]);
        time.Advance(TimeSpan.FromMinutes(5));
        tracker.MarkSeen(["live"]);
        time.Advance(TimeSpan.FromMinutes(5));
        tracker.MarkSeen(["live"]);

        Assert.That(tracker.FindStale(["gone"]), Is.Empty);
    }

    [Test]
    public void StaleProviderId_PostsAgain_IsNoLongerStale()
    {
        var time = new ManualTimeProvider();
        var tracker = CreateTracker(time);

        tracker.MarkSeen(["live", "gone"]);
        time.Advance(PostInterval);
        tracker.MarkSeen(["live"]);
        time.Advance(PostInterval);
        tracker.MarkSeen(["live"]);
        Assert.That(tracker.FindStale(["gone"]), Is.Not.Empty, "precondition: gone is stale");

        time.Advance(PostInterval);
        tracker.MarkSeen(["live", "gone"]);

        Assert.That(tracker.FindStale(["gone"]), Is.Empty);
    }

    [Test]
    public void NoPostForLongerThanTimeout_NextPost_RestartsEveryTimer()
    {
        var time = new ManualTimeProvider();
        var tracker = CreateTracker(time);

        tracker.MarkSeen(["live"]);
        tracker.FindStale(["gone"]);
        time.Advance(TimeSpan.FromHours(2));
        tracker.MarkSeen(["live"]);

        Assert.That(tracker.FindStale(["gone"]), Is.Empty,
            "nobody posted for 2h, so the silence was the manager's: gone's timer restarts");

        time.Advance(PostInterval);
        tracker.MarkSeen(["live"]);
        time.Advance(PostInterval);
        tracker.MarkSeen(["live"]);

        Assert.That(tracker.FindStale(["gone"]).Keys, Is.EquivalentTo(new[] { "gone" }),
            "a full timeout of regular POSTs after the outage, gone is stale");
    }

    [Test]
    public void PostWithNoProviderIds_StillCountsAsAPost_NoFalseOutageReset()
    {
        var time = new ManualTimeProvider();
        var tracker = CreateTracker(time);

        tracker.MarkSeen(["live"]);
        tracker.FindStale(["gone"]);
        time.Advance(PostInterval);
        tracker.MarkSeen([]);
        time.Advance(PostInterval);
        tracker.MarkSeen(["live"]);

        Assert.That(tracker.FindStale(["gone"]).Keys, Is.EquivalentTo(new[] { "gone" }),
            "the empty POST at 6 min keeps the 12-minute gap from looking like an outage");
    }

    private static StaleProviderTracker CreateTracker(TimeProvider time) =>
        new(Options.Create(new ManagerOptions { DashboardManagers = [], StaleProviderTimeout = Timeout }),
            time, NullLogger<StaleProviderTracker>.Instance);
}
