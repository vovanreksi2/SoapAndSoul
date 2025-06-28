using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData;
using ReactiveUI;
using SoupAndSoup.Data.Models;
using SoupAndSoup.Data.Services;
using SoupAndSoupApp.Helpers;
using SoupAndSoupApp.Models;
using ComponentType = SoupAndSoupApp.Models.ComponentType;
using CosmeticType = SoupAndSoupApp.Models.CosmeticType;
using MeasureType = SoupAndSoup.Data.Models.MeasureType;

namespace SoupAndSoupApp.ViewModels;

public class SoapDesignerViewModel : ViewModelBase
{
    public const string NoImage_Receipt = "Assets/No_Receipt_Photo.png";
    public const string NoImage_Component_Image = "Assets/No_Component_Photo.png";

    public ICommand NewReceiptCommand { get; private set; }
    public ICommand SaveReceiptCommand { get; private set; }
    public ICommand DeleteReceiptCommand { get; private set; }

    public ReactiveCommand<ComponentType, Unit> NewComponentCommand { get; private set; }
    public ReactiveCommand<ComponentModel, Unit> EditComponentCommand { get; private set; }
    public ReactiveCommand<ComponentModel, Unit> DeleteComponentCommand { get; private set; }


    public bool IsReceiptEditMode
    {
        get => _isReceiptEditMode;
        set => this.RaiseAndSetIfChanged(ref _isReceiptEditMode, value);
    }

    public string NewImagePath
    {
        get => _newImagePath;
        set
        {
            if (_newImagePath == value) return;
            if (SelectedReceipt != null) 
                SelectedReceipt.ImagePath = ImageHelper.LoadFromResource(value);

            this.RaiseAndSetIfChanged(ref _newImagePath, value);
        }
    }

    public ObservableCollection<RecipeModel> Recipes { get; } = new();

    public RecipeModel? SelectedReceipt
    {
        get => _selectedReceipt;
        set
        {
            if (_selectedReceipt == value || value is null)  return;

            var tmpList = new List<ComponentModel>(ComponentsByReceipt);
            foreach (var ingredientModel in tmpList)
            {
                ingredientModel.IsSelected = false;
            }

            foreach (var ingredientByReceiptModel in value.RecipeIngredients)
            {
                var component = _cachedComponents.GetValueOrDefault(ingredientByReceiptModel.ComponentId);
                if (component == null)
                {
                    Debug.WriteLine($"Component with ID {ingredientByReceiptModel.ComponentId} not found in cache.");
                    continue;
                }

                component.AmountInRecipe = ingredientByReceiptModel.Amount;
                component.IsSelected = true;
            }

            this.RaiseAndSetIfChanged(ref _selectedReceipt, value);
        }
    }

    public ObservableCollection<ComponentGroup> ComponentGroups { get; set; } = new();

    public ObservableCollection<ComponentModel> ComponentsByReceipt { get; } = new();

    private Dictionary<int, ComponentModel> _cachedComponents = new();


    public Task Initialization { get; }

    private readonly RecipeService _recipeService;
    private readonly ComponentService _componentService;
    private readonly ComponentTypeService _componentTypeService;

    private readonly IDialogService _dialogService;

    private bool _isReceiptEditMode;
    private RecipeModel? _selectedReceipt;
    private string _newImagePath;
    private readonly IUnitCostCalculator _unitCostCalc;
    private bool _suppressSelectionChange;
    private Dictionary<int, ComponentTypeModel> _cachedComponentTypes;
    private readonly MeasureTypeCache _measureTypeCache;


    public SoapDesignerViewModel()
    {
        InitView();
    }

    public SoapDesignerViewModel(
        RecipeService recipeService,
        ComponentService componentService,
        ComponentTypeService componentTypeService, 
        IDialogService dialogService, IUnitCostCalculator unitCostCalc, MeasureTypeCache measureTypeCache)
    {
        _recipeService = recipeService;
        _componentService = componentService;
        _componentTypeService = componentTypeService;

        _dialogService = dialogService;
        _unitCostCalc = unitCostCalc;
        _measureTypeCache = measureTypeCache;

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

        NewComponentCommand = ReactiveCommand.CreateFromTask<ComponentType>(AddComponentAsync);
        EditComponentCommand = ReactiveCommand.CreateFromTask<ComponentModel>(EditComponentAsync);
        DeleteComponentCommand = ReactiveCommand.CreateFromTask<ComponentModel>(DeleteComponentAsync, Observable.Return(true));

        //FillTestData();
    }

    private async Task InitializeAsync()
    {
        const int cosmeticType = (int)CosmeticType.Soap;
        try
        {
            var componentTypes = await _componentTypeService.GetAllAsync(cosmeticType);
            _cachedComponentTypes = componentTypes.ToDictionary(type => type.Id, type => new ComponentTypeModel(type));

            var components = await _componentService.GetAllAsync(cosmeticType);
         ;
            var mappedComponents = await Task.WhenAll(components.Select(async component =>
                new
                {
                    component.Id,
                    Mapped = await MapComponentModelAsync(component)
                }));

            _cachedComponents = mappedComponents.ToDictionary(x => x.Id, x => x.Mapped);

            ComponentGroups.AddRange(
                _cachedComponentTypes
                     .OrderBy(_ => _.Value.Order)
                     .Select(componentType => MapComponentGroup(componentType.Value, _cachedComponents))
            );
 
            var recipes = await _recipeService.GetAllAsync();
            var recipeModels = recipes.Select(MapRecipe);

            Recipes.AddRange(recipeModels);
            SelectedReceipt = Recipes.FirstOrDefault();

            //FillTestData();

        }
        catch (Exception e)
        {
            Debug.WriteLine($"‼️ Exception during initialization: {e.Message}");
        }
    }


    private void NewReceipt()
    {
        var newRecipe = new RecipeModel
        {
            Name = "Нова Рецептура",
            RecipeIngredients = new ObservableCollection<ComponentByRecipeModel>(),
            Description = string.Empty,
            ImagePath = ImageHelper.LoadFromResource(NoImage_Receipt),
        };
        Recipes.Add(newRecipe);
        IsReceiptEditMode = false;

        SelectedReceipt = newRecipe;
    }
   
    private async Task SaveReceiptAsync(RecipeModel newRecipeModel)
    {
        if (SelectedReceipt == null) return;

        var newRecipe = new Recipe
        {
            Amount = SelectedReceipt.Amount,
            Name = SelectedReceipt.Name,
            PreparationTime = TimeSpan.FromMinutes(SelectedReceipt.PreparationTime),
            Type = "Soap",
            Description = SelectedReceipt.Description,
            RecipeComponents = SelectedReceipt.RecipeIngredients.Select(_ => new RecipeComponent
            {
                ComponentId = _.ComponentId,
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

    
    private async Task AddComponentAsync(ComponentType parameter)
    {
        var group = ComponentGroups.First(_ => _.ComponentType.Type == parameter);
        
        var newComponentDto = await _dialogService.ShowAddEditComponentDialogAsync(false, group.ComponentType);
        if (newComponentDto is null) return;

        var component = MapComponent(newComponentDto, group.ComponentType.Type);

        var saveResult = await _componentService.CreateAsync(component);
        
        var tmpList = new List<ComponentModel>();
        tmpList.AddRange(group.Components);
        tmpList.Add(await MapComponentModelAsync(saveResult));

        group.Components.Clear();
        group.Components.AddRange(
            tmpList
                .OrderBy(x => IsLatin(x.Name))
                .ThenBy(x => x.Name));
    }

    private async Task EditComponentAsync(ComponentModel componentModel)
    {
        var group = ComponentGroups.First(_ => _.ComponentType.Type == componentModel.Type);

        var editedComponentDto = await _dialogService.ShowAddEditComponentDialogAsync(true, group.ComponentType, componentModel);
        if (editedComponentDto is null) return;

        var component = MapComponent(editedComponentDto, componentModel.Type, componentModel.Id);

        var saveResult = await _componentService.UpdateAsync(component);
        if (saveResult)
        {
            var existingComponent = _cachedComponents.GetValueOrDefault(componentModel.Id);
            if (existingComponent is null)
            {
                Debug.WriteLine($"Component with ID {componentModel.Id} not found in cache.");
                return;
            }
           
            //TODO Change to Copy pattern
            existingComponent.Name = component.Name;
            existingComponent.BuyAmount = component.BuyAmount;
            existingComponent.Cost = component.Cost;
            existingComponent.BuyPrice = component.BuyPrice;
            existingComponent.UseMeasureTypeId = component.UseMeasureTypeId;
            existingComponent.BuyMeasureTypeId = component.BuyMeasureTypeId;
            existingComponent.SuggestedAmount = component.SuggestedAmount;
            existingComponent.Type = (ComponentType)component.ComponentTypeId;
            
            existingComponent.ImagePath = component.Images.FirstOrDefault()?.ImageUrl is null
                ? componentModel.ImagePath
                : ImageHelper.LoadFromResource(component.Images.First().ImageUrl);
            return;
        }

        Debug.WriteLine(saveResult
            ? $"Success for save component with ID {componentModel.Id}"
            : $"Failed to update component with ID {componentModel.Id}");
    }

    private async Task DeleteComponentAsync(ComponentModel component)
    {
        var result = await _componentService.SoftDeleteAsync(component.Id);
        if (!result)
        {
            Debug.WriteLine($"Failed to delete component with ID {component.Id}.");
            return;
        }

        ComponentGroups.FirstOrDefault(_ => _.ComponentType.Type == component.Type)?.Components.Remove(component);
        _cachedComponents.Remove(component.Id);
    }


    private void HandleSelectedComponentChanged(ComponentModel componentModel)
    {
        if (_suppressSelectionChange) return;


        var tmpList = new List<ComponentModel>(ComponentsByReceipt);

        UpdateComponentInGroupAccordingToRules(componentModel, tmpList);

        if (componentModel.IsSelected)
            tmpList.Add(componentModel);
        else
            tmpList.Remove(componentModel);

        ComponentsByReceipt.Clear();
        ComponentsByReceipt.AddRange(

            tmpList
                .OrderBy(_ => _.Type)
                .ThenBy(x => IsLatin(x.Name))
                .ThenBy(x => x.Name));

        if (SelectedReceipt != null)
            ReCalculateUnitCost(ComponentsByReceipt);
    }

    private void HandleAmountComponentChanged(ComponentModel componentModel)
    {
        if (componentModel.IsSelected && SelectedReceipt != null)
            ReCalculateUnitCost(ComponentsByReceipt);
    }


    private void ReCalculateUnitCost(IEnumerable<ComponentModel> components)
    {
        if (!components.Any())
        {
            Debug.WriteLine("No components found to calculate unit cost.");
            return;
        }

        if (SelectedReceipt == null)
        {
            Debug.WriteLine("Selected receipt is null, cannot calculate unit cost.");
            return;
        }

        SelectedReceipt.UnitCost = _unitCostCalc.CalculateUnitCost(components);
    }

    private void UpdateComponentInGroupAccordingToRules(ComponentModel componentModel, List<ComponentModel> tmpList)
    {
        try
        {
            _suppressSelectionChange = true;
            //TODO: Change to dictionary
            switch (componentModel.Type)
            {
                case ComponentType.Form:
                    UpdateComponents(ComponentsByReceipt, componentModel.Type);

                    var craftingBase = ComponentsByReceipt.FirstOrDefault(_ => _.Type == ComponentType.CraftingBase);
                    if (craftingBase != null)
                        craftingBase.AmountInRecipe = componentModel.SuggestedAmount;
                    break;

                case ComponentType.EssentialOil:
                    UpdateComponents(ComponentsByReceipt, ComponentType.FragranceOil);
                    componentModel.AmountInRecipe = componentModel.SuggestedAmount;
                    break;

                case ComponentType.FragranceOil:
                    UpdateComponents(ComponentsByReceipt, ComponentType.EssentialOil);
                    componentModel.AmountInRecipe = componentModel.SuggestedAmount;
                    break;

                case ComponentType.CraftingBase:
                    var form = ComponentsByReceipt.FirstOrDefault(_ => _.Type == ComponentType.Form);
                    if (form != null && componentModel.IsSelected)
                        componentModel.AmountInRecipe = form.SuggestedAmount;
                    else
                        componentModel.AmountInRecipe = componentModel.SuggestedAmount;

                    break;
                case ComponentType.Pigment:
                case ComponentType.HerbalExtract:
                case ComponentType.Tools:
                case ComponentType.Other:
                    componentModel.AmountInRecipe = componentModel.SuggestedAmount;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        finally
        {
            _suppressSelectionChange = false;
        }

        void UpdateComponents(IEnumerable<ComponentModel> components, ComponentType typeComponent)
        {
            foreach (var component in components.Where(_ => _.Type == typeComponent && _.Id != componentModel.Id))
            {
                component.IsSelected = false;
                tmpList.Remove(component);
            }
        }
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
            ImagePath = ImageHelper.LoadFromResource(recipe.Images.FirstOrDefault()?.ImageUrl ?? NoImage_Receipt),
            RecipeIngredients = recipe.RecipeComponents.Select(_=> new ComponentByRecipeModel
            {
                Amount = _.Amount,
                ComponentId = _.ComponentId,
            }),
        };

        result.UnitCost = _unitCostCalc.CalculateUnitCost(result.RecipeIngredients, _cachedComponents);

        return result;
    }

    private ComponentGroup MapComponentGroup(ComponentTypeModel componentType, Dictionary<int, ComponentModel> components)
    {
        var list = components
            .Where(_ => _.Value.Type == componentType.Type)
            .Select(_ => _.Value)
            .OrderBy(x => IsLatin(x.Name))
            .ThenBy(x => x.Name).ToList();

        var componentGroup = new ComponentGroup
        {
            NewComponentCommand = NewComponentCommand,
            ComponentType = componentType
        };
        
        componentGroup.Components.AddRange(list);
        return componentGroup;
    }

    private async Task<ComponentModel> MapComponentModelAsync(Component componentModel)
    {
        var componentType = (ComponentType)componentModel.ComponentTypeId;

        var result = new ComponentModel
        {
            Id = componentModel.Id,
            Name = componentModel.Name,
            Cost = componentModel.Cost,

            SuggestedAmount = componentModel.SuggestedAmount,
            BuyPrice = componentModel.BuyPrice,
            BuyAmount = componentModel.BuyAmount,

            DeleteCommand = DeleteComponentCommand,
            EditCommand = EditComponentCommand,
            ShowAmountInButton = componentType != ComponentType.Form,
            Type = componentType,
            
            BuyMeasureTypeId = componentModel.BuyMeasureTypeId,
            UseMeasureTypeId = componentModel.UseMeasureTypeId,
            BuyMeasureTypeShortTitle = (await _measureTypeCache.GetOrAddAsync(componentModel.BuyMeasureTypeId)).ShortTitle,
            UseMeasureTypeShortTitle = (await _measureTypeCache.GetOrAddAsync(componentModel.UseMeasureTypeId)).ShortTitle,

            ImagePath = ImageHelper.LoadFromResource(componentModel.Images.FirstOrDefault()?.ImageUrl ??
                                                     NoImage_Component_Image)
        };


        result
            .WhenAnyValue(x => x.IsSelected)
            .Skip(1)
            .Subscribe(_ => { HandleSelectedComponentChanged(result); });

        result
            .WhenAnyValue(x => x.BuyAmount)
            .Skip(1)
            .Subscribe(_ => { HandleAmountComponentChanged(result);});

        return result;
    }

    private Component MapComponent(NewComponentDto componentDto, ComponentType ingredientType, int? ingredientId = null)
    {
        var ingredient = new Component
        {
            Cost = componentDto.Cost,
            Name = componentDto.Name,
            ComponentTypeId = (int)ingredientType,
            UseMeasureTypeId = componentDto.UseMeasureType.Id,
            BuyMeasureTypeId = componentDto.BuyMeasureType.Id,
            SuggestedAmount = (int)componentDto.SuggestedAmount,
            BuyAmount = (int)componentDto.BuyAmount,
            BuyPrice = componentDto.BuyPrice
        };

        if (ingredientId.HasValue)
            ingredient.Id = ingredientId.Value;

        if (!string.IsNullOrEmpty(componentDto.ImagePath))
            ingredient.Images.Add(new ComponentImage { ImageUrl = componentDto.ImagePath });

        return ingredient;
    }
    private bool IsLatin(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        char firstChar = name[0];
        return firstChar >= 'A' && firstChar <= 'z';
    }

    private IEnumerable<ComponentModel> FillEssentialOils() =>
        new List<ComponentModel>
        {new() {IsSelected = false, IsButton = true},

        new() { ImagePath = ImageHelper.LoadFromResource("Assets/efirne-limon.800x600w.jpg"), Name = "Лимон", BuyAmount = 5, Cost = 2.5m },
        }
        .OrderBy(x => IsLatin(x.Name))
        .ThenBy(x => x.Name);

    private IEnumerable<ComponentModel> FillHerbalExtracts() =>
        new List<ComponentModel>
        {new() {IsSelected = false, IsButton = true},

        new() { ImagePath = ImageHelper.LoadFromResource("Assets/greipfrut-ekstrackt.800x600w.jpg"), Name = "Екстракт Грейпфрута гліколевий", BuyAmount = 3, Cost = 1.8m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/aloe-ekstract.800x600w.jpg"), Name = "Алое віра гліколевий", BuyAmount = 7, Cost = 2.2m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/lavanda-ekstract.800x600w.jpg"), Name = "Лаванди гліколевий", BuyAmount = 2, Cost = 2.9m ,Id = 4 },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/romashki-ekstrackt.800x600w.jpg"), Name = "Квіток Ромашки гліколевий", BuyAmount = 8, Cost = 1.4m },
        }
        .OrderBy(x => IsLatin(x.Name))
        .ThenBy(x => x.Name);

    private IEnumerable<ComponentModel> FillFragranceOils() =>
        new List<ComponentModel>
        {new() {IsSelected = false, IsButton = true},

        new() { ImagePath = ImageHelper.LoadFromResource("Assets/armani-zapashka.800x600w.jpg"), Name = "Acqua Di Gio Homme, Armani (чоловіча)", BuyAmount = 4, Cost = 2.7m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/beby-bergamot-flovers.800x600w.jpg"), Name = "Baby bergamot & Orange flower", BuyAmount = 6, Cost = 1.5m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/19-vanill-cream.800x600w.jpg"), Name = "Vanilla Cream", BuyAmount = 1, Cost = 2.1m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/07-apelsin.800x600w.jpg"), Name = "Апельсин", BuyAmount = 5, Cost = 1.9m, Id = 1},
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/18-chai.800x600w.jpg"), Name = "Грінвіталіті", BuyAmount = 3, Cost = 2.3m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/solodka-dinya-zapashka.800x600w.jpg"), Name = "Диня солодка", BuyAmount = 7, Cost = 1.6m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/68-yabloko.800x600w.jpg"), Name = "Зелене яблуко", BuyAmount = 4, Cost = 2.0m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/47-karamel.800x600w.jpg"), Name = "Карамель", BuyAmount = 6, Cost = 2.5m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/myata-s-laimom-01.800x600w.jpg"), Name = "М'ята з лаймом", BuyAmount = 2, Cost = 1.7m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/44-malina.800x600w.jpg"), Name = "Малина", BuyAmount = 5, Cost = 2.8m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/95-smorodina.800x600w.jpg"), Name = "Чорна смородина", BuyAmount = 7, Cost = 1.9m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/13-arbuz.800x600w.jpg"), Name = "Кавун", BuyAmount = 3, Cost = 2.2m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/62-kokos.800x600w.jpg"), Name = "Кокос", BuyAmount = 6, Cost = 1.8m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/krya-krya-otdushka.800x600w.jpg"), Name = "Кря-Кря", BuyAmount = 1, Cost = 2.9m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/35-morskaya-svezhest.800x600w.jpg"), Name = "Морська свіжість", BuyAmount = 8, Cost = 1.5m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/92-persik-nektarin.800x600w.jpg"), Name = "Персик нектарин", BuyAmount = 4, Cost = 2.3m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/63-kludnica-so-ldom.800x600w.jpg"), Name = "Полуниця з льодом", BuyAmount = 7, Cost = 2.0m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/coca-cola-otdushka.800x600w.jpg"), Name = "Кока кола", BuyAmount = 5, Cost = 1.4m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/09-goryachii-shokolad.800x600w.jpg"), Name = "Гарячий шоколад", BuyAmount = 3, Cost = 2.7m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/81-toplenoe-moloko.800x600w.jpg"), Name = "Вівсяне молочко", BuyAmount = 7, Cost = 1.9m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/amor-cacharel-zapashka.800x600w.jpg"), Name = "Amor Amor, Cacharel (жіноча)", BuyAmount = 4, Cost = 2.8m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/roza-alaya-otdushka.800x600w.jpg"), Name = "Троянда червона запашка", BuyAmount = 8, Cost = 1.5m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/belie-cveti-otdushka.800x600w.jpg"), Name = "Білі квіти", BuyAmount = 2, Cost = 2.1m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/49-kofe-s-koricei.800x600w (1).jpg"), Name = "Кава з корицею", BuyAmount = 1, Cost = 2.9m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/laviaestbell-zapashka.800x600w.jpg"), Name = "La vie est belle, Lancome (жіноча) ", BuyAmount = 5, Cost = 1.7m },
        }
        .OrderBy(x => IsLatin(x.Name))
        .ThenBy(x => x.Name);

    private IEnumerable<ComponentModel> FillPigments() =>
        new List<ComponentModel>
        {new() {IsSelected = false, IsButton = true},

        new() { ImagePath = ImageHelper.LoadFromResource("Assets/pigment-dlya-bombochek-malinov.800x600w.jpg"), Name = "Малиновий-крафт для бомб", BuyAmount = 3, Cost = 2.4m, Id = 6},
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/vrm-zheltii-barvnik.800x600w.jpg"), Name = "Жовтий", BuyAmount = 2, Cost = 1.5m , Id = 2},
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/vrm-korichnevii-barvnik.800x600w.jpg"), Name = "Коричневий", BuyAmount = 4, Cost = 1.7m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/pigment-perlamutr-sinii.800x600w.jpg"), Name = "Перламутровий синій", BuyAmount = 1, Cost = 3.0m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/pigment-biruza-sweden.800x600w.jpg"), Name = "Рідкий Бірюзовий", BuyAmount = 5, Cost = 2.2m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/pigm-perlam-vinno-chervonii.800x600w.jpg"), Name = "Перламутровий винно-червоний", BuyAmount = 2, Cost = 2.8m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/vrm-blakitnii-barvnik.800x600w.jpg"), Name = "Блакитний", BuyAmount = 3, Cost = 1.9m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/barvnik-red-neri.800x600w.jpg"), Name = "Neri color Red", BuyAmount = 6, Cost = 2.6m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/neri-barv-zelenii-01-1.800x600w.jpg"), Name = "Neri color Зелений", BuyAmount = 2, Cost = 2.4m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/neri-barv-bila-001.800x600w.jpg"), Name = "Neri color Білий", BuyAmount = 4, Cost = 2.0m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/neri-barv-pomaranch-01-1.800x600w.jpg"), Name = "Neri color Помаранчевий", BuyAmount = 3, Cost = 2.3m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/barvnik-rose-neri.800x600w.jpg"), Name = "Neri color Rose (Рожевий)", BuyAmount = 1, Cost = 2.7m },
        new() { ImagePath = ImageHelper.LoadFromResource("Assets/neri-barv-sinii-01-1.800x600w.jpg"), Name = "Neri color Синій", BuyAmount = 3, Cost = 2.5m },
        }
        .OrderBy(x => IsLatin(x.Name))
        .ThenBy(x => x.Name);

    private IEnumerable<ComponentModel> FillCraftingBases() =>
        new List<ComponentModel>
        {
            new() {IsSelected = false, IsButton = true},

            new() {ImagePath = ImageHelper.LoadFromResource("Assets/crystal-bila_4.800x600w.jpg"), Name = "Crystal Triple Butter (масло Ши, Какао і Манго)", BuyAmount = 0, Cost = 70},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/mylnaya-osnova-stephenson-crystal-st_1.800x600w.jpg"), Name = "Crystal SLS Free прозора", BuyAmount = 0, Cost = 130},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/crystal-bila.800x600w.jpg"), Name = "Crystal Donkey Milk Біла", BuyAmount = 0, Cost = 100},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/milna-osnova-nco-organica.800x600w.jpg"), Name = "Crystal NCO (ORG) органічна", BuyAmount = 0, Cost = 600},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/neri-milna-osnova-aloe.800x600w.jpg"), Name = "Neri Aloe з екстрактом алое прозора", BuyAmount = 0, Cost = 30},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/neri-olivka-osnova-new-01.800x600w.jpg"), Name = "Neri Olive з оливковою олією напівпрозора", BuyAmount = 0, Cost = 100},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/svirli-osnova-milna.800x600w.jpg"), Name = "Основа для свірлов Neri Swirl", BuyAmount = 0, Cost = 150},
        }.OrderBy(x => IsLatin(x.Name))
            .ThenBy(x => x.Name);

    private IEnumerable<ComponentModel> FillSoapForms() =>
        new List<ComponentModel>
        {
            new() {IsSelected = false, IsButton = true},
            new() { ImagePath = ImageHelper.LoadFromResource("Assets/silikon-kvitka-ajstra-pishna.800x600w.jpg"), Name = "Айстра пишна розкрита 70г" , BuyAmount = 70, Cost = 0, IsSelected = false},
            new() { ImagePath = ImageHelper.LoadFromResource("Assets/silikon-serdechko-azhurne.800x600w.jpg"), Name = "Сердечко ажурне велике 130г", BuyAmount = 130, Cost = 0 },
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/apelsin-srednii-plastik-01.800x600w.jpg"), Name = "Апельсин середній 60г", BuyAmount = 60, Cost = 0, Id = 3},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/polyana-forma-01.500x500.jpg"), Name = "Зелена галявина в квітах 36г", BuyAmount = 36, Cost = 0, IsSelected = false, Id = 5},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/angel-v-rozah-elit-forma.800x600w.jpg"), Name = "Янгол в трояндах 90г", BuyAmount = 90, Cost = 0},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/205-zefir.800x600w.jpg"), Name = "Зефір 44г", BuyAmount = 44, Cost = 0},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/312-oduvanchik.800x600w.jpg"), Name = "Кульбаба 110г", BuyAmount = 110, Cost = 0},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/silikon-spiral.800x600w.jpg"), Name = "Спіраль 80г", BuyAmount = 80, Cost = 0},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/kofejnyj-krug-elit-forma.800x600w.jpg"), Name = "Кавове коло 87г", BuyAmount = 87, Cost = 0},
            new() {ImagePath = ImageHelper.LoadFromResource("Assets/silikon-polusferi-seredni-.800x600w.jpg"), Name = "Силіконові форми Півсфери 73г", BuyAmount = 73, Cost = 0}
        }.OrderBy(x => IsLatin(x.Name))
            .ThenBy(x => x.Name);

}