using System.Text.Json;
using Microsoft.Extensions.Options;

public class OllamaChatResponse { public OllamaMessage? Message { get; set; } }
public class OllamaMessage { public string? Content { get; set; } }

public class OllamaJobEmailClassifier : IJobEmailClassifier
{
    private const int MaxBodyChars = 3000; //Could be change if context is longer
    private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _http;
    private readonly OllamaOptions _options;
    private readonly ILogger<OllamaJobEmailClassifier> _logger;

    public OllamaJobEmailClassifier(HttpClient http, IOptions<OllamaOptions> options, ILogger<OllamaJobEmailClassifier> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<EmailClassification?> ClassifyAsync(string subject, string body, CancellationToken ct = default)
    {
        string trimmedBody;

        if (body.Length > MaxBodyChars)
        {
            trimmedBody = body.Substring(0, MaxBodyChars);
        }
        else
        {
            trimmedBody = body;
        }


        var request = new
        {
            model = _options.Model,
            stream = false,
            options = new { temperature = 0, num_ctx = _options.NumCtx },
            messages = new[]
            {
                new { role = "system", content = "You are an email classifier. Do not write code. Reply only with JSON matching the schema. " +
                                                "isJobRelated: true only if the email is about a job the user applied for or is being considered for. " +
                                                "companyName: the hiring company named in the email text, never the platform that sent it. " +
                                                "jobTitle: the role named in the email. " +
                                                "statusSignal: a short status label for the application, such as Applied, Interviewing, Offer, or Rejected. " +
                                                "Use an empty string for any field the email does not state."
                    },
                new { role = "user", content = $"Subject: {subject}\nBody: {trimmedBody}"}
            },

            format = new
            {
                type = "object",
                properties = new
                {
                    isJobRelated = new { type = "boolean" },
                    kind = new { type = "string", @enum = new[] { "application_received", "interview_invite", "offer", "rejection", "other" } },
                    companyName = new { type = "string" },
                    jobTitle = new { type = "string" },
                    statusSignal = new { type = "string" }
                },
                required = new[] { "isJobRelated", "kind", "companyName", "jobTitle", "statusSignal" }

            }
        };

        //_logger.LogInformation("Sending to Ollama: {Json}", JsonSerializer.Serialize(request));
        using var response = await _http.PostAsJsonAsync("/api/chat", request, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);
        //_logger.LogInformation("Ollama replied {Status}: {Raw}", (int)response.StatusCode, raw);
        response.EnsureSuccessStatusCode();

        var envelope = JsonSerializer.Deserialize<OllamaChatResponse>(raw, JsonOpts);
        var content = envelope?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning("Ollama returned an empty message.content");
            return null;
        }

        try
        {
            var result = JsonSerializer.Deserialize<EmailClassification>(content, JsonOpts);
            if (result is null)
                return null;

            result.CompanyName = string.IsNullOrWhiteSpace(result.CompanyName) ? null : result.CompanyName;
            result.JobTitle = string.IsNullOrWhiteSpace(result.JobTitle) ? null : result.JobTitle;
            result.StatusSignal = string.IsNullOrWhiteSpace(result.StatusSignal) ? null : result.StatusSignal;

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Could not parse model output as EmailClassification: {Content}", content);
            return null;
        }
    }
}