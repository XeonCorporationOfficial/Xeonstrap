using System;

namespace Xeonstrap.Exceptions;

internal sealed class InvalidHTTPResponseException(string message) : Exception(message)
{
}
