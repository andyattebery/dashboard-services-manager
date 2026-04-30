namespace Dsm.Shared.Tests;

public sealed class TestTempDir : IDisposable
{
    public string Path { get; }

    private TestTempDir(string path)
    {
        Path = path;
    }

    public static TestTempDir Create(string prefix)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{prefix}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return new TestTempDir(path);
    }

    public string RootedPath(string relative) => System.IO.Path.Combine(Path, relative);

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
