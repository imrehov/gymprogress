using Microsoft.EntityFrameworkCore;

namespace GymProgress.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

}
