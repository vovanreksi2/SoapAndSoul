using Avalonia.Controls;
using SoupAndSoupApp.ViewModels;

namespace SoupAndSoupApp.Views;

public partial class SoapDesignerView : UserControl
{
    public SoapDesignerView(SoapDesignerViewModel viewModel)
    {

        InitializeComponent();
        DataContext = viewModel;
    }
    
    public SoapDesignerView()
    {
        InitializeComponent();
    }
   
}