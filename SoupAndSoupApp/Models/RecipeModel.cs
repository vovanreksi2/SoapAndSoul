using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using ReactiveUI;

namespace SoupAndSoupApp.Models;

public class RecipeModel: ReactiveObject, IDisposable
{
    private bool _suppressIsDirty;

    public int Id { get; set; } 

    public bool IsNewRecipe => Id == default;

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public Bitmap? ImagePath
    {
        get => _imagePath;
        set => this.RaiseAndSetIfChanged(ref _imagePath, value);
    }

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
            .Subscribe(_ =>
            {
                IsDirty = true;
            })
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
    private Bitmap? _imagePath;
    private string _name;
    private string _description;
    private decimal _amount;
    private decimal _preparationTime;

}