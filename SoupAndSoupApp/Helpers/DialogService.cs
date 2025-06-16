using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using SoupAndSoupApp.Models;
using SoupAndSoupApp.ViewModels;
using SoupAndSoupApp.Views;

namespace SoupAndSoupApp.Helpers;

public class DialogService : IDialogService
{
    private readonly MainWindow _mainWindow;
    private readonly AddIngredientDialog _dialog;
    private readonly AddIngredientDialogViewModel _viewModel;
    private TaskCompletionSource<NewIngredientDto?> _tcs;

    public DialogService(MainWindow mainWindow, AddIngredientDialog dialog, AddIngredientDialogViewModel viewModel)
    {
        _mainWindow = mainWindow;

        _viewModel = viewModel;
        _dialog = dialog;
        _dialog.DataContext = _viewModel;

        _mainWindow.Closed += (_, __) =>
        {
            _dialog.Close();
        };

        _viewModel.ConfirmInteraction.RegisterHandler(ctx =>
        {
            _dialog.Hide();
            _viewModel.Reset();

            ctx.SetOutput(ctx.Input);

            _tcs?.TrySetResult(ctx.Input);
        });

        _viewModel.CancelInteraction.RegisterHandler(ctx =>
        {
            _dialog.Hide();
            _viewModel.Reset();
            ctx.SetOutput(Unit.Default);
           
            _tcs?.TrySetResult(null);
        });
    }

    public async Task<NewIngredientDto?> ShowAddIngredientDialogAsync(SoapTypeComponent type, IEnumerable<MeasureTypeModel> measureTypes)
    {
        _tcs = new TaskCompletionSource<NewIngredientDto?>();

        _viewModel.Init(type, measureTypes);
        await _dialog.ShowDialog(_mainWindow);

        return await _tcs.Task;
    }

    public Task<NewIngredientDto?> ShowEditIngredientDialogAsync(IngredientModel ingredientModel)
    {
        _viewModel.Init(ingredientModel);
        return _dialog.ShowDialog<NewIngredientDto?>(_mainWindow);
    }
} 