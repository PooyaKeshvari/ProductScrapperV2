using ClosedXML.Excel;
using ProductScrapperV2.Web.ViewModels;

namespace ProductScrapperV2.Web.Services;
public interface IExcelExportService
{
    Task<byte[]> ExportPriceComparisonAsync(
        IReadOnlyCollection<PriceComparisonDto> comparisons,
        CancellationToken cancellationToken);
}

public class ExcelExportService : IExcelExportService
{
    public Task<byte[]> ExportPriceComparisonAsync(
        IReadOnlyCollection<PriceComparisonDto> comparisons,
        CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("PriceComparison");

        var row = 1;
        sheet.Cell(row, 1).Value = "نام محصول";
        sheet.Cell(row, 2).Value = "قیمت ما";
        sheet.Cell(row, 3).Value = "رقیب";
        sheet.Cell(row, 4).Value = "قیمت";
        sheet.Cell(row, 5).Value = "لینک";
        sheet.Cell(row, 6).Value = "درصد تطابق";
        sheet.Cell(row, 7).Value = "امتیاز اطمینان";
        row++;

        foreach (var comparison in comparisons)
        {
            foreach (var competitor in comparison.CompetitorPrices)
            {
                sheet.Cell(row, 1).Value = comparison.ProductName;
                sheet.Cell(row, 2).Value = comparison.OwnPrice;
                sheet.Cell(row, 3).Value = competitor.CompetitorName;
                sheet.Cell(row, 4).Value = competitor.Price;
                sheet.Cell(row, 5).Value = competitor.ProductUrl;
                sheet.Cell(row, 6).Value = competitor.MatchPercentage;
                sheet.Cell(row, 7).Value = competitor.ConfidenceScore;
                row++;
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Task.FromResult(stream.ToArray());
    }
}