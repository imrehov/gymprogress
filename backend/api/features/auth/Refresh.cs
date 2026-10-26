
using MediatR;
using Microsoft.EntityFrameworkCore;

using GymProgress.Api.Data;


namespace GymProgress.Api;

public static class Refresh
{

    public sealed record Command(
        string RawRefreshToken,
        string? IpAddress) : IRequest<Result>;

    public sealed record Response(
        string AccessToken,
        DateTime AccessTokenExpiresAtUtc);

    public class Handler : IRequestHandler<Command, Result>
    {
        private readonly AppDbContext _dataBase;
        private readonly TokenService _tokenService;
        private readonly TimeProvider _timeProvider;

        public Handler(AppDbContext dataBase, TokenService tokenService, TimeProvider timeProvider)
        {
            _dataBase = dataBase;
            _tokenService = tokenService;
            _timeProvider = timeProvider;
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var tokenHash = TokenService.HashRefreshToken(
                request.RawRefreshToken);

            var existingToken = await _dataBase.RefreshTokens
                .Include(token => token.User)
                .SingleOrDefaultAsync(
                    token => token.TokenHash == tokenHash,
                    cancellationToken);

            //if token is null -> invalid
            if (existingToken is null)
            {
                return Result.Invalid();
            }
            
            //if token is expired -> invalid
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            if (existingToken.ExpiresAtUtc <= now)
            {
                return Result.Invalid();
            }

            //if a revoked token is presented -> it might be stolen -> revoke every still active token in that family
            //then return invalid
            if (existingToken.RevokedAtUtc is not null)
            {
                var activeFamilyTokens = await _dataBase.RefreshTokens
                    .Where(token =>
                        token.UserId == existingToken.UserId &&
                        token.FamilyId == existingToken.FamilyId &&
                        token.RevokedAtUtc == null)
                    .ToListAsync(cancellationToken);

                foreach (var token in activeFamilyTokens)
                {
                    token.RevokedAtUtc = now;
                }

                await _dataBase.SaveChangesAsync(cancellationToken);

                return Result.Invalid();
            }

            //if there are no problems -> generate token in same family
            var issued = _tokenService.IssueTokens(
                existingToken.User,
                request.IpAddress,
                existingToken.FamilyId);

            //then revoke the old token and link it to the new one
            existingToken.RevokedAtUtc = now;
            existingToken.ReplacedByToken = issued.RefreshToken;

            _dataBase.RefreshTokens.Add(issued.RefreshToken);

            //try catch this, so if the old-token update loses concurrency race -> the insert should roll back
            try
            {
                await _dataBase.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Invalid();
            }

            //finally return valid if no fails
            return new Result
            {
                Succeeded = true,
                Response = new Response(
                    issued.AccessToken,
                    issued.AccessTokenExpiresAtUtc),
                RawRefreshToken = issued.RawRefreshToken,
                RefreshTokenExpiresAtUtc =
                    issued.RefreshToken.ExpiresAtUtc
            };
        }
    }

    public static void MapEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/api/v1/auth/refresh",
            async (
                HttpContext httpContext,
                ISender sender,
                IHostEnvironment environment,
                CancellationToken cancellationToken) =>
            {
                if (!httpContext.Request.Cookies.TryGetValue(
                    RefreshTokenCookie.Name,
                    out var rawToken) ||
                    string.IsNullOrWhiteSpace(rawToken))
                {
                    return Results.Unauthorized();
                }

                var result = await sender.Send(
                    new Command(
                        rawToken,
                        httpContext.Connection.RemoteIpAddress?.ToString()),
                    cancellationToken);

                if (!result.Succeeded)
                {
                    RefreshTokenCookie.Delete(
                        httpContext.Response,
                        secure: !environment.IsDevelopment());

                    return Results.Unauthorized();
                }

                RefreshTokenCookie.Append(
                    httpContext.Response,
                    result.RawRefreshToken!,
                    result.RefreshTokenExpiresAtUtc!.Value,
                    secure: !environment.IsDevelopment());

                return Results.Ok(result.Response);
            });
    }

    public sealed class Result
    {
        public required bool Succeeded { get; init; }
        public Response? Response { get; init; }
        public string? RawRefreshToken { get; init; }
        public DateTime? RefreshTokenExpiresAtUtc { get; init; }

        public static Result Invalid() => new()
        {
            Succeeded = false
        };
    }
}