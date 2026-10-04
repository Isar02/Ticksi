namespace Ticksi.Application.Common.Exceptions;

public sealed class UnauthorizedException(string message) : Exception(message);
