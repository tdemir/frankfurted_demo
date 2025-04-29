using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frankfurt.Models;

namespace Frankfurt.Services.Interfaces;

public interface ICurrencyFetcher
{
    Task<bool> IsApprovedCurrency(string currency, bool checkForbidden = false);
    Task<List<string>> GetCurrencies();
    Task<LatestRate> GetLatestExchangeRates(string baseCurrency = "EUR");
    Task<decimal> ConvertCurrency(string fromCurrency, string toCurrency, decimal amount);
    Task<HistoricalRate> GetHistoricalExchangeRates(HistoricalRateInputModel model);
}