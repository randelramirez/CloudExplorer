namespace CloudExplorer.Features.ResourceExplorer;

public sealed partial record CloudAccountOption(
    string Id,
    string DisplayName,
    string Detail = "",
    bool IsDefault = false);
