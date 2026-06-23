using EntraFlow.Core.Models;
using EntraFlow.Core.Validation;

namespace EntraFlow.Core.Tests;

public class UserValidatorTests
{
    private static UserRecord User(
        string name = "Jane Doe",
        string email = "jane.doe@company.com",
        string department = "IT",
        string role = "Admin") =>
        new() { Name = name, Email = email, Department = department, Role = role };

    private static List<ValidationResult> Validate(params UserRecord[] users) =>
        new UserValidator().Validate(users).ToList();

    [Fact]
    public void ValidRecord_HasNoErrors()
    {
        var result = Validate(User()).Single();

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("", "Missing Name")]
    [InlineData("   ", "Missing Name")]
    public void MissingName_IsRejected(string name, string expected)
    {
        var result = Validate(User(name: name)).Single();

        Assert.False(result.IsValid);
        Assert.Contains(expected, result.Errors);
    }

    [Fact]
    public void MissingEmail_IsRejected()
    {
        var result = Validate(User(email: "")).Single();

        Assert.Contains("Missing Email", result.Errors);
    }

    [Fact]
    public void MissingDepartment_IsRejected()
    {
        var result = Validate(User(department: "")).Single();

        Assert.Contains("Missing Department", result.Errors);
    }

    [Fact]
    public void MissingRole_IsRejected()
    {
        var result = Validate(User(role: "")).Single();

        Assert.Contains("Missing Role", result.Errors);
    }

    [Fact]
    public void MultipleMissingFields_AreAllReported()
    {
        var result = Validate(User(name: "", role: "")).Single();

        Assert.Contains("Missing Name", result.Errors);
        Assert.Contains("Missing Role", result.Errors);
    }

    [Fact]
    public void DuplicateEmail_SecondOccurrenceIsRejected()
    {
        var results = Validate(
            User(email: "dup@company.com"),
            User(email: "dup@company.com"));

        Assert.True(results[0].IsValid);
        Assert.False(results[1].IsValid);
        Assert.Contains("Duplicate Email", results[1].Errors);
    }

    [Fact]
    public void DuplicateEmail_IsCaseInsensitive()
    {
        var results = Validate(
            User(email: "Dup@Company.com"),
            User(email: "dup@company.com"));

        Assert.False(results[1].IsValid);
        Assert.Contains("Duplicate Email", results[1].Errors);
    }

    [Fact]
    public void MissingEmail_DoesNotCountAsDuplicate()
    {
        var results = Validate(User(email: ""), User(email: ""));

        Assert.All(results, r => Assert.DoesNotContain("Duplicate Email", r.Errors));
    }
}
