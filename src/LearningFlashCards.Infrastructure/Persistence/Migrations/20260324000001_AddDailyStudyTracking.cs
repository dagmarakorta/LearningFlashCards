using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningFlashCards.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyStudyTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StudySecondsToday",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StudySecondsTrackedAt",
                table: "Users",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StudySecondsToday",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StudySecondsTrackedAt",
                table: "Users");
        }
    }
}
