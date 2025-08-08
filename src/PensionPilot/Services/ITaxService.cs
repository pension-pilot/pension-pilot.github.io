using PensionPilot.Models.Tax;

namespace PensionPilot.Services;

public interface ITaxService
{
    decimal CalculateIncomeTax(decimal annualIncome, IEnumerable<IncomeTaxBracket> brackets);
}
