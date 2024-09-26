using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using log4net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Octgn.Communication.Tcp;
using Octgn.Core;
using Octgn.DataNew.Entities;
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
    public static event Action OnOptionsChanged;

    internal static bool IsGameRunning;

    internal static bool InPreGame;

    public static JodsEngineIntegration JodsEngine { get; private set; }

    public static string CurrentOnlineGameName = "";
    
    internal static bool IsHost { get; set; }
    internal static GameMode GameMode { get; set; }
    public static bool DeveloperMode { get; private set; }

    public App()
    {
        
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        Prefs.Store??= new DefaultPreferencesStore();
        if (OperatingSystem.IsAndroid())
        {
            var stream=AssetLoader.Open(new Uri("avares://OctgnCross/Resources/appsettings.json"));
            // using var stream=Application.Current.Resources.Open("/Resources/AboutResources.txt");
            AppConfig.Load(stream);
        }
        else
        {
            var configFile = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            using var stream = File.OpenRead(configFile);
            AppConfig.Load(stream);
        }
        try
        {
            IsReleaseTest = File.Exists(Path.Combine(Config.Instance.Paths.ConfigDirectory, "TEST"));
        }
        catch(Exception ex)
        {
            Log.Warn("Error checking for test mode", ex);
        }
        var collection = new ServiceCollection();
        collection.AddCommonServices();
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
                .GetMessageBoxStandard("Octgn",$"Error loading Config:{Environment.NewLine}{ex}",icon:Icon.Error);

            await box.ShowAsync();
            //
            // Shutdown(1);

            return;
        }
        
        JodsEngine = new JodsEngineIntegration();
        Log.Info("Configuring game feeds");
        ConfigureGameFeedTimeout();
        
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

    private static void ConfigureGameFeedTimeout()
    {
        var manager = GameFeedManager.Get();
        manager.FeedUpdateTimeout = TimeSpan.FromSeconds(AppConfig.GameFeedTimeoutSeconds);
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