namespace Bondstone.Messaging;

/// <summary>
/// Marker for an immediate read-only module query.
/// </summary>
/// <typeparam name="TResult">The result type returned by the query handler.</typeparam>
public interface IQuery<TResult>
{
}
