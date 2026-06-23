using Azure.Messaging.ServiceBus;
using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Transport.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Bondstone.Transport.ServiceBus.Tests;

public sealed class ServiceBusBuilderTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void UseServiceBusDispatcher_WhenConfigured_RegistersEnvelopeDispatcher()
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            bondstone.Outbox.MarkPersistenceProvider("test persistence");
            bondstone.UseServiceBusDispatcher(options =>
            {
                options.ResolveEntityName = static _ => "fulfillment-commands";
            });
            bondstone.Outbox.MarkDispatcher("test dispatcher");
        });

        ServiceDescriptor descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(IDurableEnvelopeDispatcher));
        Assert.Equal(
            "Bondstone.Transport.ServiceBus.ServiceBusEnvelopeDispatcher",
            descriptor.ImplementationType?.FullName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseServiceBusReceiveWorker_WhenEntityIsMissing_Throws()
    {
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
                bondstone.UseServiceBusReceiveWorker(static options =>
                    options.ReceiveCommand())));

        Assert.Contains("QueueName", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ReceiveWorkerOptions_DefaultProcessorOptionsUseManualCompletion()
    {
        var options = new ServiceBusReceiveWorkerOptions();

        Assert.False(options.ProcessorOptions.AutoCompleteMessages);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseServiceBusReceiveWorker_WhenAutoCompleteMessagesIsEnabled_Throws()
    {
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
                bondstone.UseServiceBusReceiveWorker(static options =>
                {
                    options.QueueName = "fulfillment.commands";
                    options.ProcessorOptions.AutoCompleteMessages = true;
                    options.ReceiveCommand();
                })));

        Assert.Contains("AutoCompleteMessages", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseServiceBusReceiveWorker_WhenReceiveAndDeleteModeIsEnabled_Throws()
    {
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
                bondstone.UseServiceBusReceiveWorker(static options =>
                {
                    options.QueueName = "fulfillment.commands";
                    options.ProcessorOptions.ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete;
                    options.ReceiveCommand();
                })));

        Assert.Contains("ReceiveMode", exception.Message, StringComparison.Ordinal);
        Assert.Contains("PeekLock", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseServiceBusReceiveWorker_WhenOptionsAreMutatedAfterRegistration_StoresProcessorOptionsCopy()
    {
        var services = new ServiceCollection();
        ServiceBusProcessorOptions? capturedOptions = null;

        services.AddBondstone(bondstone =>
        {
            bondstone.UseServiceBusReceiveWorker(options =>
            {
                options.QueueName = "fulfillment.commands";
                options.ProcessorOptions.MaxConcurrentCalls = 7;
                options.ReceiveCommand();
                capturedOptions = options.ProcessorOptions;
            });
        });

        Assert.NotNull(capturedOptions);
        capturedOptions.AutoCompleteMessages = true;
        capturedOptions.MaxConcurrentCalls = 13;

        object registration = Assert.Single(
                services,
                service => service.ImplementationInstance?.GetType().FullName
                    == "Bondstone.Transport.ServiceBus.ServiceBusReceiveWorkerRegistration")
            .ImplementationInstance!;
        ServiceBusProcessorOptions registeredOptions =
            Assert.IsType<ServiceBusProcessorOptions>(
                registration.GetType()
                    .GetProperty("ProcessorOptions")!
                    .GetValue(registration));

        Assert.False(registeredOptions.AutoCompleteMessages);
        Assert.Equal(7, registeredOptions.MaxConcurrentCalls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseServiceBusReceiveWorker_WhenConfigured_RegistersHostedWorker()
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            bondstone.UseServiceBusReceiveWorker(static options =>
            {
                options.TopicName = "integration-events";
                options.SubscriptionName = "fulfillment-order-placed";
                options.ReceiveEvent(
                    "fulfillment",
                    "fulfillment.order-placed-projection.v1");
            });
        });

        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IHostedService)
                && service.ImplementationType?.FullName
                    == "Bondstone.Transport.ServiceBus.ServiceBusReceiveWorker");
    }
}
