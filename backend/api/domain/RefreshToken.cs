namespace GymProgress.Api;

public sealed class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public required string TokenHash { get; set; }

    public Guid FamilyId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedByIp { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public Guid? ReplacedByTokenId { get; set; }
    public RefreshToken? ReplacedByToken { get; set; }
}