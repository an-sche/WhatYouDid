using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatYouDid.Migrations
{
    /// <inheritdoc />
    public partial class AddAlternateSetData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AlternateDuration",
                table: "WorkoutExerciseSets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AlternateReps",
                table: "WorkoutExerciseSets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AlternateWeight",
                table: "WorkoutExerciseSets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "WorkoutExerciseSets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlternateDuration",
                table: "WorkoutExerciseSets");

            migrationBuilder.DropColumn(
                name: "AlternateReps",
                table: "WorkoutExerciseSets");

            migrationBuilder.DropColumn(
                name: "AlternateWeight",
                table: "WorkoutExerciseSets");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "WorkoutExerciseSets");
        }
    }
}
