using System.Runtime.InteropServices;
using InvestmentViewer;

Console.Clear();
string[] files = Methods.GetFiles();
Portfolio portfolio = Methods.GetPortfolio(files[0]);

foreach (var v in portfolio.InvestmentByCurrency)
{
    Methods.WriteCharacters('=', 20);
    Console.WriteLine($"Currency {v.Key} Cash {v.Value.Cash}");
    Methods.WriteCharacters('-', 20);

    foreach (var u in v.Value.Holdings)
    {
        Console.WriteLine($"{u.Key} quantity {u.Value.Quantity} book value {u.Value.CostTotal} dividends {u.Value.Payout} profits {u.Value.SalesProfit}");
    }
}
