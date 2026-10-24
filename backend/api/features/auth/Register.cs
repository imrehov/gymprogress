using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace GymProgress.Api;

public static class Register
{
    public record Command(string Email, string Password, string DisplayName) : IRequest<Result>;

    public record Response(Guid Id, string Email, string DisplayName);

    public record Result(bool Succeeded, Response? Data=null, Dictionary<string, string[]>? Errors=null);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(256);

            //identity handles pw rules
            RuleFor(x => x.Password)
                .NotEmpty();

            RuleFor(x => x.DisplayName)
                .NotEmpty()
                .MaximumLength(50);
        }
    }

    public class Handler : IRequestHandler<Command, Result>
    {
        private readonly UserManager<AppUser> _userManager;

        public Handler(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var user = new AppUser
            {
                UserName = request.Email.Trim(),
                Email = request.Email.Trim(),
                DisplayName = request.DisplayName.Trim(),
            };

            var identityResult = await _userManager.CreateAsync(user, request.Password);

            if (!identityResult.Succeeded)
            {
                //changed IEnumerable<string>? Errors to Dictionary<string, string[]>? Errors
                //this matches validationexcepiton shape
                var errors = identityResult.Errors
                    .GroupBy(error => error.Code)
                    .ToDictionary(
                        group => group.Key,
                        group => group
                            .Select(error => error.Description)
                            .Distinct()
                            .ToArray());
                
                return new Result(false, Errors: errors);
            }

            var response = new Response(user.Id, user.Email, user.DisplayName);

            return new Result(true, Data: response);

        }
    }

    public static void MapEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/auth/register", async (Command command, ISender sender) =>
        {
            var result = await sender.Send(command);

            return result.Succeeded
                ? Results.Created($"/api/v1/users/{result.Data!.Id}", result.Data)
                : Results.ValidationProblem(result.Errors!);
        });
    }
}