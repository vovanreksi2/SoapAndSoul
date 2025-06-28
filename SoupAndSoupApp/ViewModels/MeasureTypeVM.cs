using System.Collections.ObjectModel;
using ReactiveUI;
using SoupAndSoupApp.Models;

public class MeasureTypeVM: ReactiveObject
{
    public ObservableCollection<MeasureTypeModel> MeasureTypes
    {
        get => _measureTypes;
        set => this.RaiseAndSetIfChanged(ref _measureTypes, value);
    }

    public MeasureTypeModel? Selected
    {
        get => _selected;
        set
        {
            if (_selected == value || value is null) return;
            this.RaiseAndSetIfChanged(ref _selected, value);
        }
    }

    public bool HasMultipleMeasureTypes
    {
        get => _hasMultipleMeasureTypes;
        set => this.RaiseAndSetIfChanged(ref _hasMultipleMeasureTypes, value);
    }

    private ObservableCollection<MeasureTypeModel> _measureTypes = new();
    private bool _hasMultipleMeasureTypes;
    private MeasureTypeModel? _selected;
}