using System.Numerics;
using Microsoft.Extensions.Configuration;
namespace InvestmentViewer;

public static class Methods
{
    private const string Sell = "SELL";
    private const string Buy = "BUY";
    private const string Contribution = "CONT";
    private const string Withdrawal = "WITHDRAW";
    private const string Split = "SPLIT";
    private const string Reverse = "REVERSE";
    private const string Dividends = "DIV";
    private const string Interest = "INT";
    private const string Exchange = "EXCHANGE";
    private const string Transfer = "TRANSFER";


    private static readonly string PathData;
    private static readonly string QuotesFile;
    private static readonly string ContributionFile;
    
    static Methods()
    {
        IConfigurationRoot config = ConfigFactory.Create();

        PathData = config["General:directory"].TrimEnd();
        QuotesFile = config["General:quotes_file"].TrimEnd();
        ContributionFile = config["General:contribution_file"];
    }

    public static string[] GetFiles()
    {
        string[] results = new string[0];
        results = Directory.GetFiles(PathData, "*csv");
        return results;
    }

    public static async Task<Dictionary<string, decimal>> GetQuotes()
    {
        Dictionary<string, decimal> quotes = new Dictionary<string, decimal>();

        using (StreamReader reader = new StreamReader(QuotesFile))
        {
            string row;
            string stock;
            string[] columns;

            reader.ReadLine();
            while ((row = reader.ReadLine()) != null)
            {
                columns = row.Split(',');
                stock = columns[0];
                quotes.Add(stock.Split('.')[0].ToUpper(), await YahooService.GetQuote(stock));
            }
        }
        return quotes;
    }

    public static void UpdateMarketPrice(List<Portfolio> portfolios, Dictionary<string, decimal> quotes)
    {
        foreach (var portfolio in portfolios)
        {
            foreach (var investment in portfolio.InvestmentByCurrency)
            {
                foreach (var holding in investment.Value.Holdings)
                {
                    if (holding.Value.Quantity == 0)
                    {
                        continue;
                    }

                    if(quotes.ContainsKey(holding.Key))
                    {
                        holding.Value.MarketPrice = quotes[holding.Key];
                    }
                }
            }
        }
    }

    public static Portfolio GetPortfolio(string path)
    {
        Portfolio portfolio = new Portfolio();

        using (StreamReader reader = new StreamReader(path))
        {
            string row;
            string type;
            int rowCount = 1;
            string[] columns;

            reader.ReadLine();
            while ((row = reader.ReadLine()) != null)
            {
                rowCount++;
                columns = row.Split(",");

                type = columns[1].ToUpper().Trim();


                bool isContribution = (type == Contribution || type == Withdrawal);
                bool isPayout = (type == Dividends || type == Interest);
                bool isExchange = (type == Exchange);
                bool isTransfer = (type == Transfer);
                if (isContribution || isPayout || isExchange || isTransfer)
                {
                    columns[3] = "0"; // quantity
                }

                bool isSplit = (type == Split || type == Reverse);
                if (isSplit)
                {
                    columns[4] = "0"; // amount
                }

                bool isPurchase = (type == Buy || type == Sell);
                if ((isContribution || isPurchase || isPayout || isSplit || isExchange || isTransfer) == false)
                {
                    string e = $""""
                        unrecongnized type {type}
                        please use one of the following:
                        {Contribution},
                        {Withdrawal},
                        {Buy},
                        {Sell},
                        {Dividends},
                        {Interest},
                        {Split},
                        {Reverse},
                        {Exchange} or,
                        {Transfer}
                    """";
                    throw new Exception(e);
                }

                Transaction transaction = new Transaction()
                {
                    Date = ParseDate(columns[0], path, rowCount),
                    Type = columns[1].ToUpper().TrimStart().TrimEnd(),
                    Name = columns[2].ToUpper().TrimStart().TrimEnd(),
                    Quantity = ParseDecimal(columns[3], path, rowCount),
                    Amount = ParseDecimal(columns[4], path, rowCount),
                    Currency = columns[5].ToUpper().TrimEnd()
                };
                try
                {

                    UpdatePortfolio(portfolio, transaction, path, rowCount);
                }
                catch (Exception e)
                {
                    Console.Clear();
                    Console.WriteLine(transaction);
                    Console.WriteLine($"Currently on row {rowCount} \n\n");
                    Console.WriteLine(e);
                }
            }

        }

        portfolio.Name = path.Split("/").Last().Replace(".csv", "");
        return portfolio;
    }

    // Process transactin and store in portfolio
    private static void UpdatePortfolio(Portfolio portfolio, Transaction transaction, string path, int rowCount)
    {
        if (string.IsNullOrEmpty(transaction.Name) &&
                (transaction.Type == Buy || transaction.Type == Sell || transaction.Type == Split || transaction.Type == Reverse))
        {
            throw new Exception($"No ticker or investment name chosen in {path} at row {rowCount} for transaction type {transaction.Type}");
        }

        if(string.IsNullOrEmpty(transaction.Currency))
        {
            throw new Exception($"No currency in {path} at row {rowCount} for transaction type {transaction.Type}");
        }

        Investment investment;
        if (portfolio.InvestmentByCurrency.TryGetValue(transaction.Currency, out investment) == false)
        {
            investment = new Investment();
            portfolio.InvestmentByCurrency.Add(transaction.Currency, investment);
        }

        bool requiresHoldings = (
            transaction.Type == Buy ||
            transaction.Type == Sell ||
            transaction.Type == Dividends ||
            transaction.Type == Interest ||
            transaction.Type == Split ||
            transaction.Type == Reverse
        );

        Holding holding;
        if (investment.Holdings.TryGetValue(transaction.Name, out holding) == false && requiresHoldings)
        {
            holding = new Holding();
            investment.Holdings.Add(transaction.Name, holding);
        }

        bool requiresContribution = (
            transaction.Type == Contribution ||
            transaction.Type == Withdrawal
        );

        Contribution contribution;
        if (investment.Contributions.TryGetValue(transaction.Date.Year, out contribution) == false && requiresContribution)
        {
            contribution = new Contribution();
            investment.Contributions.Add(transaction.Date.Year, contribution);
        }

        switch (transaction.Type)
        {
            case Contribution:
                investment.Cash += transaction.Amount;
                contribution.Deposit += transaction.Amount;
                break;

            case Withdrawal:
                investment.Cash -= transaction.Amount;
                contribution.Withdrawal -= transaction.Amount;
                break;

            case Buy:
                investment.Cash -= transaction.Amount;
                holding.Quantity += transaction.Quantity;
                holding.CostTotal += transaction.Amount;
                holding.DateLastPurcahsed = transaction.Date;
                break;

            case Sell:
                decimal bookValue = holding.CostPer() * transaction.Quantity;
                decimal profit = transaction.Amount - bookValue;
                investment.Cash += transaction.Amount;
                holding.Quantity -= transaction.Quantity;
                holding.CostTotal -= bookValue;
                holding.SalesProfit += profit;
                break;

            case Dividends or Interest:
                investment.Cash += transaction.Amount;
                holding.Payout += transaction.Amount;
                break;

            case Split:
                holding.Quantity = holding.Quantity * transaction.Quantity;
                break;

            case Reverse:
                holding.Quantity = holding.Quantity / transaction.Quantity;
                break;

            case Exchange:
                investment.Cash += transaction.Amount;
                investment.Exchange += transaction.Amount;
                break;
            
            case Transfer:

                break;

            Default:
                throw new Exception($"Unknown transaction type in {path} at row {rowCount}");
        }
    }

    public static Portfolio Merge(List<Portfolio> portfolios)
    {
        Portfolio result = new Portfolio();
        Investment resultCurrency;
        Contribution resultContribution;
        Holding resultHolding;
        
        result.Name = "Total";

        foreach (var portfolio in portfolios)
        {
            foreach(var currency in portfolio.InvestmentByCurrency)
            {
                if (result.InvestmentByCurrency.ContainsKey(currency.Key) == false)
                {
                    result.InvestmentByCurrency.Add(currency.Key, new Investment());
                }

                resultCurrency = result.InvestmentByCurrency[currency.Key];
                resultCurrency.Cash += currency.Value.Cash;
                resultCurrency.Exchange += currency.Value.Exchange;
                
                foreach (var contribution in currency.Value.Contributions)
                {
                    if (resultCurrency.Contributions.ContainsKey(contribution.Key) == false)
                    {
                        resultCurrency.Contributions.Add(contribution.Key, new Contribution());
                    }

                    resultContribution = result.InvestmentByCurrency[currency.Key].Contributions[contribution.Key];
                    resultContribution.Deposit += contribution.Value.Deposit;
                    resultContribution.Withdrawal += contribution.Value.Withdrawal;
                }

                foreach (var holding in currency.Value.Holdings)
                {
                    if (resultCurrency.Holdings.ContainsKey(holding.Key) == false)
                    {
                        resultCurrency.Holdings.Add(holding.Key, new Holding());
                    }
                    
                    resultHolding = resultCurrency.Holdings[holding.Key];
                    resultHolding.Quantity += holding.Value.Quantity;
                    resultHolding.CostTotal += holding.Value.CostTotal;
                    resultHolding.Payout += holding.Value.Payout;
                    resultHolding.SalesProfit += holding.Value.SalesProfit;
                }
                
            }
        }

        return result;
    }

    private static int ParseInt(string value, string path, int rowCount)
    {
        int result = 0;
        if (int.TryParse(value.ToUpper().TrimEnd(), out result) == false)
        {
            throw new Exception($"No quantity found in {path} at row {rowCount}");
        }

        return result;
    }

    private static decimal ParseDecimal(string value, string path, int rowCount)
    {
        decimal result = 0;
        if (decimal.TryParse(value.ToUpper().TrimEnd(), out result) == false)
        {
            throw new Exception($"No data found in {path} at row {rowCount}");
        }

        return result;
    }

    private static DateTime ParseDate(string value, string path, int rowCount)
    {
        DateTime result;
        if (DateTime.TryParse(value, out result) == false)
        {
            throw new Exception($"Could not parse date in {path} at row {rowCount}");
        }

        return result;
    }
}

