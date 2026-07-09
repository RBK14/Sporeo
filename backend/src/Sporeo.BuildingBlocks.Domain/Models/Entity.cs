using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.BuildingBlocks.Domain.Rules;

namespace Sporeo.BuildingBlocks.Domain.Models;

/// <summary>
/// Base class for domain entities identified by a strongly typed identifier.
/// Provides identity-based equality.
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    /// <summary>
    /// Gets the unique identifier of the entity.
    /// </summary>
    public TId Id { get; protected set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Entity{TId}"/> class with the specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    protected Entity(TId id)
    {
        Id = id;
    }

    /// <summary>
    /// Determines whether the entity has not yet been assigned a persistent identifier.
    /// </summary>
    /// <returns><see langword="true"/> when the identifier is the default value; otherwise, <see langword="false"/>.</returns>
    protected bool IsTransient() => EqualityComparer<TId>.Default.Equals(Id, default!);

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        if (obj is not Entity<TId> other) return false;

        if (IsTransient() || other.IsTransient())
            return ReferenceEquals(this, other);

        return Id.Equals(other.Id);
    }

    /// <inheritdoc />
    public bool Equals(Entity<TId>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (other.GetType() != GetType()) return false;

        if (IsTransient() || other.IsTransient())
            return ReferenceEquals(this, other);

        return Id.Equals(other.Id);
    }

    /// <summary>
    /// Determines whether two entities are equal based on their identifiers and runtime type.
    /// </summary>
    /// <param name="left">The first entity to compare.</param>
    /// <param name="right">The second entity to compare.</param>
    /// <returns><see langword="true"/> if the entities are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;

        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two entities are not equal.
    /// </summary>
    /// <param name="left">The first entity to compare.</param>
    /// <param name="right">The second entity to compare.</param>
    /// <returns><see langword="true"/> if the entities are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !(left == right);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        if (IsTransient())
            return base.GetHashCode();

        return HashCode.Combine(GetType(), Id);
    }

    /// <summary>
    /// Evaluates a single business rule and returns a failed result when it is broken.
    /// </summary>
    /// <param name="rule">The business rule to evaluate.</param>
    /// <returns>A successful result when the rule is satisfied; otherwise, a failure with the rule error.</returns>
    protected static Result CheckRule(IBusinessRule rule) =>
        rule.IsBroken()
            ? Result.Failure(rule.Error)
            : Result.Success();

    /// <summary>
    /// Evaluates multiple business rules in order and returns the first failure encountered.
    /// </summary>
    /// <param name="rules">The business rules to evaluate.</param>
    /// <returns>A successful result when all rules are satisfied; otherwise, the first failure.</returns>
    protected static Result CheckRules(params IBusinessRule[] rules)
    {
        foreach (var rule in rules)
        {
            var result = CheckRule(rule);
            if (result.IsFailure)
                return result;
        }

        return Result.Success();
    }

#pragma warning disable CS8618
    /// <summary>
    /// Initializes a new instance of the <see cref="Entity{TId}"/> class.
    /// Intended for use by object-relational mappers.
    /// </summary>
    protected Entity() { }
#pragma warning restore CS8618
}
