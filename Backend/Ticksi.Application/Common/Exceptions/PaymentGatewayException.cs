namespace Ticksi.Application.Common.Exceptions;

public sealed class PaymentGatewayException(string message, Exception innerException) : Exception(message, innerException);
