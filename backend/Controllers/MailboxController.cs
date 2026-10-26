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
    private readonly IMailboxService _mailbox;

    public MailboxController(IGmailConnectionService connection, IOptions<GoogleOptions> google, IMailboxService mailbox)
    {
        _connection = connection;
        _google = google.Value;
        _mailbox = mailbox;
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

    
    [HttpGet("status")]
    public async Task<IActionResult> Status()
        => Ok(await _mailbox.GetStatusAsync(CurrentUserId));
    
    [HttpPost("sync")]
    public async Task<IActionResult> Sync()
    {
        var (result, retryAfter) = await _mailbox.RequestSyncAsync(CurrentUserId);
    
        return result switch
        {
            SyncRequestResult.Started => Accepted(),
            SyncRequestResult.AlreadyRunning => Conflict(new { message = "A sync is already running." }),
            SyncRequestResult.CooldownActive => StatusCode(StatusCodes.Status429TooManyRequests,
                new { message = "Please wait before checking again.", retryAfterSeconds = retryAfter }),
            SyncRequestResult.NotConnected => BadRequest(new { message = "No Gmail account is connected." }),
            _ => StatusCode(500)
        };
    }
}