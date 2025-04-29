using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Frankfurt.Exceptions;

public class CurrencyNotFoundException : Exception
{
    public CurrencyNotFoundException(string currency)
        : base($"Currency '{currency}' not found.")
    {
    }

    public CurrencyNotFoundException(string currency, Exception innerException)
        : base($"Currency '{currency}' not found.", innerException)
    {
    }
    
}