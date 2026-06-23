namespace Bondstone.Diagnostics;

public static class BondstoneSetupCodes
{
    public const string MissingModulePersistence = "bondstone.setup.missing_module_persistence";
    public const string MissingEfMapping = "bondstone.setup.missing_ef_mapping";
    public const string MissingOutboxPersistence = "bondstone.setup.missing_outbox_persistence";
    public const string MissingDispatcher = "bondstone.setup.missing_dispatcher";
    public const string DuplicateDurableRegistration = "bondstone.setup.duplicate_durable_registration";
    public const string InvalidDurableIdentity = "bondstone.setup.invalid_durable_identity";
    public const string MissingReceiveBinding = "bondstone.setup.missing_receive_binding";
    public const string AmbiguousDispatchRoute = "bondstone.setup.ambiguous_dispatch_route";
}
