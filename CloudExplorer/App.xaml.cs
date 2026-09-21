using System;
using System.Diagnostics.CodeAnalysis;
using CloudExplorer.Models;
using CloudExplorer.Services;
using CloudExplorer.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions;
using Uno.Extensions.Configuration;
using Uno.Extensions.Hosting;
using Uno.Resizetizer;
using Uno.UI;

namespace CloudExplorer;

public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }
    protected IHost? Host { get; private set; }

    public IServiceProvider Services => Host?.Services ??
        throw new InvalidOperationException("The application host has not been initialized.");

    private void RequestMaximizedWindow()
    {
        try
        {
            if (MainWindow?.AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            {
                presenter.Maximize();
                return;
            }

            Host?.Services.GetService<ILogger<App>>()?
                .LogWarning("The current desktop host does not expose an overlapped window presenter.");
        }
        catch (Exception exception)
        {
            Host?.Services.GetService<ILogger<App>>()?
                .LogWarning(exception, "The current desktop host rejected the maximize-window request.");
        }
    }

    private void MaximizeWindowOnFirstActivation(object sender, WindowActivatedEventArgs args)
    {
        if (MainWindow is not null)
        {
            MainWindow.Activated -= MaximizeWindowOnFirstActivation;
        }

        RequestMaximizedWindow();
    }

    [SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Uno.Extensions APIs are used in a way that is safe for trimming in this template context.")]
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .Configure(host => host
#if DEBUG
                // Switch to Development environment when running in DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging(configure: (context, logBuilder) =>
                {
                    // Configure log levels for different categories of logging
                    logBuilder
                        .SetMinimumLevel(
                            context.HostingEnvironment.IsDevelopment() ?
                                LogLevel.Information :
                                LogLevel.Warning)

                        // Default filters for core Uno Platform namespaces
                        .CoreLogLevel(LogLevel.Warning);

                    // Uno Platform namespace filter groups
                    // Uncomment individual methods to see more detailed logging
                    //// Generic Xaml events
                    //logBuilder.XamlLogLevel(LogLevel.Debug);
                    //// Layout specific messages
                    //logBuilder.XamlLayoutLogLevel(LogLevel.Debug);
                    //// Storage messages
                    //logBuilder.StorageLogLevel(LogLevel.Debug);
                    //// Binding related messages
                    //logBuilder.XamlBindingLogLevel(LogLevel.Debug);
                    //// Binder memory references tracking
                    //logBuilder.BinderMemoryReferenceLogLevel(LogLevel.Debug);
                    //// DevServer and HotReload related
                    //logBuilder.HotReloadCoreLogLevel(LogLevel.Information);
                    //// Debug JS interop
                    //logBuilder.WebAssemblyLogLevel(LogLevel.Debug);

                }, enableUnoLogging: true)
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<AppConfig>()
                )
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<ICommandRunner, ProcessCommandRunner>();
                    services.AddSingleton<IAwsCliService, AwsCliService>();
                    services.AddSingleton<IAzureCliService, AzureCliService>();
                    services.AddSingleton<AwsExplorerViewModel>();
                    services.AddSingleton<AzureExplorerViewModel>();
                    services.AddSingleton<MainViewModel>();
                })
            );
        MainWindow = builder.Window;
        MainWindow.Title = "Cloud Explorer";

#if DEBUG
        MainWindow.UseStudio();
#endif
        MainWindow.SetWindowIcon();

        Host = builder.Build();

        // Do not repeat app initialization when the Window already has content,
        // just ensure that the window is active
        if (MainWindow.Content is not Frame rootFrame)
        {
            // Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = new Frame();

            // Place the frame in the current Window
            MainWindow.Content = rootFrame;
        }

        if (rootFrame.Content == null)
        {
            // When the navigation stack isn't restored navigate to the first page,
            // configuring the new page by passing required information as a navigation
            // parameter
            rootFrame.Navigate(typeof(MainPage), args.Arguments);
        }
        // Ensure the current window is active
        MainWindow.Activated += MaximizeWindowOnFirstActivation;
        MainWindow.Activate();
    }
}
