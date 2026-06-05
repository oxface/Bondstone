using Microsoft.Extensions.Options;

namespace Bondstone.Hosting.Outbox;

public sealed class DurableOutboxWorkerOptionsValidator
    : IValidateOptions<DurableOutboxWorkerOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        DurableOutboxWorkerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            options.Validate();
            return ValidateOptionsResult.Success;
        }
        catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
        {
            return ValidateOptionsResult.Fail(exception.Message);
        }
    }
}
