using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduLearn.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseInstructorAndThumbnail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InstructorId",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailPath",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstructorId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "ThumbnailPath",
                table: "Courses");
        }
    }
}
