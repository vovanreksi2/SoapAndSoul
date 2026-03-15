using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData;
using ReactiveUI;

namespace SoupAndSoupApp.Models;

public class RecipeModel : BaseModel, IDisposable
{
    private static readonly TimeSpan DirtyTrackingThrottle = TimeSpan.FromMilliseconds(200);

    public bool IsPhotoChanged => !string.IsNullOrEmpty(ImagePathString);

    public string Description
    {
        get => _description;
        set => this.RaiseAndSetIfChanged(ref _description, value);
    }

    public decimal Amount
    {
        get => _amount;
        set => this.RaiseAndSetIfChanged(ref _amount, value);
    }

    public decimal UnitCost
    {
        get => _unitCost;
        set => this.RaiseAndSetIfChanged(ref _unitCost, value);
    }

    public decimal PreparationTime
    {
        get => _preparationTime;
        set => this.RaiseAndSetIfChanged(ref _preparationTime, value);
    }

    public ICommand? DeleteRecipeCommand { get; set; }

    /// <summary>
    /// Legacy property used during DB loading. Populated from DB RecipeComponents.
    /// </summary>
    public IEnumerable<ComponentByRecipeModel> RecipeComponents { get; set; }

    /// <summary>
    /// Per-recipe selection state. This is the source of truth for which components
    /// are in this recipe and their amounts. Keyed by ComponentId.
    /// </summary>
    public SourceCache<ComponentByRecipeModel, int> SelectedComponents { get; } = new(x => x.ComponentId);

    public bool IsDirty { get; set; }

    private readonly CompositeDisposable _disposables = new();

    public RecipeModel()
    {
        Id = default;

        RecipeComponents = new List<ComponentByRecipeModel>();

        Changed
            .Where(x => !_suppressIsDirty && x.PropertyName != nameof(IsDirty))
            .Throttle(DirtyTrackingThrottle)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => { IsDirty = true; })
            .DisposeWith(_disposables);
    }

    public void Dispose()
    {
        SelectedComponents.Dispose();
        CleanUp();
    }

    public void CleanUp()
    {
        _disposables.Clear();
        IsDirty = false;
    }

    public void BeginInit() => _suppressIsDirty = true;
    public void EndInit() => _suppressIsDirty = false;


    private decimal _unitCost;
    private string _description;
    private decimal _amount;
    private decimal _preparationTime;
    private bool _suppressIsDirty;
}
