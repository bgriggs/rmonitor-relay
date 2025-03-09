using Avalonia.Controls;
using Avalonia.Interactivity;
using RedMist.Relay.ViewModels;

namespace RedMist.Relay.Views;

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
            viewModel.Initialize();
        }
    }
}
