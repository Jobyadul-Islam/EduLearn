using System;
using System.Collections.Generic;
using System.Text;
using EduLearn.Services;
using Xunit;

namespace EduLearn.Tests.Services
{
    public class PdfGenerationTests
    {
        static PdfGenerationTests()
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        }

        private static void AssertIsValidPdf(byte[] bytes)
        {
            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);
            var header = Encoding.ASCII.GetString(bytes, 0, 5);
            Assert.Equal("%PDF-", header);
        }

        [Fact]
        public void CertificateService_Generate_ProducesValidPdf()
        {
            var bytes = CertificateService.Generate("Jane Student", "Intro to C#", new DateTime(2026, 1, 15));

            AssertIsValidPdf(bytes);
        }

        [Fact]
        public void InvoiceService_Generate_ProducesValidPdf()
        {
            var bytes = InvoiceService.Generate("Jane Student", "Intro to C#", "TXN-12345", 500m, new DateTime(2026, 1, 15));

            AssertIsValidPdf(bytes);
        }

        [Fact]
        public void ReportPdfService_GenerateRevenueReport_ProducesValidPdf()
        {
            var monthly = new List<(DateTime Month, decimal Revenue)>
            {
                (new DateTime(2026, 1, 1), 1200m),
                (new DateTime(2026, 2, 1), 800m)
            };

            var bytes = ReportPdfService.GenerateRevenueReport(2000m, monthly);

            AssertIsValidPdf(bytes);
        }

        [Fact]
        public void ReportPdfService_GenerateRevenueReport_WithNoData_StillProducesValidPdf()
        {
            var bytes = ReportPdfService.GenerateRevenueReport(0m, new List<(DateTime, decimal)>());

            AssertIsValidPdf(bytes);
        }

        [Fact]
        public void ReportPdfService_GenerateAnalyticsReport_ProducesValidPdf()
        {
            var mostPopular = new List<(int Id, string Title, int EnrollmentCount)>
            {
                (1, "Intro to C#", 42)
            };
            var topRated = new List<(int Id, string Title, int ReviewCount, double? AverageRating)>
            {
                (1, "Intro to C#", 10, 4.5)
            };

            var bytes = ReportPdfService.GenerateAnalyticsReport(mostPopular, topRated);

            AssertIsValidPdf(bytes);
        }

        [Fact]
        public void ReportPdfService_GenerateAnalyticsReport_WithNoReviewsYet_StillProducesValidPdf()
        {
            // Regression check: an earlier bug rendered an empty header-only table when topRated was empty.
            var mostPopular = new List<(int Id, string Title, int EnrollmentCount)>();
            var topRated = new List<(int Id, string Title, int ReviewCount, double? AverageRating)>();

            var bytes = ReportPdfService.GenerateAnalyticsReport(mostPopular, topRated);

            AssertIsValidPdf(bytes);
        }
    }
}
