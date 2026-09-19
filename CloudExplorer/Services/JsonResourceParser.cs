using System.Text.Json;

namespace CloudExplorer.Services;

internal static class JsonResourceParser
{
    private static readonly HashSet<string> CreatedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "createdAt",
        "createdDate",
        "createdTime",
        "creationDate",
        "creationTime",
    };

    private static readonly HashSet<string> ModifiedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "changedTime",
        "lastModified",
        "lastModifiedAt",
        "lastModifiedDate",
        "lastModifiedTime",
        "modifiedAt",
        "modifiedDate",
        "modifiedTime",
        "updatedAt",
        "updatedDate",
        "updatedTime",
    };

    public static IReadOnlyList<CloudResource> ParseAwsResources(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!TryGetProperty(document.RootElement, "Resources", out var resourcesElement) ||
            resourcesElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var resources = new List<CloudResource>();
        foreach (var item in resourcesElement.EnumerateArray())
        {
            var id = GetString(item, "Arn");
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var properties = TryGetProperty(item, "Properties", out var propertiesElement)
                ? propertiesElement
                : default;
            var tags = ParseAwsTags(properties);
            var (createdAt, createdPath) = FindDate(properties, CreatedNames);
            var (modifiedAt, modifiedPath) = FindDate(properties, ModifiedNames);

            resources.Add(new CloudResource
            {
                Provider = CloudProvider.Aws,
                Id = id,
                Name = GetAwsResourceName(id),
                ResourceType = ValueOrFallback(GetString(item, "ResourceType"), GetString(item, "Service"), "Unknown type"),
                GroupName = ValueOrFallback(GetString(item, "Service"), "Other"),
                Location = ValueOrFallback(GetString(item, "Region"), "Global"),
                AccountId = GetString(item, "OwningAccountId"),
                Tags = tags,
                CreatedAt = createdAt,
                CreatedAtSource = createdPath is null ? null : $"AWS Resource Explorer: {createdPath}",
                LastModifiedAt = modifiedAt,
                LastModifiedAtSource = modifiedPath is null ? null : $"AWS Resource Explorer: {modifiedPath}",
                LastObservedAt = ParseDate(GetString(item, "LastReportedAt")),
            });
        }

        return resources;
    }

    public static IReadOnlyList<CloudResource> ParseAzureResources(string json, string subscriptionId)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var resources = new List<CloudResource>();
        foreach (var item in document.RootElement.EnumerateArray())
        {
            var id = GetString(item, "id");
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var createdAt = ParseDate(GetString(item, "createdTime"));
            var modifiedAt = ParseDate(GetString(item, "changedTime"));
            string? createdSource = createdAt is null ? null : "Azure Resource Manager: createdTime";
            string? modifiedSource = modifiedAt is null ? null : "Azure Resource Manager: changedTime";

            if (TryGetProperty(item, "properties", out var properties))
            {
                if (createdAt is null)
                {
                    var result = FindDate(properties, CreatedNames);
                    createdAt = result.Date;
                    createdSource = result.Path is null ? null : $"Azure resource property: {result.Path}";
                }

                if (modifiedAt is null)
                {
                    var result = FindDate(properties, ModifiedNames);
                    modifiedAt = result.Date;
                    modifiedSource = result.Path is null ? null : $"Azure resource property: {result.Path}";
                }
            }

            resources.Add(new CloudResource
            {
                Provider = CloudProvider.Azure,
                Id = id,
                Name = ValueOrFallback(GetString(item, "name"), GetAzureResourceName(id)),
                ResourceType = ValueOrFallback(GetString(item, "type"), "Unknown type"),
                GroupName = ValueOrFallback(GetString(item, "resourceGroup"), "No resource group"),
                Location = ValueOrFallback(GetString(item, "location"), "Global"),
                AccountId = subscriptionId,
                Tags = ParseObjectTags(item, "tags"),
                CreatedAt = createdAt,
                CreatedAtSource = createdSource,
                LastModifiedAt = modifiedAt,
                LastModifiedAtSource = modifiedSource,
            });
        }

        return resources;
    }

    public static IReadOnlyList<CloudAccountOption> ParseAzureSubscriptions(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return document.RootElement
            .EnumerateArray()
            .Where(static item => !string.Equals(GetString(item, "state"), "Disabled", StringComparison.OrdinalIgnoreCase))
            .Select(static item => new CloudAccountOption(
                GetString(item, "id"),
                ValueOrFallback(GetString(item, "name"), GetString(item, "id"), "Unnamed subscription"),
                GetString(item, "tenantId"),
                GetBoolean(item, "isDefault")))
            .Where(static subscription => !string.IsNullOrWhiteSpace(subscription.Id))
            .OrderByDescending(static subscription => subscription.IsDefault)
            .ThenBy(static subscription => subscription.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string ParseAwsIdentity(string json)
    {
        using var document = JsonDocument.Parse(json);
        var account = GetString(document.RootElement, "Account");
        var arn = GetString(document.RootElement, "Arn");
        return string.IsNullOrWhiteSpace(arn) ? account : $"{account} · {arn}";
    }

    public static string ParseAzureIdentity(string json)
    {
        using var document = JsonDocument.Parse(json);
        var name = GetString(document.RootElement, "name");
        var id = GetString(document.RootElement, "id");
        return string.IsNullOrWhiteSpace(name) ? id : $"{name} · {id}";
    }

    private static IReadOnlyDictionary<string, string> ParseAwsTags(JsonElement properties)
    {
        var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (properties.ValueKind != JsonValueKind.Array)
        {
            return tags;
        }

        foreach (var property in properties.EnumerateArray())
        {
            if (!string.Equals(GetString(property, "Name"), "tags", StringComparison.OrdinalIgnoreCase) ||
                !TryGetProperty(property, "Data", out var data))
            {
                continue;
            }

            if (data.ValueKind == JsonValueKind.Array)
            {
                foreach (var tag in data.EnumerateArray())
                {
                    AddTag(tags, GetString(tag, "Key"), GetString(tag, "Value"));
                }
            }
            else if (data.ValueKind == JsonValueKind.Object)
            {
                foreach (var tag in data.EnumerateObject())
                {
                    AddTag(tags, tag.Name, ElementToString(tag.Value));
                }
            }
        }

        return tags;
    }

    private static IReadOnlyDictionary<string, string> ParseObjectTags(JsonElement item, string propertyName)
    {
        var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!TryGetProperty(item, propertyName, out var tagElement) || tagElement.ValueKind != JsonValueKind.Object)
        {
            return tags;
        }

        foreach (var tag in tagElement.EnumerateObject())
        {
            AddTag(tags, tag.Name, ElementToString(tag.Value));
        }

        return tags;
    }

    private static void AddTag(IDictionary<string, string> tags, string key, string value)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            tags[key] = value;
        }
    }

    private static (DateTimeOffset? Date, string? Path) FindDate(JsonElement root, HashSet<string> candidateNames)
    {
        if (root.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return (null, null);
        }

        return FindDateCore(root, candidateNames, "", depth: 0);
    }

    private static (DateTimeOffset? Date, string? Path) FindDateCore(
        JsonElement element,
        HashSet<string> candidateNames,
        string path,
        int depth)
    {
        if (depth > 7)
        {
            return (null, null);
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            if (TryGetProperty(element, "Name", out var nameElement) &&
                TryGetProperty(element, "Data", out var dataElement) &&
                candidateNames.Contains(ElementToString(nameElement)))
            {
                var namedDate = ParseDate(ElementToString(dataElement));
                if (namedDate is not null)
                {
                    var namedPath = string.IsNullOrWhiteSpace(path)
                        ? ElementToString(nameElement)
                        : $"{path}.{ElementToString(nameElement)}";
                    return (namedDate, namedPath);
                }
            }

            foreach (var property in element.EnumerateObject())
            {
                var propertyPath = string.IsNullOrWhiteSpace(path) ? property.Name : $"{path}.{property.Name}";
                if (candidateNames.Contains(property.Name))
                {
                    var date = ParseDate(ElementToString(property.Value));
                    if (date is not null)
                    {
                        return (date, propertyPath);
                    }
                }

                var nested = FindDateCore(property.Value, candidateNames, propertyPath, depth + 1);
                if (nested.Date is not null)
                {
                    return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var child in element.EnumerateArray())
            {
                var nested = FindDateCore(child, candidateNames, $"{path}[{index}]", depth + 1);
                if (nested.Date is not null)
                {
                    return nested;
                }

                index++;
            }
        }

        return (null, null);
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static string GetString(JsonElement element, string name) =>
        TryGetProperty(element, name, out var value) ? ElementToString(value) : "";

    private static bool GetBoolean(JsonElement element, string name) =>
        TryGetProperty(element, name, out var value) &&
        value.ValueKind is JsonValueKind.True or JsonValueKind.False &&
        value.GetBoolean();

    private static string ElementToString(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString() ?? "",
        JsonValueKind.Number => element.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => "",
    };

    private static DateTimeOffset? ParseDate(string value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : null;

    private static string GetAwsResourceName(string arn)
    {
        var separator = Math.Max(arn.LastIndexOf('/'), arn.LastIndexOf(':'));
        return separator >= 0 && separator < arn.Length - 1 ? arn[(separator + 1)..] : arn;
    }

    private static string GetAzureResourceName(string id)
    {
        var separator = id.LastIndexOf('/');
        return separator >= 0 && separator < id.Length - 1 ? id[(separator + 1)..] : id;
    }

    private static string ValueOrFallback(params string[] values) =>
        values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value)) ?? "";
}
