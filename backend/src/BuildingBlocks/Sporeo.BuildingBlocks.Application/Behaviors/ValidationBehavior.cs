using FluentValidation;
using MediatR;
using Sporeo.BuildingBlocks.Domain.Results;

namespace Sporeo.BuildingBlocks.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that validates requests using registered FluentValidation validators.
/// Returns a failed <see cref="Result"/> or <see cref="Result{TValue}"/> when validation errors occur.
/// </summary>
/// <typeparam name="TRequest">The type of the request being handled.</typeparam>
/// <typeparam name="TResponse">The type of the response returned by the handler.</typeparam>
/// <param name="validators">The validators registered for <typeparamref name="TRequest"/>.</param>
public class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var errors = failures
            .Select(f => new Error(
                string.IsNullOrWhiteSpace(f.ErrorCode) ? f.PropertyName : f.ErrorCode,
                f.ErrorMessage))
            .ToArray();

        var validationError = new ValidationError(errors);

        return ResultResponseFactory.CreateValidationFailure<TResponse>(validationError);
    }
}

/// <summary>
/// Creates failed <see cref="Result"/> responses for validation pipeline behaviors.
/// </summary>
internal static class ResultResponseFactory
{
    /// <summary>
    /// Creates a failed <see cref="Result"/> or <see cref="Result{TValue}"/> for the given validation error.
    /// </summary>
    /// <typeparam name="TResponse">The response type expected by the pipeline.</typeparam>
    /// <param name="validationError">The validation error to return.</param>
    /// <returns>A failed result instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <typeparamref name="TResponse"/> is not <see cref="Result"/> or <see cref="Result{TValue}"/>.
    /// </exception>
    public static TResponse CreateValidationFailure<TResponse>(ValidationError validationError)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
            return (TResponse)(object)Result.Failure(validationError);

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = responseType.GetGenericArguments()[0];
            var failureMethod = typeof(Result)
                .GetMethods()
                .Single(m => m.Name == nameof(Result.Failure) && m.IsGenericMethodDefinition)
                .MakeGenericMethod(valueType);

            return (TResponse)failureMethod.Invoke(null, [validationError])!;
        }

        throw new InvalidOperationException(
            $"ValidationBehavior only supports Result and Result<T> responses, but received {responseType.FullName}.");
    }
}
