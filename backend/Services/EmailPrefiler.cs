using System.Text.RegularExpressions;
using backend.DTOs;
using backend.Interfaces;

namespace backend.Services;

public class EmailPrefilter : IEmailPrefilter
{
    private const RegexOptions Opts = RegexOptions.IgnoreCase | RegexOptions.Compiled;

    private static readonly Regex Rescue = new(
        @"your application|thank(s| you) for applying|application (received|status|update)|" +
        @"interview|assessment|coding (test|challenge)|unfortunately|regret to inform|" +
        @"offer letter|next steps",
        Opts);

    private static readonly Regex NoiseSubject = new(
        @"jobs? you may be interested in|new jobs? (for you|matching|in)|job alerts?|" +
        @"recommended (jobs|for you)|top job picks|weekly (digest|newsletter)|" +
        @"jobs? similar to",
        Opts);

    private static readonly string[] NoiseLabels =
        { "CATEGORY_PROMOTIONS", "CATEGORY_SOCIAL", "CATEGORY_FORUMS" };

    private static readonly string[] BulkPrecedence = { "bulk", "list", "junk" };

    public PrefilterResult Evaluate(GmailMessageDto email)
    {
        if (Rescue.IsMatch(email.Subject))
            return PrefilterResult.Pass();

        if (NoiseSubject.IsMatch(email.Subject))
            return PrefilterResult.SkipFor("subject: job alert / digest pattern");

        var label = email.LabelIds.FirstOrDefault(l => NoiseLabels.Contains(l));
        if (label is not null)
            return PrefilterResult.SkipFor($"label: {label}");

        if (BulkPrecedence.Contains(email.Precedence.Trim().ToLowerInvariant()))
            return PrefilterResult.SkipFor("header: Precedence bulk");

        return PrefilterResult.Pass();
    }
}