using System.Reactive;
using System.Threading.Tasks;
using SoupAndSoupApp.Models;
using SoupAndSoupApp.ViewModels;
using SoupAndSoupApp.Views;

namespace SoupAndSoupApp.Helpers.UI;

public class DialogService : IDialogService
{
    private readonly MainWindow _mainWindow;
    private readonly AddIngredientDialog _dialog;
    private readonly AddIngredientDialogViewModel _dialogViewModel;
    private TaskCompletionSource<NewComponentDto?> _tcs;

    public DialogService(MainWindow mainWindow, AddIngredientDialog dialog, AddIngredientDialogViewModel dialogViewModel)
    {
        _mainWindow = mainWindow;

        _dialogViewModel = dialogViewModel;
        _dialog = dialog;
        _dialog.DataContext = _dialogViewModel;

        _mainWindow.Closed += (_, __) =>
        {
            _dialog.Close();
        };

        _dialogViewModel.ConfirmInteraction.RegisterHandler(ctx =>
        {
            _dialog.Hide();
            _dialogViewModel.Reset();

            ctx.SetOutput(ctx.Input);

            _tcs?.TrySetResult(ctx.Input);
        });

        _dialogViewModel.CancelInteraction.RegisterHandler(ctx =>
        {
            _dialog.Hide();
            _dialogViewModel.Reset();
            ctx.SetOutput(Unit.Default);
           
            _tcs?.TrySetResult(null);
        });
    }

    public async Task<NewComponentDto> ShowAddEditComponentDialogAsync(bool isEditMode, ComponentTypeModel componentType, ComponentModel? component = null)
    {
        _tcs = new TaskCompletionSource<NewComponentDto?>();

        await _dialogViewModel.InitAsync(isEditMode, componentType, component);
        await _dialog.ShowDialog(_mainWindow);

        return await _tcs.Task;
    }
} 