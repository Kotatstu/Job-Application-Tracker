using backend.DTOs;

namespace backend.Interfaces;

public record PrefilterResult(bool Skip, string? Reason)
{
    public static PrefilterResult Pass() => new(false, null);
    public static PrefilterResult SkipFor(string reason) => new(true, reason);
}

public interface IEmailPrefilter
{
    PrefilterResult Evaluate(GmailMessageDto email);
}