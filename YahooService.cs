using System.Text.Json;

namespace InvestmentViewer;

public static class YahooService
{
    private static readonly HttpClient Client = new HttpClient();

        public static async Task<decimal> GetQuote(string ticker)
        {
            string url =
                $"https://query1.finance.yahoo.com/v8/finance/chart/{ticker}?range=5d&interval=1d";

            Client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0");

            var response = await Client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();

            using JsonDocument doc = JsonDocument.Parse(json);

            var result = doc.RootElement
                .GetProperty("chart")
                .GetProperty("result")[0];

            var meta = result.GetProperty("meta");

            // Console.WriteLine($"Ticker: {ticker}");
            // Console.WriteLine($"Currency: {meta.GetProperty("currency")}");
            // Console.WriteLine($"Exchange: {meta.GetProperty("exchangeName")}");
            // Console.WriteLine($"Previous Close: {meta.GetProperty("chartPreviousClose")}");
            //
            decimal marketValue;
            meta.GetProperty("regularMarketPrice").TryGetDecimal(out marketValue);
            return marketValue;
        }
}



//  https://query1.finance.yahoo.com/v8/finance/chart/AAPL
//
//  https://query1.finance.yahoo.com/v8/finance/chart/XIC.TO?range=1d&interval=1d
//  await YahooFinance.GetQuote("XIC.TO"); // C# call

// using System;
// using System.Net.Http;
// using System.Text.Json;
// using System.Threading.Tasks;

// public class YahooFinance
// {
//     private static readonly HttpClient Client = new HttpClient();

//     public static async Task GetQuote(string ticker)
//     {
//         string url =
//             $"https://query1.finance.yahoo.com/v8/finance/chart/{ticker}?range=5d&interval=1d";

//         Client.DefaultRequestHeaders.UserAgent.ParseAdd(
//             "Mozilla/5.0");

//         var response = await Client.GetAsync(url);
//         response.EnsureSuccessStatusCode();

//         string json = await response.Content.ReadAsStringAsync();

//         using JsonDocument doc = JsonDocument.Parse(json);

//         var result = doc.RootElement
//             .GetProperty("chart")
//             .GetProperty("result")[0];

//         var meta = result.GetProperty("meta");

//         Console.WriteLine($"Ticker: {ticker}");
//         Console.WriteLine($"Price: {meta.GetProperty("regularMarketPrice")}"); // market price
//         Console.WriteLine($"Currency: {meta.GetProperty("currency")}");
//         Console.WriteLine($"Exchange: {meta.GetProperty("exchangeName")}");
//         Console.WriteLine($"Previous Close: {meta.GetProperty("chartPreviousClose")}");
//     }
// }
