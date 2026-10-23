using Microsoft.AspNetCore.Identity;

namespace GymProgress.Api;

public class AppUser : IdentityUser<Guid>
{
    public AppUser()
    {

    }

    public string DisplayName { get; set; }
}
