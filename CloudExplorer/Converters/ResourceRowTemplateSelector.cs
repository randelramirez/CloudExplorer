namespace CloudExplorer.Converters;

public sealed class ResourceRowTemplateSelector : DataTemplateSelector
{
    public DataTemplate? GroupHeaderTemplate { get; set; }

    public DataTemplate? ResourceTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item) =>
        item is ResourceListRow { IsGroupHeader: true } ? GroupHeaderTemplate : ResourceTemplate;

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container) =>
        SelectTemplateCore(item);
}
