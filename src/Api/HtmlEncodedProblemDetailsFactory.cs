using System.Diagnostics;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace Api
{
    public class HtmlEncodedProblemDetailsFactory : ProblemDetailsFactory
    {
        private readonly ApiBehaviorOptions _options;

        public HtmlEncodedProblemDetailsFactory(IOptions<ApiBehaviorOptions> options)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public override ProblemDetails CreateProblemDetails(HttpContext httpContext, int? statusCode = null, string? title = null,
            string? type = null, string? detail = null, string? instance = null)
        {
            statusCode ??= 500;

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = HtmlEncoder.Default.Encode(title ?? string.Empty),
                Type = HtmlEncoder.Default.Encode(type ?? string.Empty),
                Detail = HtmlEncoder.Default.Encode(detail ?? string.Empty),
                Instance = HtmlEncoder.Default.Encode(instance ?? string.Empty),
            };

            ApplyProblemDetailsDefaults(httpContext, problemDetails, statusCode.Value);

            return problemDetails;
        }

        public override ValidationProblemDetails CreateValidationProblemDetails(HttpContext httpContext,
            ModelStateDictionary modelStateDictionary, int? statusCode = null, string? title = null, string? type = null,
            string? detail = null, string? instance = null)
        {
            if (modelStateDictionary == null)
            {
                throw new ArgumentNullException(nameof(modelStateDictionary));
            }

            statusCode ??= 400;

            var htmlEncodedErrors = CreateErrorDictionary(modelStateDictionary);
            var problemDetails = new ValidationProblemDetails(htmlEncodedErrors)
            {
                Status = statusCode,
                Type = HtmlEncoder.Default.Encode(type ?? string.Empty),
                Detail = HtmlEncoder.Default.Encode(detail ?? string.Empty),
                Instance = HtmlEncoder.Default.Encode(instance ?? string.Empty),
            };

            if (title != null)
            {
                // For validation problem details, don't overwrite the default title with null.
                problemDetails.Title = HtmlEncoder.Default.Encode(title); ;
            }

            ApplyProblemDetailsDefaults(httpContext, problemDetails, statusCode.Value);

            return problemDetails;
        }

        private void ApplyProblemDetailsDefaults(HttpContext httpContext, ProblemDetails problemDetails, int statusCode)
        {
            problemDetails.Status ??= statusCode;

            if (_options.ClientErrorMapping.TryGetValue(statusCode, out var clientErrorData))
            {
                problemDetails.Title ??= HtmlEncoder.Default.Encode(clientErrorData.Title ?? string.Empty);
                problemDetails.Type ??= HtmlEncoder.Default.Encode(clientErrorData.Link ?? string.Empty);
            }

            var traceId = Activity.Current?.Id ?? httpContext?.TraceIdentifier;
            if (traceId != null)
            {
                problemDetails.Extensions["traceId"] = traceId;
            }
        }

        private static IDictionary<string, string[]> CreateErrorDictionary(ModelStateDictionary modelState)
        {
            if (modelState == null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            var errorDictionary = new Dictionary<string, string[]>(StringComparer.Ordinal);

            foreach (var keyModelStatePair in modelState)
            {
                var key = keyModelStatePair.Key;
                var errors = keyModelStatePair.Value.Errors;
                if (errors.Count > 0)
                {
                    if (errors.Count == 1)
                    {
                        var errorMessage = HtmlEncoder.Default.Encode(errors[0].ErrorMessage);
                        errorDictionary.Add(key, new[] { errorMessage });
                    }
                    else
                    {
                        var errorMessages = new string[errors.Count];
                        for (var i = 0; i < errors.Count; i++)
                        {
                            errorMessages[i] = HtmlEncoder.Default.Encode(errors[i].ErrorMessage);
                        }

                        errorDictionary.Add(key, errorMessages);
                    }
                }
            }

            return errorDictionary;
        }
    }
}
