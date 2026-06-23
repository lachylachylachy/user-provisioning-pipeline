using EntraFlow.Core.Io;

namespace EntraFlow.Core.Tests;

public class CsvUserReaderTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"entraflow-{Guid.NewGuid():N}.csv");

    private void WriteCsv(params string[] lines) => File.WriteAllLines(_path, lines);

    [Fact]
    public void Read_SkipsHeader_AndParsesRows()
    {
        WriteCsv(
            "Name,Email,Department,Role",
            "Jane Doe,jane@company.com,IT,Admin",
            "John Roe,john@company.com,HR,User");

        var users = new CsvUserReader().Read(_path);

        Assert.Equal(2, users.Count);
        Assert.Equal("Jane Doe", users[0].Name);
        Assert.Equal("john@company.com", users[1].Email);
        Assert.Equal(2, users[0].LineNumber);
    }

    [Fact]
    public void Read_SkipsBlankLines()
    {
        WriteCsv(
            "Name,Email,Department,Role",
            "",
            "Jane Doe,jane@company.com,IT,Admin");

        var users = new CsvUserReader().Read(_path);

        Assert.Single(users);
    }

    [Fact]
    public void Read_TreatsMissingTrailingColumnsAsEmpty()
    {
        WriteCsv(
            "Name,Email,Department,Role",
            "Jane Doe,jane@company.com");

        var users = new CsvUserReader().Read(_path);

        Assert.Equal("", users[0].Department);
        Assert.Equal("", users[0].Role);
    }

    [Fact]
    public void Read_HandlesQuotedFieldWithComma()
    {
        WriteCsv(
            "Name,Email,Department,Role",
            "\"Doe, Jane\",jane@company.com,IT,Admin");

        var users = new CsvUserReader().Read(_path);

        Assert.Equal("Doe, Jane", users[0].Name);
    }

    [Fact]
    public void Read_MissingFile_Throws()
    {
        Assert.Throws<FileNotFoundException>(() => new CsvUserReader().Read("does-not-exist.csv"));
    }

    public void Dispose()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }
}
