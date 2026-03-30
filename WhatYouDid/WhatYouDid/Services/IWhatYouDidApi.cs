namespace WhatYouDid.Services;

// Retained for backward compatibility. Step 2 will migrate Blazor components
// to inject IRoutineService or IWorkoutService directly, after which this
// interface and WhatYouDidApiDirectAccess's explicit listing of it can be removed.
public interface IWhatYouDidApi : IRoutineService, IWorkoutService
{
}
