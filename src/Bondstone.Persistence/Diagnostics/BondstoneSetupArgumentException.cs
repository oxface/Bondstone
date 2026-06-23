namespace Bondstone.Diagnostics;

public sealed class BondstoneSetupArgumentException : ArgumentException, IBondstoneSetupException
{
    public BondstoneSetupArgumentException(
        string setupCode,
        string message,
        string? paramName)
        : base(message, paramName)
    {
        SetupCode = ValidateSetupCode(setupCode);
    }

    public string SetupCode { get; }

    private static string ValidateSetupCode(string setupCode)
    {
        return string.IsNullOrWhiteSpace(setupCode)
            ? throw new ArgumentException("Setup code is required.", nameof(setupCode))
            : setupCode;
    }
}
