using System.Reflection;
using System.Linq;
using FluentValidation;
using InteractiveLeads.Application.Pipelines;
using InteractiveLeads.Application.Responses;
using Microsoft.Extensions.DependencyInjection;

namespace InteractiveLeads.Application.Dispatching;

public sealed class RequestDispatcher : IRequestDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public RequestDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> Send<TResponse>(
        IApplicationRequest<TResponse> request,
        CancellationToken cancellationToken = default)
        where TResponse : class
    {
        if (request is IValidate)
        {
            var validationOutcome = await ValidateRequestAsync<TResponse>(request, cancellationToken);

            if (!validationOutcome.IsValid)
                return validationOutcome.Response!;
        }

        var requestType = request.GetType();
        var handlerInterfaceType = typeof(IApplicationRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));

        var handler = _serviceProvider.GetRequiredService(handlerInterfaceType);
        var handleMethod = handlerInterfaceType.GetMethod(nameof(IApplicationRequestHandler<IApplicationRequest<TResponse>, TResponse>.Handle))
            ?? throw new InvalidOperationException($"Could not find Handle method for {handlerInterfaceType.Name}.");

        var task = (Task<TResponse>)handleMethod.Invoke(handler, [request, cancellationToken])!;
        return await task;
    }

    private async Task<ValidationOutcome<TResponse>> ValidateRequestAsync<TResponse>(object request, CancellationToken cancellationToken)
        where TResponse : class
    {
        var requestType = request.GetType();

        var validateMethod = GetType().GetMethod(
                nameof(ValidateAsync),
                BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not find validation method.");

        var closed = validateMethod.MakeGenericMethod(requestType, typeof(TResponse));
        var outcome = (ValidationOutcome<TResponse>)await (Task<ValidationOutcome<TResponse>>)closed.Invoke(
            this,
            [request, cancellationToken])!;

        return outcome;
    }

    private async Task<ValidationOutcome<TResponse>> ValidateAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : IValidate
        where TResponse : class
    {
        var validators = _serviceProvider.GetServices<IValidator<TRequest>>().ToList();

        if (!validators.Any())
            return new ValidationOutcome<TResponse>(true, null);

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(validators.Select(vr => vr.ValidateAsync(context, cancellationToken)));

        if (!validationResults.Any(vr => !vr.IsValid))
            return new ValidationOutcome<TResponse>(true, null);

        List<string> errors = [];
        var failures = validationResults
            .SelectMany(vr => vr.Errors)
            .Where(f => f != null)
            .ToList();

        foreach (var failure in failures)
        {
            errors.Add(failure.ErrorMessage);
        }

        var response = new ResultResponse();
        foreach (var error in errors)
        {
            var parts = error.Split(':', 2);
            var code = parts.Length > 1 ? parts[0] : "";
            var message = parts.Length > 1 ? parts[1] : error;

            response.AddErrorMessage(message: message, code: code);
        }

        if (response is not TResponse typedResponse)
        {
            throw new InvalidOperationException(
                $"Validation produced {nameof(ResultResponse)} which cannot be assigned to {typeof(TResponse).Name}. " +
                "Requests implementing IValidate must return IResponse-based types.");
        }

        return new ValidationOutcome<TResponse>(false, typedResponse);
    }

    private readonly record struct ValidationOutcome<TResponse>(bool IsValid, TResponse? Response)
        where TResponse : class;
}

