namespace CloudExplorer.Services;

internal static class CliExecutableLocator
{
    public static string Resolve(string executableName)
    {
        if (OperatingSystem.IsWindows())
        {
            return executableName;
        }

        var candidates = new List<string>();
        var path = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrWhiteSpace(path))
        {
            candidates.AddRange(path
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(directory => Path.Combine(directory, executableName)));
        }

        // Finder-launched macOS apps do not normally inherit Homebrew's shell PATH.
        // These locations also cover the common system-wide installation paths on Linux.
        candidates.Add(Path.Combine("/opt/homebrew/bin", executableName));
        candidates.Add(Path.Combine("/usr/local/bin", executableName));
        candidates.Add(Path.Combine("/usr/bin", executableName));

        return candidates.FirstOrDefault(File.Exists) ?? executableName;
    }
}
