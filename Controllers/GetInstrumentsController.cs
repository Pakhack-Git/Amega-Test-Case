using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Amega_Test_Case.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GetInstrumentsController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GetInstrumentsController> _logger;
        private const string TiingoApiKey = "39042154ebdf8fe0e48ba4cb280837b6c11830dd";

        public GetInstrumentsController(IHttpClientFactory httpClientFactory, ILogger<GetInstrumentsController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public class TopOfBook
        {
            public string Ticker { get; set; }
            public decimal? BidPrice { get; set; }
            public decimal? AskPrice { get; set; }
            public decimal? BidSize { get; set; }
            public decimal? AskSize { get; set; }
            public string QuoteTimestamp { get; set; }
        }

        [HttpGet("{ticker}", Name = "GetTopOfBookPrice")]
        public TopOfBook GetTopOfBookPrice(string ticker)
        {
            _logger.LogInformation("Received request for top of book price for ticker: {Ticker}", ticker);

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Token {TiingoApiKey}");

            var url = $"https://api.tiingo.com/tiingo/fx/{ticker}/top";
            _logger.LogDebug("Requesting data from URL: {Url}", url);

            var json = client.GetStringAsync(url).Result;

            var topList = System.Text.Json.JsonSerializer.Deserialize<List<TopOfBook>>(json);
            if (topList != null && topList.Count > 0)
            {
                _logger.LogInformation("Successfully retrieved top of book price for {Ticker}", ticker);
                return topList[0];
            }

            _logger.LogWarning("No data found for ticker: {Ticker}", ticker);
            return null;
        }

        [HttpGet("list", Name = "GetListOfInstruments")]
        public IEnumerable<string> GetListOfInstruments()
        {
            _logger.LogInformation("Returning static list of instruments.");
            return new List<string>
            {
                "EURUSD",
                "EURGBP",
                "GBPUSD"
            };
        }
    }
}