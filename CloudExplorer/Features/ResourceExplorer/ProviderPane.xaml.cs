using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace CloudExplorer.Features.ResourceExplorer;

public sealed partial class ProviderPane : UserControl
{
    public static readonly DependencyProperty ProviderNameProperty = DependencyProperty.Register(
        nameof(ProviderName), typeof(string), typeof(ProviderPane), new PropertyMetadata("Cloud"));

    public static readonly DependencyProperty ProviderInitialProperty = DependencyProperty.Register(
        nameof(ProviderInitial), typeof(string), typeof(ProviderPane), new PropertyMetadata("C"));

    public static readonly DependencyProperty AccentBrushProperty = DependencyProperty.Register(
        nameof(AccentBrush), typeof(Brush), typeof(ProviderPane), new PropertyMetadata(null));

    public static readonly DependencyProperty TintBrushProperty = DependencyProperty.Register(
        nameof(TintBrush), typeof(Brush), typeof(ProviderPane), new PropertyMetadata(null));

    public static readonly DependencyProperty AccentForegroundBrushProperty = DependencyProperty.Register(
        nameof(AccentForegroundBrush), typeof(Brush), typeof(ProviderPane), new PropertyMetadata(null));

    public static readonly DependencyProperty IsCompactProperty = DependencyProperty.Register(
        nameof(IsCompact),
        typeof(bool),
        typeof(ProviderPane),
        new PropertyMetadata(false, OnIsCompactChanged));

    public ProviderPane()
    {
        InitializeComponent();
    }

    public string ProviderName
    {
        get => (string)GetValue(ProviderNameProperty);
        set => SetValue(ProviderNameProperty, value);
    }

    public string ProviderInitial
    {
        get => (string)GetValue(ProviderInitialProperty);
        set => SetValue(ProviderInitialProperty, value);
    }

    public Brush AccentBrush
    {
        get => (Brush)GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    public Brush TintBrush
    {
        get => (Brush)GetValue(TintBrushProperty);
        set => SetValue(TintBrushProperty, value);
    }

    public Brush AccentForegroundBrush
    {
        get => (Brush)GetValue(AccentForegroundBrushProperty);
        set => SetValue(AccentForegroundBrushProperty, value);
    }

    public bool IsCompact
    {
        get => (bool)GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
    }

    private async void AccountSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        if (sender is ComboBox { SelectedItem: CloudAccountOption account } &&
            DataContext is ProviderExplorerViewModel viewModel)
        {
            await viewModel.ChangeAccountAsync(account);
        }
    }

    private static void OnIsCompactChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is ProviderPane pane)
        {
            pane.UpdateResponsiveLayout((bool)args.NewValue);
        }
    }

    private void UpdateResponsiveLayout(bool isCompact)
    {
        if (isCompact)
        {
            ControlsColumnOne.Width = new GridLength(1, GridUnitType.Star);
            ControlsColumnTwo.Width = new GridLength(1, GridUnitType.Star);
            ControlsColumnThree.Width = new GridLength(0);
            ControlsColumnFour.Width = new GridLength(0);

            Grid.SetRow(AccountSelector, 0);
            Grid.SetColumn(AccountSelector, 0);
            Grid.SetRow(GroupingSelector, 0);
            Grid.SetColumn(GroupingSelector, 1);
            Grid.SetRow(FilterBox, 1);
            Grid.SetColumn(FilterBox, 0);
            Grid.SetRow(RefreshButton, 1);
            Grid.SetColumn(RefreshButton, 1);
            return;
        }

        ControlsColumnOne.Width = new GridLength(1.4, GridUnitType.Star);
        ControlsColumnTwo.Width = new GridLength(1.2, GridUnitType.Star);
        ControlsColumnThree.Width = new GridLength(1.4, GridUnitType.Star);
        ControlsColumnFour.Width = GridLength.Auto;

        Grid.SetRow(AccountSelector, 0);
        Grid.SetColumn(AccountSelector, 0);
        Grid.SetRow(GroupingSelector, 0);
        Grid.SetColumn(GroupingSelector, 1);
        Grid.SetRow(FilterBox, 0);
        Grid.SetColumn(FilterBox, 2);
        Grid.SetRow(RefreshButton, 0);
        Grid.SetColumn(RefreshButton, 3);
    }
}
