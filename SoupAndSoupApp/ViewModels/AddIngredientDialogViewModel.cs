using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using ReactiveUI;
using SoupAndSoupApp.Models;

namespace SoupAndSoupApp.ViewModels
{
    public class AddIngredientDialogViewModel : ViewModelBase
    {
        public const string NoImage_Component_Image = "Assets/No_Component_Photo.png";

        public string? PhotoPath { get; set; }

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

        public string AmountTypeTitle
        {
            get => _amountTypeTitle;
            set => this.RaiseAndSetIfChanged(ref _amountTypeTitle, value);
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

        public ReactiveCommand<Unit, Unit> SelectPhotoCommand => _selectPhotoCommand;

        public ReactiveCommand<Unit, NewIngredientDto?> ConfirmCommand => _confirmCommand;

        public ReactiveCommand<Unit, NewIngredientDto?> CancelCommand => _cancelCommand;

        public NewIngredientDto? NewIngredient { get; private set; }

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
                    ImagePath = PhotoPath ?? string.Empty
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

        public AddIngredientDialogViewModel() { }

        public void Init(SoapTypeComponent ingredientType)
        {
            switch (ingredientType)
            {
                //TODO: Move to separate service 
                case SoapTypeComponent.Form:
                    WindowTitle = "Додати форму";
                    IngredientTitle = "форму";
                    AmountTypeTitle = "шт";
                    break;
                case SoapTypeComponent.CraftingBase:
                    WindowTitle = "Додати основу";
                    IngredientTitle = "основи";
                    AmountTypeTitle = "г";
                    break;
                case SoapTypeComponent.Pigment:
                    break;
                case SoapTypeComponent.EssentialOil:
                    break;
                case SoapTypeComponent.FragranceOil:
                    break;
                case SoapTypeComponent.HerbalExtract:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ingredientType), ingredientType, null);
            }

            Price= 0;
            Amount = 0;
            PhotoPath = null;
            Name = string.Empty;
            NewIngredient = null;
            Photo = ImageHelper.LoadFromResource(NoImage_Component_Image);
        }

        public void Init(IngredientModel ingredient)
        {
            Price = 0;
            Amount = 0;
            Name = ingredient.Name;
            UnitPrice = ingredient.Cost;
            NewIngredient = null;
            Photo = ingredient.ImagePath;
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


        private readonly Window? _dialog;
        private decimal _amount;
        private decimal _price;
        private decimal _unitPrice;
        private string _name = "";
        private Bitmap? _photo = null;
        private readonly ReactiveCommand<Unit, Unit> _selectPhotoCommand;
        private readonly ReactiveCommand<Unit, NewIngredientDto?> _confirmCommand;
        private readonly ReactiveCommand<Unit, NewIngredientDto?> _cancelCommand;
        private string _amountTypeTitle;
        private string _ingredientTitle;
        private string _windowTitle;
    }

}