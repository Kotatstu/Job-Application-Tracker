
using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[Route("api/review")]
public class ReviewController : ApiControllerBase
{
    private static readonly HashSet<string> AllowedStatuses = new() { "Pending", "Confirmed", "Dismissed" };

    private readonly IReviewService _review;

    public ReviewController(IReviewService review)
    {
        _review = review;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
        => Ok(await _review.GetSummaryAsync(CurrentUserId));

    // ---------- Pending new applications ----------

    [HttpGet("applications")]
    public async Task<IActionResult> GetApplications([FromQuery] string reviewStatus = "Pending")
    {
        if (!AllowedStatuses.Contains(reviewStatus))
            return BadRequest(new { message = "reviewStatus must be Pending, Confirmed or Dismissed." });

        return Ok(await _review.GetApplicationsAsync(CurrentUserId, reviewStatus));
    }

    [HttpPost("applications/{id:int}/confirm")]
    public async Task<IActionResult> ConfirmApplication(int id, [FromBody] ConfirmPendingApplicationDTO dto)
    {
        var (application, result) = await _review.ConfirmApplicationAsync(id, dto, CurrentUserId);
        return result == ReviewResult.Success ? Ok(application) : ToError(result);
    }

    [HttpPost("applications/{id:int}/dismiss")]
    public async Task<IActionResult> DismissApplication(int id)
    {
        var result = await _review.DismissApplicationAsync(id, CurrentUserId);
        return result == ReviewResult.Success ? NoContent() : ToError(result);
    }

    // ---------- Pending status changes ----------

    [HttpGet("status-changes")]
    public async Task<IActionResult> GetStatusChanges([FromQuery] string reviewStatus = "Pending")
    {
        if (!AllowedStatuses.Contains(reviewStatus))
            return BadRequest(new { message = "reviewStatus must be Pending, Confirmed or Dismissed." });

        return Ok(await _review.GetStatusChangesAsync(CurrentUserId, reviewStatus));
    }

    [HttpPost("status-changes/{id:int}/confirm")]
    public async Task<IActionResult> ConfirmStatusChange(int id, [FromBody] ConfirmStatusChangeDTO dto)
    {
        var (application, result) = await _review.ConfirmStatusChangeAsync(id, dto, CurrentUserId);
        return result == ReviewResult.Success ? Ok(application) : ToError(result);
    }

    [HttpPost("status-changes/{id:int}/dismiss")]
    public async Task<IActionResult> DismissStatusChange(int id)
    {
        var result = await _review.DismissStatusChangeAsync(id, CurrentUserId);
        return result == ReviewResult.Success ? NoContent() : ToError(result);
    }

    private IActionResult ToError(ReviewResult result) => result switch
    {
        ReviewResult.NotFound => NotFound(new { message = "Pending item not found." }),
        ReviewResult.AlreadyReviewed => Conflict(new { message = "This item was already confirmed or dismissed." }),
        ReviewResult.CompanyNotFound => NotFound(new { message = "Company not found." }),
        ReviewResult.ApplicationNotFound => NotFound(new { message = "Application not found." }),
        ReviewResult.InvalidStatus => BadRequest(new { message = "Status must not be empty." }),
        _ => StatusCode(500)
    };
}