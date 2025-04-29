using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Frankfurt.Models;

public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? TraceId { get; set; }
}