using Microsoft.UI.Xaml.Media;

namespace CloudExplorer.Views;

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

    private async void AccountSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        if (sender is ComboBox { SelectedItem: CloudAccountOption account } &&
            DataContext is ProviderExplorerViewModel viewModel)
        {
            await viewModel.ChangeAccountAsync(account);
        }
    }
}
