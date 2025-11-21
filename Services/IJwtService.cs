using DotNetRefreshApp.Models;

namespace DotNetRefreshApp.Services
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}
