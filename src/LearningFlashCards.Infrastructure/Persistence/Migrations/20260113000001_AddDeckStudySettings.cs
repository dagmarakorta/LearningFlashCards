using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningFlashCards.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeckStudySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StudySettings_DailyReviewLimit",
                table: "Decks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 50);

            migrationBuilder.AddColumn<int>(
                name: "StudySettings_EasyMinIntervalDays",
                table: "Decks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "StudySettings_MaxIntervalDays",
                table: "Decks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 180);

            migrationBuilder.AddColumn<bool>(
                name: "StudySettings_RepeatInSession",
                table: "Decks",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StudySettings_DailyReviewLimit",
                table: "Decks");

            migrationBuilder.DropColumn(
                name: "StudySettings_EasyMinIntervalDays",
                table: "Decks");

            migrationBuilder.DropColumn(
                name: "StudySettings_MaxIntervalDays",
                table: "Decks");

            migrationBuilder.DropColumn(
                name: "StudySettings_RepeatInSession",
                table: "Decks");
        }
    }
}
