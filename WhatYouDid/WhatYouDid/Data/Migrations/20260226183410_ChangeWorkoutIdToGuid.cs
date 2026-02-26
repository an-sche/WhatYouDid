using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatYouDid.Migrations
{
    /// <inheritdoc />
    public partial class ChangeWorkoutIdToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add temporary GUID columns
            migrationBuilder.AddColumn<Guid>(
                name: "NewWorkoutId",
                table: "Workouts",
                type: "uniqueidentifier",
                nullable: true);

            // Populate new GUIDs for existing workouts
            migrationBuilder.Sql("UPDATE Workouts SET NewWorkoutId = NEWID();");

            // Add temp GUID FK column to dependent table
            migrationBuilder.AddColumn<Guid>(
                name: "NewWorkoutId",
                table: "WorkoutExercises",
                type: "uniqueidentifier",
                nullable: true);

            // Map dependent rows to the new GUIDs
            migrationBuilder.Sql(@"
                UPDATE we
                SET NewWorkoutId = w.NewWorkoutId
                FROM WorkoutExercises we
                INNER JOIN Workouts w ON we.WorkoutId = w.WorkoutId;
            ");

            // Drop FK and index that reference the old int PK
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutExercises_Workouts_WorkoutId",
                table: "WorkoutExercises");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutExercises_WorkoutId",
                table: "WorkoutExercises");

            // Drop the old PK on Workouts
            migrationBuilder.DropPrimaryKey(
                name: "PK_Workouts",
                table: "Workouts");

            // Remove the old integer columns
            migrationBuilder.DropColumn(
                name: "WorkoutId",
                table: "WorkoutExercises");

            migrationBuilder.DropColumn(
                name: "WorkoutId",
                table: "Workouts");

            // Rename temporary GUID columns into place
            migrationBuilder.RenameColumn(
                name: "NewWorkoutId",
                table: "Workouts",
                newName: "WorkoutId");

            migrationBuilder.RenameColumn(
                name: "NewWorkoutId",
                table: "WorkoutExercises",
                newName: "WorkoutId");

            // Make the new WorkoutId columns non-nullable
            migrationBuilder.AlterColumn<Guid>(
                name: "WorkoutId",
                table: "Workouts",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkoutId",
                table: "WorkoutExercises",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            // Recreate PK, index and FK with GUIDs
            migrationBuilder.AddPrimaryKey(
                name: "PK_Workouts",
                table: "Workouts",
                column: "WorkoutId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutExercises_WorkoutId",
                table: "WorkoutExercises",
                column: "WorkoutId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutExercises_Workouts_WorkoutId",
                table: "WorkoutExercises",
                column: "WorkoutId",
                principalTable: "Workouts",
                principalColumn: "WorkoutId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Reverting ChangeWorkoutIdToGuid is not supported.");
        }
    }
}
