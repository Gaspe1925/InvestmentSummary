using System.Runtime.InteropServices;
using InvestmentViewer;

Console.Clear();
Task<Dictionary<string, decimal>> getQuotes = Methods.GetQuotes();

string[] files = Methods.GetFiles();
Task<Portfolio>[] tasks = new Task<Portfolio>[files.Length];

if (files.Length == 0)
{
    Console.WriteLine("No Files Found");
    return;
}

for (int i = 0; i < files.Length; i++)
{
    int index = i;
    tasks[i] = Task.Run<Portfolio>(() =>
    {
        return Methods.GetPortfolio(files[index]);
    });
}

Dictionary<string, decimal> quotes = await getQuotes;
List<Portfolio> portfolios = (await Task.WhenAll(tasks)).ToList();

Methods.UpdateMarketPrice(portfolios, quotes);
Writer.WritePortfolios(portfolios);
Console.Read();
