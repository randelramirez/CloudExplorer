using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CloudExplorer.Features.Shell;

public sealed partial class MainPage : Page
{
    private bool _loaded;

    public MainPage()
    {
        InitializeComponent();
        DataContext = ((App)Application.Current).Services.GetRequiredService<MainViewModel>();
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    private async void PageLoaded(object sender, RoutedEventArgs args)
    {
        if (_loaded)
        {
            return;
        }

        ViewModel.IsDarkMode = ActualTheme == ElementTheme.Dark;
        SelectViewOption(ViewModel.SelectedProviderViewMode);
        ApplyProviderViewMode(ViewModel.SelectedProviderViewMode);
        _loaded = true;
        await ViewModel.InitializeVisibleProvidersAsync();
    }

    private void ThemeToggled(object sender, RoutedEventArgs args)
    {
        RequestedTheme = ViewModel.IsDarkMode ? ElementTheme.Dark : ElementTheme.Light;
    }

    private async void ProviderViewOptionChecked(object sender, RoutedEventArgs args)
    {
        if (!_loaded ||
            sender is not RadioButton { IsChecked: true, Tag: string modeName } ||
            !Enum.TryParse(modeName, ignoreCase: false, out ProviderViewMode mode))
        {
            return;
        }

        ViewModel.SelectedProviderViewMode = mode;
        ApplyProviderViewMode(mode);
        await ViewModel.InitializeVisibleProvidersAsync();
    }

    private void SelectViewOption(ProviderViewMode mode)
    {
        BothViewOption.IsChecked = mode == ProviderViewMode.Both;
        AwsViewOption.IsChecked = mode == ProviderViewMode.Aws;
        AzureViewOption.IsChecked = mode == ProviderViewMode.Azure;
    }

    private void ApplyProviderViewMode(ProviderViewMode mode)
    {
        var showBoth = mode == ProviderViewMode.Both;
        var showAws = mode is ProviderViewMode.Both or ProviderViewMode.Aws;
        var showAzure = mode is ProviderViewMode.Both or ProviderViewMode.Azure;

        AwsProviderPane.Visibility = showAws ? Visibility.Visible : Visibility.Collapsed;
        AzureProviderPane.Visibility = showAzure ? Visibility.Visible : Visibility.Collapsed;
        AwsProviderPane.IsCompact = showBoth;
        AzureProviderPane.IsCompact = showBoth;

        Grid.SetColumn(AwsProviderPane, 0);
        Grid.SetColumnSpan(AwsProviderPane, showBoth ? 1 : 2);
        Grid.SetColumn(AzureProviderPane, showBoth ? 1 : 0);
        Grid.SetColumnSpan(AzureProviderPane, showBoth ? 1 : 2);

        if (!showAws && !showAzure)
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown provider view mode.");
        }
    }
}
