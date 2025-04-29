using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Frankfurt.Exceptions;

public class ForbiddenCurrencyException : Exception
{
    public ForbiddenCurrencyException(string currencyName) 
        : base($"The currency '{currencyName}' is not allowed.")
    {
    }
}