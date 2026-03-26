using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatYouDid.Migrations
{
    /// <inheritdoc />
    public partial class RemoveExerciseApplicationUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_AspNetUsers_ApplicationUserId",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_ApplicationUserId",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Exercises");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Exercises",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE e
                SET e.ApplicationUserId = r.CreateUserId
                FROM Exercises e
                INNER JOIN Routines r ON e.RoutineId = r.RoutineId
                """);

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
        }
    }
}
