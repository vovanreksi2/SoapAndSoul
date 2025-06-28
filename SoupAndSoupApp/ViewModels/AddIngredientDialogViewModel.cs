using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using DynamicData;
using ReactiveUI;
using SoupAndSoupApp.Helpers;
using SoupAndSoupApp.Models;
using ComponentType = SoupAndSoupApp.Models.ComponentType;
using MeasureType = SoupAndSoupApp.Models.MeasureType;

namespace SoupAndSoupApp.ViewModels
{
    public class AddIngredientDialogViewModel : ViewModelBase
    {
        public const string NoImage_Component_Image = "Assets/No_Component_Photo.png";

        public Bitmap? Photo
        {
            get => _photo;
            set => this.RaiseAndSetIfChanged(ref _photo, value);
        }

        public string NewImagePath
        {
            get => _newImagePath;
            set
            {
                if (_newImagePath == value) return;

                Photo = ImageHelper.LoadFromResource(value);

                this.RaiseAndSetIfChanged(ref _newImagePath, value);
            }
        }

        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        public decimal BuyAmount
        {
            get => _buyAmount;
            set
            {
                ReCalculateUnitPrice(_currentComponentType, SelectedUseMeasureType?.Selected?.MeasureType ?? null, BuyPrice, value);
                this.RaiseAndSetIfChanged(ref _buyAmount, value);
            }
        }

        public decimal BuyPrice
        {
            get => _buyPrice;
            set
            {
                ReCalculateUnitPrice(_currentComponentType, SelectedUseMeasureType?.Selected?.MeasureType ?? null, value, BuyAmount);
                this.RaiseAndSetIfChanged(ref _buyPrice, value);
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set => this.RaiseAndSetIfChanged(ref _unitPrice, value);
        }

        public decimal SuggestedAmount
        {
            get => _suggestedAmount;
            set => this.RaiseAndSetIfChanged(ref _suggestedAmount, value);
        }
         
        public string Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }

        public bool IsAmountVisible
        {
            get => _isAmountVisible;
            set => this.RaiseAndSetIfChanged(ref _isAmountVisible, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => this.RaiseAndSetIfChanged(ref _isEditMode, value);
        }

        public MeasureTypeVM SelectedUseMeasureType
        {
            get => _selectedUseMeasureType;
            set => this.RaiseAndSetIfChanged(ref _selectedUseMeasureType, value);
        }

        public MeasureTypeVM SelectedBuyMeasureType
        {
            get => _selectedBuyMeasureType;
            set => this.RaiseAndSetIfChanged(ref _selectedBuyMeasureType, value);
        }

        public ReactiveCommand<Unit, NewComponentDto?> ConfirmCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        public Interaction<NewComponentDto?, NewComponentDto?> ConfirmInteraction { get; } = new();
        public Interaction<Unit, Unit> CancelInteraction { get; } = new();

        
        public AddIngredientDialogViewModel() { }

        public AddIngredientDialogViewModel(MeasureTypeCache measureTypeCache, IUnitCostCalculator costCalculator)
        {
            _measureTypeCache = measureTypeCache;
            _costCalculator = costCalculator;
            
            SelectedBuyMeasureType = new MeasureTypeVM();
            SelectedUseMeasureType = new MeasureTypeVM();
            SelectedUseMeasureType.WhenAnyValue(x => x.Selected)
                .Subscribe(_ =>
                {
                    if (SelectedUseMeasureType.Selected == null) return;

                    SelectedBuyMeasureType.Selected = SelectedUseMeasureType.Selected.MeasureType == MeasureType.Drop
                        ? SelectedBuyMeasureType.MeasureTypes.FirstOrDefault(measureType => measureType.MeasureType == MeasureType.Milliliter)
                        : SelectedBuyMeasureType.MeasureTypes.FirstOrDefault(measureType => measureType.MeasureType == SelectedUseMeasureType.Selected.MeasureType);

                    ReCalculateUnitPrice(_currentComponentType, SelectedUseMeasureType.Selected.MeasureType, BuyPrice, BuyAmount);
                })
                .DisposeWith(Disposables);

            ConfirmCommand = ReactiveCommand.CreateFromTask(ConfirmAsync, this.WhenAnyValue(
                x => x.Name,
                x => x.BuyPrice,
                x => x.BuyAmount,
                x => x.SelectedUseMeasureType.Selected,
                x => x.SelectedBuyMeasureType.Selected,
                x => x.SuggestedAmount,
                (name, price, amount, useMeasure, buyMeasure, typical) =>
                    !string.IsNullOrWhiteSpace(name) &&
                    price > 0 &&
                    amount > 0 &&
                    useMeasure != null &&
                    buyMeasure != null &&
                    typical > 0));

            CancelCommand = ReactiveCommand.CreateFromTask(CancelAsync);

            Reset();
        }

        public async Task InitAsync(bool isEditMode, ComponentTypeModel componentType, ComponentModel? component)
        {
            await InitMeasureTypes(SelectedUseMeasureType, componentType.UseMeasureTypesId);
            await InitMeasureTypes(SelectedBuyMeasureType, componentType.BuyMeasureTypesId);
             
            _currentComponentType = componentType.Type;
            IsEditMode = isEditMode;

            Title = isEditMode ? $"Редагувати {componentType.ShortTitle}" : $"Додати {componentType.ShortTitle}";
            IsAmountVisible = !componentType.IsSingleSelected;

            if (component != null)
            {
                BuyPrice = component.BuyPrice;
                BuyAmount = component.Type == ComponentType.Form ? BuyAmount : component.BuyAmount;
                Name = component.Name;
                UnitPrice = component.Cost;
                Photo = component.ImagePath;
                SuggestedAmount = component.SuggestedAmount;
                
                SelectedUseMeasureType.Selected = SelectedUseMeasureType.MeasureTypes.FirstOrDefault(m => m.Id == component.UseMeasureTypeId);
                SelectedBuyMeasureType.Selected = SelectedBuyMeasureType.MeasureTypes.FirstOrDefault(m => m.Id == component.BuyMeasureTypeId);
            }
            else
            {
                Photo = ImageHelper.LoadFromResource(NoImage_Component_Image);
                BuyAmount = componentType.BuyAmount > 0 ? componentType.BuyAmount : 1;
            }
        }

        public void Reset()
        {
            Photo = null;
            Name = string.Empty;
            BuyPrice = 0;
            BuyAmount = 0;
            UnitPrice = 0;
            SuggestedAmount = 0;
            IsAmountVisible = true;

            SelectedUseMeasureType.MeasureTypes.Clear();
            SelectedUseMeasureType.Selected = null;

            SelectedBuyMeasureType.MeasureTypes.Clear();
            SelectedBuyMeasureType.Selected = null;
        }

        private void ReCalculateUnitPrice(ComponentType componentType, MeasureType? measureType, decimal buyPrice, decimal buyAmount)
        {
            UnitPrice = _costCalculator.CalculateComponentCostForOneMeasure(componentType, measureType, buyPrice, buyAmount);
        }

        private async Task InitMeasureTypes(MeasureTypeVM measureTypeVm, IEnumerable<int> ids)
        {
            var selectedUseMeasureTypes = ids.Select(_measureTypeCache.GetOrAddAsync);

            measureTypeVm.MeasureTypes.AddRange(await Task.WhenAll(selectedUseMeasureTypes));
            measureTypeVm.HasMultipleMeasureTypes = measureTypeVm.MeasureTypes.Count > 1;
            measureTypeVm.Selected = measureTypeVm.MeasureTypes.FirstOrDefault() ?? throw new ArgumentException("Measure type must be selected.");
        }

        private async Task CancelAsync()
        {
            await CancelInteraction.Handle(Unit.Default); 
        }

        private async Task<NewComponentDto?> ConfirmAsync()
        {
            var newIngredient = new NewComponentDto
            {
                Name = Name,
                Cost = UnitPrice,
                ImagePath = NewImagePath,
                BuyAmount = BuyAmount,
                BuyPrice = BuyPrice,
                SuggestedAmount = SuggestedAmount, 

                BuyMeasureType = SelectedBuyMeasureType.Selected ?? throw new ArgumentException("Buy measure type must be selected."),
                UseMeasureType = SelectedUseMeasureType.Selected ?? throw new ArgumentException("Use measure type must be selected.")
            };

            await ConfirmInteraction.Handle(newIngredient);

            return (NewComponentDto?)null;
        }

        private decimal _buyAmount;
        private decimal _buyPrice;
        private decimal _unitPrice;
        private string _name = "";
        private Bitmap? _photo = null;
        private string _title;
        private decimal _suggestedAmount;
        private bool _isAmountVisible;
        private string _newImagePath;
        private bool _isEditMode;
        private readonly MeasureTypeCache _measureTypeCache;
        private readonly IUnitCostCalculator _costCalculator;

        private MeasureTypeVM _selectedUseMeasureType;
        private MeasureTypeVM _selectedBuyMeasureType;
        private ComponentType _currentComponentType;
    }
}