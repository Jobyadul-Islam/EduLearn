using System;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EduLearn.Services
{
    public static class CertificateService
    {
        public static byte[] Generate(string studentName, string courseTitle, DateTime completionDate)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontFamily("Georgia"));

                    page.Content().Border(2).BorderColor("#1D4ED8").Padding(30).Column(column =>
                    {
                        column.Spacing(16);

                        column.Item().AlignCenter().Text("Certificate of Completion")
                            .FontSize(32).Bold().FontColor("#1D4ED8");

                        column.Item().AlignCenter().Text("This certifies that").FontSize(14).FontColor(Colors.Grey.Darken1);

                        column.Item().AlignCenter().Text(studentName).FontSize(26).Bold();

                        column.Item().AlignCenter().Text("has successfully completed the course").FontSize(14).FontColor(Colors.Grey.Darken1);

                        column.Item().AlignCenter().Text(courseTitle).FontSize(20).Bold().FontColor("#1D4ED8");

                        column.Item().PaddingTop(20).AlignCenter().Text($"Completed on {completionDate:MMMM d, yyyy}").FontSize(12).FontColor(Colors.Grey.Darken1);

                        column.Item().PaddingTop(30).AlignCenter().Text("EduLearn").FontSize(14).Bold();
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
