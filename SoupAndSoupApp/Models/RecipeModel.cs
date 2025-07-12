using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;

namespace SoupAndSoupApp.Models;

public class RecipeModel : BaseModel, IDisposable
{
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

    public ICommand? DeleteReceiptCommand { get; set; }

    public IEnumerable<ComponentByRecipeModel> RecipeComponents { get; set; }

    public bool IsDirty { get; set; }

    private readonly CompositeDisposable _disposables = new();

    public RecipeModel()
    {
        Id = default;

        RecipeComponents = new List<ComponentByRecipeModel>();

        Changed
            .Where(x => !_suppressIsDirty && x.PropertyName != nameof(IsDirty))
            .Throttle(TimeSpan.FromMilliseconds(200))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => { IsDirty = true; })
            .DisposeWith(_disposables);
    }

    public void Dispose()
    {
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