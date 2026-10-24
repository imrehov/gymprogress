using Microsoft.AspNetCore.Identity;

namespace GymProgress.Api;

public class AppUser : IdentityUser<Guid>
{
    public required string DisplayName { get; set; }

    //one user to many tokens relation
    public ICollection<RefreshToken> RefreshTokens { get; } = new List<RefreshToken>();
}
