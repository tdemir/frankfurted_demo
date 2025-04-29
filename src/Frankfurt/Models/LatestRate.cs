using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Frankfurt.Models;

public class LatestRate
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    [JsonPropertyName("base")]
    public string BaseCurrency { get; set; }
    [JsonPropertyName("date")]
    public string Date { get; set; }
    [JsonPropertyName("rates")]
    public Dictionary<string,decimal> Rates { get; set; }
}