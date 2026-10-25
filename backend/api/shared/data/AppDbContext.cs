using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GymProgress.Api.Data;

public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        var refreshToken = builder.Entity<RefreshToken>();

        //optimistic concurrency for EF -> 2 simultaneous requests cannot rotate the same token successfully
        refreshToken
            .Property(token => token.RevokedAtUtc)
            .IsConcurrencyToken();

        //sha256 hex needs 64chars
        refreshToken.Property(token => token.TokenHash)
            .HasMaxLength(64)
            .IsRequired();

        //so that two records cannot represent same raw token
        refreshToken.HasIndex(token => token.TokenHash)
            .IsUnique();

        //longest ipv6 can fit in 45 chars, should be nullable incase .net doesnt know the address
        refreshToken.Property(token => token.CreatedByIp)
            .HasMaxLength(45);

        //deleting an user deletes their tokens
        refreshToken
            .HasOne(token => token.User)
            .WithMany(user => user.RefreshTokens)
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        //self referencing; new token cannot be replacement for more than 1 old tokens
        refreshToken
            .HasOne(token => token.ReplacedByToken)
            .WithOne()
            .HasForeignKey<RefreshToken>(
                token => token.ReplacedByTokenId)
            //restrict for audit history
            .OnDelete(DeleteBehavior.Restrict);

        //for query "find every token in family A belonging to user X"
        refreshToken.HasIndex(token => new
        {
            token.UserId,
            token.FamilyId
        });

        //db invariants
        refreshToken.ToTable("RefreshTokens", table =>
        {
            //token cannot expire before it was created
            table.HasCheckConstraint(
                "CK_RefreshTokens_ExpiresAfterCreation",
                "\"ExpiresAtUtc\" > \"CreatedAtUtc\"");

            //token cannot be revoked before it was created
            table.HasCheckConstraint(
                "CK_RefreshTokens_RevokedAfterCreation",
                "\"RevokedAtUtc\" IS NULL OR " +
                "\"RevokedAtUtc\" >= \"CreatedAtUtc\"");

            //token cannot point to replacement while still unrevoked
            table.HasCheckConstraint(
                "CK_RefreshTokens_ReplacementRequiresRevocation",
                "\"ReplacedByTokenId\" IS NULL OR " +
                "\"RevokedAtUtc\" IS NOT NULL");
        });
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
}