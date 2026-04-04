using System.Net.Http.Json;
using WhatYouDid.Shared;

namespace WhatYouDid.Client.Services;

public class RoutineHttpService(HttpClient http) : IRoutineService
{
    public async Task<List<RoutineDto>> GetUserRoutinesAsync()
        => await http.GetFromJsonAsync<List<RoutineDto>>("/api/routines") ?? [];

    public async Task<List<ExerciseDto>> GetExercisesAsync(int routineId)
        => await http.GetFromJsonAsync<List<ExerciseDto>>($"/api/routines/{routineId}/exercises") ?? [];

    public async Task<RoutineDetailDto?> GetRoutineAsync(int routineId)
        => await http.GetFromJsonAsync<RoutineDetailDto>($"/api/routines/{routineId}");

    public async Task<bool> AddRoutineAsync(CreateRoutineDto routine)
    {
        var response = await http.PostAsJsonAsync("/api/routines", routine);
        return response.IsSuccessStatusCode;
    }
}
