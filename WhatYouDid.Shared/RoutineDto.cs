namespace WhatYouDid.Shared;

public class RoutineDto
{
    public int RoutineId { get; init; }
    public string Name { get; init; } = string.Empty;
}

public class RoutineDetailDto
{
    public int RoutineId { get; init; }
    public string Name { get; init; } = string.Empty;
    public List<ExerciseDto> Exercises { get; init; } = [];
}

public class ExerciseDto
{
    public int ExerciseId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Sequence { get; init; }
    public int Sets { get; init; }
    public bool HasReps { get; init; }
    public bool HasWeight { get; init; }
    public bool HasDuration { get; init; }
}
