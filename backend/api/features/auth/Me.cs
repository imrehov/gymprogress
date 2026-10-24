using MediatR;
using Microsoft.AspNetCore.Identity;

namespace GymProgress.Api;

public static class Me
{
    public sealed record Response(
        Guid Id,
        string Email,
        string DisplayName);

    public sealed record Query : IRequest<Response>;

    public sealed class Handler : IRequestHandler<Query, Response>
    {
        private readonly ICurrentUser _currentUser;
        private readonly UserManager<AppUser> _userManager;

        public Handler(ICurrentUser currentUser, UserManager<AppUser> userManager)
        {
            _currentUser = currentUser;
            _userManager = userManager;
        }

        public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(_currentUser.UserId.ToString());
            if (user is null)
            {
                throw new InvalidOperationException("Authenticated user could not be found.");
            }

            return new Response(user.Id, user.Email!, user.DisplayName);
        }
    }

    public static void MapEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/auth/me", async (ISender sender, CancellationToken ct) =>
        {
            var response = await sender.Send(new Query(), ct);
            return Results.Ok(response);
        })
        .RequireAuthorization();
    }
}