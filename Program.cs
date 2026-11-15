using System.Runtime.InteropServices;
using InvestmentViewer;

Console.Clear();
string[] files = Methods.GetFiles();
Task<Portfolio>[] tasks = new Task<Portfolio>[files.Length];

if (files.Length == 0)
{
    Console.WriteLine("No Files Found");
    return;
}

Task<Dictionary<string, decimal>> quotes = Methods.GetQuotes();


for (int i = 0; i < files.Length; i++)
{
    int index = i;
    tasks[i] = Task.Run<Portfolio>(() =>
    {
        return Methods.GetPortfolio(files[index]);
    });
}

await quotes;

Methods.WritePortfolios(await Task.WhenAll(tasks));
Console.Read();
