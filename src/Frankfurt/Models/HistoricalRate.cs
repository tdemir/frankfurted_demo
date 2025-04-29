using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Frankfurt.Models;

public class HistoricalRate
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    [JsonPropertyName("base")]
    public string BaseCurrency { get; set; }
    [JsonPropertyName("start_date")]
    public string StartDate { get; set; }
    [JsonPropertyName("end_date")]
    public string EndDate { get; set; }
    [JsonPropertyName("rates")]
    public Dictionary<string, Dictionary<string, decimal>> Rates { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("page_count")]
    public int PageCount { get; set; }

    public HistoricalRate Clone()
    {
        var clonedVal = new HistoricalRate
        {
            Amount = this.Amount,
            BaseCurrency = this.BaseCurrency,
            StartDate = this.StartDate,
            EndDate = this.EndDate,
            Rates = new Dictionary<string, Dictionary<string, decimal>>(this.Rates),
            Page = this.Page,
            PageCount = this.PageCount
        };
        return clonedVal;
    }
}