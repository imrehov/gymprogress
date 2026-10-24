using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace GymProgress.Api;

public interface ICurrentUser
{
    Guid UserId { get; }
}

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;

            var subClaim = user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(subClaim) || !Guid.TryParse(subClaim, out var userId))
            {
                throw new InvalidOperationException("User is not authenticated or the subject claim is invalid.");
            }

            return userId;
        }
    }
}