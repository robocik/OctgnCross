using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using log4net;
using Octgn.Communication;
using Octgn.Communication.Modules;
using Octgn.Core.Annotations;

namespace Octgn.Views;

public partial class MainView : UserControl,INotifyPropertyChanged
{
    internal new static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);
    public ConnectionStatus ConnectionStatus => App.LobbyClient.Status;
    public MainView()
    {
        InitializeComponent();
        
        App.LobbyClient.Disconnected += LobbyClient_Disconnected;
        App.LobbyClient.Connected += LobbyClient_Connected;
        App.LobbyClient.Connecting += LobbyClient_Connecting;
        App.LobbyClient.Stats().StatsModuleUpdate += LobbyClient_StatsModuleUpdate;
    }
    
    public new event PropertyChangedEventHandler PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    private void LobbyClient_StatsModuleUpdate(object sender, StatsModuleUpdateEventArgs e) {
        OnPropertyChanged(nameof(ConnectionStatus));
    }

    private async void LobbyClient_Connected(object sender, ConnectedEventArgs args)
    {
        try {
            OnPropertyChanged(nameof(ConnectionStatus));

            // await Dispatcher.UIThread.InvokeAsync(async ()=> {
            //     ProfileTab.IsEnabled = true;
            //     await ProfileTabContent.Load(Program.LobbyClient.User);
            // });
        } catch (Exception ex) {
            Log.Error($"{nameof(LobbyClient_Connected)}", ex);
        }
    }

    private async void LobbyClient_Disconnected(object sender, DisconnectedEventArgs args)
    {
        try {
            OnPropertyChanged(nameof(ConnectionStatus));

            await Dispatcher.UIThread.InvokeAsync(() => {
                ProfileTab.IsEnabled = false;
            });
        } catch (Exception ex) {
            Log.Error($"{nameof(LobbyClient_Connected)}", ex);
        }
    }

    private async void LobbyClient_Connecting(object sender, ConnectingEventArgs e) {
        try {
            OnPropertyChanged(nameof(ConnectionStatus));

            await Dispatcher.UIThread.InvokeAsync(() => {
                ProfileTab.IsEnabled = false;
            });
        } catch (Exception ex) {
            Log.Error($"{nameof(LobbyClient_Connecting)}", ex);
        }
    }
    
    /// <summary>
    /// Happens when the window is closing
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="cancelEventArgs">
    /// The cancel event args.
    /// </param>
    private void OnClosing(object sender, CancelEventArgs cancelEventArgs)
    {
        App.LobbyClient.Disconnected -= LobbyClient_Disconnected;
        App.LobbyClient.Connected -= LobbyClient_Connected;
        App.LobbyClient.Connecting -= LobbyClient_Connecting;
        App.LobbyClient.Stats().StatsModuleUpdate -= LobbyClient_StatsModuleUpdate;
        App.LobbyClient.Stop();
        // GameUpdater.Get().Dispose();
        Task.Factory.StartNew(App.Exit);
    }

    private void TabControlMain_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        TabCustomGamesList?.VisibleChanged(TabCustomGames.IsSelected);
        // TabHistory.VisibleChanged(TabItemHistory.IsSelected);
    }
}