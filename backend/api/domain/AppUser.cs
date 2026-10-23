using Microsoft.AspNetCore.Identity;

namespace GymProgress.Api;

public class AppUser : IdentityUser<Guid>
{
    public AppUser(string userName)
    {
        UserName = userName;
    }
}
