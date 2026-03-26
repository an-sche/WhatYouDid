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

This is the most impactful change for long-term usefulness of the data.

---

## Missing Unique Constraint on `(RoutineId, Sequence)`

Two exercises in the same routine could have the same `Sequence`, which would cause non-deterministic ordering. A unique constraint on `(RoutineId, Sequence)` would enforce this.

---

## `datetime2` vs `datetimeoffset`

`StartTime`/`EndTime` store no timezone. If a user logs a workout while traveling, the time is ambiguous. `datetimeoffset` is a safe default for any timestamp that represents a user action. Minor for a personal app with one timezone, but worth noting.

---

## Summary
| Issue | Impact | Effort |
|---|---|---|
| Serialized Reps/Weights/Durations | High (kills analytics) | High (migration + app changes) |
| Missing unique constraint on `(RoutineId, Sequence)` | Low (data integrity) | Low |
