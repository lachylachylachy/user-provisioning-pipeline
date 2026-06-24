using EntraFlow.Core.Configuration;
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
        UserRecord.FromCoreFields(name, email, department, role);

    private static List<ValidationResult> Validate(params UserRecord[] users) =>
        new UserValidator().Validate(users).ToList();

    private static List<ValidationResult> Validate(SchemaOptions schema, params UserRecord[] users) =>
        new UserValidator(schema).Validate(users).ToList();

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

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@domain")]
    [InlineData("@company.com")]
    [InlineData("spaces in@email.com")]
    public void InvalidEmailFormat_IsRejected(string email)
    {
        var result = Validate(User(email: email)).Single();

        Assert.False(result.IsValid);
        Assert.Contains("Invalid Email format", result.Errors);
    }

    [Theory]
    [InlineData("jane.doe@company.com")]
    [InlineData("a.b-c@sub.domain.co.uk")]
    public void ValidEmailFormat_IsAccepted(string email)
    {
        var result = Validate(User(email: email)).Single();

        Assert.True(result.IsValid);
    }

    [Fact]
    public void AllowedValues_RejectValuesOutsideWhitelist()
    {
        var schema = new SchemaOptions
        {
            Fields =
            [
                new FieldRule { Name = "Name", Required = true },
                new FieldRule { Name = "Email", Required = true, Format = FieldFormat.Email },
                new FieldRule { Name = "Department", Required = true },
                new FieldRule
                {
                    Name = "Role",
                    Required = true,
                    AllowedValues = ["Admin", "User", "Manager"],
                },
            ],
        };

        var rejected = Validate(schema, User(role: "Wizard")).Single();
        var accepted = Validate(schema, User(role: "admin")).Single(); // case-insensitive

        Assert.Contains("Invalid Role value", rejected.Errors);
        Assert.True(accepted.IsValid);
    }

    [Fact]
    public void ExtraFields_AreValidatedWhenInSchema()
    {
        var schema = new SchemaOptions
        {
            Fields =
            [
                new FieldRule { Name = "Email", Required = true, Format = FieldFormat.Email },
                new FieldRule { Name = "EmployeeId", Required = true },
            ],
            UniqueField = "Email",
        };

        var record = new UserRecord(new Dictionary<string, string>
        {
            ["Email"] = "jane@company.com",
            ["EmployeeId"] = "",
        });

        var result = Validate(schema, record).Single();

        Assert.Contains("Missing EmployeeId", result.Errors);
    }

    [Fact]
    public void UniqueField_CanBeDisabled()
    {
        var schema = SchemaOptions.Default;
        schema.UniqueField = null;

        var results = Validate(
            schema,
            User(email: "dup@company.com"),
            User(email: "dup@company.com"));

        Assert.All(results, r => Assert.True(r.IsValid));
    }
}
