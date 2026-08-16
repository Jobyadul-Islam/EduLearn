using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduLearn.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: (int)EduLearn.Models.CourseStatus.Approved);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Courses");
        }
    }
}
