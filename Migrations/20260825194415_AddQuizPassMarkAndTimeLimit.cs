using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduLearn.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizPassMarkAndTimeLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PassMarkPercentage",
                table: "Quizzes",
                type: "int",
                nullable: false,
                defaultValue: 60);

            migrationBuilder.AddColumn<int>(
                name: "TimeLimitMinutes",
                table: "Quizzes",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<bool>(
                name: "Passed",
                table: "QuizResults",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PassMarkPercentage",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "TimeLimitMinutes",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "Passed",
                table: "QuizResults");
        }
    }
}
