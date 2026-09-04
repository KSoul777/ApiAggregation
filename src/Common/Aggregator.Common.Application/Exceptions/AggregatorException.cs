
using Aggregator.Common.Domain;

namespace Aggregator.Common.Application.Exceptions;

public sealed class AggregatorException(string requestName, Error? error = default, Exception? innerException = default)
    : Exception("Application exception", innerException)
{
    public string RequestName { get; } = requestName;

    public Error? Error { get; } = error;
}
