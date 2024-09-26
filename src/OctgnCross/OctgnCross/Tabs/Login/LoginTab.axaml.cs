using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Octgn.Core;

namespace Octgn.Tabs.Login;

public partial class LoginTab : UserControl
{

    private static log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public LoginTabViewModel LoginVM { get; set; }

    public LoginTab()
    {
        LoginVM = new LoginTabViewModel();
        Loaded += RefreshNews_EventCallback;
        InitializeComponent();

        // labelRegister.MouseLeftButtonUp += (sender, args) => Program.LaunchUrl(AppConfig.WebsitePath);
        // labelForgot.MouseLeftButtonUp +=
        //     (sender, args) => Program.LaunchUrl(AppConfig.WebsitePath);
        // labelSubscribe.MouseLeftButtonUp += delegate
        // {
        //     var url = SubscriptionModule.Get().GetSubscribeUrl(new SubType() { Description = "", Name = "" });
        //     if (url != null)
        //     {
        //         Program.LaunchUrl(url);
        //     }
        // };

        var timer = new DispatcherTimer(TimeSpan.FromMinutes(2), DispatcherPriority.Normal, RefreshNews_EventCallback);
        timer.Start();
    }

    private async void RefreshNews_EventCallback(object? sender, EventArgs e) {
        await LoginVM.News.Refresh();
    }

    #region UI Events
    private void Button1Click(object sender, RoutedEventArgs e)
    {
        LoginVM.LoginAsync();
    }
    private void PasswordBox1KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            LoginVM.LoginAsync();
        }
    }
    #endregion

    // private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) {
    //     Program.LaunchUrl(e.Uri.ToString());
    // }
}