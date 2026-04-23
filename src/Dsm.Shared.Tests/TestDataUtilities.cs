namespace Dsm.Shared.Tests;
public static class TestDataUtilities
{
    public static string GetTestDataPath(string fileName)
    {
        return Path.Combine("TestData", fileName);
    }
    
    public static string GetUnitTestTestDataPath(string fileName)
    {
        return Path.Combine("UnitTests", "TestData", fileName);
    }
}