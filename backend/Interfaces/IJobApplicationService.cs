using backend.DTOs;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Interfaces;

public interface IJobApplicationService
{
    Task<List<JobApplicationResponseDTO>> GetAllAsync(Guid userId);
    Task<JobApplicationResponseDTO?> GetByIdAsync(int id, Guid userId);
    Task<(JobApplicationResponseDTO?, JobApplicationCreateResult)> CreateAsync(JobApplicationCreateDTO dto, Guid userId, string source = "Manual", int? sourceEmailId = null, DateTime? changedAt = null);
    Task<(JobApplicationResponseDTO?, JobApplicationUpdateResponse)> UpdateByIdAsync(JobApplicationUpdateDTO dto, int id, Guid userId);
    Task<(JobApplicationResponseDTO?, UpdateStatusResult)> UpdateStatusAsync(ChangeStatusDTO dto, int id, Guid userId, string source = "Manual", int? sourceEmailId = null, DateTime? changedAt = null);
    Task<(List<StatusHistoryEntryDTO>?, GetStatusHistoryResult)> GetStatusHistoryById(int id, Guid userId);
    Task<(JobPostingDetailReponseDTO?, JobPostingDetailUpsertResult)> UpsertAsync(JobPostingDetailUpsertDTO dto, int JobApplicationId, Guid userId);
    Task<(JobPostingDetailReponseDTO?, GetJobPostingDetailResult)> GetPostingDetailById(int JobApplicationId, Guid userId);
}