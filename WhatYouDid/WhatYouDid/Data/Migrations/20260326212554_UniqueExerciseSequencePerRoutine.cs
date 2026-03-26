using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatYouDid.Migrations
{
    /// <inheritdoc />
    public partial class UniqueExerciseSequencePerRoutine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Exercises_RoutineId",
                table: "Exercises");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_RoutineId_Sequence",
                table: "Exercises",
                columns: new[] { "RoutineId", "Sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Exercises_RoutineId_Sequence",
                table: "Exercises");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_RoutineId",
                table: "Exercises",
                column: "RoutineId");
        }
    }
}
