using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CloudExplorer.Features.ResourceExplorer;

public sealed class ResourceRowTemplateSelector : DataTemplateSelector
{
    public DataTemplate? GroupHeaderTemplate { get; set; }

    public DataTemplate? ResourceTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item) =>
        item is ResourceListRow { IsGroupHeader: true } ? GroupHeaderTemplate : ResourceTemplate;

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container) =>
        SelectTemplateCore(item);
}
