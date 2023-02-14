namespace Dsm.Providers.Tests.UnitTests;
public static class TestDataUtilities
{
    public static string GetUnitTestTestDataPath(string fileName)
    {
        return Path.Combine("UnitTests", "TestData", fileName);
    }
}