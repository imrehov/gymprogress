using FluentValidation.TestHelper;
using GymProgress.Api;
using NUnit.Framework;

namespace GymProgress.Tests;

[TestFixture]
public class RegisterValidatorTests
{
    private Register.Validator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new Register.Validator();
    }

    [Test]
    public void ValidCommand_ShouldPassValidation()
    {
        var command = new Register.Command("user@example.com", "Password123!", "Test User");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void EmptyOrNullEmail_ShouldHaveValidationError(string? email)
    {
        var command = new Register.Command(email!, "Password123!", "Test User");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [TestCase("notanemail")]
    [TestCase("test@")]
    [TestCase("@domain.com")]
    public void InvalidEmailFormat_ShouldHaveValidationError(string invalidEmail)
    {
        var command = new Register.Command(invalidEmail, "Password123!", "Test User");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Test]
    public void EmptyPassword_ShouldHaveValidationError()
    {
        var command = new Register.Command("user@example.com", "", "Test User");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Test]
    public void DisplayNameExceedingMaxLength_ShouldHaveValidationError()
    {
        var longName = new string('a', 51); // 50 is max
        var command = new Register.Command("user@example.com", "Password123!", longName);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DisplayName);
    }
}