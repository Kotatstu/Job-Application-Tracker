using backend.Controllers;
using backend.DTOs;
using backend.Interfaces;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/emailTest")]
public class EmailTestController : ApiControllerBase
{
    private readonly IJobEmailClassifier _classifier;
    private const string SampleToken = "sample-refresh-token";

    public EmailTestController(IJobEmailClassifier classifier) { _classifier = classifier; }

    [HttpPost]
    [Route("classify")]
    public async Task<IActionResult> Classify([FromBody] ClassifyTestDto dto, CancellationToken ct)
    {
        var result = await _classifier.ClassifyAsync(dto.Subject, dto.Body, ct);
        return result is null ? StatusCode(502, "Classifier returned no usable result.") : Ok(result);
    }

    //Temp
    [HttpGet]
    [Route("protectSample")]
    public IActionResult ProtectSample([FromServices] ITokenProtector protector)
    {
        var protectedValue = protector.Protect(SampleToken);
        return Ok(new
        {
            protectedValue,
            differsFromOriginal = protectedValue != SampleToken,
            roundTripMatches = protector.Unprotect(protectedValue) == SampleToken
        });
    }

    [HttpGet]
    [Route("unprotectSample")]
    public IActionResult UnprotectSample([FromQuery] string value, [FromServices] ITokenProtector protector)
    {
        try
        {
            return Ok(new { decrypts = protector.Unprotect(value) == SampleToken });
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return Ok(new { decrypts = false });
        }
    }

    [HttpGet]
    [Route("gmailNew")]
    public async Task<IActionResult> GmailNew(
        [FromServices] IGmailReader reader,
        [FromQuery] int max = 10,
        [FromQuery] bool classify = false,
        CancellationToken ct = default)
    {
        max = Math.Clamp(max, 1, 25);
        var result = await reader.GetNewMessagesAsync(CurrentUserId, max, ct);

        if (result.Status != GmailReadStatus.Success)
            return Ok(new { status = result.Status.ToString() });

        var items = new List<object>();
        foreach (var m in result.Messages)
        {
            EmailClassification? classification = null;
            if (classify)
                classification = await _classifier.ClassifyAsync(m.Subject, m.BodyText, ct);

            items.Add(new { message = m, classification });
        }

        return Ok(new
        {
            status = "Success",
            startHistoryId = result.StartHistoryId,
            latestHistoryId = result.LatestHistoryId,
            totalNew = result.TotalNewMessages,
            returned = items.Count,
            items
        });
    }

    [HttpPost]
    [Route("sync")]
    public async Task<IActionResult> Sync(
        [FromServices] IEmailSyncService sync,
        CancellationToken ct = default)
    {
        var summary = await sync.SyncAsync(CurrentUserId, ct);

        if (summary.ReadStatus != GmailReadStatus.Success)
            return Ok(new { status = summary.ReadStatus.ToString() });

        return Ok(new { status = "Success", summary });
    }
}