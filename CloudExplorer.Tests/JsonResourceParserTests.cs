using System;
using System.Linq;
using CloudExplorer.Models;
using CloudExplorer.Services;
using FluentAssertions;
using NUnit.Framework;

namespace CloudExplorer.Tests;

public class JsonResourceParserTests
{
    [Test]
    public void ParseAwsResources_MapsTagsDatesAndObservationTime()
    {
        const string json = """
            {
              "Resources": [
                {
                  "Arn": "arn:aws:ec2:us-east-1:123456789012:instance/i-123",
                  "LastReportedAt": "2026-09-18T03:00:00Z",
                  "OwningAccountId": "123456789012",
                  "Region": "us-east-1",
                  "ResourceType": "ec2:instance",
                  "Service": "ec2",
                  "Properties": [
                    {
                      "Name": "tags",
                      "Data": [
                        { "Key": "Environment", "Value": "Production" },
                        { "Key": "Owner", "Value": "Platform" }
                      ]
                    },
                    {
                      "Name": "CreationTime",
                      "Data": "2025-01-02T04:00:00Z"
                    },
                    {
                      "Name": "details",
                      "Data": { "LastModifiedAt": "2026-04-05T06:00:00Z" }
                    }
                  ]
                }
              ]
            }
            """;

        var result = JsonResourceParser.ParseAwsResources(json);

        result.Should().ContainSingle();
        var resource = result[0];
        resource.Provider.Should().Be(CloudProvider.Aws);
        resource.Name.Should().Be("i-123");
        resource.ResourceType.Should().Be("ec2:instance");
        resource.Tags.Should().Contain("Environment", "Production");
        resource.CreatedAt.Should().Be(DateTimeOffset.Parse("2025-01-02T04:00:00Z"));
        resource.LastModifiedAt.Should().Be(DateTimeOffset.Parse("2026-04-05T06:00:00Z"));
        resource.LastObservedAt.Should().Be(DateTimeOffset.Parse("2026-09-18T03:00:00Z"));
    }

    [Test]
    public void ParseAzureResources_MapsResourceGroupTagsAndTopLevelDates()
    {
        const string json = """
            [
              {
                "id": "/subscriptions/sub-1/resourceGroups/rg-app/providers/Microsoft.Web/sites/demo",
                "name": "demo",
                "type": "Microsoft.Web/sites",
                "resourceGroup": "rg-app",
                "location": "eastus",
                "createdTime": "2025-02-03T04:05:06Z",
                "changedTime": "2026-07-08T09:10:11Z",
                "tags": {
                  "environment": "test"
                }
              }
            ]
            """;

        var result = JsonResourceParser.ParseAzureResources(json, "sub-1");

        result.Should().ContainSingle();
        var resource = result[0];
        resource.Provider.Should().Be(CloudProvider.Azure);
        resource.GroupName.Should().Be("rg-app");
        resource.AccountId.Should().Be("sub-1");
        resource.Tags.Should().Contain("environment", "test");
        resource.CreatedAtSource.Should().Be("Azure Resource Manager: createdTime");
        resource.LastModifiedAtSource.Should().Be("Azure Resource Manager: changedTime");
    }

    [Test]
    public void ParseAzureSubscriptions_PutsDefaultEnabledSubscriptionFirst()
    {
        const string json = """
            [
              { "id": "disabled", "name": "Disabled", "state": "Disabled", "isDefault": false },
              { "id": "sub-2", "name": "Second", "state": "Enabled", "isDefault": false },
              { "id": "sub-1", "name": "Primary", "state": "Enabled", "isDefault": true }
            ]
            """;

        var result = JsonResourceParser.ParseAzureSubscriptions(json);

        result.Select(static account => account.Id).Should().Equal("sub-1", "sub-2");
    }
}
