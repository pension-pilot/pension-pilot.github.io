using System.Text;

namespace PensionPilot.Services;

public static class CsvExportHelper
{
    private static readonly string[] Header =
    [
        nameof(YearResult.YearIndex),
        nameof(YearResult.Age),
        nameof(YearResult.SalaryGross),
        nameof(YearResult.SalaryTax),
        nameof(YearResult.SalaryNet),
        nameof(YearResult.RsuVested),
        nameof(YearResult.RsuTax),
        nameof(YearResult.RsuNet),
        nameof(YearResult.PensionPayoutGross),
        nameof(YearResult.PensionTax),
        nameof(YearResult.PensionPayoutNet),
        nameof(YearResult.PortfolioStart),
        nameof(YearResult.PortfolioReturn),
        nameof(YearResult.PortfolioWithdrawalGross),
        nameof(YearResult.PortfolioCapGainsTax),
        nameof(YearResult.PortfolioEnd),
        nameof(YearResult.StudyFundStart),
        nameof(YearResult.StudyFundReturn),
        nameof(YearResult.StudyFundWithdrawal),
        nameof(YearResult.StudyFundEnd),
        nameof(YearResult.Expenses),
        nameof(YearResult.NetCashflow),
        nameof(YearResult.NetWorthEnd),
        nameof(YearResult.PensionBalanceEnd),
        nameof(YearResult.NetIncome)
    ];

    private static readonly ReadOnlyMemory<byte> HeaderBytes = Encoding.UTF8.GetBytes(string.Join(',', Header));

    /// <summary>
    /// Builds a UTF-8 CSV for the given results.
    /// </summary>
    public static byte[] BuildYearResultsCsv(IEnumerable<YearResult> results)
    {
        using var ms = new MemoryStream();
        ms.Write(HeaderBytes.Span);

        foreach (var r in results)
        {
            ms.WriteWithComma(r.YearIndex);
            ms.WriteWithComma(r.Age);
            ms.WriteWithComma(r.SalaryGross);
            ms.WriteWithComma(r.SalaryTax);
            ms.WriteWithComma(r.SalaryNet);
            ms.WriteWithComma(r.RsuVested);
            ms.WriteWithComma(r.RsuTax);
            ms.WriteWithComma(r.RsuNet);
            ms.WriteWithComma(r.PensionPayoutGross);
            ms.WriteWithComma(r.PensionTax);
            ms.WriteWithComma(r.PensionPayoutNet);
            ms.WriteWithComma(r.PortfolioStart);
            ms.WriteWithComma(r.PortfolioReturn);
            ms.WriteWithComma(r.PortfolioWithdrawalGross);
            ms.WriteWithComma(r.PortfolioCapGainsTax);
            ms.WriteWithComma(r.PortfolioEnd);
            ms.WriteWithComma(r.StudyFundStart);
            ms.WriteWithComma(r.StudyFundReturn);
            ms.WriteWithComma(r.StudyFundWithdrawal);
            ms.WriteWithComma(r.StudyFundEnd);
            ms.WriteWithComma(r.Expenses);
            ms.WriteWithComma(r.NetCashflow);
            ms.WriteWithComma(r.NetWorthEnd);
            ms.WriteWithComma(r.PensionBalanceEnd);
            ms.Write(r.NetIncome);
            ms.WriteByte((byte)'\n');
        }

        return ms.ToArray();
    }

    private static void WriteWithComma(this Stream writer, decimal value)
    {
        Write(writer, value);
        writer.WriteByte((byte)',');
    }

    private static void Write(this Stream writer, decimal value)
    {
        var bytes = (stackalloc byte[128]);
        value.TryFormat(bytes, out var bytesWritten);
        writer.Write(bytes[..bytesWritten]);
    }
}
