using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduLearn.Migrations
{
    /// <inheritdoc />
    public partial class AddEnrollmentPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "Enrollments",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDate",
                table: "Enrollments",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "PaymentDate",
                table: "Enrollments");
        }
    }
}
