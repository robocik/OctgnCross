using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DialogHostAvalonia;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using NuGet.Packaging;
using Octgn;
using Octgn.Communication;
using Octgn.Core;
using Octgn.Core.DataManagers;
using Octgn.Library;
using Octgn.Library.Exceptions;
using Octgn.Library.Networking;
using Octgn.Online.Hosting;
using Octgn.Site.Api;
using OctgnCross.Controls;

namespace OctgnCross.Tabs.Play;

public partial class PlayTab : UserControl,INotifyPropertyChanged, IDisposable
{
    private static log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public ObservableCollection<HostedGameViewModel> HostedGameList { get; set; }
    
    public ObservableCollection<HostedGameViewModel> SpectatedGamesList { get; set; }

    private bool _loadingGame;
    public bool LoadingGame
    {
        get => _loadingGame;
        set => NotifyAndUpdate(ref _loadingGame, value);
    }

    private HostedGameViewModel _selectedGame;
    public HostedGameViewModel SelectedGame
    {
        get { return _selectedGame; }

        set {
            this.IsJoinableGameSelected = value?.CanPlay == true;
            if (value != null)
                Log.InfoFormat("Selected game {0} {1}", value.GameId, value.Name);
            NotifyAndUpdate(ref _selectedGame, value);
}
    }

    public bool HideUninstalledGames  {
        get => Prefs.HideUninstalledGamesInList;
        set
        {
            Prefs.HideUninstalledGamesInList = value;
            OnPropertyChanged(nameof(HideUninstalledGames));
        }
    }
    private bool _showKillGameButton;
    public bool ShowKillGameButton {
        get => _showKillGameButton;
        set => NotifyAndUpdate(ref _showKillGameButton, value);
    }

    private int _gameCount;
    public int GameCount {
        get => _gameCount;
        set => NotifyAndUpdate(ref _gameCount, value);
    }

    private bool _isLoggedIn;
    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        set => NotifyAndUpdate(ref _isLoggedIn, value);
    }

    private readonly GameBroadcastListener broadcastListener;

    public PlayTab() {
        InitializeComponent();
        broadcastListener = new GameBroadcastListener();
        broadcastListener.StartListening();
        HostedGameList = new ObservableCollection<HostedGameViewModel>();
        SpectatedGamesList = new ObservableCollection<HostedGameViewModel>();
        App.LobbyClient.Connected += LobbyClient_OnConnect;
        App.LobbyClient.Disconnected += LobbyClient_OnDisconnect;
        // if (App.Discord != null) {
        //     App.Discord.JoinGame += Discord_JoinGame;
        // }
        LoadingGame = true;
        ShowKillGameButton = Prefs.IsAdmin;
        _refreshGameListTimer = new DispatcherTimer(InitialRefreshDelay, DispatcherPriority.Normal, RefreshGameListTimer_Tick);
        _refreshGameListTimer.IsEnabled = false;
    }

    #region Game List Refreshing

    private readonly DispatcherTimer _refreshGameListTimer;

    private bool _isRefreshingGameList;
    public bool IsRefreshingGameList {
        get => _isRefreshingGameList;
        set => NotifyAndUpdate(ref _isRefreshingGameList, value);
    }

    public static TimeSpan InitialRefreshDelay { get; } = TimeSpan.FromSeconds(2);
    public static TimeSpan NormalRefreshDelay { get; } = TimeSpan.FromSeconds(15);
    
    private TimeSpan _currentRefreshDelay = TimeSpan.FromDays(10);
    public TimeSpan CurrentRefreshDelay {
        get => _currentRefreshDelay;
        set {
            if (!NotifyAndUpdate(ref _currentRefreshDelay, value)) return;
            OnPropertyChanged(nameof(IsInitialRefresh));
            _refreshGameListTimer.Interval = value;
        }
    }

    public bool IsInitialRefresh => CurrentRefreshDelay == InitialRefreshDelay;

    
    private async void Discord_JoinGame(object sender, Guid guid)
    {
        var game = HostedGameList.FirstOrDefault(x => x.Id == guid);
        if (game == null) return;
        await JoinGame(game);
    }
    private async void RefreshGameListTimer_Tick(object sender, EventArgs e) {
        ShowKillGameButton = Prefs.IsAdmin;

        try {
            IsRefreshingGameList = true;

            _refreshGameListTimer.IsEnabled = false;

            if(CurrentRefreshDelay == InitialRefreshDelay) {
                CurrentRefreshDelay = NormalRefreshDelay;
            }

            var games = new List<HostedGameViewModel>();

            // Add online hosted games
            if (App.LobbyClient?.IsConnected == true) {
                var client = new ApiClient();
                var hostedGames = await client.GetGameList();
                games.AddRange(hostedGames.Select(x=>new HostedGameViewModel(x)));
                IsLoggedIn = true;
            }

            // Add local and lan games
            games.AddRange(broadcastListener.Games.Select(x => new HostedGameViewModel(x)));

            // /////// Update the visual list
            HostedGameList.Clear();
            SpectatedGamesList.Clear();
            // Remove the games that don't exist anymore
            // var removeList = HostedGameList.Where(i => games.All(x => x.Id != i.Id)).ToList();
            // removeList.ForEach(x => HostedGameList.Remove(x));
            //
            // removeList = SpectatedGamesList.Where(i => games.All(x => x.Id != i.Id)).ToList();
            // removeList.ForEach(x => SpectatedGamesList.Remove(x));
            var dbgames = GameManager.Get().Games.ToArray();
            // Add games that don't already exist in the list
            var addList = games;
            foreach (var game in addList)
            {
                if (!game.CanPlay && HideUninstalledGames)
                {
                    continue;
                }

                if (game.Status is HostedGameStatus.StoppedHosting)
                {
                    continue;
                }

                if (game.Status is HostedGameStatus.StartedHosting)
                {
                    HostedGameList.Add(game);    
                }
                else
                {
                    SpectatedGamesList.Add(game);
                }
                var li = games.FirstOrDefault(x => x.Id == game.Id);
                await game.Update(li, dbgames);
            }
            

            // Update all the existing items with new data
            
            // foreach (var g in HostedGameList) {
            //     var li = games.FirstOrDefault(x => x.Id == g.Id);
            //     await g.Update(li, dbgames);
            // }
            
            GameCount = HostedGameList.Count;
        } catch (Exception ex) {
            Log.Warn(nameof(RefreshGameListTimer_Tick), ex);
        } finally {
            IsRefreshingGameList = false;
            _refreshGameListTimer.IsEnabled = true;

            await ShowProgressAnimation();
        }
    }

    private async Task ShowProgressAnimation()
    {
        Animation animation;
        if (IsInitialRefresh)
        {
            animation = (Animation)this.Resources["InitialRefreshDelayAnimation"];
        }
        else
        {
            animation = (Animation)this.Resources["NormalRefreshDelayAnimation"];
        }

        await animation.RunAsync(RefreshProgressBar);
    }

    public async void VisibleChanged(bool visible) {
        // Switching the interval on this timer allows the list to refresh quickly initially when the tab is ever viewed, then it'll wait the normal delay
        if(visible)
            CurrentRefreshDelay = InitialRefreshDelay;

        _refreshGameListTimer.IsEnabled = visible;

        await this.ShowProgressAnimation();
    }

    void LobbyClient_OnConnect(object sender, ConnectedEventArgs e) {
        Log.Info("Connected");
        Dispatcher.UIThread.InvokeAsync(new Action(() => {
            this.HostedGameList.Clear();
            this.IsLoggedIn = true;
            this.CurrentRefreshDelay = InitialRefreshDelay;
        }));
    }

    void LobbyClient_OnDisconnect(object sender, DisconnectedEventArgs e) {
        Log.Info("Disconnected"); 
        Dispatcher.UIThread.InvokeAsync(new Action(() => {
            this.HostedGameList.Clear();
            this.IsLoggedIn = false;
        }));
    }

    #endregion Game List Refreshing

    #region Host Game

    private async void ButtonHostClick(object sender, RoutedEventArgs e) {
        try {
            try {
                LoadingGame = false;
                using var dialog = new HostGameSettings();
                
                await DialogHost.Show(dialog,delegate(object _, DialogOpenedEventArgs args)
                {
                    dialog.DialogSession = args.Session;
                });
                // dialog.OnClose += HostGameSettingsDialogOnClose;
                LoadingGame = false;
            } finally {
                // dialog.OnClose -= HostGameSettingsDialogOnClose;
                // dialog.Dispose();
                LoadingGame = true;
            }
        } catch (Exception ex) {
            await HandleException(ex);
        }
    }

    // private void HostGameSettingsDialogOnClose(object sender, DialogResult dialogResult) {
    //     LoadingGame = true;
    //     // using (var dialog = sender as HostGameSettings) {
    //     //     dialog.OnClose -= HostGameSettingsDialogOnClose;
    //     // }
    // }


    #endregion Host Game

    #region Join Game

    private bool _isJoinableGameSelected;
    public bool IsJoinableGameSelected
    {
        get =>_isJoinableGameSelected;
        private set
        {
            _isJoinableGameSelected = value;
            OnPropertyChanged();
        }
    }

    private async void HostingGamesList_OnDoubleTapped(object sender, TappedEventArgs e)
    {
        await JoinGame(SelectedGame);
    }
    // private async void GameListItemDoubleClick(object sender, MouseButtonEventArgs e)
    // {
    //     await JoinGame(SelectedGame);
    // }

    private async void ButtonJoinClick(object sender, RoutedEventArgs e)
    {
        await JoinGame(SelectedGame);
    }

    private async Task JoinGame(HostedGameViewModel game) {
        try {
            Log.Info($"{nameof(JoinGame)}");

            LoadingGame = false;

            var hostedGame = await VerifyCanJoinGame();

            string username;
            if (hostedGame.GameSource == "Online") {
                username = App.LobbyClient.User.DisplayName;
            } else {
                username = App.LobbyClient.User?.DisplayName ?? Prefs.Username ?? Randomness.RandomRoomName();
            }

            var spectate
                = hostedGame.Status == HostedGameStatus.GameInProgress
                && hostedGame.Spectator;

            await App.JodsEngine.JoinGame(hostedGame, username, spectate);
        } catch (Exception ex) {
            await HandleException(ex);
        } finally {
            LoadingGame = true;
       }
    }

    private async Task<HostedGameViewModel> VerifyCanJoinGame() {
        if (SelectedGame == null) return null;
        if (SelectedGame.Status == HostedGameStatus.GameInProgress && SelectedGame.Spectator == false) {
            throw new UserMessageException("You can't join a game that is already in progress.");
        }

        if (SelectedGame.GameSource == "Online") {
            var client = new ApiClient();
            try {
                if (!await client.IsGameServerRunning(Prefs.Username, App.SessionKey)) {
                    throw new UserMessageException("The game server is currently down. Please try again later.");
                }
            } catch (Exception ex) {
                throw new UserMessageException("The game server is currently down. Please try again later.", ex);
            }
        }

        var game = GameManager.Get().GetById(SelectedGame.GameId);
        if (game == null) {
            throw new UserMessageException("You don't have the required game installed.");
        }

        return SelectedGame;
    }

    #endregion Join Game

    #region Join Offline Game

    private async void ButtonJoinOfflineGame(object sender, RoutedEventArgs e) {
        // try {
        //     ConnectOfflineGame dialog = null;
        //     try {
        //         dialog = new ConnectOfflineGame();
        //         dialog.VerticalAlignment = VerticalAlignment.Center;
        //         dialog.Show(DialogPlaceHolder);
        //         dialog.OnClose += ConnectOfflineGameDialogOnClose;
        //         LoadingGame = false;
        //     } catch {
        //         dialog.OnClose -= ConnectOfflineGameDialogOnClose;
        //         dialog.Dispose();
        //         LoadingGame = true;
        //         throw;
        //     }
        // } catch (Exception ex) {
        //     await HandleException(ex);
        // }
    }

    // private void ConnectOfflineGameDialogOnClose(object sender, DialogResult dialogResult) {
    //     LoadingGame = true;
    //     // using (var dialog = sender as ConnectOfflineGame) {
    //     //     dialog.OnClose -= ConnectOfflineGameDialogOnClose;
    //     // }
    // }

    #endregion Join Offline Game

    private void ButtonKillGame(object sender, RoutedEventArgs e) {
        if (SelectedGame == null) return;
        if (App.LobbyClient != null && App.LobbyClient.User != null && App.LobbyClient.IsConnected) {
            throw new NotImplementedException("sorry bro");
        }
    }

    public void Dispose() {
        broadcastListener.StopListening();
        broadcastListener.Dispose();
        App.LobbyClient.Disconnected -= LobbyClient_OnDisconnect;
        // if (App.Discord != null) {
        //     App.Discord.JoinGame -= Discord_JoinGame;
        // }
        _refreshGameListTimer.IsEnabled = false;
    }

    private async Task HandleException(Exception ex, [CallerMemberName]string caller = null) {
        Log.Error($"{nameof(HandleException)}: {caller}", ex);

        var error = "Unknown Error: Please try again";
        if(ex is UserMessageException um) {
            error = um.Message;
        }

        var box = MessageBoxManager
            .GetMessageBoxStandard("OCTGN",error,icon:Icon.Error);

        await box.ShowAsync();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual bool NotifyAndUpdate<T>(ref T privateField, T value, [CallerMemberName]string propertyName = null) {
        if (object.Equals(privateField, value)) return false;
        privateField = value;

        OnPropertyChanged(propertyName);
        return true;
    }

    protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null) {
        if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentNullException(nameof(propertyName));

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    
}