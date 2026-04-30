namespace Dsm.Shared.Tests;

public static class TestTimeouts
{
    /// <summary>
    /// Threshold (ms) above which a single test is treated as hung and cancelled by NUnit's
    /// <c>[CancelAfter]</c>. Picked to comfortably accommodate the slowest legitimate test —
    /// the jsDelivr CDN smoke probes in the icon-source tests, which can take several seconds
    /// each under load — while still catching real infinite loops or wedged HTTP calls.
    /// </summary>
    public const int HungThresholdMs = 30_000;
}
