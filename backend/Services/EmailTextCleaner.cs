using System.Text.RegularExpressions;

namespace backend.Services;

public static class EmailTextCleaner
{
    private const int MaxChars = 3000;

    public static string Clean(string text)
    {
        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');

        // everything after "On ... wrote:" is a quoted earlier message
        var wrote = Regex.Match(normalized, @"^On .{5,200} wrote:\s*$",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);
        if (wrote.Success)
            normalized = normalized[..wrote.Index];

        var lines = normalized.Split('\n')
            .Select(l => l.TrimEnd())
            .Where(l => !l.TrimStart().StartsWith('>'));

        var joined = string.Join("\n", lines);
        joined = Regex.Replace(joined, @"[ \t]+", " ");
        joined = Regex.Replace(joined, @"\n{3,}", "\n\n").Trim();

        return joined.Length > MaxChars ? joined[..MaxChars] : joined;
    }
}