using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatYouDid.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWorkoutExerciseApplicationUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutExercises_AspNetUsers_ApplicationUserId",
                table: "WorkoutExercises");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutExercises_ApplicationUserId",
                table: "WorkoutExercises");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "WorkoutExercises");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "WorkoutExercises",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE we
                SET we.ApplicationUserId = w.ApplicationUserId
                FROM WorkoutExercises we
                INNER JOIN Workouts w ON we.WorkoutId = w.WorkoutId
                """);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutExercises_ApplicationUserId",
                table: "WorkoutExercises",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutExercises_AspNetUsers_ApplicationUserId",
                table: "WorkoutExercises",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
