using backend.Controllers;
using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/emailTest")]
public class EmailTestController : ApiControllerBase
{
    private readonly IJobEmailClassifier _classifier;
    public EmailTestController(IJobEmailClassifier classifier) { _classifier = classifier; }

    [HttpPost]
    [Route("classify")]
    public async Task<IActionResult> Classify([FromBody] ClassifyTestDto dto, CancellationToken ct)
    {
        var result = await _classifier.ClassifyAsync(dto.Subject, dto.Body, ct);
        return result is null ? StatusCode(502, "Classifier returned no usable result.") : Ok(result);
    }
}