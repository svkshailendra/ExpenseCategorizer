using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace ExpenseCategorizerFunction
{
    public class PdfService
    {
        public byte[] GenerateReportPdf(Report report)
        {
            // Create a new MigraDoc document
            var doc = new Document();
            var section = doc.AddSection();

            // Title
            var title = section.AddParagraph("Expense Report");
            title.Format.Font.Size = 20;
            title.Format.Font.Bold = true;
            title.Format.Alignment = ParagraphAlignment.Center;

            // Total Expenses
            var total = section.AddParagraph($"Total Expenses: {report.TotalExpenses:C}");
            total.Format.Font.Size = 14;
            total.Format.Font.Bold = true;
            total.Format.SpaceAfter = "1cm";

            // Table
            var table = section.AddTable();
            table.Borders.Width = 0.75;

            table.AddColumn("6cm");
            table.AddColumn("6cm");

            var headerRow = table.AddRow();
            headerRow.Shading.Color = Colors.LightGray;
            headerRow.Cells[0].AddParagraph("Category").Format.Font.Bold = true;
            headerRow.Cells[1].AddParagraph("Amount").Format.Font.Bold = true;

            foreach (var item in report.Categories)
            {
                var row = table.AddRow();
                row.Cells[0].AddParagraph(item.Name);
                row.Cells[1].AddParagraph(item.Amount.ToString("C"));
            }

            // Footer
            var footer = section.AddParagraph($"Generated on {DateTime.Now:dd-MMM-yyyy}");
            footer.Format.Alignment = ParagraphAlignment.Center;
            footer.Format.SpaceBefore = "1cm";

            // Render the PDF
            var renderer = new PdfDocumentRenderer()
            {
                Document = doc
            };
            renderer.RenderDocument();

            using var ms = new MemoryStream();
            renderer.PdfDocument.Save(ms, false);
            return ms.ToArray();
        }
    }

    public class CustomFontResolver : IFontResolver
    {
        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            // Always return Helvetica (or any font you embed)
            if (familyName.Equals("Courier New", StringComparison.OrdinalIgnoreCase))
                return new FontResolverInfo("Roboto#");

            return new FontResolverInfo("Roboto#");
        }

        public byte[] GetFont(string faceName)
        {
            // Load font bytes from embedded resource or file
            var path = Path.Combine(AppContext.BaseDirectory, "Fonts", "Roboto-Regular.ttf");
            return File.ReadAllBytes(path);
        }
    }
}