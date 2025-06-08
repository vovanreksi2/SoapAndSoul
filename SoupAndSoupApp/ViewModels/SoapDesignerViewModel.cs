using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using DynamicData;
using ReactiveUI;
using SoupAndSoup.Data.Models;
using SoupAndSoup.Data.Services;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.ViewModels;

public class SoapDesignerViewModel : ViewModelBase
{
    private readonly RecipeService _recipeService;
    private readonly IngredientService _ingredientService;
    private readonly IngredientTypeService _ingredientTypeService;

    private readonly AddIngredientDialog _addIngredientDialogWindow;
    private readonly AddIngredientDialogViewModel _addIngredientDialogViewModel;

    public const string NoImage_Ingredient_Image = "Assets/efirne-limon.800x600w.jpg";
    public const string NoImage_Receipt_Image = "Assets/efirne-limon.800x600w.jpg";

    public ICommand NewReceiptCommand { get; private set; }
    public ICommand SaveReceiptCommand { get; private set; }
    public ICommand DeleteReceiptCommand { get; private set; }

    public ReactiveCommand<SoapTypeComponent, Unit> NewIngredientCommand { get; private set; }
    public ReactiveCommand<IngredientModel, Unit> EditIngredientCommand { get; private set; }
    public ReactiveCommand<IngredientModel, Unit> DeleteIngredientCommand { get; private set; }
   

    private bool _isReceiptEditMode;
    public bool IsReceiptEditMode
    {
        get => _isReceiptEditMode;
        set => this.RaiseAndSetIfChanged(ref _isReceiptEditMode, value);
    }


    public ObservableCollection<RecipeModel> Recipes { get; set; } = new();


    private RecipeModel? _selectedReceipt;
    public RecipeModel? SelectedReceipt
    {
        get => _selectedReceipt;
        set
        {
            foreach (var soapGroup in SoapGroups)
            {
                foreach (var ingredient in soapGroup.Ingredients)
                {
                    if (value?.RecipeIngredients.Any(i => i.IngredientId == ingredient.Id) == true)
                    {
                        ingredient.IsSelected = true;
                    }
                    else
                    {
                        ingredient.IsSelected = false;
                    }
                }
            }

            this.RaiseAndSetIfChanged(ref _selectedReceipt, value);
        }
    }

    public ObservableCollection<SoapGroup> SoapGroups { get; set; } = new();

    public Task Initialization { get; private set; }


    public SoapDesignerViewModel()
    {
        InitView();
    }

    public SoapDesignerViewModel(RecipeService recipeService,
        IngredientService ingredientService,
        IngredientTypeService ingredientTypeService,
        AddIngredientDialog addIngredientDialogWindow, 
        AddIngredientDialogViewModel addIngredientDialogViewModel)
    {
        _recipeService = recipeService;
        _ingredientService = ingredientService;
        _ingredientTypeService = ingredientTypeService;

        _addIngredientDialogWindow = addIngredientDialogWindow;
        _addIngredientDialogViewModel = addIngredientDialogViewModel;

        try
        {
            Initialization = InitializeAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"‼️ Exception: {ex.Message}");
            Debug.WriteLine(ex.StackTrace);
        }

        InitView();
    }

    private void InitView()
    {
        IsReceiptEditMode = true;


        NewReceiptCommand = ReactiveCommand.Create(NewReceipt, this.WhenAnyValue(_ => _.IsReceiptEditMode));
        SaveReceiptCommand = ReactiveCommand.CreateFromTask<RecipeModel>(SaveReceiptAsync);
        DeleteReceiptCommand = ReactiveCommand.CreateFromTask<RecipeModel>(DeleteReceiptAsync);

        NewIngredientCommand = ReactiveCommand.CreateFromTask<SoapTypeComponent>(OpenAddIngredientDialogAsync);
        EditIngredientCommand = ReactiveCommand.CreateFromTask<IngredientModel>(EditIngredientAsync);
        DeleteIngredientCommand = ReactiveCommand.CreateFromTask<IngredientModel>(DeleteIngredientAsync, Observable.Return(true));

        //FillTestData();
    }

    private async Task InitializeAsync()
    {
        var ingredientTypes = await _ingredientTypeService.GetAllAsync();
        SoapGroups.Clear();
        SoapGroups.AddRange(ingredientTypes.Select(_ => new SoapGroup
        {
            Title = _.Name,
            Type = (SoapTypeComponent)_.Id,
            NewIngredientCommand = NewIngredientCommand
        }));

        var ingredients = await _ingredientService.GetAllAsync();
        var groped = ingredients.GroupBy(_ => _.IngredientType);
        foreach (var group in groped.OrderBy(_ => _.Key.Order))
        {
            var ingredientModels = group
                .Select(ingredient =>
                {
                    var ingredientModel = MapIngredientModel(ingredient);

                    ingredientModel
                        .WhenAnyValue(x => x.IsSelected)
                        .Skip(1)
                        .Subscribe(_ => { HandleSelectedComponentChanged(ingredientModel); });

                    return ingredientModel;
                })
                .OrderBy(_ => IsLatin(_.Name))
                .ThenBy(_ => _.Name);

            var soapGroup = SoapGroups.FirstOrDefault(_ => _.Type == (SoapTypeComponent)group.Key.Id);
            soapGroup?.Ingredients.AddRange(ingredientModels);
        }

        var recipes = await _recipeService.GetAllAsync();
        var recipeModels = recipes.Select(MapRecipe);

        Recipes.AddRange(recipeModels);
        SelectedReceipt = Recipes.FirstOrDefault();
    }


    private void NewReceipt()
    {
        var newRecipe = new RecipeModel
        {
            Name = "Нова Рецептура",
            RecipeIngredients = new ObservableCollection<IngredientByReceiptModel>(),
            Description = string.Empty
        };
        Recipes.Add(newRecipe);
        IsReceiptEditMode = false;

        SelectedReceipt = newRecipe;
        SelectedReceipt.RecipeIngredients.Clear();
    }
   
    private async Task SaveReceiptAsync(RecipeModel newRecipeModel)
    {
        if (SelectedReceipt == null) return;

        var newRecipe = new Recipe
        {
            Amount = SelectedReceipt.Amount,
            Name = SelectedReceipt.Name,
            PreparationTime = TimeSpan.FromMinutes(SelectedReceipt.PreparationTime),
            Type = RecipeType.Soap.ToString(),
            UnitCost = SelectedReceipt.UnitCost,
            Description = SelectedReceipt.Description,
            RecipeIngredients = SelectedReceipt.RecipeIngredients.Select(_ => new RecipeIngredient
            {
                IngredientId = _.IngredientId,
                Amount = _.Amount
            }).ToList(),
        };

        var createdRecipe = await _recipeService.CreateAsync(newRecipe);
        if (createdRecipe != null)
        {
            SelectedReceipt.Id = createdRecipe.Id;
        }
    }

    private async Task<RecipeModel> DeleteReceiptAsync(RecipeModel recipeModel)
    {
        
        await _recipeService.SoftDelete(recipeModel.Id);
        
        Recipes.Remove(recipeModel);
        SelectedReceipt = Recipes.Count > 0 ? Recipes.FirstOrDefault() : null;
        return recipeModel;
    }

    
    private async Task OpenAddIngredientDialogAsync(SoapTypeComponent parameter)
    {
        var soapGroup = SoapGroups.FirstOrDefault(_ => _.Type == parameter);

        _addIngredientDialogWindow.DataContext = _addIngredientDialogViewModel;
        _addIngredientDialogViewModel.Init(soapGroup.Type);

        //TODO: Move to separate "Widnows Manager"
        await _addIngredientDialogWindow.ShowDialog<NewIngredientDto?>(
            (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow
        );

        if (_addIngredientDialogViewModel.NewIngredient == null)
        {
            return;
        }

        var ingredient = new Ingredient
        {
            Cost = _addIngredientDialogViewModel.NewIngredient.Cost,
            Name = _addIngredientDialogViewModel.NewIngredient.Name,
            IngredientTypeId = (int)soapGroup.Type,
            Images = new List<IngredientImage>
            {
                new()
                {
                    ImageUrl = _addIngredientDialogViewModel.NewIngredient.ImagePath
                }
            }
        };

        var saveResult = await _ingredientService.CreateAsync(ingredient);
        var currentImageUrl = saveResult.Images.FirstOrDefault()?.ImageUrl;
        var imageUrl = currentImageUrl?.Substring(currentImageUrl.IndexOf("Assets"));

        var newIngredient = new IngredientModel
        {
            Id = saveResult.Id,
            Name = saveResult.Name,
            ImagePath = ImageHelper.LoadFromResource(imageUrl)
        };
        soapGroup.Ingredients.Add(newIngredient);
    }

    private async Task DeleteIngredientAsync(IngredientModel ingredient)
    {
        var result = await _ingredientService.SoftDeleteAsync(ingredient.Id);
        if (!result) return;
     
        foreach (var soapGroup in SoapGroups)
        {
            var item = soapGroup.Ingredients.FirstOrDefault(i => i.Id == ingredient.Id);
            if (item != null)
            {
                soapGroup.Ingredients.Remove(item);
            }
        }
    }

    private async Task EditIngredientAsync(IngredientModel arg)
    {

        _addIngredientDialogWindow.DataContext = _addIngredientDialogViewModel;
        _addIngredientDialogViewModel.Init(arg);

        //TODO: Move to separate "Widnows Manager"
        await _addIngredientDialogWindow.ShowDialog<NewIngredientDto?>(
            (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow
        );

        if (_addIngredientDialogViewModel.NewIngredient == null)
        {
            return;
        }
    }

    private void HandleSelectedComponentChanged(IngredientModel ingredientModel)
    {
        if (SelectedReceipt == null)
        {
            return;
        }

        if (ingredientModel.IsSelected)
        {
            if (SelectedReceipt.RecipeIngredients.Any(_ => _.IngredientId == ingredientModel.Id))
                return;

            SelectedReceipt.RecipeIngredients.Add(new IngredientByReceiptModel
            {
                IngredientId = ingredientModel.Id,
                Name = ingredientModel.Name,
                Amount = ingredientModel.Count,
                Cost = ingredientModel.Cost,
                ImagePath = ingredientModel.ImagePath
            });
        }
        else
        {
            var item = SelectedReceipt.RecipeIngredients.FirstOrDefault(i => i.IngredientId == ingredientModel.Id);
            SelectedReceipt.RecipeIngredients.Remove(item);
        }

        ReCalculateUnitCost(SelectedReceipt);
    }

    private void ReCalculateUnitCost(RecipeModel? selectedReceipt)
    {
        if (selectedReceipt == null)
        {
            return;
        }

        selectedReceipt.UnitCost = selectedReceipt.RecipeIngredients
                .Sum(i => i.Cost);
    }
    private decimal CalculateIngredientCost(RecipeIngredient ri)
    {
        if (ri.Ingredient.IngredientType.Id == (int)SoapTypeComponent.Form)
        {
            return 0;
        }
        return ri.Ingredient.Cost * ri.Amount;
    }


    private RecipeModel MapRecipe(Recipe recipe)
    {
        var result = new RecipeModel
        {
            Id = recipe.Id,
            Name = recipe.Name,
            Description = recipe.Description,
            Amount = recipe.Amount,
            PreparationTime = (int)recipe.PreparationTime.TotalMinutes,
            DeleteReceiptCommand = DeleteReceiptCommand,
            RecipeIngredients = new(recipe.RecipeIngredients.Select(MapRecipeIngredients))
        };

        var imageUrl = recipe.Images.FirstOrDefault()?.ImageUrl
                          ?? recipe.RecipeIngredients
                              .FirstOrDefault(_ => _.Ingredient.IngredientTypeId == (int)SoapTypeComponent.Form)
                              ?.Ingredient.Images.FirstOrDefault()?.ImageUrl
                          ?? NoImage_Receipt_Image;
        result.ImagePath = ImageHelper.LoadFromResource(imageUrl);
        
        ReCalculateUnitCost(result);

        return result;
    }

    private IngredientByReceiptModel MapRecipeIngredients(RecipeIngredient ri)
    {
        var model = new IngredientByReceiptModel();
        model.IngredientId = ri.IngredientId;
        model.Type = (SoapTypeComponent)ri.Ingredient.IngredientTypeId;
        model.Name = ri.Ingredient.Name;
        model.Amount = ri.Amount;
        model.Cost = CalculateIngredientCost(ri);
        model.CountTitle = "";
        model.ImagePath = ImageHelper.LoadFromResource(ri.Ingredient.Images.FirstOrDefault()?.ImageUrl ?? NoImage_Ingredient_Image);
        return model;
    }

    private IngredientModel MapIngredientModel(Ingredient ingredientModel)
    {
        var result = new IngredientModel
        {
            Id = ingredientModel.Id,
            Name = ingredientModel.Name,
            ImagePath = ImageHelper.LoadFromResource(ingredientModel.Images.FirstOrDefault()?.ImageUrl ??
                                                     NoImage_Ingredient_Image),
            Count = 0, // Default count, can be set later
            Cost = ingredientModel.Cost,
            IsButton = false,
            IsSelected = false,
            DeleteCommand = DeleteIngredientCommand,
            EditCommand = EditIngredientCommand
        };
        return result;
    }

    private bool IsLatin(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        char firstChar = name[0];
        return firstChar >= 'A' && firstChar <= 'z';
    }

    private IEnumerable<IngredientModel> FillEssentialOils() =>
        new List<IngredientModel>
        {new() {IsSelected = false, IsButton = true},

        new() { ImagePath = ImageHelper.LoadFromResource("Assets/efirne-limon.800x600w.jpg"), Name = "Лимон", Count = 5, Cost = 2.5m },
        }
        .OrderBy(x => IsLatin(x.Name))
        .ThenBy(x => x.Name);

    private IEnumerable<IngredientModel> FillHerbalExtracts() =>
        new List<IngredientModel>
        {new() {IsSelected = false, IsButton = true},

        new() { ImagePath = ImageHelper.LoadFromResource("Assets/greipfrut-ekstrackt.800x600w.jpg"), Name = "Екстракт Грейпфрута гліколевий", Count = 3, Cost = 1.8m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/aloe-ekstract.800x600w.jpg"), Name = "Алое віра гліколевий", Count = 7, Cost = 2.2m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/lavanda-ekstract.800x600w.jpg"), Name = "Лаванди гліколевий", Count = 2, Cost = 2.9m ,Id = 4 },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/romashki-ekstrackt.800x600w.jpg"), Name = "Квіток Ромашки гліколевий", Count = 8, Cost = 1.4m },
        }
        .OrderBy(x => IsLatin(x.Name))
        .ThenBy(x => x.Name);

    private IEnumerable<IngredientModel> FillFragranceOils() =>
        new List<IngredientModel>
        {new() {IsSelected = false, IsButton = true},

        new() { ImagePath = ImageHelper.LoadFromResource("Assets/armani-zapashka.800x600w.jpg"), Name = "Acqua Di Gio Homme, Armani (чоловіча)", Count = 4, Cost = 2.7m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/beby-bergamot-flovers.800x600w.jpg"), Name = "Baby bergamot & Orange flower", Count = 6, Cost = 1.5m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/19-vanill-cream.800x600w.jpg"), Name = "Vanilla Cream", Count = 1, Cost = 2.1m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/07-apelsin.800x600w.jpg"), Name = "Апельсин", Count = 5, Cost = 1.9m, Id = 1},
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/18-chai.800x600w.jpg"), Name = "Грінвіталіті", Count = 3, Cost = 2.3m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/solodka-dinya-zapashka.800x600w.jpg"), Name = "Диня солодка", Count = 7, Cost = 1.6m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/68-yabloko.800x600w.jpg"), Name = "Зелене яблуко", Count = 4, Cost = 2.0m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/47-karamel.800x600w.jpg"), Name = "Карамель", Count = 6, Cost = 2.5m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/myata-s-laimom-01.800x600w.jpg"), Name = "М'ята з лаймом", Count = 2, Cost = 1.7m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/44-malina.800x600w.jpg"), Name = "Малина", Count = 5, Cost = 2.8m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/95-smorodina.800x600w.jpg"), Name = "Чорна смородина", Count = 7, Cost = 1.9m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/13-arbuz.800x600w.jpg"), Name = "Кавун", Count = 3, Cost = 2.2m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/62-kokos.800x600w.jpg"), Name = "Кокос", Count = 6, Cost = 1.8m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/krya-krya-otdushka.800x600w.jpg"), Name = "Кря-Кря", Count = 1, Cost = 2.9m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/35-morskaya-svezhest.800x600w.jpg"), Name = "Морська свіжість", Count = 8, Cost = 1.5m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/92-persik-nektarin.800x600w.jpg"), Name = "Персик нектарин", Count = 4, Cost = 2.3m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/63-kludnica-so-ldom.800x600w.jpg"), Name = "Полуниця з льодом", Count = 7, Cost = 2.0m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/coca-cola-otdushka.800x600w.jpg"), Name = "Кока кола", Count = 5, Cost = 1.4m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/09-goryachii-shokolad.800x600w.jpg"), Name = "Гарячий шоколад", Count = 3, Cost = 2.7m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/81-toplenoe-moloko.800x600w.jpg"), Name = "Вівсяне молочко", Count = 7, Cost = 1.9m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/amor-cacharel-zapashka.800x600w.jpg"), Name = "Amor Amor, Cacharel (жіноча)", Count = 4, Cost = 2.8m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/roza-alaya-otdushka.800x600w.jpg"), Name = "Троянда червона запашка", Count = 8, Cost = 1.5m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/belie-cveti-otdushka.800x600w.jpg"), Name = "Білі квіти", Count = 2, Cost = 2.1m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/49-kofe-s-koricei.800x600w (1).jpg"), Name = "Кава з корицею", Count = 1, Cost = 2.9m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/laviaestbell-zapashka.800x600w.jpg"), Name = "La vie est belle, Lancome (жіноча) ", Count = 5, Cost = 1.7m },
        }
        .OrderBy(x => IsLatin(x.Name))
        .ThenBy(x => x.Name);

    private IEnumerable<IngredientModel> FillPigments() =>
        new List<IngredientModel>
        {new() {IsSelected = false, IsButton = true},

        new() { ImagePath = ImageHelper.LoadFromResource("Assets/pigment-dlya-bombochek-malinov.800x600w.jpg"), Name = "Малиновий-крафт для бомб", Count = 3, Cost = 2.4m, Id = 6},
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/vrm-zheltii-barvnik.800x600w.jpg"), Name = "Жовтий", Count = 2, Cost = 1.5m , Id = 2},
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/vrm-korichnevii-barvnik.800x600w.jpg"), Name = "Коричневий", Count = 4, Cost = 1.7m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/pigment-perlamutr-sinii.800x600w.jpg"), Name = "Перламутровий синій", Count = 1, Cost = 3.0m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/pigment-biruza-sweden.800x600w.jpg"), Name = "Рідкий Бірюзовий", Count = 5, Cost = 2.2m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/pigm-perlam-vinno-chervonii.800x600w.jpg"), Name = "Перламутровий винно-червоний", Count = 2, Cost = 2.8m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/vrm-blakitnii-barvnik.800x600w.jpg"), Name = "Блакитний", Count = 3, Cost = 1.9m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/barvnik-red-neri.800x600w.jpg"), Name = "Neri color Red", Count = 6, Cost = 2.6m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/neri-barv-zelenii-01-1.800x600w.jpg"), Name = "Neri color Зелений", Count = 2, Cost = 2.4m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/neri-barv-bila-001.800x600w.jpg"), Name = "Neri color Білий", Count = 4, Cost = 2.0m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/neri-barv-pomaranch-01-1.800x600w.jpg"), Name = "Neri color Помаранчевий", Count = 3, Cost = 2.3m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/barvnik-rose-neri.800x600w.jpg"), Name = "Neri color Rose (Рожевий)", Count = 1, Cost = 2.7m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/neri-barv-sinii-01-1.800x600w.jpg"), Name = "Neri color Синій", Count = 3, Cost = 2.5m },
        }
        .OrderBy(x => IsLatin(x.Name))
        .ThenBy(x => x.Name);

    private IEnumerable<IngredientModel> FillCraftingBases() =>
        new List<IngredientModel>
        {
            new() {IsSelected = false, IsButton = true},

            new() {ImagePath = ImageHelper.LoadFromResource("Assets/crystal-bila_4.800x600w.jpg"), Name = "Crystal Triple Butter (масло Ши, Какао і Манго)", Count = 0, Cost = 70},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/mylnaya-osnova-stephenson-crystal-st_1.800x600w.jpg"), Name = "Crystal SLS Free прозора", Count = 0, Cost = 130},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/crystal-bila.800x600w.jpg"), Name = "Crystal Donkey Milk Біла", Count = 0, Cost = 100},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/milna-osnova-nco-organica.800x600w.jpg"), Name = "Crystal NCO (ORG) органічна", Count = 0, Cost = 600},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/neri-milna-osnova-aloe.800x600w.jpg"), Name = "Neri Aloe з екстрактом алое прозора", Count = 0, Cost = 30},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/neri-olivka-osnova-new-01.800x600w.jpg"), Name = "Neri Olive з оливковою олією напівпрозора", Count = 0, Cost = 100},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/svirli-osnova-milna.800x600w.jpg"), Name = "Основа для свірлов Neri Swirl", Count = 0, Cost = 150},
        }.OrderBy(x => IsLatin(x.Name))
            .ThenBy(x => x.Name);

    private IEnumerable<IngredientModel> FillSoapForms() =>
        new List<IngredientModel>
        {
            new() {IsSelected = false, IsButton = true},
            new() { ImagePath = ImageHelper.LoadFromResource("Assets/silikon-kvitka-ajstra-pishna.800x600w.jpg"), Name = "Айстра пишна розкрита 70г" , Count = 70, Cost = 0, IsSelected = false},
            new() { ImagePath = ImageHelper.LoadFromResource("Assets/silikon-serdechko-azhurne.800x600w.jpg"), Name = "Сердечко ажурне велике 130г", Count = 130, Cost = 0 },
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/apelsin-srednii-plastik-01.800x600w.jpg"), Name = "Апельсин середній 60г", Count = 60, Cost = 0, Id = 3},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/polyana-forma-01.500x500.jpg"), Name = "Зелена галявина в квітах 36г", Count = 36, Cost = 0, IsSelected = false, Id = 5},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/angel-v-rozah-elit-forma.800x600w.jpg"), Name = "Янгол в трояндах 90г", Count = 90, Cost = 0},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/205-zefir.800x600w.jpg"), Name = "Зефір 44г", Count = 44, Cost = 0},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/312-oduvanchik.800x600w.jpg"), Name = "Кульбаба 110г", Count = 110, Cost = 0},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/silikon-spiral.800x600w.jpg"), Name = "Спіраль 80г", Count = 80, Cost = 0},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/kofejnyj-krug-elit-forma.800x600w.jpg"), Name = "Кавове коло 87г", Count = 87, Cost = 0},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/silikon-polusferi-seredni-.800x600w.jpg"), Name = "Силіконові форми Півсфери 73г", Count = 73, Cost = 0}
        }.OrderBy(x => IsLatin(x.Name))
            .ThenBy(x => x.Name);

}