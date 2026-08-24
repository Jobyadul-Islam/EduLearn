using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduLearn.Migrations
{
    /// <inheritdoc />
    public partial class AddEnrollmentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Enrollments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Backfill: an enrollment was effectively Active if it had already been marked
            // paid, or if the course itself is free (no payment step was ever required).
            migrationBuilder.Sql(@"
                UPDATE e SET e.Status = 1
                FROM Enrollments e
                JOIN Courses c ON c.Id = e.CourseId
                WHERE e.IsPaid = 1 OR c.Price = 0;");

            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "Enrollments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Enrollments");

            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "Enrollments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
