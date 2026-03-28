using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatYouDid.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutExerciseSetTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkoutExerciseSets",
                columns: table => new
                {
                    WorkoutExerciseSetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkoutExerciseId = table.Column<int>(type: "int", nullable: false),
                    SetNumber = table.Column<int>(type: "int", nullable: false),
                    Reps = table.Column<int>(type: "int", nullable: true),
                    Weight = table.Column<int>(type: "int", nullable: true),
                    Duration = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutExerciseSets", x => x.WorkoutExerciseSetId);
                    table.ForeignKey(
                        name: "FK_WorkoutExerciseSets_WorkoutExercises_WorkoutExerciseId",
                        column: x => x.WorkoutExerciseId,
                        principalTable: "WorkoutExercises",
                        principalColumn: "WorkoutExerciseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutExerciseSets_WorkoutExerciseId",
                table: "WorkoutExerciseSets",
                column: "WorkoutExerciseId");

            // Shred existing JSON blobs into WorkoutExerciseSets rows
            migrationBuilder.Sql("""
                INSERT INTO WorkoutExerciseSets (WorkoutExerciseId, SetNumber, Reps, Weight, Duration)
                SELECT
                    we.WorkoutExerciseId,
                    CAST(s.SetNumber AS int),
                    TRY_CAST(r.value AS int),
                    TRY_CAST(w.value AS int),
                    TRY_CAST(d.value AS int)
                FROM WorkoutExercises we
                CROSS APPLY (
                    SELECT v FROM (VALUES(1),(2),(3),(4),(5),(6),(7),(8),(9),(10)) n(v)
                ) s(SetNumber)
                OUTER APPLY (SELECT value FROM OPENJSON(we.Reps)      WHERE [key] = CAST(s.SetNumber - 1 AS nvarchar)) r
                OUTER APPLY (SELECT value FROM OPENJSON(we.Weights)   WHERE [key] = CAST(s.SetNumber - 1 AS nvarchar)) w
                OUTER APPLY (SELECT value FROM OPENJSON(we.Durations) WHERE [key] = CAST(s.SetNumber - 1 AS nvarchar)) d
                WHERE (r.value IS NOT NULL OR w.value IS NOT NULL OR d.value IS NOT NULL)
                """);

            migrationBuilder.DropColumn(
                name: "Durations",
                table: "WorkoutExercises");

            migrationBuilder.DropColumn(
                name: "Reps",
                table: "WorkoutExercises");

            migrationBuilder.DropColumn(
                name: "Weights",
                table: "WorkoutExercises");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Durations",
                table: "WorkoutExercises",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reps",
                table: "WorkoutExercises",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Weights",
                table: "WorkoutExercises",
                type: "nvarchar(max)",
                nullable: true);

            // Reconstitute JSON arrays from WorkoutExerciseSets rows
            migrationBuilder.Sql("""
                UPDATE we SET
                    Reps      = agg.Reps,
                    Weights   = agg.Weights,
                    Durations = agg.Durations
                FROM WorkoutExercises we
                JOIN (
                    SELECT
                        WorkoutExerciseId,
                        '[' + STRING_AGG(ISNULL(CAST(Reps     AS nvarchar), 'null'), ',') WITHIN GROUP (ORDER BY SetNumber) + ']' AS Reps,
                        '[' + STRING_AGG(ISNULL(CAST(Weight   AS nvarchar), 'null'), ',') WITHIN GROUP (ORDER BY SetNumber) + ']' AS Weights,
                        '[' + STRING_AGG(ISNULL(CAST(Duration AS nvarchar), 'null'), ',') WITHIN GROUP (ORDER BY SetNumber) + ']' AS Durations
                    FROM WorkoutExerciseSets
                    GROUP BY WorkoutExerciseId
                ) agg ON agg.WorkoutExerciseId = we.WorkoutExerciseId
                """);

            migrationBuilder.DropTable(
                name: "WorkoutExerciseSets");
        }
    }
}
