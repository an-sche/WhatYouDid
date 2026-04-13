using System.Net.Http.Json;
using WhatYouDid.Shared;

namespace WhatYouDid.Client.Services;

public class WorkoutHttpService(HttpClient http) : IWorkoutService
{
    public async Task<PagedList<WorkoutListItemDto>> GetWorkoutsAsync(int page, int pageSize, string? search = null)
    {
        var url = $"/api/workouts?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(search))
            url += $"&search={Uri.EscapeDataString(search)}";
        return await http.GetFromJsonAsync<PagedList<WorkoutListItemDto>>(url)
            ?? new PagedList<WorkoutListItemDto>([], page, pageSize, 0);
    }

    public async Task<WorkoutDto?> GetCompletedWorkoutDtoAsync(Guid workoutId)
        => await http.GetFromJsonAsync<WorkoutDto>($"/api/workouts/{workoutId}");

    public async Task<bool> UpdateWorkoutExerciseAsync(Guid workoutId, WorkoutExerciseDto exercise)
    {
        var response = await http.PatchAsJsonAsync($"/api/workouts/{workoutId}/exercises", exercise);
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
