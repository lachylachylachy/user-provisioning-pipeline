using EntraFlow.Core.Models;

namespace EntraFlow.Core.Io;

/// <summary>
/// Reads user records from a provisioning source. The CSV implementation is the
/// default today; this seam lets other sources (e.g. a database) be added later.
/// </summary>
public interface IUserReader
{
    IReadOnlyList<UserRecord> Read(string path);
}
