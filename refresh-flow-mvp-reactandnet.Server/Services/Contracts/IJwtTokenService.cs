using refresh_flow_mvp_reactandnet.Server.Data;

namespace refresh_flow_mvp_reactandnet.Server.Services.Contracts
{
    public interface IJwtTokenService
    {
        Task<(string token, TimeSpan expiresIn)> GenerateAsync(AppUser user, TimeSpan? lifetimeOverride = null);
    }
}
