using CloudExplorer.Configuration;
using FluentAssertions;
using NUnit.Framework;

namespace CloudExplorer.Tests.Configuration;

public class AppConfigTests
{
    [Test]
    public void AppConfigCreation()
    {
        var appConfig = new AppConfig { Environment = "Test" };

        appConfig.Should().NotBeNull();
        appConfig.Environment.Should().Be("Test");
    }
}
