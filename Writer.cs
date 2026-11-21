using System.Numerics;
using Microsoft.Extensions.Configuration;

namespace InvestmentViewer;

public static class Writer
{
    private const string Tab = "    ";
    private const string Underline = "\x1B[4m";
    private const string Reset = "\x1B[0m";
    private static readonly int WidthSmall;
    private static readonly int WidthNormal;

    private static readonly char PortfolioSeparatorChar;
    private static readonly int PortfolioSeparatorWidth;
    private static readonly ConsoleColor PortfolioSeparatorColor;
    private static readonly ConsoleColor PortfolioMessageColour;

    private static readonly char InvestmentSeparatorChar;
    private static readonly int InvestmentSeparatorWidth;
    private static readonly ConsoleColor InvestmentSeparatorColor;
    private static readonly ConsoleColor InvestmentMessageColour;

    private static readonly char HoldingSeparatorChar;
    private static readonly int HoldingSeparatorWidth;
    private static readonly ConsoleColor HoldingSeparatorColor;
    private static readonly ConsoleColor HoldingMessageColour;

    private enum Position
    {
        Left,
        Center,
        Right
    }

    static Writer()
    {
        IConfigurationRoot config = ConfigFactory.Create();
        int.TryParse(config["General:width_small"], out WidthSmall);
        int.TryParse(config["General:width_normal"], out WidthNormal);

        PortfolioSeparatorChar = config["Portfolio:separator_char"][0];
        int.TryParse(config["Portfolio:separator_width"], out PortfolioSeparatorWidth);
        Enum.TryParse(config["Portfolio:separator_colour"] , out PortfolioSeparatorColor);
        Enum.TryParse(config["Portfolio:message_colour"], out PortfolioMessageColour);

        InvestmentSeparatorChar = config["Investment:separator_char"][0];
        int.TryParse(config["Investment:separator_width"], out InvestmentSeparatorWidth);
        Enum.TryParse(config["Investment:separator_colour"] , out InvestmentSeparatorColor);
        Enum.TryParse(config["Investment:message_colour"] , out InvestmentMessageColour);

        HoldingSeparatorChar = config["Holding:separator_char"][0];
        int.TryParse(config["Holding:separator_width"], out HoldingSeparatorWidth);
        Enum.TryParse(config["Holding:separator_colour"] , out HoldingSeparatorColor);
        Enum.TryParse(config["Holding:message_colour"], out HoldingMessageColour);
    }

    private static void WriteHeader(string message,char c, int repeat,
        ConsoleColor lineColour = ConsoleColor.White,
        ConsoleColor messageColour = ConsoleColor.White,
        bool upperCase = false)
    {
        Console.ForegroundColor = lineColour;
        WriteSeparator(c, repeat);

        Console.ForegroundColor = messageColour;
        if (upperCase)
        {
            Console.WriteLine(message.ToUpper());
        }
        else
        {
            Console.WriteLine(message);
        }

        Console.ForegroundColor = lineColour;
        WriteSeparator(c, repeat);
        Console.ResetColor();
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

    public static void WritePortfolios(List<Portfolio> portfolios)
    {
        foreach(var portfolio in portfolios)
        {
            WriteHeader($"{Tab}{portfolio.Name}",
                PortfolioSeparatorChar,
                PortfolioSeparatorWidth,
                PortfolioSeparatorColor,
                PortfolioMessageColour, true);

            WriteInvestment(portfolio);
            Console.WriteLine();
        }
    }

    private static void WriteInvestment(Portfolio portfolio)
    {
        string output = "";
        foreach (var investment in portfolio.InvestmentByCurrency)
        {
            WriteHeader($"{Tab}Currency {investment.Key}", InvestmentSeparatorChar, InvestmentSeparatorWidth,
                InvestmentSeparatorColor,
                InvestmentMessageColour,
                false
            );

            decimal deposits = investment.Value.Contributions.Sum(x => x.Value.Deposit);
            decimal withdrwals = investment.Value.Contributions.Sum(x => x.Value.Withdrawal);
            decimal netDeposits = deposits + withdrwals;
            decimal cash = investment.Value.Cash;
            decimal profits = investment.Value.Holdings.Sum(x => x.Value.SalesProfit);
            decimal payouts = investment.Value.Holdings.Sum(x => x.Value.Payout);
            decimal bookValue = investment.Value.Holdings.Sum(x => x.Value.CostTotal);

            output = "{0}{1}{2}";
            Console.WriteLine(output, Tab, WriteLabel("Deposits", WidthNormal), WriteNumeric(deposits, WidthNormal));
            Console.WriteLine(output, Tab, WriteLabel("Withdrawal", WidthNormal), WriteNumeric(withdrwals, WidthNormal));
            Console.WriteLine(output, Tab, WriteLabel("Net Deposits", WidthNormal), WriteNumeric(netDeposits, WidthNormal));
            Console.WriteLine();

            Console.WriteLine(output, Tab, WriteLabel("Payouts", WidthNormal), WriteNumeric(payouts, WidthNormal));
            Console.WriteLine(output, Tab, WriteLabel("Sales Profits", WidthNormal), WriteNumeric(profits, WidthNormal));
            Console.WriteLine();

            Console.WriteLine(output, Tab, WriteLabel("Book Value", WidthNormal), WriteNumeric(bookValue, WidthNormal));
            Console.WriteLine(output, Tab, WriteLabel("Market Value", WidthNormal), 
                WriteLabel(investment.Value.Holdings.Sum(x=> x.Value.MarketValue()).ToString("N2"), 
                    WidthNormal, Position.Right));
            Console.WriteLine(output, Tab, WriteLabel("Open Profit", WidthNormal), 
                WriteNumeric(investment.Value.Holdings.Sum(x=> x.Value.OpenProfit()), WidthNormal));
            Console.WriteLine();
            
            Console.WriteLine(output, Tab, WriteLabel("Cash", WidthNormal), WriteNumeric(cash, WidthNormal));
            Console.WriteLine();

            decimal totalValue = investment.Value.Holdings.Sum(x => x.Value.CostTotal)
                                 + investment.Value.Holdings.Sum(x => x.Value.OpenProfit())
                                 + investment.Value.Cash;
                                     
            decimal percentage = 0;
            if (investment.Value.Contributions.Values.Sum(x => x.Deposit) > 0)
            {
                percentage = totalValue / investment.Value.Contributions.Sum(x => x.Value.Deposit) -1;
            }
            
            Console.WriteLine(output, Tab, WriteLabel("Total Value", WidthNormal),
                WriteNumeric(
                    totalValue,
                    WidthNormal) 
            );
            
            Console.WriteLine(output, Tab, WriteLabel("%", WidthNormal), WriteLabel( percentage.ToString("P1"), WidthNormal, Position.Right) );
            Console.WriteLine();
            

            WriteHoldings(investment.Value);

        }
    }

    private static void WriteHoldings(Investment investment)
    {
        Dictionary<string, Holding> holdings = investment.Holdings;
        IEnumerable<KeyValuePair<string, Holding>> aboveZero = holdings.Where(x => x.Value.Quantity > 0).OrderBy(x=> x.Key);
        
        if (aboveZero.Count() > 0)
        {
            WriteTableHoldingCurrent(aboveZero);
        }

        if(holdings.Count() > 0)
        {
            WriteTableHoldings(investment);
        }

    }

    private static void WriteTableHoldingCurrent(IEnumerable<KeyValuePair<string, Holding>> holdings)
    {
        string template= "_{0}{1}_{2}_{3}_{4}_{5}_{6}_{7}_{8}{9}";
        string header = template.Replace('_',' ');
        string row = template.Replace('_', '|');

        Console.WriteLine(header,
            Underline, 
            WriteLabel("Name", WidthSmall, Position.Center), 
            WriteLabel("Quantity", WidthNormal, Position.Center), 
            WriteLabel("Cost Per", WidthSmall, Position.Center), 
            WriteLabel("Market Value", WidthNormal, Position.Center),
            WriteLabel("Book Value", WidthNormal, Position.Center), 
            WriteLabel("Market Value", WidthNormal, Position.Center), 
            WriteLabel("Open Profit", WidthNormal, Position.Center), 
            WriteLabel("%", WidthSmall, Position.Center), 
            Reset
        );

        foreach (var holding in holdings)
        {
            Console.WriteLine(row,
                Underline,
                WriteLabel(holding.Key, WidthSmall),
                WriteNumeric(holding.Value.Quantity, WidthNormal),
                WriteLabel(holding.Value.CostPer().ToString("N2"), WidthSmall,Position.Right),
                WriteLabel(holding.Value.MarketPrice.ToString("N2"), WidthNormal, Position.Right),
                WriteNumeric(holding.Value.CostTotal, WidthNormal),
                WriteNumeric(holding.Value.MarketValue(), WidthNormal),
                WriteNumeric(holding.Value.OpenProfit(), WidthNormal),
                WriteLabel($"{holding.Value.MarketValue() / holding.Value.CostTotal - 1:P1}",
                    WidthSmall, Position.Right), 
                Reset
            );
        }

        Console.WriteLine();
    }

    private static void WriteTableHoldings(Investment investment)
    {
        Dictionary<string, Holding> holdings = investment.Holdings;
        
        string template= "_{0}{1}_{2}_{3}_{4}_{5}_{6}_{7}_{8}_{9}{10}_";
        string header = template.Replace('_',' ');
        string row = template.Replace('_', '|');

        Console.WriteLine(header,
            Underline, WriteLabel("Name", WidthSmall, Position.Center), 
            WriteLabel("Quantity", WidthNormal, Position.Center), 
            WriteLabel("Cost Per", WidthSmall, Position.Center), 
            WriteLabel("Book Value", WidthNormal, Position.Center), 
            WriteLabel("Payout", WidthNormal, Position.Center), 
            WriteLabel("Sales Profit", WidthNormal, Position.Center),
            WriteLabel("Open Profit", WidthNormal, Position.Center),
            WriteLabel("Total Profit", WidthNormal, Position.Center),
            WriteLabel("%", WidthSmall, Position.Center),
            Reset
        );

        foreach (var holding in holdings)
        {
            decimal percentage = 0;
            if (investment.Contributions.Sum(x => x.Value.Deposit) > 0)
            {
                percentage = (holding.Value.TotalProfit() / investment.Contributions.Sum(x => x.Value.Deposit));
            }
            
            Console.WriteLine(row,
                Underline, WriteLabel(holding.Key, WidthSmall), 
                WriteNumeric(holding.Value.Quantity, WidthNormal), 
                WriteNumeric(holding.Value.CostPer(), WidthSmall), 
                WriteNumeric(holding.Value.CostTotal, WidthNormal), 
                WriteNumeric(holding.Value.Payout, WidthNormal), 
                WriteNumeric(holding.Value.SalesProfit, WidthNormal), 
                WriteNumeric(holding.Value.OpenProfit(), WidthNormal),
                WriteNumeric(holding.Value.TotalProfit(), WidthNormal),
                WriteLabel(percentage.ToString("P1") , WidthSmall, Position.Center),
                Reset
            );

        }

        Console.WriteLine();
    }

    private static string WriteNumeric<T>(T value, int width) where T : INumber<T>
    {

        char[] result = new char[width];
        string valueAsString = value.ToString();

        if(typeof(T) == typeof(decimal))
        {
            IFormattable formattable = value;
            valueAsString = ((decimal)formattable).ToString("N2");
        }

        if (valueAsString.Length > width) { throw new Exception("Can not write numeric value is too wide"); }

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

        switch (position)
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

                for (int i = 0; i < value.Length; i++)
                {
                    result[i + leftPadCount] = value[i];
                }

                break;

            case Position.Right:
                int difference = width - value.Length;

                for (int i = value.Length - 1; i > -1; i--)
                {
                    result[i + difference] = value[i];
                }

                for(int i = 0; i < difference; i++)
                {
                    result[i] = ' ';
                }
                break;
        }

        return new string(result);
    }
}
