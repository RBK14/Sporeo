using Sporeo.BuildingBlocks.Domain.Models;
using Sporeo.BuildingBlocks.Domain.Results;
using Sporeo.Fixtures.Domain.Common;
using Sporeo.Fixtures.Domain.Sports.ValueObjects;

namespace Sporeo.Fixtures.Domain.Sports;

/// <summary>
/// Represents a sport category used to classify leagues and fixtures.
/// </summary>
public sealed class Sport : AggregateRoot<SportId>, IAuditable, IDeletable
{
    /// <summary>
    /// Gets the display name of the sport.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the name of the external data provider, if the sport originated from synchronization.
    /// </summary>
    public string? ExternalProviderName { get; private set; }

    /// <summary>
    /// Gets the identifier assigned by the external data provider, if the sport originated from synchronization.
    /// </summary>
    public string? ExternalProviderId { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOn { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOn { get; private set; }

    /// <inheritdoc />
    public bool IsDeleted { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedOn { get; private set; }

    private Sport(
        SportId id,
        string name,
        string? externalProviderName,
        string? externalProviderId) : base(id)
    {
        Name = name;
        ExternalProviderName = externalProviderName;
        ExternalProviderId = externalProviderId;
        IsDeleted = false;
    }

    /// <summary>
    /// Creates a new sport.
    /// </summary>
    /// <param name="name">The display name of the sport.</param>
    /// <param name="externalProviderName">The name of the external data provider, if known.</param>
    /// <param name="externalProviderId">The identifier assigned by the external data provider, if known.</param>
    /// <returns>A successful result containing the new sport, or a failure when validation fails.</returns>
    public static Result<Sport> Create(
        string name,
        string? externalProviderName = null,
        string? externalProviderId = null)
    {
        var nameValidation = ValidateName(name);
        if (nameValidation.IsFailure)
            return Result.Failure<Sport>(nameValidation.Error);

        return new Sport(
            SportId.New(),
            name,
            externalProviderName,
            externalProviderId);
    }

    /// <summary>
    /// Updates the display name of the sport.
    /// </summary>
    /// <param name="name">The new display name.</param>
    /// <returns>A successful result when the update succeeds; otherwise, a failure when the sport cannot be modified or validation fails.</returns>
    public Result Update(string name)
    {
        var guard = EnsureModifiable();
        if (guard.IsFailure)
            return guard;

        var nameValidation = ValidateName(name);
        if (nameValidation.IsFailure)
            return nameValidation;

        Name = name;

        return Result.Success();
    }

    /// <summary>
    /// Soft-deletes the sport.
    /// </summary>
    /// <returns>A successful result. Idempotent when the sport is already deleted.</returns>
    public Result Delete()
    {
        if (IsDeleted)
            return Result.Success();

        IsDeleted = true;

        return Result.Success();
    }

    private Result EnsureModifiable() =>
        EnsureNotDeleted();

    private Result EnsureNotDeleted() =>
        IsDeleted
            ? Result.Failure(Errors.Sport.Deleted)
            : Result.Success();

    private static Result ValidateName(string name) =>
        string.IsNullOrWhiteSpace(name)
            ? Result.Failure(Errors.Sport.EmptyName)
            : Result.Success();

#pragma warning disable CS8618
    /// <summary>
    /// Initializes a new instance of the <see cref="Sport"/> class.
    /// Intended for use by object-relational mappers.
    /// </summary>
    private Sport() { }
#pragma warning restore CS8618
}
