namespace Bondstone.Diagnostics;

public sealed class BondstoneSetupException : InvalidOperationException, IBondstoneSetupException
{
    public BondstoneSetupException(
        string setupCode,
        string message)
        : base(message)
    {
        SetupCode = ValidateSetupCode(setupCode);
    }

    public BondstoneSetupException(
        string setupCode,
        string message,
        Exception innerException)
        : base(message, innerException)
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
