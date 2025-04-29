using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Frankfurt.Exceptions;
using Frankfurt.Models;
using Frankfurt.Services.Interfaces;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace Frankfurt.Services;

public class CurrencyFetcher : ICurrencyFetcher
{
    private const string CURRENCY_LIST_CACHE_KEY = "currencies";
    private readonly TimeSpan CURRENCY_LIST_EXPIRATION_TIME = TimeSpan.FromHours(1);

    private const string LATEST_CURRENCY_LIST_CACHE_KEY = "latest_currencies_{0}";
    private readonly TimeSpan LATEST_CURRENCY_LIST_EXPIRATION_TIME = TimeSpan.FromSeconds(10);


    private const string LATEST_CURRENCY_HISTORICAL_LIST_CACHE_KEY = "latest_currencies_historical_{0}_{1}_{2}";
    private readonly TimeSpan LATEST_CURRENCY_HISTORICAL_LIST_EXPIRATION_TIME = TimeSpan.FromHours(1);

    private readonly AppSettings _appSettings;
    private readonly ICustomCacheService _cacheService;
    private readonly ILogger<CurrencyFetcher> _logger;
    //private readonly AsyncRetryPolicy _retryPolicy;
    private readonly HttpClient _client;

    public CurrencyFetcher(ICustomCacheService cacheService, ILogger<CurrencyFetcher> logger, IHttpClientFactory factory, IOptions<AppSettings> appSettings)
    {
        // Constructor logic here
        _cacheService = cacheService;
        _logger = logger;
        // _retryPolicy = Policy
        //      .Handle<HttpRequestException>()
        //      .Or<TimeoutException>()
        //      .WaitAndRetryAsync(
        //          3, // number of retries
        //          retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // exponential backoff
        //          onRetry: (exception, timeSpan, retryCount, context) =>
        //          {
        //              _logger.LogWarning(
        //                  exception,
        //                  "Attempt {RetryCount} failed with {ExceptionType}. Waiting {TimeSpan} before next retry.",
        //                  retryCount,
        //                  exception.GetType().Name,
        //                  timeSpan);
        //          }
        //      );
        _client = factory.CreateClient("frankfurtClient");
        _appSettings = appSettings.Value;
    }

    public async Task<List<string>> GetCurrencies()
    {
        // return await _retryPolicy.ExecuteAsync(async () =>
        // {
        //     var val = _cacheService.GetOrCreate<List<string>>(CURRENCY_LIST_CACHE_KEY, 
        //         CURRENCY_LIST_EXPIRATION_TIME, () =>
        //     {
        //         // Simulate fetching currencies from an API
        //         return new List<string> { "USD", "EUR", "GBP" };
        //     });

        //     return val;
        // });     
        var val = await _cacheService.GetOrCreate<List<string>>(CURRENCY_LIST_CACHE_KEY,
                CURRENCY_LIST_EXPIRATION_TIME, async () =>
                {
                    var resp = await _client.GetAsync("currencies");
                    resp.EnsureSuccessStatusCode();
                    using (var contentStream = await resp.Content.ReadAsStreamAsync())
                    {
                        // Deserialize the JSON response into a Dictionary
                        var dict = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(contentStream);

                        return dict.Keys.ToList();
                    }

                }
            );

        return val;
    }

    public async Task<bool> IsApprovedCurrency(string currency, bool checkForbidden = false)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return false;
        }
        currency = currency.ToUpperInvariant();
        if (checkForbidden
            && _appSettings.ForbiddenCurrencies != null
            && _appSettings.ForbiddenCurrencies.Contains(currency))
        {
            return false;
        }
        var getRates = await GetCurrencies();
        if (getRates != null && getRates.Contains(currency))
        {
            return true;
        }
        return false;
    }

    public async Task<LatestRate> GetLatestExchangeRates(string baseCurrency = "EUR")
    {
        if (!await IsApprovedCurrency(baseCurrency))
        {
            throw new ForbiddenCurrencyException(baseCurrency);
        }
        var cacheKey = string.Format(LATEST_CURRENCY_LIST_CACHE_KEY, baseCurrency);
        var cacheExpireTimespan = LATEST_CURRENCY_LIST_EXPIRATION_TIME;

        var val = await _cacheService.GetOrCreate<LatestRate>(cacheKey,
                cacheExpireTimespan, async () =>
                {
                    var resp = await _client.GetAsync("latest?base=" + baseCurrency);
                    resp.EnsureSuccessStatusCode();
                    using (var contentStream = await resp.Content.ReadAsStreamAsync())
                    {
                        var data = await JsonSerializer.DeserializeAsync<LatestRate>(contentStream);

                        return data;
                    }
                }
            );

        return val;

    }

    public async Task<decimal> ConvertCurrency(string fromCurrency, string toCurrency, decimal amount)
    {
        if (!await IsApprovedCurrency(fromCurrency, checkForbidden: true))
        {
            throw new ForbiddenCurrencyException(fromCurrency);
        }
        if (!await IsApprovedCurrency(toCurrency, checkForbidden: true))
        {
            throw new ForbiddenCurrencyException(toCurrency);
        }
        fromCurrency = fromCurrency.ToUpperInvariant();
        toCurrency = toCurrency.ToUpperInvariant();
        if (fromCurrency == toCurrency)
        {
            return amount;
        }
        var latestRates = await GetLatestExchangeRates(fromCurrency);
        if (latestRates != null
            && latestRates.Rates != null
            && latestRates.Rates.ContainsKey(toCurrency))
        {
            var newAmount = latestRates.Rates[toCurrency] * amount;
            return newAmount;
        }
        throw new CurrencyNotFoundException(toCurrency);
    }

    public async Task<HistoricalRate> GetHistoricalExchangeRates(HistoricalRateInputModel model)
    {
        if (!await IsApprovedCurrency(model.BaseCurrency))
        {
            throw new ForbiddenCurrencyException(model.BaseCurrency);
        }
        model.BaseCurrency = model.BaseCurrency.ToUpperInvariant();
        var cacheKey = string.Format(LATEST_CURRENCY_HISTORICAL_LIST_CACHE_KEY, model.BaseCurrency, model.StartDate, model.EndDate);
        var cacheExpireTimespan = LATEST_CURRENCY_HISTORICAL_LIST_EXPIRATION_TIME;

        //https://api.frankfurter.dev/v1/2021-01-01..2022-12-31?base=try

        var val = (await _cacheService.GetOrCreate<HistoricalRate>(cacheKey,
                cacheExpireTimespan, async () =>
                {
                    //https://api.frankfurter.dev/v1/2021-01-01..2022-12-31?base=try
                    var resp = await _client.GetAsync($"{model.StartDate}..{model.EndDate}?base=" + model.BaseCurrency);
                    resp.EnsureSuccessStatusCode();
                    using (var contentStream = await resp.Content.ReadAsStreamAsync())
                    {
                        var data = await JsonSerializer.DeserializeAsync<HistoricalRate>(contentStream);

                        return data;
                    }
                }
            )).Clone();
               

        var totalRecordCount = val.Rates.Count;
        val.PageCount = totalRecordCount / _appSettings.MaxItemsPerPage + (totalRecordCount % _appSettings.MaxItemsPerPage == 0 ? 0 : 1);
        if (val.PageCount < model.Page)
        {
            model.Page = 1;
        }

        val.Page = model.Page;
        

        var paginatedRates = val.Rates
            .Skip((val.Page - 1) * _appSettings.MaxItemsPerPage)
            .Take(_appSettings.MaxItemsPerPage)
            .ToDictionary(x => x.Key, x => x.Value);

        val.Rates = paginatedRates; 
        

        return val;
    }



}