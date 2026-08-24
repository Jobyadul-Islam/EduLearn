using System;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EduLearn.Services
{
    public static class InvoiceService
    {
        public static byte[] Generate(string studentName, string courseTitle, string transactionId, decimal amount, DateTime paidOn)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontFamily("Georgia").FontSize(11));

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().Text("EduLearn").FontSize(22).Bold().FontColor("#1D4ED8");
                        column.Item().Text("Payment Receipt").FontSize(14).FontColor(Colors.Grey.Darken1);

                        column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        column.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Text("Billed to:").FontColor(Colors.Grey.Darken1);
                            row.RelativeItem().AlignRight().Text(studentName).Bold();
                        });

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Course:").FontColor(Colors.Grey.Darken1);
                            row.RelativeItem().AlignRight().Text(courseTitle).Bold();
                        });

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Transaction ID:").FontColor(Colors.Grey.Darken1);
                            row.RelativeItem().AlignRight().Text(transactionId);
                        });

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Date Paid:").FontColor(Colors.Grey.Darken1);
                            row.RelativeItem().AlignRight().Text(paidOn.ToString("MMMM d, yyyy"));
                        });

                        column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        column.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Text("Total Paid").FontSize(14).Bold();
                            row.RelativeItem().AlignRight().Text($"${amount:0.00}").FontSize(14).Bold().FontColor("#1D4ED8");
                        });

                        column.Item().PaddingTop(20).Text("This is a simulated receipt for demonstration purposes.").FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
