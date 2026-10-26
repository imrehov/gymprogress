using MediatR;
using Microsoft.EntityFrameworkCore;

using GymProgress.Api.Data;

namespace GymProgress.Api;

public static class Logout
{

    //nullable token, because logging out without a cookie is valid
    public sealed record Command(
        string? RawRefreshToken) : IRequest;

    public class Handler : IRequestHandler<Command>
    {
        private readonly AppDbContext _dataBase;
        private readonly TimeProvider _timeProvider;

        public Handler(AppDbContext dataBase, TimeProvider timeProvider)
        {
            _dataBase = dataBase;
            _timeProvider = timeProvider;
        }

        public async Task Handle(
        Command request,
        CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.RawRefreshToken))
            {
                return;
            }
            
            //hash the token to compare it against db
            var tokenHash = TokenService.HashRefreshToken(
                request.RawRefreshToken);

            //compare hashed token from request to db
            var token = await _dataBase.RefreshTokens
                .SingleOrDefaultAsync(
                    token => token.TokenHash == tokenHash,
                    cancellationToken);

            //if it doesnt exist or its been revoked already still return, no error
            if (token is null || token.RevokedAtUtc is not null)
            {
                return;
            }

            //if it exists and not revoked, then set it to revoked
            token.RevokedAtUtc =
                _timeProvider.GetUtcNow().UtcDateTime;

            try
            {
                await _dataBase.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                //cacth the exception because another request already revoked it -> logout should still succeed
            }
        }
    }

    public static void MapEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/api/v1/auth/logout",
            async (
                HttpContext httpContext,
                ISender sender,
                IHostEnvironment environment,
                CancellationToken cancellationToken) =>
            {
                httpContext.Request.Cookies.TryGetValue(
                    RefreshTokenCookie.Name,
                    out var rawToken);

                await sender.Send(
                    new Command(rawToken),
                    cancellationToken);

                RefreshTokenCookie.Delete(
                    httpContext.Response,
                    secure: !environment.IsDevelopment());

                return Results.NoContent();
            }    
        );
    }
}