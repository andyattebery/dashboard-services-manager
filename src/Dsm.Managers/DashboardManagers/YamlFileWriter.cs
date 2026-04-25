namespace Dsm.Managers.DashboardManagers;

internal static class YamlFileWriter
{
    public static async Task<bool> WriteIfChanged(string path, string content)
    {
        if (File.Exists(path))
        {
            var existing = await File.ReadAllTextAsync(path);
            if (existing == content) return false;
        }
        await File.WriteAllTextAsync(path, content);
        return true;
    }
}
