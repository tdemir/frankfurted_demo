using Frankfurt.Models;
using Frankfurt.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Frankfurt.Controllers;


[Authorize(Roles = "User")]
public class CurrencyController : BaseController
{
    private readonly ILogger<CurrencyController> _logger;
    private readonly ICurrencyFetcher _currencyFetcher;

    public CurrencyController(ILogger<CurrencyController> logger, ICurrencyFetcher currencyFetcher)
    {
        _logger = logger;
        _currencyFetcher = currencyFetcher;
    }

    // [HttpGet()]
    // [Route("list")]
    // [Authorize(Roles = "User,Admin")]// or [Authorize(Policy = "RequireUserRole")] 
    // public async Task<List<string>> List()
    // {
    //     return await _currencyFetcher.GetCurrencies();
    // }

    //http://localhost:5225/api/v1/currency/latest
    [HttpGet("latest")]
    [AllowAnonymous]
    public async Task<LatestRate> Latest(string baseCurrency = "EUR")
    {
        return await _currencyFetcher.GetLatestExchangeRates(baseCurrency);
    }

    //http://localhost:5225/api/v1/currency/convert?fromCurrency=EUR&toCurrency=USD&amount=1
    [HttpGet("convert")]
    [AllowAnonymous]
    public async Task<IResult> Convert(string fromCurrency, string toCurrency, decimal amount)
    {
        var result = await _currencyFetcher.ConvertCurrency(fromCurrency, toCurrency, amount);
        return Results.Ok(new
        {
            from = fromCurrency,
            to = toCurrency,
            amount_from = amount,
            amount_to = result
        });
    }

    //http://localhost:5225/api/v1/currency/historical?base=EUR&startDate=2023-01-01&endDate=2023-01-31&page=1
    [HttpGet("historical")]
    public async Task<IResult> Historical([FromQuery] HistoricalRateInputModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            
            _logger.LogWarning("Invalid login attempt: {Errors}", string.Join(", ", errors));

            return Results.BadRequest("Invalid parameters request");
        }

        var result = await _currencyFetcher.GetHistoricalExchangeRates(model);
        return Results.Ok(result);
    }
}