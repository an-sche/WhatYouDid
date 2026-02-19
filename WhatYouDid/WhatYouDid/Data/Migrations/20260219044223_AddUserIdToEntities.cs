using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatYouDid.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "WorkoutExercises",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Exercises",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutExercises_ApplicationUserId",
                table: "WorkoutExercises",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_ApplicationUserId",
                table: "Exercises",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_AspNetUsers_ApplicationUserId",
                table: "Exercises",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutExercises_AspNetUsers_ApplicationUserId",
                table: "WorkoutExercises",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            // Populate the new ApplicationUserId columns from related parent tables.
            // For WorkoutExercises, copy from the parent Workouts.ApplicationUserId
            migrationBuilder.Sql(@"
                UPDATE we
                SET we.ApplicationUserId = w.ApplicationUserId
                FROM WorkoutExercises we
                INNER JOIN Workouts w ON we.WorkoutId = w.WorkoutId
                WHERE we.ApplicationUserId IS NULL AND w.ApplicationUserId IS NOT NULL
            ");

            // For Exercises, use the owning Routine's CreateUserId where available
            migrationBuilder.Sql(@"
                UPDATE e
                SET e.ApplicationUserId = r.CreateUserId
                FROM Exercises e
                INNER JOIN Routines r ON e.RoutineId = r.RoutineId
                WHERE e.ApplicationUserId IS NULL AND r.CreateUserId IS NOT NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_AspNetUsers_ApplicationUserId",
                table: "Exercises");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutExercises_AspNetUsers_ApplicationUserId",
                table: "WorkoutExercises");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutExercises_ApplicationUserId",
                table: "WorkoutExercises");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_ApplicationUserId",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "WorkoutExercises");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Exercises");
        }
    }
}
