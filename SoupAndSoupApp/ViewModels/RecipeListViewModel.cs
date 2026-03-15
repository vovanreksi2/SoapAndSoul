using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using FuzzySharp;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using SoupAndSoupApp.Helpers;
using SoupAndSoupApp.Helpers.Coordinators;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.ViewModels;

public class RecipeListViewModel : ViewModelBase
{
    public const string NoImageRecipe = "Assets/No_Receipt_Photo.png";

    private static readonly TimeSpan SearchDebounceDelay = TimeSpan.FromMilliseconds(300);
    private const int FuzzyMatchThreshold = 60;

    private readonly SourceCache<RecipeModel, int> _cachedRecipes = new(r => r.Id);
    private ReadOnlyObservableCollection<RecipeModel> _recipes = ReadOnlyObservableCollection<RecipeModel>.Empty;

    public ReadOnlyObservableCollection<RecipeModel> Recipes => _recipes;
    public SourceCache<RecipeModel, int> CachedRecipes => _cachedRecipes;

    public RecipeModel? SelectedRecipe
    {
        get => _selectedRecipe;
        set
        {
            if (_selectedRecipe == value || value is null) return;
            _previousSelectedRecipe = _selectedRecipe;
            this.RaiseAndSetIfChanged(ref _selectedRecipe, value);
        }
    }

    public RecipeModel? PreviousSelectedRecipe => _previousSelectedRecipe;

    public string SearchString
    {
        get => _searchString;
        set
        {
            if (_searchString == value) return;
            this.RaiseAndSetIfChanged(ref _searchString, value?.Trim() ?? string.Empty);
        }
    }

    public bool IsRecipeAddMode
    {
        get => _isRecipeAddMode;
        set => this.RaiseAndSetIfChanged(ref _isRecipeAddMode, value);
    }

    public ICommand NewRecipeCommand { get; }
    public ReactiveCommand<RecipeModel, Unit> DeleteRecipeCommand { get; }

    private readonly RecipeCoordinator _recipeCoordinator;
    private readonly ILogger<RecipeListViewModel> _logger;

    private RecipeModel? _selectedRecipe;
    private RecipeModel? _previousSelectedRecipe;
    private string _searchString = string.Empty;
    private bool _isRecipeAddMode;

    public RecipeListViewModel(RecipeCoordinator recipeCoordinator, ILogger<RecipeListViewModel> logger)
    {
        _recipeCoordinator = recipeCoordinator;
        _logger = logger;

        DeleteRecipeCommand = ReactiveCommand.CreateFromTask<RecipeModel>(DeleteRecipeAsync);
        NewRecipeCommand = ReactiveCommand.Create(CreateNewRecipePlaceholder);

        _cachedRecipes.Connect()
            .Filter(FilterRecipes)
            .Sort(SortExpressionComparer<RecipeModel>.Ascending(r => r.Name))
            .Bind(out _recipes)
            .Subscribe()
            .DisposeWith(Disposables);

        this.WhenAnyValue(x => x.SearchString)
            .Throttle(SearchDebounceDelay)
            .DistinctUntilChanged()
            .Subscribe(_ => _cachedRecipes.Refresh())
            .DisposeWith(Disposables);
    }

    public void CreateNewRecipePlaceholder()
    {
        var newRecipe = new RecipeModel();
        newRecipe.BeginInit();
        newRecipe.Name = "Нова Рецептура";
        newRecipe.Description = string.Empty;
        newRecipe.ImagePath = ImageHelper.LoadFromResource(NoImageRecipe);
        newRecipe.DeleteRecipeCommand = DeleteRecipeCommand;
        newRecipe.EndInit();

        _cachedRecipes.AddOrUpdate(newRecipe);
        IsRecipeAddMode = true;
        SelectedRecipe = newRecipe;
    }

    private async Task DeleteRecipeAsync(RecipeModel recipeModel)
    {
        var success = await _recipeCoordinator.DeleteRecipeAsync(recipeModel);
        if (!success) return;

        _cachedRecipes.Remove(recipeModel.Id);
        SelectedRecipe = Recipes.Count > 0 ? Recipes.FirstOrDefault() : null;
    }

    private bool FilterRecipes(RecipeModel recipe)
    {
        var searchString = SearchString?.Trim()?.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(searchString)) return true;

        var contains = recipe.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                       recipe.Description.Contains(searchString, StringComparison.OrdinalIgnoreCase);

        var fuzzyScore = Fuzz.PartialRatio(searchString, recipe.Name.ToLowerInvariant()) > FuzzyMatchThreshold ||
                         Fuzz.PartialRatio(searchString, recipe.Description.ToLowerInvariant()) > FuzzyMatchThreshold;

        return contains || fuzzyScore;
    }
}
