namespace EntraFlow.Core.Logging;

/// <summary>
/// Helpers for masking personally identifiable information before it reaches logs.
/// Provisioning touches identity data, so emails/UPNs are partially redacted in log
/// output (full values still appear in the authenticated UI and downloads).
/// </summary>
public static class Pii
{
    /// <summary>
    /// Masks an email/UPN, keeping a short prefix and the domain:
    /// <c>jane.doe@company.com</c> → <c>ja****@company.com</c>. Non-email values keep
    /// only their first character.
    /// </summary>
    public static string Mask(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        var at = value.IndexOf('@');
        if (at > 0)
        {
            var local = value[..at];
            var domain = value[at..];
            var shown = local.Length <= 2 ? local[..1] : local[..2];
            return shown + new string('*', Math.Max(1, local.Length - shown.Length)) + domain;
        }

        return value.Length <= 2 ? "*" : value[..1] + new string('*', value.Length - 1);
    }
}
