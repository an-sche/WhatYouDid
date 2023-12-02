using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatYouDid.Migrations
{
    /// <inheritdoc />
    public partial class RoutineReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_Routines_RoutineId",
                table: "Exercises");

            migrationBuilder.AddColumn<string>(
                name: "CreateUserId",
                table: "Routines",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RoutineId",
                table: "Exercises",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Routines_CreateUserId",
                table: "Routines",
                column: "CreateUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_Routines_RoutineId",
                table: "Exercises",
                column: "RoutineId",
                principalTable: "Routines",
                principalColumn: "RoutineId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Routines_AspNetUsers_CreateUserId",
                table: "Routines",
                column: "CreateUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_Routines_RoutineId",
                table: "Exercises");

            migrationBuilder.DropForeignKey(
                name: "FK_Routines_AspNetUsers_CreateUserId",
                table: "Routines");

            migrationBuilder.DropIndex(
                name: "IX_Routines_CreateUserId",
                table: "Routines");

            migrationBuilder.DropColumn(
                name: "CreateUserId",
                table: "Routines");

            migrationBuilder.AlterColumn<int>(
                name: "RoutineId",
                table: "Exercises",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_Routines_RoutineId",
                table: "Exercises",
                column: "RoutineId",
                principalTable: "Routines",
                principalColumn: "RoutineId");
        }
    }
}
