using backend.Controllers;
using backend.DTOs;
using backend.Interfaces;
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
}