using System;
using System.Net;
using Samples.JwtAuth.Api.Services;

namespace Samples.JwtAuth.Api.Models
{
    public abstract class AuthApiException : Exception
    {
        protected AuthApiException(Exception throwException)
            : this(null, throwException, HttpStatusCode.InternalServerError) { }

        protected AuthApiException(string message, Exception throwException)
            : this(message, throwException, HttpStatusCode.InternalServerError) { }

        protected AuthApiException(string message, HttpStatusCode statusCode)
            : this(message, null, statusCode) { }

        protected AuthApiException(string message, Exception throwException = null, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
            : base(message.Coalesce(throwException?.Message), throwException)
        {
            ThrowException = throwException;
            HttpStatusCode = statusCode;
        }

        public Exception ThrowException { get; }
        public HttpStatusCode HttpStatusCode { get; protected init; }
    }

    public class RecordExistsException : AuthApiException
    {
        public RecordExistsException(string message = null)
            : base($"Record already exists or a conflicting resource was found {(message ?? $" - [{message}]")}.",
                   null, HttpStatusCode.Conflict) { }
    }

    public class BadRequestException : AuthApiException
    {
        public BadRequestException(string message = "Request was invalid or malformed")
            : base(message.Coalesce("Request was invalid or malformed"), null, HttpStatusCode.BadRequest) { }
    }
}
