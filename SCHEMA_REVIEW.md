# Schema Review

## The Big One: Reps/Weights/Durations as Serialized Collections

```
WorkoutExercise.Reps      → nvarchar(max) (primitive collection)
WorkoutExercise.Weights   → nvarchar(max) (primitive collection)
WorkoutExercise.Durations → nvarchar(max) (primitive collection)
```

These are JSON blobs in disguise. You **cannot**:
- Query "what's my best set of bench press?"
- Calculate average weight lifted across sessions
- Filter workouts where reps > 10 on any set

The fix is a `WorkoutExerciseSet` table:

```sql
WorkoutExerciseSet (
    WorkoutExerciseSetId  int  PK
    WorkoutExerciseId     int  FK (cascade delete)
    SetNumber             int  NOT NULL
    Reps                  int  NULL
    Weight                int  NULL
    Duration              int  NULL
)
```

## Summary
| Issue | Impact | Effort |
|---|---|---|
| Serialized Reps/Weights/Durations | High (kills analytics) | High (migration + app changes) |
