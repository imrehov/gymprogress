using GymProgress.Api.Data;
using GymProgress.Api;

using MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionString));

//identity stuff
builder.Services.AddIdentityCore<AppUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    //add signinmanager, it supports the lockout system from identity
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
    .AddRoles<IdentityRole<Guid>>()
    .AddSignInManager()
    .AddEntityFrameworkStores<AppDbContext>();


//mediatr and fluentvalidation

/*
request flow:
Endpoint
    -> MediatR
        -> ValidationBehavior
            -> Register.Handler
*/

//this adds register.handler
builder.Services.AddMediatR(configuration =>
    configuration.RegisterServicesFromAssemblyContaining<Program>());

//this adds register.validator
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

//this inserts validationbehavior around every mediatr request
builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>),
    typeof(ValidationBehavior<,>));

//adding validationexception handling
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();


builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");


//jwt registering and validation

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(
        JwtOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.Issuer),
        "JWT issuer is required.")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.Audience),
        "JWT audience is required.")
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(options.SigningKey) &&
            options.SigningKey.Length >= 64,
        "JWT signing key must contain at least 32 random bytes encoded as hexadecimal.")
    .Validate(
        options => options.AccessTokenMinutes > 0,
        "JWT access-token lifetime must be positive.")
    .Validate(
        options => options.RefreshTokenDays > 0,
        "Refresh-token lifetime must be positive.")
    .ValidateOnStart();

// jwt bearer validation config
builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services
    .AddOptions<JwtBearerOptions>(
        JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>(
        (bearer, jwtOptions) =>
        {
            var jwt = jwtOptions.Value;

            bearer.MapInboundClaims = false;

            bearer.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Convert.FromHexString(
                                jwt.SigningKey)),

                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,

                    ClockSkew = TimeSpan.FromSeconds(30),

                    NameClaimType = "display_name"
                };
        });

builder.Services.AddAuthorization();

//replaceable clock so tests dont have to call datetime.utcnow
builder.Services.AddSingleton(TimeProvider.System);


//register tokenservice
builder.Services.AddSingleton<TokenService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.UseHttpsRedirection();
app.MapHealthChecks("/api/health");

//activate the validationexception handler
app.UseExceptionHandler();

//after jwt is set up; authentication must be before authorization
app.UseAuthentication();
app.UseAuthorization();

Register.MapEndpoint(app);


app.Run();

public partial class Program
{
    
}