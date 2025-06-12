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
using Avalonia.Collections;

namespace SoupAndSoupApp.ViewModels;

public class SoapDesignerViewModel : ViewModelBase
{
    private readonly RecipeService _recipeService;
    private readonly IngredientService _ingredientService;
    private readonly IngredientTypeService _ingredientTypeService;

    private readonly AddIngredientDialog _addIngredientDialogWindow;
    private readonly AddIngredientDialogViewModel _addIngredientDialogViewModel;

    public const string NoImage_Ingredient_Image = "Assets/65fdbf22-c38e-434a-aca6-859009c6c51d.png";
    public const string NoImage_Receipt_Image = "Assets/65fdbf22-c38e-434a-aca6-859009c6c51d.png";

    public ICommand NewReceiptCommand { get; private set; }
    public ICommand SaveReceiptCommand { get; private set; }
    public ICommand DeleteReceiptCommand { get; private set; }

    public ReactiveCommand<SoapTypeComponent, Unit> NewComponentCommand { get; private set; }
    public ReactiveCommand<IngredientModel, Unit> EditComponentCommand { get; private set; }
    public ReactiveCommand<IngredientModel, Unit> DeleteComponentCommand { get; private set; }
   
    public bool IsReceiptEditMode
    {
        get => _isReceiptEditMode;
        set => this.RaiseAndSetIfChanged(ref _isReceiptEditMode, value);
    }


    public ObservableCollection<RecipeModel> Recipes { get; } = new();

    public RecipeModel? SelectedReceipt
    {
        get => _selectedReceipt;
        set
        {
            if (_selectedReceipt == value || value is null)  return;

            var tmpList = new List<IngredientModel>(ComponentsByReceipt);
            foreach (var ingredientModel in tmpList)
            {
                ingredientModel.IsSelected = false;
            }

            foreach (var ingredientByReceiptModel in value.RecipeIngredients)
            {
                var component = _cachedComponents.GetValueOrDefault(ingredientByReceiptModel.IngredientId);
                if (component == null)
                {
                    Debug.WriteLine($"Ingredient with ID {ingredientByReceiptModel.IngredientId} not found in cache.");
                    continue;
                }

                component.Amount = ingredientByReceiptModel.Amount;
                component.IsSelected = true;
            }

            this.RaiseAndSetIfChanged(ref _selectedReceipt, value);
        }
    }

    public ObservableCollection<ComponentGroup> ComponentGroups { get; set; } = new();

    public ObservableCollection<IngredientModel> ComponentsByReceipt { get; } = new();

    private Dictionary<int, IngredientModel> _cachedComponents = new();


    public Task Initialization { get; private set; }


    private bool _isReceiptEditMode;
    private RecipeModel? _selectedReceipt;


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

        NewComponentCommand = ReactiveCommand.CreateFromTask<SoapTypeComponent>(OpenAddIngredientDialogAsync);
        EditComponentCommand = ReactiveCommand.CreateFromTask<IngredientModel>(EditIngredientAsync);
        DeleteComponentCommand = ReactiveCommand.CreateFromTask<IngredientModel>(DeleteIngredientAsync, Observable.Return(true));

        //FillTestData();
    }


    private async Task InitializeAsync()
    {
        var ingredientTypes = await _ingredientTypeService.GetAllAsync();
        var ingredients = await _ingredientService.GetAllAsync();

        _cachedComponents = ingredients.ToDictionary(ingredient => ingredient.Id, MapIngredientModel);

        ComponentGroups.AddRange(
            ingredientTypes
                .OrderBy(_ => _.Order)
                .Select(ingredientType => MapComponentGroup(ingredientType, _cachedComponents)));

        var recipes = await _recipeService.GetAllAsync();
        var recipeModels = recipes.Select(MapRecipe);

        Recipes.AddRange(recipeModels);
        SelectedReceipt = Recipes.FirstOrDefault();

        //FillTestData();
       
    }

    private void FillTestData()
    {
       ComponentGroups.AddRange( new []
       {
            new ComponentGroup()
            {
                Components = new ObservableCollection<IngredientModel>(FillSoapForms()),
                Type = SoapTypeComponent.Form,
                Title = "Форми",
            },
            new ComponentGroup()
            {
                Components = new ObservableCollection<IngredientModel>(FillCraftingBases()),
                Type = SoapTypeComponent.CraftingBase,
                Title = "Основа",
            },

            new ComponentGroup()
            {
                Components = new ObservableCollection<IngredientModel>(FillPigments()),
                Type = SoapTypeComponent.Pigment,
                Title = "Барвники",
            },
            new ComponentGroup()
            {
                Components = new ObservableCollection<IngredientModel>(FillFragranceOils()),
                Type = SoapTypeComponent.FragranceOil,
                Title = "Ароматизатори",
            },
            new ComponentGroup()
            {
                Components = new ObservableCollection<IngredientModel>(FillEssentialOils()),
                Type = SoapTypeComponent.EssentialOil,
                Title = "Ефірні олії",
            },
            new ComponentGroup()
            {
                Components = new ObservableCollection<IngredientModel>(FillHerbalExtracts()),
                Type = SoapTypeComponent.HerbalExtract,
                Title = "Трав'яні екстракти",
            }
        });

       //Recipes.Add(new RecipeModel
       //{
       //    Name = "Нова Рецептура",
       //    RecipeIngredients = new ObservableCollection<IngredientModel>(),
       //    Description = string.Empty,
       //    Amount = 0,
       //    PreparationTime = 0,
       //    UnitCost = 0,
       //    ImagePath = ImageHelper.LoadFromResource(NoImage_Receipt_Image)
       //}
       //);
    }


    private void NewReceipt()
    {
        var newRecipe = new RecipeModel
        {
            Name = "Нова Рецептура",
            RecipeIngredients = new ObservableCollection<IngredientByReceiptModel>(),
            Description = string.Empty,
            ImagePath = ImageHelper.LoadFromResource(NoImage_Receipt_Image),
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
        var soapGroup = ComponentGroups.FirstOrDefault(_ => _.Type == parameter);

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
        soapGroup.Components.Add(newIngredient);
    }

    private async Task DeleteIngredientAsync(IngredientModel ingredient)
    {
        var result = await _ingredientService.SoftDeleteAsync(ingredient.Id);
        if (!result) return;
     
        foreach (var soapGroup in ComponentGroups)
        {
            var item = soapGroup.Components.FirstOrDefault(i => i.Id == ingredient.Id);
            if (item != null)
            {
                soapGroup.Components.Remove(item);
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
        if (ingredientModel.IsSelected)
        {
            ComponentsByReceipt.Add(ingredientModel);
        }
        else
        {
            ComponentsByReceipt.Remove(ingredientModel);
        }

        ReCalculateUnitCost(SelectedReceipt);
    }

    private void ReCalculateUnitCost(RecipeModel? receipt)
    {
        if (receipt == null)
        {
            return;
        }

        receipt.UnitCost = ComponentsByReceipt.Sum(i => i.Amount * i.Cost);
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
            ImagePath = ImageHelper.LoadFromResource(recipe.Images.FirstOrDefault()?.ImageUrl ?? NoImage_Receipt_Image),
            RecipeIngredients = new(recipe.RecipeIngredients.Select(_=> new IngredientByReceiptModel
            {
                Amount = _.Amount,
                IngredientId = _.IngredientId,
            }))
        };
        
        ReCalculateUnitCost(result);

        return result;
    }

    private ComponentGroup MapComponentGroup(IngredientType ingredientType, Dictionary<int, IngredientModel> ingredients)
    {
        var list = ingredients
            .Where(_ => (int)_.Value.Type == ingredientType.Id)
            .Select(_ => _.Value)
            .OrderBy(x => IsLatin(x.Name))
            .ThenBy(x => x.Name);

        var componentGroup = new ComponentGroup
        {
            Title = ingredientType.Name,
            Type = (SoapTypeComponent)ingredientType.Id,
            NewComponentCommand = NewComponentCommand
        };

        componentGroup.Components.AddRange(list);
        return componentGroup;
    }

    private IngredientModel MapIngredientModel(Ingredient ingredientModel)
    {
        var result = new IngredientModel
        {
            Id = ingredientModel.Id,
            Name = ingredientModel.Name,
            Amount = 1,
            Cost = ingredientModel.Cost,
            DeleteCommand = DeleteComponentCommand,
            EditCommand = EditComponentCommand,
            Type = (SoapTypeComponent)ingredientModel.IngredientType.Id,
            MeasureType = new MeasureTypeModel(ingredientModel.AmountType.Id, ingredientModel.AmountType.ShortName),
            ImagePath = ImageHelper.LoadFromResource(ingredientModel.Images.FirstOrDefault()?.ImageUrl ??
                                                     NoImage_Ingredient_Image),
        };

        result
            .WhenAnyValue(x => x.IsSelected)
            .Subscribe(_ => { HandleSelectedComponentChanged(result); });
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

        new() { ImagePath = ImageHelper.LoadFromResource("Assets/efirne-limon.800x600w.jpg"), Name = "Лимон", Amount = 5, Cost = 2.5m },
        }
        .OrderBy(x => IsLatin(x.Name))
        .ThenBy(x => x.Name);

    private IEnumerable<IngredientModel> FillHerbalExtracts() =>
        new List<IngredientModel>
        {new() {IsSelected = false, IsButton = true},

        new() { ImagePath = ImageHelper.LoadFromResource("Assets/greipfrut-ekstrackt.800x600w.jpg"), Name = "Екстракт Грейпфрута гліколевий", Amount = 3, Cost = 1.8m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/aloe-ekstract.800x600w.jpg"), Name = "Алое віра гліколевий", Amount = 7, Cost = 2.2m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/lavanda-ekstract.800x600w.jpg"), Name = "Лаванди гліколевий", Amount = 2, Cost = 2.9m ,Id = 4 },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/romashki-ekstrackt.800x600w.jpg"), Name = "Квіток Ромашки гліколевий", Amount = 8, Cost = 1.4m },
        }
        .OrderBy(x => IsLatin(x.Name))
        .ThenBy(x => x.Name);

    private IEnumerable<IngredientModel> FillFragranceOils() =>
        new List<IngredientModel>
        {new() {IsSelected = false, IsButton = true},

        new() { ImagePath = ImageHelper.LoadFromResource("Assets/armani-zapashka.800x600w.jpg"), Name = "Acqua Di Gio Homme, Armani (чоловіча)", Amount = 4, Cost = 2.7m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/beby-bergamot-flovers.800x600w.jpg"), Name = "Baby bergamot & Orange flower", Amount = 6, Cost = 1.5m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/19-vanill-cream.800x600w.jpg"), Name = "Vanilla Cream", Amount = 1, Cost = 2.1m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/07-apelsin.800x600w.jpg"), Name = "Апельсин", Amount = 5, Cost = 1.9m, Id = 1},
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/18-chai.800x600w.jpg"), Name = "Грінвіталіті", Amount = 3, Cost = 2.3m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/solodka-dinya-zapashka.800x600w.jpg"), Name = "Диня солодка", Amount = 7, Cost = 1.6m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/68-yabloko.800x600w.jpg"), Name = "Зелене яблуко", Amount = 4, Cost = 2.0m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/47-karamel.800x600w.jpg"), Name = "Карамель", Amount = 6, Cost = 2.5m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/myata-s-laimom-01.800x600w.jpg"), Name = "М'ята з лаймом", Amount = 2, Cost = 1.7m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/44-malina.800x600w.jpg"), Name = "Малина", Amount = 5, Cost = 2.8m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/95-smorodina.800x600w.jpg"), Name = "Чорна смородина", Amount = 7, Cost = 1.9m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/13-arbuz.800x600w.jpg"), Name = "Кавун", Amount = 3, Cost = 2.2m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/62-kokos.800x600w.jpg"), Name = "Кокос", Amount = 6, Cost = 1.8m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/krya-krya-otdushka.800x600w.jpg"), Name = "Кря-Кря", Amount = 1, Cost = 2.9m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/35-morskaya-svezhest.800x600w.jpg"), Name = "Морська свіжість", Amount = 8, Cost = 1.5m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/92-persik-nektarin.800x600w.jpg"), Name = "Персик нектарин", Amount = 4, Cost = 2.3m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/63-kludnica-so-ldom.800x600w.jpg"), Name = "Полуниця з льодом", Amount = 7, Cost = 2.0m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/coca-cola-otdushka.800x600w.jpg"), Name = "Кока кола", Amount = 5, Cost = 1.4m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/09-goryachii-shokolad.800x600w.jpg"), Name = "Гарячий шоколад", Amount = 3, Cost = 2.7m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/81-toplenoe-moloko.800x600w.jpg"), Name = "Вівсяне молочко", Amount = 7, Cost = 1.9m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/amor-cacharel-zapashka.800x600w.jpg"), Name = "Amor Amor, Cacharel (жіноча)", Amount = 4, Cost = 2.8m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/roza-alaya-otdushka.800x600w.jpg"), Name = "Троянда червона запашка", Amount = 8, Cost = 1.5m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/belie-cveti-otdushka.800x600w.jpg"), Name = "Білі квіти", Amount = 2, Cost = 2.1m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/49-kofe-s-koricei.800x600w (1).jpg"), Name = "Кава з корицею", Amount = 1, Cost = 2.9m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/laviaestbell-zapashka.800x600w.jpg"), Name = "La vie est belle, Lancome (жіноча) ", Amount = 5, Cost = 1.7m },
        }
        .OrderBy(x => IsLatin(x.Name))
        .ThenBy(x => x.Name);

    private IEnumerable<IngredientModel> FillPigments() =>
        new List<IngredientModel>
        {new() {IsSelected = false, IsButton = true},

        new() { ImagePath = ImageHelper.LoadFromResource("Assets/pigment-dlya-bombochek-malinov.800x600w.jpg"), Name = "Малиновий-крафт для бомб", Amount = 3, Cost = 2.4m, Id = 6},
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/vrm-zheltii-barvnik.800x600w.jpg"), Name = "Жовтий", Amount = 2, Cost = 1.5m , Id = 2},
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/vrm-korichnevii-barvnik.800x600w.jpg"), Name = "Коричневий", Amount = 4, Cost = 1.7m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/pigment-perlamutr-sinii.800x600w.jpg"), Name = "Перламутровий синій", Amount = 1, Cost = 3.0m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/pigment-biruza-sweden.800x600w.jpg"), Name = "Рідкий Бірюзовий", Amount = 5, Cost = 2.2m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/pigm-perlam-vinno-chervonii.800x600w.jpg"), Name = "Перламутровий винно-червоний", Amount = 2, Cost = 2.8m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/vrm-blakitnii-barvnik.800x600w.jpg"), Name = "Блакитний", Amount = 3, Cost = 1.9m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/barvnik-red-neri.800x600w.jpg"), Name = "Neri color Red", Amount = 6, Cost = 2.6m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/neri-barv-zelenii-01-1.800x600w.jpg"), Name = "Neri color Зелений", Amount = 2, Cost = 2.4m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/neri-barv-bila-001.800x600w.jpg"), Name = "Neri color Білий", Amount = 4, Cost = 2.0m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/neri-barv-pomaranch-01-1.800x600w.jpg"), Name = "Neri color Помаранчевий", Amount = 3, Cost = 2.3m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/barvnik-rose-neri.800x600w.jpg"), Name = "Neri color Rose (Рожевий)", Amount = 1, Cost = 2.7m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/neri-barv-sinii-01-1.800x600w.jpg"), Name = "Neri color Синій", Amount = 3, Cost = 2.5m },
        }
        .OrderBy(x => IsLatin(x.Name))
        .ThenBy(x => x.Name);

    private IEnumerable<IngredientModel> FillCraftingBases() =>
        new List<IngredientModel>
        {
            new() {IsSelected = false, IsButton = true},

            new() {ImagePath = ImageHelper.LoadFromResource("Assets/crystal-bila_4.800x600w.jpg"), Name = "Crystal Triple Butter (масло Ши, Какао і Манго)", Amount = 0, Cost = 70},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/mylnaya-osnova-stephenson-crystal-st_1.800x600w.jpg"), Name = "Crystal SLS Free прозора", Amount = 0, Cost = 130},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/crystal-bila.800x600w.jpg"), Name = "Crystal Donkey Milk Біла", Amount = 0, Cost = 100},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/milna-osnova-nco-organica.800x600w.jpg"), Name = "Crystal NCO (ORG) органічна", Amount = 0, Cost = 600},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/neri-milna-osnova-aloe.800x600w.jpg"), Name = "Neri Aloe з екстрактом алое прозора", Amount = 0, Cost = 30},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/neri-olivka-osnova-new-01.800x600w.jpg"), Name = "Neri Olive з оливковою олією напівпрозора", Amount = 0, Cost = 100},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/svirli-osnova-milna.800x600w.jpg"), Name = "Основа для свірлов Neri Swirl", Amount = 0, Cost = 150},
        }.OrderBy(x => IsLatin(x.Name))
            .ThenBy(x => x.Name);

    private IEnumerable<IngredientModel> FillSoapForms() =>
        new List<IngredientModel>
        {
            new() {IsSelected = false, IsButton = true},
            new() { ImagePath = ImageHelper.LoadFromResource("Assets/silikon-kvitka-ajstra-pishna.800x600w.jpg"), Name = "Айстра пишна розкрита 70г" , Amount = 70, Cost = 0, IsSelected = false},
            new() { ImagePath = ImageHelper.LoadFromResource("Assets/silikon-serdechko-azhurne.800x600w.jpg"), Name = "Сердечко ажурне велике 130г", Amount = 130, Cost = 0 },
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/apelsin-srednii-plastik-01.800x600w.jpg"), Name = "Апельсин середній 60г", Amount = 60, Cost = 0, Id = 3},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/polyana-forma-01.500x500.jpg"), Name = "Зелена галявина в квітах 36г", Amount = 36, Cost = 0, IsSelected = false, Id = 5},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/angel-v-rozah-elit-forma.800x600w.jpg"), Name = "Янгол в трояндах 90г", Amount = 90, Cost = 0},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/205-zefir.800x600w.jpg"), Name = "Зефір 44г", Amount = 44, Cost = 0},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/312-oduvanchik.800x600w.jpg"), Name = "Кульбаба 110г", Amount = 110, Cost = 0},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/silikon-spiral.800x600w.jpg"), Name = "Спіраль 80г", Amount = 80, Cost = 0},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/kofejnyj-krug-elit-forma.800x600w.jpg"), Name = "Кавове коло 87г", Amount = 87, Cost = 0},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/silikon-polusferi-seredni-.800x600w.jpg"), Name = "Силіконові форми Півсфери 73г", Amount = 73, Cost = 0}
        }.OrderBy(x => IsLatin(x.Name))
            .ThenBy(x => x.Name);

}