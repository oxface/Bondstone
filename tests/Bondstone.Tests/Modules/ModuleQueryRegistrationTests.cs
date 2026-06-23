using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Tests.Modules;

public sealed class ModuleQueryRegistrationTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ModuleQueries_WhenHandlerIsRegistered_ExecutesHandlerAndReturnsResult()
    {
        var services = new ServiceCollection();
        services.AddSingleton<QueryCallLog>();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.Queries.RegisterHandler<GetOrderStatusQuery, OrderStatus, GetOrderStatusHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        OrderStatus result = await scope.ServiceProvider
            .GetRequiredService<IModuleQueryExecutor>()
            .ExecuteAsync(
                " sales ",
                new GetOrderStatusQuery("order-123"));

        Assert.Equal(new OrderStatus("order-123", "accepted"), result);
        Assert.Equal(
            ["query:order-123"],
            serviceProvider.GetRequiredService<QueryCallLog>().Calls);

        IModuleQueryRouteRegistry routeRegistry =
            serviceProvider.GetRequiredService<IModuleQueryRouteRegistry>();
        ModuleQueryRoute route = routeRegistry.GetByQueryType(
            "sales",
            typeof(GetOrderStatusQuery));

        Assert.Equal("sales", route.ModuleName);
        Assert.Equal(typeof(GetOrderStatusQuery), route.QueryType);
        Assert.Equal(typeof(GetOrderStatusHandler), route.HandlerType);
        Assert.Equal(typeof(OrderStatus), route.ResultType);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ModuleQueries_WhenRouteIsMissing_DiagnosticNamesModuleAndQueryType()
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", _ => { });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        KeyNotFoundException exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await scope.ServiceProvider
                .GetRequiredService<IModuleQueryExecutor>()
                .ExecuteAsync(
                    " sales ",
                    new GetOrderStatusQuery("order-123")));

        Assert.Contains("No query route is registered", exception.Message, StringComparison.Ordinal);
        Assert.Contains("module 'sales'", exception.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(GetOrderStatusQuery).FullName!, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("durable", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void RegisterHandler_WhenQueryRouteAlreadyExists_Throws()
    {
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
                bondstone.Module("sales", module =>
                {
                    module.Queries.RegisterHandler<GetOrderStatusQuery, OrderStatus, GetOrderStatusHandler>();
                    module.Queries.RegisterHandler<
                        GetOrderStatusQuery,
                        OrderStatus,
                        AlternateGetOrderStatusHandler>();
                });
            }));

        Assert.Contains("already has a query route", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Module 'sales'", exception.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(GetOrderStatusQuery).FullName!, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ModuleQueries_WhenExecuting_DoesNotSetCommandModuleExecutionContext()
    {
        var services = new ServiceCollection();
        services.AddSingleton<QueryCallLog>();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.Queries.RegisterHandler<InspectQueryContextQuery, string, InspectQueryContextHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IModuleExecutionContextAccessor accessor =
            serviceProvider.GetRequiredService<IModuleExecutionContextAccessor>();

        Assert.Null(accessor.Current);

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            string result = await scope.ServiceProvider
                .GetRequiredService<IModuleQueryExecutor>()
                .ExecuteAsync(
                    "sales",
                    new InspectQueryContextQuery());

            Assert.Equal("none", result);
        }

        Assert.Equal(
            ["context:none"],
            serviceProvider.GetRequiredService<QueryCallLog>().Calls);
        Assert.Null(accessor.Current);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ModuleQueries_WhenInvokedFromCommandHandler_DoNotInheritCommandModuleExecutionContext()
    {
        var outboxWriter = new CapturingOutboxWriter();
        var services = new ServiceCollection();
        services.AddSingleton<IDurableOutboxWriter>(outboxWriter);

        services.AddBondstone(bondstone =>
        {
            bondstone.RegisterMessage<ReserveOrderCommand>();
            bondstone.Module("sales", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<
                    CommandInvokesQueryThatAttemptsDurableSend,
                    CommandInvokesQueryThatAttemptsDurableSendHandler>();
                module.Queries.RegisterHandler<
                    QueryAttemptsDurableSend,
                    string,
                    QueryAttemptsDurableSendHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await scope.ServiceProvider
                .GetRequiredService<IModuleCommandExecutor>()
                .ExecuteAsync(
                    "sales",
                    new CommandInvokesQueryThatAttemptsDurableSend("order-123")));

        Assert.Contains("module execution context", exception.Message, StringComparison.Ordinal);
        Assert.Empty(outboxWriter.Envelopes);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ModuleQueries_WhenHandlerAttemptsDurableSend_RejectsBecauseQueryIsReadOnly()
    {
        var outboxWriter = new CapturingOutboxWriter();
        var services = new ServiceCollection();
        services.AddSingleton<IDurableOutboxWriter>(outboxWriter);

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Queries.RegisterHandler<
                    QueryAttemptsDurableSend,
                    string,
                    QueryAttemptsDurableSendHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await scope.ServiceProvider
                .GetRequiredService<IModuleQueryExecutor>()
                .ExecuteAsync(
                    "sales",
                    new QueryAttemptsDurableSend("order-123")));

        Assert.Contains("module execution context", exception.Message, StringComparison.Ordinal);
        Assert.Empty(outboxWriter.Envelopes);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ModuleQueries_WhenHandlerAttemptsDurablePublish_RejectsBecauseQueryIsReadOnly()
    {
        var outboxWriter = new CapturingOutboxWriter();
        var services = new ServiceCollection();
        services.AddSingleton<IDurableOutboxWriter>(outboxWriter);

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Events.RegisterPublishedEvent<OrderViewedEvent>();
                module.Queries.RegisterHandler<
                    QueryAttemptsDurablePublish,
                    string,
                    QueryAttemptsDurablePublishHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await scope.ServiceProvider
                .GetRequiredService<IModuleQueryExecutor>()
                .ExecuteAsync(
                    "sales",
                    new QueryAttemptsDurablePublish("order-123")));

        Assert.Contains("module execution context", exception.Message, StringComparison.Ordinal);
        Assert.Empty(outboxWriter.Envelopes);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RegisterFromAssemblyContaining_WhenQueryHandlerExists_RegistersRoute()
    {
        var services = new ServiceCollection();
        services.AddSingleton<QueryCallLog>();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.Queries.RegisterFromAssemblyContaining<GetAssemblyOnlyOrderQuery>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        OrderStatus result = await scope.ServiceProvider
            .GetRequiredService<IModuleQueryExecutor>()
            .ExecuteAsync(
                "sales",
                new GetAssemblyOnlyOrderQuery("order-456"));

        Assert.Equal(new OrderStatus("order-456", "accepted"), result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void RegisterFromAssembly_WhenOpenGenericQueryHandlerExists_IgnoresHandler()
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.Queries.RegisterFromAssembly(typeof(OpenGenericQueryHandler<,>).Assembly);
            });
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IModuleQueryRouteRegistry routeRegistry =
            serviceProvider.GetRequiredService<IModuleQueryRouteRegistry>();

        Assert.DoesNotContain(
            routeRegistry.Routes,
            route => route.HandlerType.IsGenericTypeDefinition);
    }

    public sealed class QueryCallLog
    {
        public List<string> Calls { get; } = [];
    }

    public sealed record GetOrderStatusQuery(string OrderId) : IQuery<OrderStatus>;

    public sealed record OrderStatus(string OrderId, string Status);

    public sealed class GetOrderStatusHandler(QueryCallLog log)
        : IQueryHandler<GetOrderStatusQuery, OrderStatus>
    {
        public ValueTask<OrderStatus> HandleAsync(
            GetOrderStatusQuery query,
            CancellationToken ct = default)
        {
            log.Calls.Add($"query:{query.OrderId}");
            return ValueTask.FromResult(new OrderStatus(query.OrderId, "accepted"));
        }
    }

    public abstract class AlternateGetOrderStatusHandler
        : IQueryHandler<GetOrderStatusQuery, OrderStatus>
    {
        public ValueTask<OrderStatus> HandleAsync(
            GetOrderStatusQuery query,
            CancellationToken ct = default)
        {
            return ValueTask.FromResult(new OrderStatus(query.OrderId, "alternate"));
        }
    }

    public sealed record GetAssemblyOnlyOrderQuery(string OrderId) : IQuery<OrderStatus>;

    public sealed class GetAssemblyOnlyOrderHandler
        : IQueryHandler<GetAssemblyOnlyOrderQuery, OrderStatus>
    {
        public ValueTask<OrderStatus> HandleAsync(
            GetAssemblyOnlyOrderQuery query,
            CancellationToken ct = default)
        {
            return ValueTask.FromResult(new OrderStatus(query.OrderId, "accepted"));
        }
    }

    public sealed class OpenGenericQueryHandler<TQuery, TResult>
        : IQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        public ValueTask<TResult> HandleAsync(
            TQuery query,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }
    }

    public sealed record InspectQueryContextQuery : IQuery<string>;

    public sealed class InspectQueryContextHandler(
        IModuleExecutionContextAccessor executionContextAccessor,
        QueryCallLog log)
        : IQueryHandler<InspectQueryContextQuery, string>
    {
        public ValueTask<string> HandleAsync(
            InspectQueryContextQuery query,
            CancellationToken ct = default)
        {
            string moduleName = executionContextAccessor.Current?.ModuleName ?? "none";
            log.Calls.Add($"context:{moduleName}");
            return ValueTask.FromResult(moduleName);
        }
    }

    public sealed record CommandInvokesQueryThatAttemptsDurableSend(string OrderId) : ICommand;

    public sealed class CommandInvokesQueryThatAttemptsDurableSendHandler(
        IModuleQueryExecutor queryExecutor)
        : ICommandHandler<CommandInvokesQueryThatAttemptsDurableSend>
    {
        public async ValueTask HandleAsync(
            CommandInvokesQueryThatAttemptsDurableSend command,
            CancellationToken ct = default)
        {
            await queryExecutor.ExecuteAsync(
                "sales",
                new QueryAttemptsDurableSend(command.OrderId),
                ct);
        }
    }

    public sealed record QueryAttemptsDurableSend(string OrderId) : IQuery<string>;

    public sealed class QueryAttemptsDurableSendHandler(IDurableCommandSender sender)
        : IQueryHandler<QueryAttemptsDurableSend, string>
    {
        public async ValueTask<string> HandleAsync(
            QueryAttemptsDurableSend query,
            CancellationToken ct = default)
        {
            await sender.SendAsync(
                new ReserveOrderCommand(query.OrderId),
                "fulfillment",
                ct);
            return query.OrderId;
        }
    }

    public sealed record QueryAttemptsDurablePublish(string OrderId) : IQuery<string>;

    public sealed class QueryAttemptsDurablePublishHandler(IDurableEventPublisher publisher)
        : IQueryHandler<QueryAttemptsDurablePublish, string>
    {
        public async ValueTask<string> HandleAsync(
            QueryAttemptsDurablePublish query,
            CancellationToken ct = default)
        {
            await publisher.PublishAsync(
                new OrderViewedEvent(query.OrderId),
                ct);
            return query.OrderId;
        }
    }

    [DurableCommandIdentity("query-tests.fulfillment.order.reserve.v1")]
    public sealed record ReserveOrderCommand(string OrderId) : IDurableCommand;

    [IntegrationEventIdentity("sales.order.viewed.v1")]
    public sealed record OrderViewedEvent(string OrderId) : IIntegrationEvent;

    private sealed class CapturingOutboxWriter : IDurableOutboxWriter
    {
        public List<DurableMessageEnvelope> Envelopes { get; } = [];

        public ValueTask WriteAsync(
            DurableMessageEnvelope envelope,
            CancellationToken ct = default)
        {
            Envelopes.Add(envelope);
            return ValueTask.CompletedTask;
        }
    }
}
