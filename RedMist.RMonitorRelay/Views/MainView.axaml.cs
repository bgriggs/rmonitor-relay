using Avalonia.Controls;
using Avalonia.Interactivity;
using RedMist.RMonitorRelay.ViewModels;

namespace RedMist.RMonitorRelay.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is MainViewModel viewModel)
        {
            viewModel.LoadSettings();
        }
    }
}
