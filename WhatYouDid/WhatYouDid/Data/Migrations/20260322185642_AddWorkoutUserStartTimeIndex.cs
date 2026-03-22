using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatYouDid.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutUserStartTimeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Workouts_ApplicationUserId",
                table: "Workouts");

            migrationBuilder.CreateIndex(
                name: "IX_Workouts_User_StartTime",
                table: "Workouts",
                columns: new[] { "ApplicationUserId", "StartTime" },
                descending: new[] { false, true },
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Workouts_User_StartTime",
                table: "Workouts");

            migrationBuilder.CreateIndex(
                name: "IX_Workouts_ApplicationUserId",
                table: "Workouts",
                column: "ApplicationUserId");
        }
    }
}
