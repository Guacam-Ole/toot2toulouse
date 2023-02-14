
using System.Text.Json.Serialization;

namespace Toot2ToulouseWeb
{
    public class ApiException : Exception
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum ErrorTypes
        {
            None = 0,
            Auth,
            Unknown= 42,
            Exception = 666,

            RegistrationNoBots,
            RegistrationWrongInstance,
            RegistrationClosed,
            RegistrationInvite,
            Mastodon,
            Twitter,
            
            

        }

        public int StatusCode { get; }

        public ErrorTypes ErrorType { get; }

        public ApiException(ErrorTypes errorType, string? message = null, int statusCode = 500) : base(message)
        {
            StatusCode = statusCode;
            ErrorType = errorType;
        }
        public ApiException(Exception innerException, string? message=null) : base(message, innerException)
        {
            StatusCode =  500;
            ErrorType =  ErrorTypes.Exception;
        }
    }
}