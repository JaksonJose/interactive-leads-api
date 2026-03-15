using FluentValidation;
using InteractiveLeads.Application.Responses;
using MediatR;

namespace InteractiveLeads.Application.Pipelines
{
    public class ValidationPipelineBenaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>, IValidate
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationPipelineBenaviour(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);
                var validationResults = await Task
                    .WhenAll(_validators.Select(vr => vr.ValidateAsync(context, cancellationToken)));

                if (validationResults.Any(vr => !vr.IsValid))
                {
                    List<string> errors = [];

                    var failures = validationResults.SelectMany(vr => vr.Errors)
                        .Where(f => f != null)
                        .ToList();

                    foreach (var failure in failures)
                    {
                        errors.Add(failure.ErrorMessage);
                    }

                    var response = new ResultResponse();
                    foreach (var error in errors)
                    {
                        // Extract code and message from "code:message" format
                        var parts = error.Split(':', 2);
                        var code = parts.Length > 1 ? parts[0] : "";
                        var message = parts.Length > 1 ? parts[1] : error;
                        
                        response.AddErrorMessage(message: message, code: code);
                    }

                    return (TResponse)(object)response;
                }
            }

            return await next(cancellationToken);
        }
    }
}
