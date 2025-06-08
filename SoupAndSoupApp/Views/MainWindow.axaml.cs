using System;
using Avalonia.Controls;
using SoupAndSoupApp.ViewModels;
using Avalonia.Data;

namespace SoupAndSoupApp.Views;

public partial class MainWindow : Window
{
    private bool _initialized;

    public MainWindow()
    {
        InitializeComponent();
    }
}
