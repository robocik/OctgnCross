using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using log4net;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Octgn.Communication.Tcp;
using Octgn.Core;
using Octgn.Library;
using Octgn.Library.Communication;
using Octgn.Online;
using Octgn.Site.Api;
using Octgn.ViewModels;
using Octgn.Views;

namespace Octgn;

public partial class App : Application
{
    internal static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);
    public static Library.Communication.Client LobbyClient;
    public static bool IsReleaseTest { get; set; }
    public static string SessionKey => Prefs.SessionKey;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        ApiClient.DefaultUrl = new Uri(AppConfig.WebsitePath);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        Log.Info("Creating Config class");
        try
        {
            Config.Instance = new Config();
        }
        catch (Exception ex)
        {
            //TODO: Find a better user experience for dealing with this error. Like a wizard to fix the problem or something.
            Log.Fatal("Error loading config", ex);

            var box = MessageBoxManager
                .GetMessageBoxStandard($"Error loading Config:{Environment.NewLine}{ex}","Octgn",icon:Icon.Error);

            await box.ShowAsync();
            //
            // Shutdown(1);

            return;
        }
        
        Log.Info("Creating Lobby Client");
        var handshaker = new DefaultHandshaker();
        var connectionCreator = new TcpConnectionCreator(handshaker);
        var lobbyClientConfig = new LibraryCommunicationClientConfig(connectionCreator);

        LobbyClient = new Client(
            lobbyClientConfig,
            typeof(App).Assembly.GetName().Version
        );
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    public static Task LaunchUrl(string url)
    {
        var box = MessageBoxManager
            .GetMessageBoxStandard("Error","Needs to be implemented: "+url
                ,icon:Icon.Warning);
        return box.ShowAsync();
    }
    
    public static void Exit()
    {
        // try { SSLHelper.Dispose(); }
        // catch (Exception e) {
        //     Log.Error( "SSLHelper Dispose Exception", e );
        // };
        // Sounds.Close();
        // UpdateManager.Instance.Stop();
        LogManager.Shutdown();
        Dispatcher.UIThread.Invoke(new Action(() =>
        {
            if (LobbyClient != null)
                LobbyClient.Stop();
           // WindowManager.Shutdown();

            //Apparently this can be null sometimes?
            // if (Application.Current != null)
            //     Application.Current.Shutdown(0);
        }));

    }
}