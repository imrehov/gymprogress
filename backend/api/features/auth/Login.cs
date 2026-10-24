using GymProgress.Api.Data;

using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace GymProgress.Api;


public static class Login
{
    private const string RefreshCookieName = "gymprogress.refresh_token";
    public sealed record Request(
        string Email,
        string Password);

    public sealed record UserResponse(
        Guid Id,
        string Email,
        string DisplayName);

    public sealed record Response(
        string AccessToken,
        DateTime AccessTokenExpiresAtUtc,
        UserResponse User);

    public sealed record Command(
        string Email,
        string Password,
        string? IpAddress)
        : IRequest<Result>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(256);

            RuleFor(command => command.Password)
                .NotEmpty();
        }
    }

    public sealed class Result
    {
        public required bool Succeeded { get; init; }

        public Response? Response { get; init; }

        public string? RawRefreshToken { get; init; }

        public DateTime? RefreshTokenExpiresAtUtc { get; init; }

        public static Result Invalid() =>
            new()
            {
                Succeeded = false
            };
    }

    public sealed class Handler : IRequestHandler<Command, Result>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly TokenService _tokenService;
        private readonly AppDbContext _database;

        public Handler(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            TokenService tokenService,
            AppDbContext database)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _database = database;
        }

        public async Task<Result> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            var email = request.Email.Trim();

            var user = await _userManager.FindByEmailAsync(email);

            if (user is null)
            {
                return Result.Invalid();
            }
            
            //here is the lockout used
            //verify the creds
            var signInResult =
                await _signInManager.CheckPasswordSignInAsync(
                    user,
                    request.Password,
                    lockoutOnFailure: true);

            if (!signInResult.Succeeded)
            {
                return Result.Invalid();
            }

            //generate tokens
            var issued = _tokenService.IssueTokens(
                user,
                request.IpAddress);

            //add refreshtoken hash to db
            _database.RefreshTokens.Add(
                issued.RefreshToken);

            await _database.SaveChangesAsync(
                cancellationToken);

            return new Result
            {
                Succeeded = true,

                Response = new Response(
                    issued.AccessToken,
                    issued.AccessTokenExpiresAtUtc,
                    new UserResponse(
                        user.Id,
                        user.Email!,
                        user.DisplayName)),

                RawRefreshToken =
                    issued.RawRefreshToken,

                RefreshTokenExpiresAtUtc =
                    issued.RefreshToken.ExpiresAtUtc
            };
        }
    }

    public static void MapEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/api/v1/auth/login",
            async (
                Request request,
                HttpContext httpContext,
                ISender sender,
                IHostEnvironment environment,
                CancellationToken cancellationToken) =>
            {
                var command = new Command(
                    request.Email,
                    request.Password,
                    httpContext.Connection
                        .RemoteIpAddress?
                        .ToString());

                var result = await sender.Send(
                    command,
                    cancellationToken);

                if (!result.Succeeded)
                {
                    return Results.Unauthorized();
                }

                httpContext.Response.Cookies.Append(
                    RefreshCookieName,
                    result.RawRefreshToken!,
                    new CookieOptions
                    {
                        HttpOnly = true,

                        Secure =
                            !environment.IsDevelopment(),

                        SameSite = SameSiteMode.Strict,

                        Path = "/api/v1/auth",

                        Expires =
                            result.RefreshTokenExpiresAtUtc,

                        IsEssential = true
                    });

                return Results.Ok(result.Response);
            });
    }
}