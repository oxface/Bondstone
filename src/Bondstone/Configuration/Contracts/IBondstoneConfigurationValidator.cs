namespace Bondstone.Configuration;

public interface IBondstoneConfigurationValidator
{
    void Validate(BondstoneConfigurationValidationContext context);
}
