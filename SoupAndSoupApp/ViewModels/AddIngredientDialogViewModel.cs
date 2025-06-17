using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using DynamicData;
using ReactiveUI;
using SoupAndSoupApp.Helpers;
using SoupAndSoupApp.Models;

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
                ReCalculateUnitPrice(value, BuyPrice);
                this.RaiseAndSetIfChanged(ref _buyAmount, value);
            }
        }


        public decimal BuyPrice
        {
            get => _buyPrice;
            set
            {
                ReCalculateUnitPrice(BuyAmount, value);
                this.RaiseAndSetIfChanged(ref _buyPrice, value);
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set => this.RaiseAndSetIfChanged(ref _unitPrice, value);
        }

        public decimal TypicalAmountInRecipe
        {
            get => _typicalAmountInRecipe;
            set => this.RaiseAndSetIfChanged(ref _typicalAmountInRecipe, value);
        }

        public string TypicalAmountMeasure
        {
            get => _typicalAmountMeasure;
            set => this.RaiseAndSetIfChanged(ref _typicalAmountMeasure, value);
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

        #region MeasureTypeModel

        public ObservableCollection<MeasureTypeModel> MeasureTypes { get; } = new();


        private MeasureTypeModel? _selectedMeasureType;
        public MeasureTypeModel? SelectedMeasureType
        {
            get => _selectedMeasureType;
            set
            {
                if (_selectedMeasureType == value || value is null) return;

                TypicalAmountMeasure = (MeasureType)value.Id switch
                {
                    MeasureType.Gram => value.ShortTitle,
                    MeasureType.Milliliter => "крап",
                    MeasureType.Piece => value.ShortTitle,
                    _ => throw new ArgumentOutOfRangeException()
                };
                this.RaiseAndSetIfChanged(ref _selectedMeasureType, value);
            }
        }


        private bool _hasMultipleMeasureTypes;
        public bool HasMultipleMeasureTypes
        {
            get => _hasMultipleMeasureTypes;
            private set => this.RaiseAndSetIfChanged(ref _hasMultipleMeasureTypes, value);
        }

        #endregion

        public ReactiveCommand<Unit, NewIngredientDto?> ConfirmCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        public Interaction<NewIngredientDto?, NewIngredientDto?> ConfirmInteraction { get; } = new();
        public Interaction<Unit, Unit> CancelInteraction { get; } = new();


        public bool CanConfirm => !string.IsNullOrWhiteSpace(Name) && BuyPrice > 0 && BuyAmount > 0 && SelectedMeasureType!= null && TypicalAmountInRecipe > 0;


        public AddIngredientDialogViewModel()
        {
            var canConfirm = this.WhenAnyValue(
                x => x.Name,
                x => x.BuyPrice,
                x => x.BuyAmount,
                x => x.SelectedMeasureType,
                x => x.TypicalAmountInRecipe,
                (name, price, amount, measure, typical) =>
                    !string.IsNullOrWhiteSpace(name) &&
                    price > 0 &&
                    amount > 0 &&
                    measure != null &&
                    typical > 0
            );

            ConfirmCommand = ReactiveCommand.CreateFromTask(ConfirmAsync, canConfirm);
            CancelCommand = ReactiveCommand.CreateFromTask(CancelAsync);
        }

        public void Init(IngredientModel ingredient)
        {
            Init(ingredient.Type, new List<MeasureTypeModel> { ingredient.MeasureType });

            BuyPrice = ingredient.BuyPrice;
            BuyAmount = ingredient.Type == SoapTypeComponent.Form ? BuyAmount : ingredient.Amount;
            Name = ingredient.Name;
            UnitPrice = ingredient.Cost;
            Photo = ingredient.ImagePath;
            TypicalAmountInRecipe = ingredient.DefaultAmount;

            IsEditMode = true;
        }


        public void Init(SoapTypeComponent ingredientType, IEnumerable<MeasureTypeModel> measureTypes)
        {
            MeasureTypes.Clear();
            MeasureTypes.AddRange(measureTypes.Select(_ => new MeasureTypeModel(_.Id, _.Title, _.ShortTitle, _.DisplayTitle, GetBitmapByMeasureType(_.Id))));

            //MeasureTypes.AddRange(new[]
            //{
            //    new MeasureTypeModel(1, "Грами", "гр",GetBitmapByMeasureType(1)),
            //    new MeasureTypeModel(2, "Мілілітри", "мл", GetBitmapByMeasureType(2)),
            //    new MeasureTypeModel(3, "Штуки", "шт", GetBitmapByMeasureType(3)),
            //    new MeasureTypeModel(4, "Краплі", "крап", GetBitmapByMeasureType(4)),
            //});

            SelectedMeasureType = MeasureTypes.Count > 0 ? MeasureTypes.FirstOrDefault() : null;
            HasMultipleMeasureTypes = MeasureTypes.Count > 1;

            IsAmountVisible = true;
            Title = IsEditMode ? "Редагувати " : "Додати ";
            switch (ingredientType)
            {
                case SoapTypeComponent.Form:
                    Title += "форму";
                    IsAmountVisible = false;
                    BuyAmount = 1;
                    break;
                case SoapTypeComponent.CraftingBase:
                    Title += "основу";
                    BuyAmount = 200;
                    break;
                case SoapTypeComponent.Pigment:
                    Title += "пігмент";
                    BuyAmount = 10;
                    break;
                case SoapTypeComponent.EssentialOil:
                    Title += "запашку";
                    BuyAmount = 10;
                    break;
                case SoapTypeComponent.FragranceOil:
                    Title += "ефірне масло";
                    BuyAmount = 10;
                    break;
                case SoapTypeComponent.HerbalExtract:
                    Title += "екстракт";
                    BuyAmount = 10;
                    break;
                case SoapTypeComponent.Tools:
                    Title += "інструмент";
                    IsAmountVisible = false;
                    BuyAmount = 1;
                    break;
                case SoapTypeComponent.Other:  
                    Title += "інший компонент";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ingredientType), ingredientType, null);
            }

            Photo = ImageHelper.LoadFromResource(NoImage_Component_Image);
        }


        public void Reset()
        {
            Photo = null;
            Name = string.Empty;
            BuyPrice = 0;
            BuyAmount = 0;
            UnitPrice = 0;
            TypicalAmountInRecipe = 0;
            SelectedMeasureType = null;
            IsAmountVisible = true;

            MeasureTypes.Clear();
            HasMultipleMeasureTypes = false;
        }

        private void ReCalculateUnitPrice(decimal amount, decimal price)
        {
            UnitPrice = amount > 0
                ? price / amount
                : 0;
        }


        private async Task CancelAsync()
        {
            await CancelInteraction.Handle(Unit.Default); 
        }

        private async Task<NewIngredientDto?> ConfirmAsync()
        {
            var newIngredient = new NewIngredientDto
            {
                Name = Name,
                Cost = UnitPrice,
                ImagePath = NewImagePath,
                MeasureType = SelectedMeasureType,
                BuyAmount = BuyAmount,
                BuyPrice = BuyPrice,
                TypicalAmountInRecipe = BuyAmount
            };

            await ConfirmInteraction.Handle(newIngredient);

            return (NewIngredientDto?)null;
        }

        private Bitmap GetBitmapByMeasureType(int id)
        {
            return (MeasureType)id switch
            {
                MeasureType.Gram => ImageHelper.LoadFromResource("Assets/Gram.png"),
                MeasureType.Milliliter => ImageHelper.LoadFromResource("Assets/Milliliter.png"),
                MeasureType.Piece => ImageHelper.LoadFromResource("Assets/Gram.png"),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }


        private decimal _buyAmount;
        private decimal _buyPrice;
        private decimal _unitPrice;
        private string _name = "";
        private Bitmap? _photo = null;
        private string _title;
        private decimal _typicalAmountInRecipe;
        private bool _isAmountVisible;
        private string _newImagePath;
        private string _typicalAmountMeasure;
        private bool _isEditMode;
    }
}