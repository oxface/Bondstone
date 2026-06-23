using Microsoft.Extensions.Options;

namespace Bondstone.Hosting.IncomingInbox;

internal sealed class DurableIncomingInboxWorkerOptionsValidator
    : IValidateOptions<DurableIncomingInboxWorkerOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        DurableIncomingInboxWorkerOptions options)
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
