namespace InvestmentViewer;

// Portfolio holds investments by currency
public class Portfolio
{
    public string Name { get; set; } = "";
    public Dictionary<string, Investment> InvestmentByCurrency = new Dictionary<string, Investment>();
}

/// <summary>
/// Reprisents a stock, bond, GIC, or savings account
/// </summary>
public class Investment
{
    public decimal Cash { get; set; }
    public Dictionary<int, Contribution> Contributions { get; set; } = new Dictionary<int, Contribution>(); // int is year
    public Dictionary<string, Holding> Holdings { get; set; } = new Dictionary<string, Holding>(); // string is the position name
}

public class Contribution
{
    public decimal Deposit { get; set; } = 0;
    public decimal Withdrawal { get; set; } = 0;
}

/// <summary>
/// Reprisents a stock, bond, GIC, or savings account
/// </summary>
public class Holding
{
    public DateTime DateLastPurcahsed { get; set; }
    public int Quantity { get; set; } = 0;
    public decimal CostTotal { get; set; } = 0; // Book value
    public decimal Payout { get; set; } = 0;    // Dividend or interest depending on holding type
    public decimal SalesProfit { get; set; } = 0;
    public decimal CostPer()
    {
        if(Quantity == 0){ return 0; }
        return CostTotal / Quantity;
    }
}

public record Transaction
{
    public DateTime Date { get; init; }
    public string Type { get; init; } = "";   // Buy, Sell, Contribution, Withdrawal, Split
    public string Name { get; set; } = "";
    public int Quantity { get; init; }
    public decimal Amount { get; init; } // Total value of transaction
    public string Currency { get; set; } = "";
}
