using System;
using System.Net;

namespace Xeonstrap.Exceptions;

public class InvalidChannelException(HttpStatusCode? statusCode) : Exception
{
	public HttpStatusCode? StatusCode = statusCode;
}
