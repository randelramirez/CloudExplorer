namespace CloudExplorer.Models;

public sealed record ResourceListRow
{
    private ResourceListRow()
    {
    }

    public bool IsGroupHeader { get; private init; }

    public string GroupTitle { get; private init; } = "";

    public string GroupCountDisplay { get; private init; } = "";

    public CloudResource? Resource { get; private init; }

    public static ResourceListRow Header(string title, int count) => new()
    {
        IsGroupHeader = true,
        GroupTitle = title,
        GroupCountDisplay = count == 1 ? "1 resource" : $"{count:N0} resources",
    };

    public static ResourceListRow Item(CloudResource resource) => new()
    {
        Resource = resource,
    };
}
