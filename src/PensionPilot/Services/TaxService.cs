using PensionPilot.Models.Tax;

namespace PensionPilot.Services;

public class TaxService : ITaxService
{
    public decimal CalculateIncomeTax(decimal annualIncome, IEnumerable<IncomeTaxBracket> brackets)
    {
        decimal tax = 0;
        if (annualIncome <= 0) return 0;

        decimal lower = 0;
        foreach (var b in brackets.OrderBy(b => b.Value ?? decimal.MaxValue))
        {
            var upper = b.Value ?? decimal.MaxValue;
            if (annualIncome <= lower) break;
            var taxableInBracket = Math.Min(annualIncome, upper) - lower;
            if (taxableInBracket > 0)
            {
                tax += taxableInBracket * b.Rate;
            }
            if (annualIncome <= upper) break;
            lower = upper;
        }
        return Math.Max(tax, 0);
    }
}
