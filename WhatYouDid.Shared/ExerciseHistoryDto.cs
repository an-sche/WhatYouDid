namespace WhatYouDid.Shared;

public record ExerciseSetHistoryDto(int SetNumber, int? Reps, int? Weight, int? Duration);

public record ExerciseSessionHistoryDto(DateTimeOffset Date, string RoutineName, List<ExerciseSetHistoryDto> Sets);

public record ExerciseHistoryDto(
    string ExerciseName,
    bool HasReps,
    bool HasWeight,
    bool HasDuration,
    List<string> Routines,
    List<ExerciseSessionHistoryDto> Sessions
);
