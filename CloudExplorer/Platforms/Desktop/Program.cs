using System;
using CloudExplorer;
using Uno.UI.Hosting;

namespace CloudExplorer.Platforms.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main()
    {
        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWin32()
            .Build();

        host.Run();
    }
}
