using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Avalonia;
using Avalonia.Android;
using Octgn.Core;

namespace Octgn.Android;

[Activity(
    Label = "Octgn.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        
        Prefs.Store = new SecureStoragePreferencesStore();
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}