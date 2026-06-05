namespace Bondstone.Utility;

public static class StringExtensions
{
    public static string? NormalizeOptional(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    public static string NormalizeRequired(this string? value, string parameterName, string valueName = "Value")
    {
        string? normalized = value.NormalizeOptional();
        return normalized ?? throw new ArgumentException($"{valueName} is required.", parameterName);
    }
}
