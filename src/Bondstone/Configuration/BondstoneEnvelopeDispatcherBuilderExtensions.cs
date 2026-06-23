using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bondstone.Configuration;

/// <summary>
/// Adds app-owned durable envelope dispatchers to Bondstone outbox composition.
/// </summary>
public static class BondstoneEnvelopeDispatcherBuilderExtensions
{
    /// <summary>
    /// Registers an app-owned durable envelope dispatcher for outbound durable messages.
    /// </summary>
    /// <typeparam name="TDispatcher">The dispatcher implementation type.</typeparam>
    /// <param name="builder">The Bondstone host builder.</param>
    /// <param name="dispatcherName">A diagnostic name for the app-owned dispatcher.</param>
    /// <param name="lifetime">The dispatcher service lifetime.</param>
    /// <returns>The same Bondstone builder for chained setup.</returns>
    public static BondstoneBuilder UseDurableEnvelopeDispatcher<TDispatcher>(
        this BondstoneBuilder builder,
        string dispatcherName = "AppOwned",
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TDispatcher : class, IDurableEnvelopeDispatcher
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Outbox.UseDurableEnvelopeDispatcher<TDispatcher>(
            dispatcherName,
            lifetime);

        return builder;
    }

    /// <summary>
    /// Registers an app-owned durable envelope dispatcher factory for outbound durable messages.
    /// </summary>
    /// <param name="builder">The Bondstone host builder.</param>
    /// <param name="factory">Creates the dispatcher from the application service provider.</param>
    /// <param name="dispatcherName">A diagnostic name for the app-owned dispatcher.</param>
    /// <param name="lifetime">The dispatcher service lifetime.</param>
    /// <returns>The same Bondstone builder for chained setup.</returns>
    public static BondstoneBuilder UseDurableEnvelopeDispatcher(
        this BondstoneBuilder builder,
        Func<IServiceProvider, IDurableEnvelopeDispatcher> factory,
        string dispatcherName = "AppOwned",
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Outbox.UseDurableEnvelopeDispatcher(
            factory,
            dispatcherName,
            lifetime);

        return builder;
    }

    /// <summary>
    /// Registers an app-owned durable envelope dispatcher for outbound durable messages.
    /// </summary>
    /// <typeparam name="TDispatcher">The dispatcher implementation type.</typeparam>
    /// <param name="outbox">The Bondstone outbox builder.</param>
    /// <param name="dispatcherName">A diagnostic name for the app-owned dispatcher.</param>
    /// <param name="lifetime">The dispatcher service lifetime.</param>
    /// <returns>The same outbox builder for chained setup.</returns>
    public static BondstoneOutboxBuilder UseDurableEnvelopeDispatcher<TDispatcher>(
        this BondstoneOutboxBuilder outbox,
        string dispatcherName = "AppOwned",
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TDispatcher : class, IDurableEnvelopeDispatcher
    {
        ArgumentNullException.ThrowIfNull(outbox);

        outbox.Services.Replace(
            ServiceDescriptor.Describe(
                typeof(IDurableEnvelopeDispatcher),
                typeof(TDispatcher),
                lifetime));
        outbox.MarkTransport(dispatcherName);

        return outbox;
    }

    /// <summary>
    /// Registers an app-owned durable envelope dispatcher factory for outbound durable messages.
    /// </summary>
    /// <param name="outbox">The Bondstone outbox builder.</param>
    /// <param name="factory">Creates the dispatcher from the application service provider.</param>
    /// <param name="dispatcherName">A diagnostic name for the app-owned dispatcher.</param>
    /// <param name="lifetime">The dispatcher service lifetime.</param>
    /// <returns>The same outbox builder for chained setup.</returns>
    public static BondstoneOutboxBuilder UseDurableEnvelopeDispatcher(
        this BondstoneOutboxBuilder outbox,
        Func<IServiceProvider, IDurableEnvelopeDispatcher> factory,
        string dispatcherName = "AppOwned",
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        ArgumentNullException.ThrowIfNull(outbox);
        ArgumentNullException.ThrowIfNull(factory);

        outbox.Services.Replace(
            ServiceDescriptor.Describe(
                typeof(IDurableEnvelopeDispatcher),
                factory,
                lifetime));
        outbox.MarkTransport(dispatcherName);

        return outbox;
    }
}
