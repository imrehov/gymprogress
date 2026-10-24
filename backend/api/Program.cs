using GymProgress.Api.Data;
using GymProgress.Api;

using MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionString));

builder.Services.AddIdentityCore<AppUser>(options =>
{
    options.User.RequireUniqueEmail = true;
})
    .AddRoles<IdentityRole<Guid>>()
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


builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.UseHttpsRedirection();

app.MapHealthChecks("/api/health");


Register.MapEndpoint(app);


app.Run();

public partial class Program
{
    
}