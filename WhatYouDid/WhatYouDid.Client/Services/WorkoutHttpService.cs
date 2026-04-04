using System.Net.Http.Json;
using WhatYouDid.Shared;

namespace WhatYouDid.Client.Services;

public class WorkoutHttpService(HttpClient http) : IWorkoutService
{
    public async Task<List<WorkoutListItemDto>> GetWorkoutsAsync(int startIndex, int count, string? search = null)
    {
        var url = $"/api/workouts?startIndex={startIndex}&count={count}";
        if (!string.IsNullOrEmpty(search))
            url += $"&search={Uri.EscapeDataString(search)}";
        return await http.GetFromJsonAsync<List<WorkoutListItemDto>>(url) ?? [];
    }

    public async Task<int> GetWorkoutsCountAsync(string? search = null)
    {
        var url = "/api/workouts/count";
        if (!string.IsNullOrEmpty(search))
            url += $"?search={Uri.EscapeDataString(search)}";
        return await http.GetFromJsonAsync<int>(url);
    }

    public async Task<WorkoutDto?> GetCompletedWorkoutDtoAsync(Guid workoutId)
        => await http.GetFromJsonAsync<WorkoutDto>($"/api/workouts/{workoutId}");

    public async Task<bool> UpdateWorkoutExerciseAsync(Guid workoutId, WorkoutExerciseDto exercise)
    {
        var response = await http.PatchAsJsonAsync($"/api/workouts/{workoutId}/exercises/{exercise.ExerciseId}", exercise);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteWorkoutAsync(Guid workoutId)
    {
        var response = await http.DeleteAsync($"/api/workouts/{workoutId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<WorkoutDto?> GetStartWorkoutDtoAsync(int routineId)
        => await http.GetFromJsonAsync<WorkoutDto>($"/api/workouts/start/{routineId}");

    public async Task<bool> SaveWorkoutAsync(WorkoutDto workout)
    {
        var response = await http.PostAsJsonAsync("/api/workouts", workout);
        return response.IsSuccessStatusCode;
    }
}
