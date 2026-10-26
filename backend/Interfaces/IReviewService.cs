using backend.DTOs;

namespace backend.Interfaces;

public interface IReviewService
{
    Task<ReviewSummaryDTO> GetSummaryAsync(Guid userId);

    Task<List<PendingApplicationDTO>> GetApplicationsAsync(Guid userId, string reviewStatus);
    Task<(JobApplicationResponseDTO?, ReviewResult)> ConfirmApplicationAsync(int id, ConfirmPendingApplicationDTO dto, Guid userId);
    Task<ReviewResult> DismissApplicationAsync(int id, Guid userId);

    Task<List<PendingStatusChangeDTO>> GetStatusChangesAsync(Guid userId, string reviewStatus);
    Task<(JobApplicationResponseDTO?, ReviewResult)> ConfirmStatusChangeAsync(int id, ConfirmStatusChangeDTO dto, Guid userId);
    Task<ReviewResult> DismissStatusChangeAsync(int id, Guid userId);
}