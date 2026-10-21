using backend.DTOs;
using backend.Interfaces;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace backend.Controllers;

[ApiController]
[Route("api/mailbox")]
public class MailboxController : ApiControllerBase
{
    private readonly IGmailConnectionService _connection;
    private readonly GoogleOptions _google;

    public MailboxController(IGmailConnectionService connection, IOptions<GoogleOptions> google)
    {
        _connection = connection;
        _google = google.Value;
    }

    [HttpGet]
    [Route("connectUrl")]
    public IActionResult GetConnectUrl()
    {
        var url = _connection.BuildAuthorizationUrl(CurrentUserId);
        return Ok(new { url });
    }

    [AllowAnonymous]
    [HttpGet]
    [Route("oauth/callback")]
    public async Task<IActionResult> OAuthCallback(
        [FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, CancellationToken ct)
    {
        string status;

        if (!string.IsNullOrEmpty(error))
        {
            status = "denied";
        }
        else if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            status = "invalid";
        }
        else
        {
            var result = await _connection.CompleteConnectionAsync(code, state, ct);
            status = result switch
            {
                GmailConnectResult.Success => "connected",
                GmailConnectResult.InvalidState => "invalid",
                GmailConnectResult.NoRefreshToken => "norefresh",
                _ => "failed"
            };
        }

        return Redirect($"{_google.FrontendReturnPath}?status={status}");
    }
}