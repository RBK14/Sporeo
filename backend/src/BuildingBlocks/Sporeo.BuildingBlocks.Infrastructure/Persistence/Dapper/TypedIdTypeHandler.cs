using System.Collections.Concurrent;
using System.Data;
using Dapper;
using Sporeo.BuildingBlocks.Domain.Models;

namespace Sporeo.BuildingBlocks.Infrastructure.Persistence.Dapper;

/// <summary>
/// Dapper type handler that maps a <see cref="TypedIdBase"/> subtype to and from <see cref="Guid"/>.
/// </summary>
/// <typeparam name="TId">The strongly typed identifier type.</typeparam>
public sealed class TypedIdTypeHandler<TId> : SqlMapper.TypeHandler<TId>
    where TId : TypedIdBase
{
    private readonly Func<Guid, TId> _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypedIdTypeHandler{TId}"/> class.
    /// </summary>
    /// <param name="factory">Factory that creates a typed ID from a GUID.</param>
    public TypedIdTypeHandler(Func<Guid, TId> factory)
    {
        _factory = factory;
    }

    /// <inheritdoc />
    public override void SetValue(IDbDataParameter parameter, TId? value)
    {
        parameter.DbType = DbType.Guid;
        parameter.Value = value is null ? DBNull.Value : value.Value;
    }

    /// <inheritdoc />
    public override TId Parse(object value) => value switch
    {
        Guid guid => _factory(guid),
        string text when Guid.TryParse(text, out var parsed) => _factory(parsed),
        _ => throw new DataException($"Cannot convert {value.GetType().Name} to {typeof(TId).Name}.")
    };
}

/// <summary>
/// Registers Dapper type handlers for strongly typed identifiers shared across modules.
/// </summary>
public static class TypedIdTypeHandlerRegistration
{
    private static readonly ConcurrentDictionary<Type, byte> RegisteredTypes = new();

    /// <summary>
    /// Registers a Dapper type handler for <typeparamref name="TId"/> if one is not already registered.
    /// </summary>
    /// <typeparam name="TId">The strongly typed identifier type.</typeparam>
    /// <param name="factory">Factory that creates a typed ID from a GUID.</param>
    public static void Register<TId>(Func<Guid, TId> factory)
        where TId : TypedIdBase
    {
        ArgumentNullException.ThrowIfNull(factory);

        if (!RegisteredTypes.TryAdd(typeof(TId), 0))
        {
            return;
        }

        SqlMapper.AddTypeHandler(new TypedIdTypeHandler<TId>(factory));
    }
}
