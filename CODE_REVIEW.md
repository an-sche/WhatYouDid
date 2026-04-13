# Code Review Findings

Issues found during review of the Client pages and components. Items marked **fixed** have already been addressed.

---

## `StartWorkout.razor`

- **`StartTime` is re-stamped on every page load** — `OnInitializedAsync` always sets `WorkoutDto.StartTime = DateTimeOffset.Now`, then `OnAfterRenderAsync` may overwrite `WorkoutDto` from storage (restoring the correct time). Should only set `StartTime` if no stored workout exists.
- **`OnDropdownValueChanged` doesn't save to browser storage** — navigating via the dropdown skips `BrowserStorage.SetAsync`, so that jump is not persisted.
- **`OnAutofillLastTime` doesn't save to browser storage** — autofilled values are lost if the tab is closed before the next navigation.
- **Resume is silent** — when a browser-stored workout is restored in `OnAfterRenderAsync`, the user gets no prompt ("Resume previous workout?" / "Start fresh").
- **`ExerciseNames` not refreshed after browser storage restore** — `OnAfterRenderAsync` replaces `WorkoutDto` with the stored value but leaves `ExerciseNames` pointing at the stale array built from the API response. In practice the names don't change mid-workout, but the two are now out of sync.
- **`HasAnyAlternateData` duplicated in `WorkoutCard.razor`** — **fixed**, moved to `WorkoutExerciseDto.HasAnyAlternateData(int setIndex)`.

---


## `WorkoutCard.razor`

- **`CancelExerciseEdit` always re-fetches from the server** — even if no edits were made, it hits the API. Could keep a snapshot of the original data and restore locally on cancel.
- **`editingExercise` not cleared on collapse** — if the user collapses the card while in edit mode, `editingExercise` remains set. Re-expanding shows the edit UI again with whatever in-memory state was left, which could be surprising.
- **Duration format is inconsistent with `WorkoutReview`** — `WorkoutCard` formats duration as `Xh Ym` or `Xm`, omitting seconds. `WorkoutReview` shows `Xm Ys`. Should use a shared helper.

---

## `WorkoutService.cs`

- **`GetStartWorkoutDtoAsync` pre-populates `Notes` from the last workout** — `Notes` is set to `orderedSets?.Select(s => s.Note).ToArray()`, which unconditionally carries over the previous workout's notes without user action. This is inconsistent with how `LastReps`/`LastWeights`/`LastDurations` work (they are shown as goals, not pre-filled). Clicking the autofill button then copies `LastNotes` into `Notes` — a no-op since they are already identical.
- **`GetStartWorkoutDtoAsync` does not order exercises by `Sequence`** — exercises are projected from `routine.Exercises` (loaded via `Include`) without an explicit `.OrderBy(e => e.Sequence)`. EF Core makes no ordering guarantee for included collections; exercises may appear in an arbitrary order.
- **`GetCompletedWorkoutDtoAsync` uses the exercise's current `Sets` value, not the stored set count** — `Sets = row.ex != null ? row.ex.Sets : 0` reflects the routine's current definition. If the routine was edited after the workout was recorded, `Sets` will disagree with the actual number of `WorkoutExerciseSet` rows. The edit UI iterates `exercise.Sets` times and indexes into the data arrays, so a mismatch can expose out-of-bounds accesses or hide stored sets. Should use `row.we.Sets.Count` (or the count of the fetched sets).
- **`GetCompletedWorkoutDtoAsync` runs two separate queries without a transaction** — the `WorkoutExercise` query and the `Workout` query run on the same `DbContext` instance but are executed independently. A soft-delete between the two could result in orphaned exercise data being returned for a workout that the second query can no longer find. Wrapping both in a single query (or a transaction) would make this consistent.

---

## `RoutineService.cs`

- **`GetExercisesAsync` has no ordering guarantee** — the query projects exercises without `.OrderBy(e => e.Sequence)`. SQL Server doesn't guarantee row order without `ORDER BY`, so exercises could be returned in a different order than expected in `RoutineCard`.
- **`GetRoutineAsync` also lacks exercise ordering** — same issue in the `r.Exercises.Select(...)` projection inside the LINQ query.

---

## `DashboardService.cs`

- **Cache has no invalidation** — the `ConcurrentDictionary` cache inside `DashboardService` is never cleared. In Blazor Server, the service scope lives for the circuit lifetime, so a user who records a new workout and then checks the dashboard in the same circuit will see stale stats.
- **Cache is scoped, not singleton — no caching occurs in WASM** — `DashboardService` is registered as `Scoped`. In WASM mode each HTTP request creates a new scope, so the cache dictionary is freshly constructed on every request and provides no benefit. The field appears to have been written with a singleton lifetime in mind.
