using Microsoft.AspNetCore.Identity;

namespace GymProgress.Api;

public class AppUser : IdentityUser<Guid>
{
    public AppUser()
    {

    }

    public required string DisplayName { get; set; }
}
