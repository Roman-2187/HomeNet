using HomeNet.Models.Shared.Entity;

namespace HomeNetCore.Interfaces.Services
{
    public interface IProfileService
    {
        public record ProfileVerdict(bool IsValid, string ErrorMessage, UserEntity? User = null);
        
        Task<ProfileVerdict> DeleteAccountAsync(Guid userId);
    }
}