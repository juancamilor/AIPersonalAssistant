using AIPersonalAssistant.Web.Models;
using Azure;
using Azure.AI.DocumentIntelligence;
using ClosedXML.Excel;

namespace AIPersonalAssistant.Web.Services;

public class TaxesService : ITaxesService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TaxesService> _logger;

    // 2025 MFJ Federal Tax Brackets
    private static readonly (decimal Limit, decimal Rate)[] OrdinaryBrackets = new[]
    {
        (23_850m, 0.10m),
        (96_950m, 0.12m),
        (206_700m, 0.22m),
        (394_600m, 0.24m),
        (501_050m, 0.32m),
        (751_600m, 0.35m),
        (decimal.MaxValue, 0.37m)
    };

    private static readonly (decimal Limit, decimal Rate)[] LongTermCapGainsBrackets = new[]
    {
        (96_700m, 0.00m),
        (600_050m, 0.15m),
        (decimal.MaxValue, 0.20m)
    };

    private const decimal StandardDeductionMFJ2025 = 30_000m;

    public TaxesService(IConfiguration configuration, ILogger<TaxesService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<W2Data> ExtractW2Async(Stream imageStream, string contentType)
    {
        var endpoint = _configuration["AzureDocumentIntelligence:Endpoint"];
        var apiKey = _configuration["AzureDocumentIntelligence:ApiKey"];

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Azure Document Intelligence is not configured.");

        var client = new DocumentIntelligenceClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms);
        var binaryData = BinaryData.FromBytes(ms.ToArray());

        var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-tax.us.w2", binaryData);
        var result = operation.Value;

        var w2 = new W2Data();

        if (result.Documents != null && result.Documents.Count > 0)
        {
            var doc = result.Documents[0];
            var fields = doc.Fields;

            w2.EmployerName = GetFieldString(fields, "EmployerName");
            w2.EmployerEin = GetFieldString(fields, "EmployerIdNumber");
            w2.EmployerAddress = GetFieldString(fields, "EmployerAddress");
            w2.EmployeeName = GetFieldString(fields, "EmployeeName");
            w2.EmployeeSsn = GetFieldString(fields, "EmployeeSocialSecurityNumber");
            w2.WagesTipsOtherCompensation = GetFieldDecimal(fields, "WagesTipsAndOtherCompensation");
            w2.FederalIncomeTaxWithheld = GetFieldDecimal(fields, "FederalIncomeTaxWithheld");
            w2.SocialSecurityWages = GetFieldDecimal(fields, "SocialSecurityWages");
            w2.SocialSecurityTaxWithheld = GetFieldDecimal(fields, "SocialSecurityTaxWithheld");
            w2.MedicareWagesAndTips = GetFieldDecimal(fields, "MedicareWagesAndTips");
            w2.MedicareTaxWithheld = GetFieldDecimal(fields, "MedicareTaxWithheld");
            w2.StateIncomeTaxWithheld = GetFieldDecimal(fields, "StateTaxWithheld");
            w2.State = GetFieldString(fields, "State");
        }

        return w2;
    }

    private static string? GetFieldString(IReadOnlyDictionary<string, DocumentField> fields, string key)
    {
        if (fields.TryGetValue(key, out var field) && field.Content != null)
            return field.Content;
        return null;
    }

    private static decimal GetFieldDecimal(IReadOnlyDictionary<string, DocumentField> fields, string key)
    {
        if (fields.TryGetValue(key, out var field))
        {
            if (field.FieldType == DocumentFieldType.Currency && field.ValueCurrency != null)
                return (decimal)field.ValueCurrency.Amount;
            if (field.FieldType == DocumentFieldType.Double)
                return (decimal)(field.ValueDouble ?? 0);
            if (field.Content != null && decimal.TryParse(field.Content.Replace(",", "").Replace("$", ""), out var val))
                return val;
        }
        return 0m;
    }

    public List<StockSale> ParseStockSalesExcel(Stream excelStream)
    {
        var sales = new List<StockSale>();
        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheets.First();

        var headerRow = worksheet.Row(1);
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var rangeUsed = worksheet.RangeUsed();
        if (rangeUsed == null) return sales;
        int lastCol = rangeUsed.LastColumn().ColumnNumber();
        int lastRow = rangeUsed.LastRow().RowNumber();
        for (int col = 1; col <= lastCol; col++)
        {
            var header = headerRow.Cell(col).GetString().Trim();
            if (!string.IsNullOrEmpty(header))
                headers[header] = col;
        }

        for (int row = 2; row <= lastRow; row++)
        {
            try
            {
                var sale = new StockSale
                {
                    Symbol = GetCellString(worksheet, row, headers, "Symbol"),
                    BuyDate = GetCellDate(worksheet, row, headers, "Buy Date"),
                    BuyPrice = GetCellDecimal(worksheet, row, headers, "Buy Price"),
                    SellDate = GetCellDate(worksheet, row, headers, "Sell Date"),
                    SellPrice = GetCellDecimal(worksheet, row, headers, "Sell Price"),
                    Quantity = (int)GetCellDecimal(worksheet, row, headers, "Quantity"),
                    HoldingType = GetCellString(worksheet, row, headers, "Type").Contains("Long", StringComparison.OrdinalIgnoreCase) ? "LongTerm" : "ShortTerm"
                };
                if (!string.IsNullOrEmpty(sale.Symbol))
                    sales.Add(sale);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse row {Row} in stock sales Excel", row);
            }
        }

        return sales;
    }

    private static string GetCellString(IXLWorksheet ws, int row, Dictionary<string, int> headers, string key)
    {
        return headers.TryGetValue(key, out var col) ? ws.Cell(row, col).GetString().Trim() : "";
    }

    private static decimal GetCellDecimal(IXLWorksheet ws, int row, Dictionary<string, int> headers, string key)
    {
        if (!headers.TryGetValue(key, out var col)) return 0;
        var cell = ws.Cell(row, col);
        if (cell.DataType == XLDataType.Number) return (decimal)cell.GetDouble();
        if (decimal.TryParse(cell.GetString().Replace(",", "").Replace("$", ""), out var val)) return val;
        return 0;
    }

    private static DateTime GetCellDate(IXLWorksheet ws, int row, Dictionary<string, int> headers, string key)
    {
        if (!headers.TryGetValue(key, out var col)) return DateTime.MinValue;
        var cell = ws.Cell(row, col);
        if (cell.DataType == XLDataType.DateTime) return cell.GetDateTime();
        if (DateTime.TryParse(cell.GetString(), out var date)) return date;
        return DateTime.MinValue;
    }

    public TaxEstimateResult CalculateTaxEstimate(W2Data w2, List<StockSale> stockSales)
    {
        var summary = new StockSalesSummary { Sales = stockSales };
        summary.TotalShortTermGain = stockSales.Where(s => s.HoldingType == "ShortTerm").Sum(s => s.GainLoss);
        summary.TotalLongTermGain = stockSales.Where(s => s.HoldingType == "LongTerm").Sum(s => s.GainLoss);
        summary.NetCapitalGain = summary.TotalShortTermGain + summary.TotalLongTermGain;

        // Ordinary income = W2 wages + short-term capital gains
        var ordinaryIncome = w2.WagesTipsOtherCompensation + Math.Max(0, summary.TotalShortTermGain);
        var longTermGains = Math.Max(0, summary.TotalLongTermGain);

        // Apply standard deduction to ordinary income first
        var taxableOrdinary = Math.Max(0, ordinaryIncome - StandardDeductionMFJ2025);
        var taxableIncome = taxableOrdinary + longTermGains;

        // Calculate ordinary income tax
        var ordinaryTax = CalculateProgressiveTax(taxableOrdinary, OrdinaryBrackets);

        // Calculate long-term capital gains tax
        var capGainsTax = CalculateProgressiveTax(longTermGains, LongTermCapGainsBrackets);

        var totalTax = ordinaryTax + capGainsTax;
        var totalWithheld = w2.FederalIncomeTaxWithheld;
        var refundOrOwed = totalWithheld - totalTax;

        // Build filing cheat sheet
        var cheatSheet = BuildCheatSheet(w2, stockSales, summary, taxableIncome, totalTax, totalWithheld, refundOrOwed);

        return new TaxEstimateResult
        {
            W2Data = w2,
            StockSalesSummary = summary,
            TaxableIncome = taxableIncome,
            OrdinaryIncomeTax = ordinaryTax,
            CapitalGainsTax = capGainsTax,
            TotalTaxOwed = totalTax,
            TotalWithheld = totalWithheld,
            EstimatedRefundOrOwed = refundOrOwed,
            FilingCheatSheet = cheatSheet
        };
    }

    private static decimal CalculateProgressiveTax(decimal income, (decimal Limit, decimal Rate)[] brackets)
    {
        decimal tax = 0;
        decimal previousLimit = 0;
        foreach (var (limit, rate) in brackets)
        {
            if (income <= 0) break;
            var taxableInBracket = Math.Min(income, limit - previousLimit);
            tax += taxableInBracket * rate;
            income -= taxableInBracket;
            previousLimit = limit;
        }
        return Math.Round(tax, 2);
    }

    private FilingCheatSheet BuildCheatSheet(W2Data w2, List<StockSale> sales, StockSalesSummary summary, decimal taxableIncome, decimal totalTax, decimal totalWithheld, decimal refundOrOwed)
    {
        var sheet = new FilingCheatSheet
        {
            DeductionAmount = StandardDeductionMFJ2025,
            TotalIncome = w2.WagesTipsOtherCompensation + summary.NetCapitalGain,
            AdjustedGrossIncome = w2.WagesTipsOtherCompensation + summary.NetCapitalGain,
            TaxableIncome = taxableIncome,
            TotalTax = totalTax,
            TotalWithheld = totalWithheld,
            RefundOrOwed = refundOrOwed
        };

        // W2 entries
        sheet.W2Entries.Add(new CheatSheetEntry { Label = "Employer Name", Value = w2.EmployerName ?? "" });
        sheet.W2Entries.Add(new CheatSheetEntry { Label = "Employer EIN", Value = w2.EmployerEin ?? "" });
        sheet.W2Entries.Add(new CheatSheetEntry { Label = "Box 1 - Wages", Value = w2.WagesTipsOtherCompensation.ToString("C") });
        sheet.W2Entries.Add(new CheatSheetEntry { Label = "Box 2 - Federal Tax Withheld", Value = w2.FederalIncomeTaxWithheld.ToString("C") });
        sheet.W2Entries.Add(new CheatSheetEntry { Label = "Box 3 - Social Security Wages", Value = w2.SocialSecurityWages.ToString("C") });
        sheet.W2Entries.Add(new CheatSheetEntry { Label = "Box 4 - SS Tax Withheld", Value = w2.SocialSecurityTaxWithheld.ToString("C") });
        sheet.W2Entries.Add(new CheatSheetEntry { Label = "Box 5 - Medicare Wages", Value = w2.MedicareWagesAndTips.ToString("C") });
        sheet.W2Entries.Add(new CheatSheetEntry { Label = "Box 6 - Medicare Tax Withheld", Value = w2.MedicareTaxWithheld.ToString("C") });
        if (w2.StateIncomeTaxWithheld > 0)
            sheet.W2Entries.Add(new CheatSheetEntry { Label = "Box 17 - State Tax Withheld", Value = w2.StateIncomeTaxWithheld.ToString("C") });

        // Schedule D entries
        foreach (var sale in sales)
        {
            sheet.ScheduleDEntries.Add(new ScheduleDEntry
            {
                Description = $"{sale.Quantity} shares {sale.Symbol}",
                DateAcquired = sale.BuyDate.ToString("MM/dd/yyyy"),
                DateSold = sale.SellDate.ToString("MM/dd/yyyy"),
                Proceeds = sale.Proceeds,
                CostBasis = sale.CostBasis,
                GainLoss = sale.GainLoss,
                Term = sale.HoldingType == "LongTerm" ? "Long-Term" : "Short-Term"
            });
        }

        return sheet;
    }
}
