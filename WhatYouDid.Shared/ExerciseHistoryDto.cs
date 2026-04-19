namespace WhatYouDid.Shared;

public record ExerciseSetHistoryDto(int SetNumber, int? Reps, int? Weight, int? Duration);

public record ExerciseSessionHistoryDto(DateTimeOffset Date, List<ExerciseSetHistoryDto> Sets);

public record ExerciseHistoryDto(
    string ExerciseName,
    bool HasReps,
    bool HasWeight,
    bool HasDuration,
    List<ExerciseSessionHistoryDto> Sessions
);
