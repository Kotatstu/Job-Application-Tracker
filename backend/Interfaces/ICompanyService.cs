using backend.DTOs;
using backend.Services;

namespace backend.Interfaces;

//Company service interface
public interface ICompanyService
{
    Task<List<CompanyResponseDTO>> GetAllAsync(Guid userId);
    Task<(CompanyResponseDTO Company, bool WasCreated)> CreateAsync(CompanyCreateDTO dto, Guid userId);
    Task<CompanyResponseDTO?> GetByIdAsync(int id, Guid userId);
    Task<(CompanyResponseDTO?, CompanyUpdateResult)> UpdateByIdAsync(int id, Guid userId, CompanyUpdateDTO dto);
    
    // //Pactice delete pattern only
    // Task<bool> DeleteByIdAsync(int id, Guid userId);
}

