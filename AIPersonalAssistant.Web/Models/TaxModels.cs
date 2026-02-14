namespace AIPersonalAssistant.Web.Models;

public class W2Data
{
    public string? EmployerName { get; set; }
    public string? EmployerEin { get; set; }
    public string? EmployerAddress { get; set; }
    public string? EmployeeName { get; set; }
    public string? EmployeeSsn { get; set; }
    public decimal WagesTipsOtherCompensation { get; set; } // Box 1
    public decimal FederalIncomeTaxWithheld { get; set; } // Box 2
    public decimal SocialSecurityWages { get; set; } // Box 3
    public decimal SocialSecurityTaxWithheld { get; set; } // Box 4
    public decimal MedicareWagesAndTips { get; set; } // Box 5
    public decimal MedicareTaxWithheld { get; set; } // Box 6
    public decimal StateIncomeTaxWithheld { get; set; } // Box 17
    public string? State { get; set; } // Box 15
}

public class StockSale
{
    public string Symbol { get; set; } = "";
    public DateTime BuyDate { get; set; }
    public decimal BuyPrice { get; set; }
    public DateTime SellDate { get; set; }
    public decimal SellPrice { get; set; }
    public int Quantity { get; set; }
    public string HoldingType { get; set; } = "ShortTerm"; // ShortTerm or LongTerm
    public decimal Proceeds => SellPrice * Quantity;
    public decimal CostBasis => BuyPrice * Quantity;
    public decimal GainLoss => Proceeds - CostBasis;
}

public class StockSalesSummary
{
    public decimal TotalShortTermGain { get; set; }
    public decimal TotalLongTermGain { get; set; }
    public decimal NetCapitalGain { get; set; }
    public List<StockSale> Sales { get; set; } = new();
}

public class FilingCheatSheet
{
    public string FilingStatus { get; set; } = "Married Filing Jointly";
    public List<CheatSheetEntry> W2Entries { get; set; } = new();
    public List<ScheduleDEntry> ScheduleDEntries { get; set; } = new();
    public string DeductionType { get; set; } = "Standard Deduction";
    public decimal DeductionAmount { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal AdjustedGrossIncome { get; set; }
    public decimal TaxableIncome { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalWithheld { get; set; }
    public decimal RefundOrOwed { get; set; }
}

public class CheatSheetEntry
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
}

public class ScheduleDEntry
{
    public string Description { get; set; } = "";
    public string DateAcquired { get; set; } = "";
    public string DateSold { get; set; } = "";
    public decimal Proceeds { get; set; }
    public decimal CostBasis { get; set; }
    public decimal GainLoss { get; set; }
    public string Term { get; set; } = "";
}

public class TaxEstimateResult
{
    public W2Data? W2Data { get; set; }
    public StockSalesSummary? StockSalesSummary { get; set; }
    public decimal TaxableIncome { get; set; }
    public decimal OrdinaryIncomeTax { get; set; }
    public decimal CapitalGainsTax { get; set; }
    public decimal TotalTaxOwed { get; set; }
    public decimal TotalWithheld { get; set; }
    public decimal EstimatedRefundOrOwed { get; set; }
    public string FilingStatus { get; set; } = "Married Filing Jointly";
    public string Disclaimer { get; set; } = "⚠️ This is an estimate for informational purposes only. It is not tax advice. Consult a tax professional for accurate filing.";
    public FilingCheatSheet? FilingCheatSheet { get; set; }
}

public class StockSalesUploadRequest
{
    public List<StockSale> StockSales { get; set; } = new();
}

public class TaxEstimateRequest
{
    public W2Data? W2Data { get; set; }
    public List<StockSale> StockSales { get; set; } = new();
}
