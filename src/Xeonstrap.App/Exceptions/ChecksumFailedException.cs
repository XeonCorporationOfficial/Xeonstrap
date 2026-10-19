using System;

namespace Xeonstrap.Exceptions;

internal sealed class ChecksumFailedException(string message) : Exception(message)
{
}
