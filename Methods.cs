using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
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
    private const int WidthLabel = 18;
    private const int WidthDecimal = 15;
    private const int WidthSeperator = 50;
    const string Underline = "\x1B[4m";
    const string Reset = "\x1B[0m";

    private enum Position
    {
        Left,
        Center
    }

    public static string[] GetFiles()
    {
        string[] results = new string[0];

        string path = "Investments";
        if (Directory.Exists(path))
        {
            results = Directory.GetFiles(path, "*csv");
        }

        return results;
    }

    public static Portfolio GetPortfolio(string path)
    {
        Portfolio portfolio = new Portfolio();

        using (StreamReader reader = new StreamReader(path))
        {
            string row;
            string type;
            int rowCount = 1;

            reader.ReadLine();
            while ((row = reader.ReadLine()) != null)
            {
                rowCount++;
                string[] columns = row.Split(",");

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
                if ((isContribution || isPurchase || isPayout || isSplit) == false)
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
                    Type = columns[1].ToUpper().TrimEnd(),
                    Name = columns[2].ToUpper().TrimEnd(),
                    Quantity = ParseInt(columns[3], path, rowCount),
                    Amount = ParseDecimal(columns[4], path, rowCount),
                    Currency = columns[5].ToUpper().TrimEnd()
                };
                try
                {

                    ProcessTransaction(portfolio, transaction, path, rowCount);
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

        portfolio.Name = path.Split("/")[1].Replace(".csv", "");
        return portfolio;
    }

    // Process transactin and store in portfolio
    private static void ProcessTransaction(Portfolio portfolio, Transaction transaction, string path, int rowCount)
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

    public static void WriteSeparator(char c, int repeat)
    {
        string result = string.Empty;
        for (int i = 0; i < repeat; i++)
        {
            result += c;
        }
        Console.WriteLine(result);

    }

    public static void WritePortfolios(Portfolio[] portfolios)
    {
        foreach(var portfolio in portfolios)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            WriteSeparator('*', WidthSeperator);

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"    {portfolio.Name.ToUpper()}");

            Console.ForegroundColor = ConsoleColor.DarkGray;
            WriteSeparator('*', WidthSeperator);
            Console.ResetColor();

            Console.WriteLine();

            WritePortfolio(portfolio);
            Console.WriteLine();
        }
    }

    private static void WritePortfolio(Portfolio portfolio)
    {

        string output = "";
        foreach (var investment in portfolio.InvestmentByCurrency)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            WriteSeparator('=', WidthSeperator);
            Console.WriteLine($"    Currency {investment.Key}");
            WriteSeparator('=', WidthSeperator);
            Console.ResetColor();

            decimal deposits = investment.Value.Contributions.Sum(x => x.Value.Deposit);
            decimal withdrwals = investment.Value.Contributions.Sum(x => x.Value.Withdrawal);
            decimal netDeposits = deposits + withdrwals;
            decimal cash = investment.Value.Cash;
            decimal profits = investment.Value.Holdings.Sum(x => x.Value.SalesProfit);
            decimal payouts = investment.Value.Holdings.Sum(x => x.Value.Payout);
            decimal bookValue = investment.Value.Holdings.Sum(x => x.Value.CostTotal);

            output = "    {0} {1}";
            Console.WriteLine(output, WriteLabel("Deposits", WidthLabel), WriteNumeric(deposits, WidthDecimal));
            Console.WriteLine(output, WriteLabel("Withdrawal", WidthLabel), WriteNumeric(withdrwals, WidthDecimal));
            Console.WriteLine(output, WriteLabel("Net Deposits", WidthLabel), WriteNumeric(netDeposits, WidthDecimal));
            Console.WriteLine();

            Console.WriteLine(output, WriteLabel("Payouts", WidthLabel),WriteNumeric(payouts, WidthDecimal));
            Console.WriteLine(output, WriteLabel("Profits", WidthLabel), WriteNumeric(profits, WidthDecimal));
            Console.WriteLine();

            Console.WriteLine(output, WriteLabel("Book Value", WidthLabel), WriteNumeric(bookValue, WidthDecimal));
            Console.WriteLine(output, WriteLabel("Cash", WidthLabel), WriteNumeric(cash, WidthDecimal));
            Console.WriteLine();

            WriteHoldings(investment.Value.Holdings);

        }
    }

    private static void WriteHoldings(Dictionary<string, Holding> holdings)
    {
        IEnumerable<KeyValuePair<string, Holding>> aboveZero = holdings.Where(x => x.Value.Quantity > 0).OrderBy(x=> x.Key);
        IEnumerable<KeyValuePair<string, Holding>> belowZero = holdings.Where(x => x.Value.Quantity <= 0).OrderBy(x=> x.Key);

        if (aboveZero.Count() > 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            WriteSeparator('-', WidthSeperator);
            Console.WriteLine("Current Holdings");
            WriteSeparator('-', WidthSeperator);
            Console.ResetColor();
            WriteHoldingTable(aboveZero);
        }

        if(belowZero.Count()> 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            WriteSeparator('-', WidthSeperator);
            Console.WriteLine("    Past Holdings");
            WriteSeparator('-', WidthSeperator);
            Console.ResetColor();
            WriteHoldingTable(belowZero);
        }

    }

    private static void WriteHoldingTable(IEnumerable<KeyValuePair<string, Holding>> holdings)
    {
        string header = " {0}{1} {2} {3} {4} {5} {6} {7}{8} ";
        string row = header.Replace(' ', '|');

        Console.WriteLine(header,
            Underline,
            WriteLabel("Name", WidthLabel, Position.Center),
            WriteLabel("Quantity", WidthLabel, Position.Center),
            WriteLabel("Cost Per", WidthLabel, Position.Center),
            WriteLabel("Cost Total", WidthLabel, Position.Center),
            WriteLabel("Payout", WidthLabel, Position.Center),
            WriteLabel("Sales Profit", WidthLabel, Position.Center),
            WriteLabel("Last Purchased", WidthLabel, Position.Center),
            Reset
        );

        foreach (var holding in holdings)
        {
            Console.WriteLine(row,
                Underline,
                WriteLabel(holding.Key, WidthLabel),
                WriteNumeric(holding.Value.Quantity, WidthLabel),
                WriteNumeric(holding.Value.CostPer(), WidthLabel),
                WriteNumeric(holding.Value.CostTotal, WidthLabel),
                WriteNumeric(holding.Value.Payout, WidthLabel),
                WriteNumeric(holding.Value.SalesProfit, WidthLabel),
                WriteLabel(holding.Value.DateLastPurcahsed.ToString("yyyy-MM-dd"), WidthLabel),
                Reset
            );

        }

        Console.WriteLine();
    }

    private static string WriteNumeric<T>(T value, int width) where T : INumber<T>
    {
        char[] result = new char[width];
        string valueAsString = value.ToString();

        if (valueAsString.Length > width) { throw new Exception("Can not write label value is too wide"); }

        if(typeof(T) == typeof(decimal))
        {
            IFormattable formattable = value;
            valueAsString = ((decimal)formattable).ToString("N2");
        }

        int difference = width - valueAsString.Length;

        for (int i = valueAsString.Length - 1; i > -1; i--)
        {
            result[i + difference] = valueAsString[i];
        }

        for(int i = 0; i < difference; i++)
        {
            result[i] = ' ';
        }

        return new string(result);
    }

    private static string WriteLabel(string value, int width, Position position = Position.Left)
    {
        if (value.Length > width) { throw new Exception("Can not write label value is too wide"); }
        char[] result = new char[width];

        switch(position)
        {
            case Position.Left:
                for (int i = 0; i < value.Length; i++)
                {
                    result[i] = value[i];
                }
                for (int i = value.Length; i < width; i++)
                {
                    result[i] = ' ';
                }
                break;

            case Position.Center:
                int leftPadCount = (width - value.Length) / 2;
                for (int i = 0; i < width; i++)
                {
                    result[i] = ' ';
                }

                for(int i = 0; i < value.Length; i++)
                {
                    result[i + leftPadCount] = value[i];
                }

                break;
        }

        return new string(result);
    }
}
