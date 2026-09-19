using System;

namespace gestaotcc.Application.UseCases;

public class SecureApplicationException : Exception
{
    public SecureApplicationException(string message) : base(message)
    {
    }
}
