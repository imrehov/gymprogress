using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace GymProgress.Api;

public static class Register
{
    public record Command(string Email, string Password, string DisplayName) : IRequest<Result>;

    public record Response(Guid Id, string Email, string DisplayName);

    public record Result(bool Succeded, Response? Data=null, IEnumerable<string>? Errors=null);

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
                var errors = identityResult.Errors.Select(e => e.Description);
                return new Result(false, Errors: errors);
            }

            var response = new Response(user.Id, user.Email, user.DisplayName);

            return new Result(true, Data: response);

        }
    }
}