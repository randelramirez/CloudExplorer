using CloudExplorer.Models;
using FluentAssertions;
using NUnit.Framework;

namespace CloudExplorer.Tests;

public class AppInfoTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void AppInfoCreation()
    {
        var appInfo = new AppConfig { Environment = "Test" };

        appInfo.Should().NotBeNull();
        appInfo.Environment.Should().Be("Test");
    }
}
