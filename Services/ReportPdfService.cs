using System;
using System.Collections.Generic;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EduLearn.Services
{
    public static class ReportPdfService
    {
        public static byte[] GenerateRevenueReport(decimal totalRevenue, List<(DateTime Month, decimal Revenue)> monthlyRevenue)
        {
            return BuildDocument("Revenue Report", column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Total Revenue").FontSize(14).Bold();
                    row.RelativeItem().AlignRight().Text($"TK {totalRevenue:0.00}").FontSize(14).Bold().FontColor("#1D4ED8");
                });

                column.Item().PaddingTop(15).Text("Monthly Revenue — Last 6 Months").FontSize(13).Bold();

                column.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    AddHeaderRow(table, "Month", "Revenue (TK)");

                    foreach (var row in monthlyRevenue)
                    {
                        AddDataRow(table, row.Month.ToString("MMM yyyy"), row.Revenue.ToString("0.00"));
                    }
                });
            });
        }

        public static byte[] GenerateAnalyticsReport(
            List<(int Id, string Title, int EnrollmentCount)> mostPopular,
            List<(int Id, string Title, int ReviewCount, double? AverageRating)> topRated)
        {
            return BuildDocument("Course Analytics Report", column =>
            {
                column.Item().Text("Most Popular Courses").FontSize(13).Bold();
                column.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn();
                    });

                    AddHeaderRow(table, "#", "Course", "Enrollments");

                    var rank = 1;
                    foreach (var course in mostPopular)
                    {
                        AddDataRow(table, rank.ToString(), course.Title, course.EnrollmentCount.ToString());
                        rank++;
                    }
                });

                column.Item().PaddingTop(20).Text("Top Rated Courses").FontSize(13).Bold();

                if (topRated.Count == 0)
                {
                    column.Item().PaddingTop(5).Text("No reviews yet.").FontColor(Colors.Grey.Darken1);
                }
                else
                {
                    column.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        AddHeaderRow(table, "#", "Course", "Rating", "Reviews");

                        var rank = 1;
                        foreach (var course in topRated)
                        {
                            AddDataRow(table, rank.ToString(), course.Title, $"{course.AverageRating:0.0} / 5", course.ReviewCount.ToString());
                            rank++;
                        }
                    });
                }
            });
        }

        private static byte[] BuildDocument(string title, Action<ColumnDescriptor> content)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontFamily("Georgia").FontSize(10));

                    page.Content().Column(outer =>
                    {
                        outer.Spacing(4);
                        outer.Item().Text("EduLearn").FontSize(20).Bold().FontColor("#1D4ED8");
                        outer.Item().Text(title).FontSize(16).Bold();
                        outer.Item().PaddingBottom(10).Text($"Generated on {DateTime.Now:MMMM d, yyyy 'at' h:mm tt}").FontSize(9).FontColor(Colors.Grey.Medium);

                        outer.Item().Column(content);
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static void AddHeaderRow(TableDescriptor table, params string[] headers)
        {
            foreach (var header in headers)
            {
                table.Cell().Background("#1D4ED8").Padding(5).Text(header).FontColor(Colors.White).Bold();
            }
        }

        private static void AddDataRow(TableDescriptor table, params string[] values)
        {
            foreach (var value in values)
            {
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(value);
            }
        }
    }
}
