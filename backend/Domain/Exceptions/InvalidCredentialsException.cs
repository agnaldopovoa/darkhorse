namespace Darkhorse.Domain.Exceptions;

public class InvalidCredentialsException(string message = "Invalid email or password.")
    : Exception(message);
