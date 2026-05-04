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
        // Write to a temp sibling and rename — File.Move with overwrite is atomic on
        // POSIX (rename(2)) and on Windows NTFS, so a crash mid-write can't leave the
        // dashboard with a half-written or zero-byte config.
        var temp = path + ".tmp";
        await File.WriteAllTextAsync(temp, content);
        File.Move(temp, path, overwrite: true);
        return true;
    }
}
