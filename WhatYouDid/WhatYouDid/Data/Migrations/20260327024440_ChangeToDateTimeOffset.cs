using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatYouDid.Migrations
{
    /// <inheritdoc />
    public partial class ChangeToDateTimeOffset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "StartTime",
                table: "Workouts",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "EndTime",
                table: "Workouts",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "DeletedDt",
                table: "Workouts",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            // Backfill Eastern time offset (EDT Apr-Oct = -04:00, EST Nov-Mar = -05:00)
            migrationBuilder.Sql("""
                UPDATE Workouts
                SET
                    StartTime = TODATETIMEOFFSET(StartTime, CASE
                        WHEN DATEPART(MONTH, StartTime) BETWEEN 4 AND 10 THEN '-04:00'
                        ELSE '-05:00'
                    END),
                    EndTime = CASE WHEN EndTime IS NOT NULL THEN TODATETIMEOFFSET(EndTime, CASE
                        WHEN DATEPART(MONTH, EndTime) BETWEEN 4 AND 10 THEN '-04:00'
                        ELSE '-05:00'
                    END) END,
                    DeletedDt = CASE WHEN DeletedDt IS NOT NULL THEN TODATETIMEOFFSET(DeletedDt, CASE
                        WHEN DATEPART(MONTH, DeletedDt) BETWEEN 4 AND 10 THEN '-04:00'
                        ELSE '-05:00'
                    END) END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "StartTime",
                table: "Workouts",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndTime",
                table: "Workouts",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedDt",
                table: "Workouts",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);
        }
    }
}
