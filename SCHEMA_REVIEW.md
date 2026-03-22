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

## Redundant `ApplicationUserId` on Child Tables

`Exercise` already belongs to a `Routine` which has `CreateUserId`. `WorkoutExercise` already belongs to a `Workout` which has `ApplicationUserId`. The denormalized `ApplicationUserId` on both child tables exists purely for the EF global query filters, but it creates a potential inconsistency vector: a `WorkoutExercise` could theoretically have a different `ApplicationUserId` than its parent `Workout`.

Since the tenant filter on `WorkoutExercise` is redundant (you can't reach a `WorkoutExercise` without its parent `Workout`, which is already tenant-filtered), you could remove it and filter via the join. Fewer columns to keep in sync.

---

## Soft Delete Leaves Orphaned Data

`Workout` has `IsDeleted`/`DeletedDt`, but `WorkoutExercise` has no soft delete flag. The `WorkoutExercise` global query filter is on its own `ApplicationUserId` — not on `Workout.IsDeleted`. So soft-deleted workout data isn't truly hidden if `WorkoutExercises` is ever queried independently.

---

## `datetime2` vs `datetimeoffset`

`StartTime`/`EndTime` store no timezone. If a user logs a workout while traveling, the time is ambiguous. `datetimeoffset` is a safe default for any timestamp that represents a user action. Minor for a personal app with one timezone, but worth noting.

---

## Summary

| Issue | Impact | Effort |
|---|---|---|
| Serialized Reps/Weights/Durations | High (kills analytics) | High (migration + app changes) |
| Missing unique constraint on `(RoutineId, Sequence)` | Low (data integrity) | Low |
| Redundant `ApplicationUserId` on child tables | Low (complexity/risk) | Medium |
| Soft delete orphan risk | Low | Low |
