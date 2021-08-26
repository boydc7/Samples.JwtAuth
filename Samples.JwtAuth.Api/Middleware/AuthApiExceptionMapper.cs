using System;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Opw.HttpExceptions;
using Opw.HttpExceptions.AspNetCore;
using Opw.HttpExceptions.AspNetCore.Mappers;
using Samples.JwtAuth.Api.Models;

namespace Samples.JwtAuth.Api.Middleware
{
    public class AuthApiExceptionMapper : IExceptionMapper
    {
        private const string _exception = "Exception";

        public bool CanMap(Type type)
        {
            if (type == typeof(AuthApiException) || type == typeof(ValidationException))
            {
                return true;
            }

            if (type.BaseType != null && type.BaseType == typeof(AuthApiException))
            {
                return true;
            }

            if (type.BaseType != null && type.BaseType.BaseType != null && type.BaseType.BaseType == typeof(AuthApiException))
            {
                return true;
            }

            return false;
        }

        public bool TryMap(Exception exception, HttpContext context, out IStatusCodeActionResult actionResult)
        {
            actionResult = default;

            if (!CanMap(exception.GetType()))
            {
                return false;
            }

            try
            {
                actionResult = Map(exception, context);

                return actionResult != null;
            }
            catch
            {
                return false;
            }
        }

        public IStatusCodeActionResult Map(Exception exception, HttpContext context)
        {
            if (exception is AuthApiException stx)
            {
                var problemDetails = ToProblemDetails(stx, context.Request?.Path.Value);

                return new ProblemDetailsResult(problemDetails);
            }

            return default;
        }

        private static ProblemDetails ToProblemDetails(AuthApiException authApiException, string requestPath)
        {
            var problemDetails = new ProblemDetails
                                 {
                                     Status = (int)authApiException.HttpStatusCode,
                                     Title = ToProblemDetailsTitle(authApiException),
                                     Detail = authApiException.Message,
                                     Instance = requestPath
                                 };

#if DEBUG
            problemDetails.Extensions.Add("exceptionDetails", new SerializableException(authApiException));
#endif

            return problemDetails;
        }

        private static string ToProblemDetailsTitle(AuthApiException exception)
        {
            var xTypeName = exception.GetType().Name;

            var indexOfTick = xTypeName.IndexOf('`');

            if (indexOfTick >= 0)
            {
                xTypeName = xTypeName.Substring(0, indexOfTick);
            }

            if (xTypeName.EndsWith(_exception, StringComparison.OrdinalIgnoreCase))
            {
                xTypeName = xTypeName.Substring(0, xTypeName.Length - _exception.Length);
            }

            return xTypeName;
        }
    }
}
