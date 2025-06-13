using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using DynamicData;
using ReactiveUI;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.ViewModels
{
    public class AddIngredientDialogViewModel : ViewModelBase
    {
        public string? PhotoPath
        {
            get => _photoPath;
            set => _photoPath = value;
        }

        public Bitmap? Photo
        {
            get => _photo;
            set => this.RaiseAndSetIfChanged(ref _photo, value);
        }

        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        public decimal Amount
        {
            get => _amount;
            set
            {
                ReCalculateUnitPrice(value, Price);
                this.RaiseAndSetIfChanged(ref _amount, value);
            }
        }


        public decimal Price
        {
            get => _price;
            set
            {
                ReCalculateUnitPrice(Amount, value);
                this.RaiseAndSetIfChanged(ref _price, value);
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set => this.RaiseAndSetIfChanged(ref _unitPrice, value);
        }

        public decimal DefaultAmount
        {
            get => _defaultAmount;
            set => this.RaiseAndSetIfChanged(ref _defaultAmount, value);
        }

        public string IngredientTitle
        {
            get => _ingredientTitle;
            set => this.RaiseAndSetIfChanged(ref _ingredientTitle, value);
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set => this.RaiseAndSetIfChanged(ref _windowTitle, value);
        }

        public bool IsAmountVisible
        {
            get => _isAmountVisible;
            set => this.RaiseAndSetIfChanged(ref _isAmountVisible, value);
        }

        #region MeasureTypeModel

        public ObservableCollection<MeasureTypeModel> MeasureTypes => _measureTypes;


        private MeasureTypeModel? _selectedMeasureType;
        public MeasureTypeModel? SelectedMeasureType
        {
            get => _selectedMeasureType;
            set => this.RaiseAndSetIfChanged(ref _selectedMeasureType, value);
        }


        private bool _hasMultipleMeasureTypes;
        public bool HasMultipleMeasureTypes
        {
            get => _hasMultipleMeasureTypes;
            private set => this.RaiseAndSetIfChanged(ref _hasMultipleMeasureTypes, value);
        }

        #endregion

        public ReactiveCommand<Unit, Unit> SelectPhotoCommand => _selectPhotoCommand;

        public ReactiveCommand<Unit, NewIngredientDto?> ConfirmCommand => _confirmCommand;

        public ReactiveCommand<Unit, NewIngredientDto?> CancelCommand => _cancelCommand;

        public NewIngredientDto? NewIngredient
        {
            get => _newIngredient;
            private set => _newIngredient = value;
        }

        public bool CanConfirm => !string.IsNullOrWhiteSpace(Name) && Price > 0 && Amount > 0;


        public AddIngredientDialogViewModel(AddIngredientDialog dialog)
        {
            _dialog = dialog;

            _selectPhotoCommand = ReactiveCommand.CreateFromTask(SelectPhotoAsync);
            _confirmCommand = ReactiveCommand.Create(() =>
            {
                NewIngredient = new NewIngredientDto
                {
                    Name = Name,
                    Cost = UnitPrice,
                    ImagePath = PhotoPath ?? string.Empty,
                    MeasureType = SelectedMeasureType,
                    Amount = Amount,
                    Price = Price,
                    DefaultAmount = Amount
                };
                _dialog?.Hide();
                return (NewIngredientDto?)null;
            });

            _cancelCommand = ReactiveCommand.Create(() =>
            {
                NewIngredient = null;
                _dialog?.Hide();
                return (NewIngredientDto?)null;
            });

        }

        public AddIngredientDialogViewModel()
        {
        }

        public void Init(SoapTypeComponent ingredientType, IEnumerable<MeasureTypeModel> measureTypes)
        {
            switch (ingredientType)
            {
                //TODO: Move to separate service 
                case SoapTypeComponent.Form:
                    WindowTitle = "Додати форму";
                    IngredientTitle = "форму";
                    IsAmountVisible = false;
                    Amount = 1;
                    break;
                case SoapTypeComponent.CraftingBase:
                    WindowTitle = "Додати основу";
                    IngredientTitle = "основи";
                    break;
                case SoapTypeComponent.Pigment:
                    break;
                case SoapTypeComponent.EssentialOil:
                    break;
                case SoapTypeComponent.FragranceOil:
                    break;
                case SoapTypeComponent.HerbalExtract:
                    break;
                case SoapTypeComponent.Tools:
                case SoapTypeComponent.Other:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ingredientType), ingredientType, null);
            }

            Price = 0;
            Amount = 1;
            PhotoPath = null;
            Name = string.Empty;
            NewIngredient = null;
            Photo = null;

            MeasureTypes.Clear();
            MeasureTypes.AddRange(measureTypes.Select(_ => new MeasureTypeModel(_.Id, _.Title, _.ShortTitle, GetBitmapByMeasureType(_.Id))));

            //MeasureTypes.AddRange(new []
            //{
            //    new MeasureTypeModel(1, "Грами", "гр",GetBitmapByMeasureType(1)),
            //    new MeasureTypeModel(2, "Мілілітри", "мл", GetBitmapByMeasureType(2)),
            //    new MeasureTypeModel(3, "Штуки", "шт", GetBitmapByMeasureType(3)),
            //    new MeasureTypeModel(4, "Краплі", "крап", GetBitmapByMeasureType(4)),
            //});
            SelectedMeasureType = MeasureTypes.Count > 0 ? MeasureTypes.FirstOrDefault() : null;
            HasMultipleMeasureTypes = MeasureTypes.Count > 1;
        }

        public void Init(IngredientModel ingredient)
        {
            Price = ingredient.CostPrice;
            Amount = ingredient.Amount;
            Name = ingredient.Name;
            UnitPrice = ingredient.Cost;
            NewIngredient = null;
            Photo = ingredient.ImagePath;

            DefaultAmount = ingredient.DefaultAmount;
        }

        private void ReCalculateUnitPrice(decimal amount, decimal price)
        {
            UnitPrice = amount > 0
                ? price / amount
                : 0;
        }

        private async Task SelectPhotoAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "Зображення", Extensions = { "png", "jpg", "jpeg" } }
                }
            };
            var result = await dialog.ShowAsync(_dialog);
            if (result?.FirstOrDefault() is string path)
            {
                PhotoPath = path;
                var tmp = path.Remove(0, path.IndexOf("Assets"));
                Photo = ImageHelper.LoadFromResource(tmp);
            }
        }

        private Bitmap? GetBitmapByMeasureType(int id)
        {
            switch ((MeasureType)id)
            {
                case MeasureType.Gram:
                    return ImageHelper.LoadFromResource("Assets/Gram.png");
                case MeasureType.Milliliter:
                    return ImageHelper.LoadFromResource("Assets/Milliliter.png");

                case MeasureType.Piece:
                    return ImageHelper.LoadFromResource("Assets/Gram.png");

                case MeasureType.Drop:
                    return ImageHelper.LoadFromResource("Assets/Drop.png");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        private readonly Window? _dialog;
        private decimal _amount;
        private decimal _price;
        private decimal _unitPrice;
        private string _name = "";
        private Bitmap? _photo = null;
        private string? _photoPath;
        private readonly ReactiveCommand<Unit, Unit> _selectPhotoCommand;
        private readonly ReactiveCommand<Unit, NewIngredientDto?> _confirmCommand;
        private readonly ReactiveCommand<Unit, NewIngredientDto?> _cancelCommand;
        private string _ingredientTitle;
        private string _windowTitle;
        private decimal _defaultAmount;
        private bool _isAmountVisible;
        private readonly ObservableCollection<MeasureTypeModel> _measureTypes = new();
        private NewIngredientDto? _newIngredient;
    }

    public enum MeasureType
    {
        Gram =1,
        Milliliter,
        Piece,
        Drop
    }
}