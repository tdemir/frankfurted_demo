using Frankfurt.Services;
using Frankfurt.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using FluentAssertions;
using Frankfurt.Config;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Frankfurt.Models;

namespace Frankfurt.Tests.Services;

public class CurrencyFetcherTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ICustomCacheService> _cacheMock;
    private readonly Mock<ILogger<CurrencyFetcher>> _loggerMock;
    private readonly Mock<IOptions<AppSettings>> _settingsMock;
    private readonly CurrencyFetcher _sut;

    public CurrencyFetcherTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _cacheMock = new Mock<ICustomCacheService>();
        _loggerMock = new Mock<ILogger<CurrencyFetcher>>();
        _settingsMock = new Mock<IOptions<AppSettings>>();

        _cacheMock.Setup(x => x.GetOrCreate("currencies",It.IsAny<TimeSpan>(), It.IsNotNull<Func<Task<List<string>>>>()))
            .ReturnsAsync(new List<string> { "USD", "GBP", "EUR", "TRY" });

        // Setup mock HTTP handler
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    amount = 1,
                    @base = "EUR",
                    rates = new Dictionary<string, decimal>
                    {
                        { "USD", 1.1m },
                        { "GBP", 0.9m }
                    }
                }))
            });

        // Setup HttpClient factory
        var client = new HttpClient(mockHttpMessageHandler.Object);
        client.BaseAddress = new Uri("https://api.example.com/");
        _httpClientFactoryMock.Setup(x => x.CreateClient("frankfurtClient"))
            .Returns(client);

        _settingsMock.Setup(x => x.Value)
            .Returns(new AppSettings 
            { 
                MaxItemsPerPage = 10,
                ForbiddenCurrencies = new[] { "TRY" }
            });

        _sut = new CurrencyFetcher(
            _cacheMock.Object,
            _loggerMock.Object,
            _httpClientFactoryMock.Object,
            _settingsMock.Object);
    }

    [Fact]
    public async Task GetLatestExchangeRates_ShouldReturnRates_WhenApiCallSucceeds()
    {
        // Arrange
        var baseCurrency = "EUR";        

        _cacheMock.Setup(x => x.GetOrCreate(
                "latest_currencies_" + baseCurrency,
                It.IsAny<TimeSpan>(),
                It.IsAny<Func<Task<LatestRate>>>()))
            .ReturnsAsync(new LatestRate
            {
                BaseCurrency = baseCurrency,
                Rates = new Dictionary<string, decimal>
                {
                    { "USD", 1.1m },
                    { "GBP", 0.9m }
                }
            });

        // Act
        var result = await _sut.GetLatestExchangeRates(baseCurrency);

        // Assert
        result.Should().NotBeNull();
        result.BaseCurrency.Should().Be(baseCurrency);
        result.Rates.Should().ContainKey("USD");
        result.Rates["USD"].Should().Be(1.1m);
    }

    [Fact]
    public async Task GetLatestExchangeRates_ShouldUseCache_WhenDataIsCached()
    {
        // Arrange
        var baseCurrency = "EUR";
        var cachedData = new LatestRate
        {
            BaseCurrency = "EUR",
            Rates = new Dictionary<string, decimal>
            {
                { "USD", 1.2m }
            }
        };
        _cacheMock.Setup(x => x.GetOrCreate(
                "latest_currencies_" + baseCurrency,
                It.IsAny<TimeSpan>(),
                It.IsAny<Func<Task<LatestRate>>>()))
            .ReturnsAsync(cachedData);

        _cacheMock.Setup(x => x.GetOrCreate(
            It.IsAny<string>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<Func<Task<LatestRate>>>()))
            .ReturnsAsync(cachedData);

        // Act
        var result = await _sut.GetLatestExchangeRates(baseCurrency);

        // Assert
        result.Should().NotBeNull();
        result.Rates["USD"].Should().Be(1.2m);
        _httpClientFactoryMock.Verify(x => x.CreateClient(It.IsAny<string>()), Times.AtMost(1));
    }

    [Fact]
    public async Task IsApprovedCurrency_WithValidCurrency_ReturnsTrue()
    {
        // Arrange
        var currency = "EUR";
        _cacheMock.Setup(x => x.GetOrCreate(
            It.IsAny<string>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<Func<Task<List<string>>>>()))
            .ReturnsAsync(new List<string> { "EUR", "USD" });

        // Act
        var result = await _sut.IsApprovedCurrency(currency);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsApprovedCurrency_WithForbiddenCurrency_ReturnsFalse()
    {
        // Arrange
        var currency = "XXX";

        // Act
        var result = await _sut.IsApprovedCurrency(currency, checkForbidden: true);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetLatestExchangeRates_WithValidBaseCurrency_ReturnsRates()
    {
        // Arrange
        var baseCurrency = "EUR";
        var expectedRates = new LatestRate 
        { 
            BaseCurrency = "EUR",
            Rates = new Dictionary<string, decimal> 
            { 
                { "USD", 1.1m } 
            }
        };

        _cacheMock.Setup(x => x.GetOrCreate(
            It.IsAny<string>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<Func<Task<LatestRate>>>()))
            .ReturnsAsync(expectedRates);

        // Act
        var result = await _sut.GetLatestExchangeRates(baseCurrency);

        // Assert
        result.Should().NotBeNull();
        result.BaseCurrency.Should().Be(baseCurrency);
        result.Rates.Should().ContainKey("USD");
    }

    [Fact]
    public async Task GetHistoricalExchangeRates_WithValidInput_ReturnsHistoricalRates()
    {
        // Arrange
        var model = new HistoricalRateInputModel
        {
            BaseCurrency = "EUR",
            StartDate = "2023-01-01",
            EndDate = "2023-01-31",
            Page = 1
        };

        var expectedRates = new HistoricalRate
        {
            BaseCurrency = "EUR",
            StartDate = "2023-01-01",
            EndDate = "2023-01-31",
            Rates = new Dictionary<string, Dictionary<string, decimal>>()
        };

        _cacheMock.Setup(x => x.GetOrCreate(
            It.IsAny<string>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<Func<Task<HistoricalRate>>>()))
            .ReturnsAsync(expectedRates);

        // Act
        var result = await _sut.GetHistoricalExchangeRates(model);

        // Assert
        result.Should().NotBeNull();
        result.BaseCurrency.Should().Be(model.BaseCurrency);
        result.StartDate.Should().Be(model.StartDate);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData(" ")]
    public async Task IsApprovedCurrency_WithInvalidCurrency_ReturnsFalse(string currency)
    {
        // Act
        var result = await _sut.IsApprovedCurrency(currency);

        // Assert
        result.Should().BeFalse();
    }
}