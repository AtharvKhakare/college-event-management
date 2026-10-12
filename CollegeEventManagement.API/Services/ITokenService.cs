using CollegeEventManagement.Core.Entities;

namespace CollegeEventManagement.API.Services;

public interface ITokenService
{
    Task<(string Token, DateTime Expiration)> GenerateTokenAsync(ApplicationUser user, IList<string> roles);
}
