namespace DesktopPortal.Services;

public static class WindowMatchPlan
{
    public static IReadOnlyList<string> BuildFileTitleCandidates(string filePath, string? titleHint)
    {
        var candidates = new List<string>();
        AddCandidate(candidates, titleHint);
        AddCandidate(candidates, Path.GetFileName(filePath));
        AddCandidate(candidates, Path.GetFileNameWithoutExtension(filePath));
        return candidates;
    }

    public static IReadOnlyList<string> BuildUrlTitleCandidates(string url, string? titleHint)
    {
        var candidates = new List<string>();
        AddCandidate(candidates, titleHint);

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            AddCandidate(candidates, uri.Host);
            if (uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            {
                AddCandidate(candidates, uri.Host[4..]);
            }
        }
        else
        {
            AddCandidate(candidates, url);
        }

        return candidates;
    }

    public static IReadOnlyList<string> BuildFolderTitleCandidates(string folderPath)
    {
        var candidates = new List<string>();
        AddCandidate(candidates, Path.GetFileName(folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
        AddCandidate(candidates, folderPath);
        return candidates;
    }

    public static bool TitleMatches(string title, IEnumerable<string> candidates)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        return candidates.Any(candidate =>
            !string.IsNullOrWhiteSpace(candidate) &&
            title.Contains(candidate.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static bool FileWindowMatches(string title, string processName, IEnumerable<string> titleCandidates)
    {
        return TitleMatches(title, titleCandidates) && !IsKnownBrowserProcess(processName);
    }

    public static bool ProcessNameMatches(string processName, IEnumerable<string> candidates)
    {
        var normalized = Path.GetFileNameWithoutExtension(processName);
        return candidates.Any(candidate =>
            string.Equals(normalized, Path.GetFileNameWithoutExtension(candidate), StringComparison.OrdinalIgnoreCase));
    }

    public static bool PathsEqual(string? left, string? right)
    {
        var normalizedLeft = NormalizePath(left);
        var normalizedRight = NormalizePath(right);
        return normalizedLeft is not null &&
               normalizedRight is not null &&
               string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
    }

    public static string? NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var cleaned = path.Trim().Trim('"').Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        try
        {
            cleaned = Path.GetFullPath(cleaned);
        }
        catch
        {
            // Keep best-effort normalization for paths that cannot be resolved by Path.GetFullPath.
        }

        cleaned = cleaned.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        var root = Path.GetPathRoot(cleaned);
        while (cleaned.Length > (root?.Length ?? 0) &&
               cleaned.EndsWith(Path.DirectorySeparatorChar))
        {
            cleaned = cleaned[..^1];
        }

        return cleaned;
    }

    private static void AddCandidate(List<string> candidates, string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return;
        }

        var cleaned = candidate.Trim();
        if (!candidates.Contains(cleaned, StringComparer.OrdinalIgnoreCase))
        {
            candidates.Add(cleaned);
        }
    }

    private static bool IsKnownBrowserProcess(string processName)
    {
        var normalized = Path.GetFileNameWithoutExtension(processName);
        return normalized.Equals("chrome", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("msedge", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("firefox", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("brave", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("opera", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("opera_gx", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("vivaldi", StringComparison.OrdinalIgnoreCase);
    }
}
