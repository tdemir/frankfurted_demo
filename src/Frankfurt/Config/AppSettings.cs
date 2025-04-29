using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Frankfurt.Config;

public class AppSettings
{
    public string ApiBaseUrl { get; set; } = string.Empty;
    public int MaxItemsPerPage { get; set; }
    public string[] ForbiddenCurrencies { get; set; } = Array.Empty<string>();
}