using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CloudExplorer.Features.ResourceExplorer;

public sealed partial record CloudResource
{
    public required CloudProvider Provider { get; init; }

    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string ResourceType { get; init; }

    public string GroupName { get; init; } = "—";

    public string Location { get; init; } = "Global";

    public string AccountId { get; init; } = "";

    public IReadOnlyDictionary<string, string> Tags { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public DateTimeOffset? CreatedAt { get; init; }

    public string? CreatedAtSource { get; init; }

    public DateTimeOffset? LastModifiedAt { get; init; }

    public string? LastModifiedAtSource { get; init; }

    public DateTimeOffset? LastObservedAt { get; init; }

    public string CreatedDisplay => FormatDate(CreatedAt);

    public string LastModifiedDisplay => FormatDate(LastModifiedAt);

    public string TagsDisplay => Tags.Count == 0
        ? "No tags"
        : string.Join("  •  ", Tags.OrderBy(static tag => tag.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static tag => $"{tag.Key}={tag.Value}"));

    internal string SearchableTextPrefix => string.Join(
        ' ',
        Name,
        ResourceType,
        GroupName,
        Location,
        AccountId);

    public string SearchableText => string.Concat(SearchableTextPrefix, " ", TagsDisplay);

    private static string FormatDate(DateTimeOffset? value) => value is null
        ? "Not exposed"
        : value.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
}
