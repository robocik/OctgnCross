using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using log4net;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Octgn.Core;
using Octgn.Core.DataExtensionMethods;
using Octgn.Core.DataManagers;
using Octgn.Core.Play;
using Octgn.DataNew;
using Octgn.DataNew.Entities;
using Octgn.JodsEngine.Controls;
using Octgn.JodsEngine.Play.Gui;
using Octgn.JodsEngine.Utils;
using Octgn.JodsEngine.Windows;
using Octgn.Library;
using Octgn.Library.Exceptions;
using Octgn.Play;
using Octgn.Play.Gui;
using Octgn.Play.Save;
using Octgn.Scripting;
using Octgn.UI;
using Card = Octgn.Play.Card;
using Counter = Octgn.Play.Counter;
using Group = System.Text.RegularExpressions.Group;
using Player = Octgn.Play.Player;

namespace Octgn.JodsEngine.Play;

public partial class PlayWindow : OctgnCross.UI.WindowBase
{
        private bool _isLocal;

        protected Engine ScriptEngine;
        internal static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);


        #region Dependency Properties

        public bool IsFullScreen
        {
            get { return _isFullScreen; }
            set
            {
                _isFullScreen=value;
                OnPropertyChanged(nameof(IsFullScreen));
            }
        }

        public bool ShowSubscribeMessage
        {
            get { return _showSubscribeMessage; }
            set
            {
                _showSubscribeMessage=value;
                OnPropertyChanged(nameof(IsFullScreen));
            }
        }

        #endregion

        private SolidColorBrush _backBrush = new SolidColorBrush(Color.FromArgb(210, 33, 33, 33));
        private SolidColorBrush _offBackBrush = new SolidColorBrush(Color.FromArgb(55, 33, 33, 33));
        // private Storyboard _fadeIn, _fadeOut;
        private static System.Collections.ArrayList fontName = new System.Collections.ArrayList();
        private GameMessageDispatcherReader _gameMessageReader;

        private Card _currentCard;
        private bool _currentCardUpStatus;
        private bool _newCard;

        // public PreviewCardWindow _previewCardWindow;

        // private Storyboard _showBottomBar;

        private TableControl table;

        private DateTime lastMessageSoundTime = DateTime.MinValue;

        internal GameLog GameLogWindow = new GameLog();

        public ObservableCollection<IGameMessage> GameMessages { get; set; }

        private bool chatIsDocked;
        // public ChatWindow _chatWindow;
        private const int DefaultChatWidth = 300;


        public bool IsHost { get; set; }

        public bool ChatVisible
        {
            get
            {
                return this.chatVisible;
            }
            set
            {
                if (value == this.chatVisible) return;
                this.chatVisible = value;
                OnPropertyChanged("ChatVisible");
            }
        }

        public bool EnableGameScripts
        {
            get
            {
                return Prefs.EnableGameScripts;
            }
            set
            {
                Prefs.EnableGameScripts = value;
                OnPropertyChanged("EnableGameScripts");
            }
        }

        public bool ShowExtendedTooltips
        {
            get { return Prefs.ExtendedTooltips; }
            set { Prefs.ExtendedTooltips = value; }
        }

        public bool EnableChatTextShadows {
            get { return Prefs.InGameChatTextShadows; }
            set {
                Prefs.InGameChatTextShadows = value;
                // chat.UpdateVisualsFromPreferences();
            }
        }

        public GameSettings GameSettings { get; set; }

        public ReplayEngine ReplayEngine { get; }

        public async Task Init()
        {
            //Application.Current.MainWindow = this;
            Version oversion = Assembly.GetExecutingAssembly().GetName().Version;
            Title = "Octgn  version : " + oversion + " : " + Program.GameEngine.Definition.Name;

            // Program.GameEngine.ComposeParts(this);
            Program.ScriptEngine = new Engine();
            if (Program.GameEngine.AllPhases.Count() < 1) PhaseControl.IsVisible = false;
            this.Loaded += OnLoaded;

            // this.chat.MouseEnter += ChatOnMouseEnter;
            // this.chat.MouseLeave += ChatOnMouseLeave;
            this.playerTabs.PointerEntered += PlayerTabsOnMouseEnter;
            this.playerTabs.PointerExited += PlayerTabsOnMouseLeave;
            this.chatIsDocked = true;
            // this.ChatToggleChecked.IsChecked = false;
            this.PreGameLobby.OnClose += async delegate
            {
                try {
                    if (this.PreGameLobby.StartingGame) {
                        PreGameLobby.IsVisible =false;
                        if (Player.LocalPlayer.Spectator == false && Program.GameEngine.IsReplay == false)
                            Program.GameEngine.ScriptEngine.SetupEngine(false);


                        table = new TableControl { DataContext = Program.GameEngine.Table, IsTabStop = true };
                        KeyboardNavigation.SetIsTabStop(table, true);
                        TableHolder.Child = table;

                        table.UpdateSided();
                        // Keyboard.Focus(table);

                        Dispatcher.UIThread.InvokeAsync(new Action(Program.GameEngine.Ready), DispatcherPriority.ContextIdle);

                        // if (Program.DeveloperMode && Player.LocalPlayer.Spectator == false && Program.GameEngine.IsReplay == false) {
                        //     MenuConsole.IsVisible = IsVisible.Visible;
                        //     var wnd = new DeveloperWindow() { Owner = this };
                        //     wnd.Show();
                        // }
                        Program.GameSettings.PropertyChanged += (sender, args) => {
                            if (Program.IsHost) {
                                Program.Client.Rpc.Settings(Program.GameSettings.UseTwoSidedTable,
                                                            Program.GameSettings.AllowSpectators,
                                                            Program.GameSettings.MuteSpectators,
                                                            Program.GameSettings.AllowCardList);
                            }
                        };
                    } else {
                        IsRealClosing = true;
                        this.TryClose();
                    }
                } catch (Exception ex) {
                    Log.Fatal($"PreGameLobby On Close Error: {ex.Message}", ex);

                    IsRealClosing = true;

                    TryClose();
                }
            };

            this.Loaded += delegate
            {
                Program.OnOptionsChanged += ProgramOnOnOptionsChanged;
                _gameMessageReader.Start(
                    x =>
                    {
                        Dispatcher.UIThread.Invoke(new Action(
                            () =>
                            {
                                bool gotOne = false;
                                foreach (var m in x)
                                {
                                    // var b = Octgn.Play.Gui.ChatControl.GameMessageToBlock(m);
                                    // if (b == null) continue;

                                    if (m is NotifyBarMessage)
                                    {
                                        GameMessages.Insert(0, m);
                                        gotOne = true;
                                        while (GameMessages.Count > 60)
                                        {
                                            GameMessages.Remove(GameMessages.Last());
                                        }
                                    }
                                }
                                if (!gotOne) return;

                                // if (_showBottomBar != null && _showBottomBar.GetCurrentProgress(BottomBar) > 0)
                                // {
                                //     _showBottomBar.Seek(BottomBar, TimeSpan.FromMilliseconds(500), TimeSeekOrigin.BeginTime);
                                // }
                                // else
                                // {
                                //     if (_showBottomBar == null)
                                //     {
                                //         _showBottomBar = BottomBar.Resources["ShowBottomBar"] as Storyboard;
                                //     }
                                //     _showBottomBar.Begin(BottomBar, HandoffBehavior.Compose, true);
                                // }
                                if (this.IsActive == false)
                                {
                                    //this.FlashWindow();
                                }
                                if (this.IsActive == false && Prefs.EnableGameSound && DateTime.Now > lastMessageSoundTime.AddSeconds(10))
                                {
                                    //Octgn.Utils.Sounds.PlayGameMessageSound();
                                    lastMessageSoundTime = DateTime.Now;
                                }
                            }));
                    });
            };
            this.Activated += delegate
            {
                // this.StopFlashingWindow();
            };
            this.Unloaded += delegate
            {
                Program.OnOptionsChanged -= ProgramOnOnOptionsChanged;
                _gameMessageReader.Stop();
            };
        }
        public PlayWindow()
        {
            GameSettings = Program.GameSettings;
            IsHost = Program.IsHost;
            GameMessages = new ObservableCollection<IGameMessage>();
            _gameMessageReader = new GameMessageDispatcherReader(Program.GameMess);
            var isLocal = Program.GameEngine.IsLocal;
            DataContext = Program.GameEngine;

            ReplayEngine = Program.GameEngine.ReplayEngine;

            InitializeComponent();


            if (Program.GameEngine.IsReplay) {
                foreach (var eve in ReplayEngine.AllEvents) {
                    if (eve.Type == ReplayEventType.NextTurn) {
                        ReplaySlider.Ticks.Add(eve.Time.Ticks);
                    }
                }

                ReplayControls.IsVisible =true;
            } else {
                ReplayControls.IsVisible = false;
            }

            _isLocal = isLocal;
            


            //this.chat.NewMessage = x =>
            //{
            //    GameMessages.Insert(0, x);
            //};
        }

        private void ProgramOnOnOptionsChanged()
        {
            OnPropertyChanged("EnableGameScripts");
        }

        private void PlayerTabsOnMouseLeave(object sender, PointerEventArgs PointerEventArgs)
        {
            playerTabs.Background = _offBackBrush;
        }

        private void PlayerTabsOnMouseEnter(object sender, PointerEventArgs PointerEventArgs)
        {
            playerTabs.Background = _backBrush;
        }

        private void ChatOnMouseLeave(object sender, PointerEventArgs PointerEventArgs)
        {
            // chat.Background = _offBackBrush;
        }

        private void ChatOnMouseEnter(object sender, PointerEventArgs PointerEventArgs)
        {
            // chat.Background = _backBrush;
        }

        private async void OnLoaded(object sen, RoutedEventArgs routedEventArgs)
        {
            this.Loaded -= OnLoaded;
            // _fadeIn = (Storyboard)Resources["ImageFadeIn"];
            // _fadeOut = (Storyboard)Resources["ImageFadeOut"];

            // I think this is the thing that previews a card if you hover it.
            cardViewer.Source = await StringExtensionMethods.BitmapFromUri(new Uri(Program.GameEngine.Definition.DefaultSize().Back));
            //if (Program.GameEngine.Definition.CardCornerRadius > 0)
            cardViewer.Clip = new RectangleGeometry();
            // AddHandler(CardControl.CardHoveredEvent, new CardEventHandler(CardHovered));
            // AddHandler(CardRun.ViewCardModelEvent, new EventHandler<CardModelEventArgs>(ViewCardModel));

            // Loaded += (sender, args) => Keyboard.Focus(table);
            // Solve various issues, like disabled menus or non-available keyboard shortcuts

            GroupControl.groupFont = new FontFamily("Segoe UI");
            GroupControl.fontsize = Prefs.ContextFontSize;
            // chat.output.FontFamily = new FontFamily("Segoe UI");
            // chat.output.FontSize = Prefs.ChatFontSize;
            // chat.watermark.FontFamily = new FontFamily("Segoe UI");
            // chat.watermark.FontSize = Prefs.ContextFontSize;

            // Apply game defined fonts
            if (Prefs.UseGameFonts)
            {
                // chat.output.SetFont(Program.GameEngine.Definition.ChatFont);
                // chat.watermark.SetFont(Program.GameEngine.Definition.ContextFont);
                //
                // GroupControl.groupFont = new FontFamily(chat.watermark.FontFamily.Source);
                if (Program.GameEngine.Definition.ContextFont?.Size > 0)
                {
                    GroupControl.fontsize = Program.GameEngine.Definition.ContextFont.Size;
                }
            }
            Log.Info(string.Format("Checking if the loaded game has boosters for limited play."));
            int setsWithBoosterCount = Program.GameEngine.Definition.Sets().Where(x => x.Packs.Count() > 0).Count();
            Log.Info(string.Format("Found #{0} sets with boosters.", setsWithBoosterCount));
            if (setsWithBoosterCount == 0)
            {
                // LimitedGameMenuItem.IsVisible = IsVisible.Collapsed;
                Log.Info("Hiding limited play in the menu.");
            }
            Log.Info("Checking if the loaded game has a Decks folder for prebuilt decks.");
            if (!Directory.Exists(Path.Combine(Program.GameEngine.Definition.InstallPath, "Decks")))
            {
                Log.Info("No Decks folder found, hiding Load Prebuilt Decks in the menu.");
                // PrebuiltDeckMenuItem.IsVisible = IsVisible.Collapsed;
            };
            //SubTimer.Start();

            if (!X.Instance.Debug)
            {
                // Show the Scripting console in dev only
                // if (Application.Current.Properties["ArbitraryArgName"] == null) return;
                // string fname = Application.Current.Properties["ArbitraryArgName"].ToString();
                // if (fname != "/developer") return;
            }

        }

        private void InitializePlayerSummary(object sender, EventArgs e)
        {
            var textBlock = (TextBlock)sender;
            var player = textBlock.DataContext as Player;
//            if (player != null && player.IsGlobalPlayer)
//            {
//                textBlock.IsVisible = IsVisible.Collapsed;
//                return;
//            }
            string format;
            if (player != null && player.IsGlobalPlayer)
            {
                format = Program.GameEngine.Definition.GlobalPlayer.IndicatorsFormat;
            }
            else
            {
                format = Program.GameEngine.Definition.Player.IndicatorsFormat;
            }

            if (string.IsNullOrWhiteSpace(format))
            {
                textBlock.IsVisible = false;
                return;
            }

            var multi = new MultiBinding();
            int placeholder = 0;
            format = Regex.Replace(format, @"{#([^}]*)}", delegate(Match match)
                                                              {
                                                                  string name = match.Groups[1].Value;
                                                                  if (player != null)
                                                                  {
                                                                      Counter counter =
                                                                          player.Counters.FirstOrDefault(
                                                                              c => c.Name == name);
                                                                      if (counter != null)
                                                                      {
                                                                          multi.Bindings.Add(new Binding("Value") { Source = counter });
                                                                          return "{" + placeholder++ + "}";
                                                                      }
                                                                  }
                                                                  if (player != null)
                                                                  {
                                                                      var group =
                                                                          player.IndexedGroups.FirstOrDefault(
                                                                              g => g != null && g.Name == name);
                                                                      if (@group != null)
                                                                      {
                                                                          multi.Bindings.Add(new Binding("Count") { Source = @group.Cards });
                                                                          return "{" + placeholder++ + "}";
                                                                      }
                                                                  }
                                                                  return "?";
                                                              });
            multi.StringFormat = format;
            multi.FallbackValue = format;
            
            textBlock.Bind(TextBlock.TextProperty, multi);
        }

        protected void Close(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (this.PreGameLobby.IsVisible ) return;
            //GameLogWindow.RealClose();
            //SubTimer.Stop();
            //SubTimer.Elapsed -= this.SubTimerOnElapsed;
            if (IsRealClosing == false)
                Close();
        }

        public void ShowGameLog(object sender, RoutedEventArgs routedEventArgs)
        {
            if (this.PreGameLobby.IsVisible) return;
            //GameLogWindow.IsVisible = IsVisible.Visible;
        }

        private bool IsRealClosing = false;


        protected override async void OnClosing(WindowClosingEventArgs e)
        {
            var box = MessageBoxManager
                .GetMessageBoxStandard("Octgn","Are you sure you want to quit the game?",ButtonEnum.YesNo,MsBox.Avalonia.Enums.Icon.Question);

            var res=await box.ShowAsync();
            if (res != ButtonResult.Yes)
                e.Cancel = true;
            if (e.Cancel == false)
            {
                IsRealClosing = true;
            }
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            WindowManager.PlayWindow = null;
            Program.StopGame();
            // Fix: Don't do this earlier (e.g. in OnClosing) because an animation (e.g. card turn) may try to access Program.Game
        }

        public bool TryClose()
        {
            try
            {
                this.Close();
                return IsRealClosing;
            }
            catch(Exception ex)
            {
                Log.Warn(ex.Message, ex);
            }
            return false;
        }

        private async void Open(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            await LoadDeck(Program.GameEngine.Definition.GetDefaultDeckPath());
        }

        private async void OpenPrebuilt(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            await LoadDeck(Path.Combine(Program.GameEngine.Definition.InstallPath, "Decks"));
        }

        private async Task LoadDeck(string path)
        {

            if (this.PreGameLobby.IsVisible) return;
            if (Player.LocalPlayer.Spectator || Program.GameEngine.IsReplay) return;
            var topLevel = TopLevel.GetTopLevel(this);
            // Show the dialog to choose the file
            var param = new FilePickerOpenOptions
            {
                Title = "Open Text File",
                AllowMultiple = false,
                SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(path),
                FileTypeFilter = [new ("Octgn Game File (*.o8g)") { Patterns = ["*.o8g"]}]
            };
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(param);

            //deckpath.InitialDirectory = Program.Game.Definition.DecksPath;
            if (files.Count==0) return;

            // Try to load the file contents
            try
            {
                var deckpath = files.First();
                var game = GameManager.Get().GetById(Program.GameEngine.Definition.Id);

                var newDeck = await (new Deck().Load(game, deckpath));
                //DataNew.Entities.Deck newDeck = Deck.Load(ofd.FileName,
                //                         Program.GamesRepository.Games.First(g => g.Id == Program.Game.Definition.Id));
                // Load the deck into the game
                Program.GameEngine.LoadDeck(newDeck, false);
                if (!String.IsNullOrWhiteSpace(newDeck.Notes))
                {
                    this.table.AddNote(100, 0, newDeck.Notes);
                }
            }
            catch (DeckException ex)
            {
                var box = MessageBoxManager
                    .GetMessageBoxStandard("Error",ex.Message,icon:MsBox.Avalonia.Enums.Icon.Error);
                await box.ShowAsync();
            }
            catch (Exception ex)
            {
                var box = MessageBoxManager
                    .GetMessageBoxStandard("Error","Octgn couldn't load the deck.\r\nDetails:\r\n\r\n" + ex.Message,icon:MsBox.Avalonia.Enums.Icon.Error);
                await box.ShowAsync();
            }
        }

        private void LimitedGame(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (this.PreGameLobby.IsVisible == true) return;
            // if (LimitedDialog.Singleton == null)
            //     new LimitedDialog { Owner = this }.Show();
            // else
            //     LimitedDialog.Singleton.Activate();
        }

        private void ToggleFullScreen(object sender, RoutedEventArgs e)
        {
            if (this.PreGameLobby.IsVisible == true) return;
            if (IsFullScreen)
            {
                Topmost = false;
                // WindowStyle = WindowStyle.None;
                WindowState = WindowState.Normal;
                //menuRow.Height = GridLength.Auto;
                //this.TitleBarIsVisible = IsVisible.Visible;
            }
            else
            {
                //Topmost = true;
                // WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                //menuRow.Height = new GridLength(2);
                //this.TitleBarIsVisible = IsVisible.Collapsed;
            }
            IsFullScreen = !IsFullScreen;
        }

        private void ResetScreen(object sender, RoutedEventArgs e)
        {
            table.ResetScreen();
        }

        private async void ResetGame(object sender, RoutedEventArgs e)
        {
            if (this.PreGameLobby.IsVisible) return;
            if (Program.GameEngine.Definition.Events.ContainsKey("OverrideGameReset"))
            {
                Program.GameEngine.EventProxy.OverrideGameReset_3_1_0_2();
                return;
            }
            await this.Reset(false);
        }
        private async void SoftResetGame(object sender, RoutedEventArgs e)
        {
            if (this.PreGameLobby.IsVisible) return;
            if (Program.GameEngine.Definition.Events.ContainsKey("OverrideGameSoftReset"))
            {
                Program.GameEngine.EventProxy.OverrideGameSoftReset_3_1_0_2();
                return;
            }
            await this.Reset(true);
        }

        private async Task Reset(bool isSoft)
        {
            // Prompt for a confirmation
            var box = MessageBoxManager
                .GetMessageBoxStandard("Confirmation","The current game will end. Are you sure you want to continue?",ButtonEnum.YesNo);
            var res=await box.ShowAsync();
            if (res==ButtonResult.Yes)
            {
                Program.Client.Rpc.ResetReq(isSoft);
            }
        }

        protected void MouseEnteredMenu(object sender, RoutedEventArgs e)
        {
            if (!IsFullScreen) return;
            //menuRow.Height = GridLength.Auto;
        }

        protected void MouseLeftMenu(object sender, RoutedEventArgs e)
        {
            if (!IsFullScreen) return;
            //menuRow.Height = new GridLength(2);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (_currentCard != null
                && _currentCardUpStatus
                && e.KeyModifiers.HasFlag(KeyModifiers.Control)
                && Prefs.ZoomOption == Prefs.ZoomType.ProxyOnKeypress
                && _newCard)
            {
                // if (_previewCardWindow == null)
                // {
                //     var img = _currentCard.GetBitmapImage(_currentCardUpStatus, true);
                //     ShowCardPicture(_currentCard, img);
                // }
                // else
                //     _previewCardWindow.SetCard(_currentCard, _currentCardUpStatus, true);
                MyHelper.NotImplemented();
                _newCard = false;
            }

            if (e.Source is TextBox)
                return; // Do not tinker with the keyboard events when the focus is inside a textbox

            // if (e.IsRepeat)
            //     return;
            // IInputElement mouseOver = Mouse.DirectlyOver;
            var te = new TableKeyEventArgs(this, e);
            // if (mouseOver != null) mouseOver.RaiseEvent(te);
            if (te.Handled) return;

            // If the event was unhandled, check if there's a selection and try to apply a shortcut action to it
            if (!Selection.IsEmpty() && Selection.Source.CanManipulate())
            {
                ActionShortcut match =
                    Selection.Source.CardShortcuts.FirstOrDefault(
                        shortcut => shortcut.Key.Matches(te.KeyEventArgs));
                if (match != null)
                {
                    if (match.ActionDef.AsAction().IsBatchExecutable)
                        ScriptEngine.ExecuteOnBatch(match.ActionDef.AsAction().Execute, Selection.Cards);
                    else
                        ScriptEngine.ExecuteOnCards(match.ActionDef.AsAction().Execute, Selection.Cards);
                    e.Handled = true;
                    return;
                }
            }

            // The event was still unhandled, try all groups, starting with the table
            if (table == null) return;
            table.RaiseEvent(te);
            if (te.Handled) return;
            foreach (var g in Player.LocalPlayer.Groups.Where(g => g.CanManipulate()))
            {
                ActionShortcut a = g.GroupShortcuts.FirstOrDefault(shortcut => shortcut.Key.Matches(e));
                if (a != null)
                {
                    if (a.ActionDef.AsAction().Execute != null)
                        ScriptEngine.ExecuteOnGroup(a.ActionDef.AsAction().Execute, g);
                    e.Handled = true;
                    return;
                }
                else if (g is Pile pile && pile.ShufflePileShortcut != null && pile.ShufflePileShortcut.Matches( e))
                {
                    pile.Shuffle();
                    e.Handled = true;
                    return;
                }
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if(e.Key == Key.F9) {
                if (Program.GameSettings.AllowCardList)
                    Program.GameEngine.DeckStats.IsVisible = !Program.GameEngine.DeckStats.IsVisible;

                e.Handled = true;

                return;
            }
            if (_currentCard != null
                && _currentCardUpStatus
                && e.KeyModifiers.HasFlag(KeyModifiers.Control)
                && Prefs.ZoomOption == Prefs.ZoomType.ProxyOnKeypress)
            {
                // if (_previewCardWindow == null)
                // {
                //     var img = _currentCard.GetBitmapImage(_currentCardUpStatus);
                //     ShowCardPicture(_currentCard, img);
                // }
                // else
                //     _previewCardWindow.SetCard(_currentCard, _currentCardUpStatus, false);
                MyHelper.NotImplemented();
                _newCard = true;
            }
        }

        private void CardHovered(object sender, CardEventArgs e)
        {
            _currentCard = e.Card;
            _currentCardUpStatus = false;
            if (e.Card == null && e.CardModel == null)
            {
                // _fadeOut.Begin(outerCardViewer, HandoffBehavior.SnapshotAndReplace);
                // _fadeOut.Begin(outerCardViewer2, HandoffBehavior.SnapshotAndReplace);
            }
            else
            {
                // Point mousePt = Mouse.GetPosition(table);
                // if (mousePt.X < 0.4 * clientArea.ActualWidth)
                //     outerCardViewer.HorizontalAlignment = cardViewer.HorizontalAlignment = outerCardViewer2.HorizontalAlignment = cardViewer2.HorizontalAlignment = HorizontalAlignment.Right;
                // else if (mousePt.X > 0.6 * clientArea.ActualWidth)
                //     outerCardViewer.HorizontalAlignment = cardViewer.HorizontalAlignment = outerCardViewer2.HorizontalAlignment = cardViewer2.HorizontalAlignment = HorizontalAlignment.Left;
                //
                // var ctrl = e.Source as CardControl;
                // if (e.Card != null)
                // {
                //
                //     bool up = ctrl != null && ctrl.IsAlwaysUp
                //             || (e.Card.FaceUp || e.Card.PeekingPlayers.Contains(Player.LocalPlayer));
                //
                //     _currentCardUpStatus = up;
                //
                //     if (_previewCardWindow == null)
                //     {
                //         var img = e.Card.GetBitmapImage(up);
                //         double width = ShowCardPicture(e.Card, img);
                //
                //         if (up && Prefs.ZoomOption == Prefs.ZoomType.OriginalAndProxy && !e.Card.IsProxy())
                //         {
                //             var proxyImg = e.Card.GetBitmapImage(true, true);
                //             ShowSecondCardPicture(e.Card, proxyImg, width);
                //         }
                //     }
                //     else
                //         _previewCardWindow.SetCard(e.Card, up);
                //     _newCard = true;
                // }
                // else
                // {
                //     //probably for hovering in the limited deck editor
                //     if (_previewCardWindow == null)
                //     {
                //         var img = ImageUtils.CreateFrozenBitmap(new Uri(e.CardModel.GetPicture()));
                //         this.ShowCardPicture(e.Card, img);
                //     }
                //     else
                //         _previewCardWindow.SetCard(e.CardModel, true);
                // }
            }
        }

        private void ShowSecondCardPicture(Card card, Bitmap img, double requiredMargin)
        {
            var maxWidth = this.Width * 0.20;
            cardViewer2.Height = img.PixelSize.Height;
            cardViewer2.Width = img.PixelSize.Width > maxWidth ? maxWidth : img.PixelSize.Width;
            cardViewer2.Source = img;

            if (cardViewer2.HorizontalAlignment == Avalonia.Layout.HorizontalAlignment.Left)
            {
                outerCardViewer2.Margin = new Thickness(requiredMargin + 15, 10, 10, 10);
            }
            else
            {
                outerCardViewer2.Margin = new Thickness(10, 10, requiredMargin + 15, 10);
            }

            // _fadeIn.Begin(outerCardViewer2, HandoffBehavior.SnapshotAndReplace);

            if (cardViewer2.Clip == null) return;
            var clipRect = ((RectangleGeometry)cardViewer2.Clip);
            double height = Math.Min(cardViewer2.MaxHeight, cardViewer2.Height);
            double width = cardViewer2.Width * height / cardViewer2.Height;
            clipRect.Rect = new Rect(new Size(width, height));
            clipRect.RadiusX = clipRect.RadiusY = card.RealCornerRadius * height / card.RealHeight;
        }

        private void ViewCardModel(object sender, CardModelEventArgs e)
        {
            // if (e.CardModel == null)
            //     _fadeOut.Begin(outerCardViewer, HandoffBehavior.SnapshotAndReplace);
            // else if (_previewCardWindow == null)
            //     ShowCardPicture(e.CardModel.GameCard, ImageUtils.CreateFrozenBitmap(new Uri(e.CardModel.Card.GetPicture())));
            // else
            // {
            //     _previewCardWindow.SetCard(e.CardModel.Card, true);
            // }
            MyHelper.NotImplemented();
        }

        private double ShowCardPicture(Card card, Bitmap img)
        {
            cardViewer.Height = img.PixelSize.Height;
            cardViewer.Width = img.PixelSize.Width;
            Dispatcher.UIThread.Invoke( new Action(() => { }),DispatcherPriority.Render);
            cardViewer.Source = img;

            // _fadeIn.Begin(outerCardViewer, HandoffBehavior.SnapshotAndReplace);

            double height = Math.Min(cardViewer.MaxHeight, cardViewer.Height);
            double width = cardViewer.Width * height / cardViewer.Height;
            if (img.PixelSize.Width > img.PixelSize.Height)
            {
                width = Math.Min(cardViewer.MaxWidth, cardViewer.Width);
                height = cardViewer.Height * width / cardViewer.Width;
            }

            if (cardViewer.Clip == null) return width;

            var clipRect = ((RectangleGeometry)cardViewer.Clip);
            clipRect.Rect = new Rect(new Size(width, height));

            if (card == null)
                clipRect.RadiusX = clipRect.RadiusY = Program.GameEngine.Definition.DefaultSize().CornerRadius * height / Program.GameEngine.Definition.DefaultSize().Height;
            else
            {
                clipRect.RadiusX = clipRect.RadiusY = card.RealCornerRadius*height/card.RealHeight;
            }

            return width;
        }

        private void NextTurnClicked(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            var targetPlayer = (Player)btn.DataContext;
            if (Program.GameEngine.ActivePlayer == null || Program.GameEngine.ActivePlayer == Player.LocalPlayer)
            {
                if (Program.GameEngine.Definition.Events.ContainsKey("OverrideTurnPassed"))
                {
                    Program.GameEngine.EventProxy.OverrideTurnPassed_3_1_0_2(targetPlayer);
                    return;
                }
                Program.Client.Rpc.NextTurn(targetPlayer, true, false);
            }
            else
            {
                Program.GameEngine.StopTurn = !Program.GameEngine.StopTurn;
                Program.Client.Rpc.StopTurnReq(Program.GameEngine.TurnNumber, Program.GameEngine.StopTurn);
            }
        }

        public void PhaseClicked(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            var phase = (Phase)btn.DataContext;
            if (Program.GameEngine.Definition.Events.ContainsKey("OverridePhaseClicked"))
            {
                Program.GameEngine.EventProxy.OverridePhaseClicked_3_1_0_2(phase.Name, phase.Id);
                return;
            }
            phase.Hold = !phase.Hold;
            Program.Client.Rpc.StopPhaseReq(phase.Id, phase.Hold);
        }

        private bool LockPhaseList = false;

        private void ShowPhaseStoryboard(object sender, PointerEventArgs e)
        {
            if (!LockPhaseList)
            {
                // Storyboard sb = (Storyboard)PhaseControl.FindResource("ShowPhaseStoryboard");
                // sb.Begin(PhaseControl);
            }
        }

        private void HidePhaseStoryboard(object sender, PointerEventArgs e)
        {
            if (!LockPhaseList)
            {
                // Storyboard sb = (Storyboard)PhaseControl.FindResource("HidePhaseStoryboard");
                // sb.Begin(PhaseControl);
            }
        }

        private void LockPhaseStoryboard(object sender, PointerPressedEventArgs e)
        {
            LockPhaseList = !LockPhaseList;
        }

        // private void ActivateChat(object sender, ExecutedRoutedEventArgs e)
        // {
        //     e.Handled = true;
        //     if (this.PreGameLobby.IsVisible) return;
        //     if (this.chatIsDocked)
        //     {
        //         chat.FocusInput();
        //     }
        //     else
        //     {
        //         this._chatWindow.FocusInput();
        //     }
        //
        // }

        private void ShowAboutWindow(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (this.PreGameLobby.IsVisible) return;
            //var wnd = new AboutWindow() { Owner = this };
            //wnd.ShowDialog();
            Program.LaunchUrl(AppConfig.WebsitePath);
        }

        private void ConsoleClicked(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (this.PreGameLobby.IsVisible) return;
            if (Player.LocalPlayer.Spectator == true || Program.GameEngine.IsReplay) return;

            // if (Program.DeveloperMode)
            // {
            //     var wnd = new DeveloperWindow() { Owner = this };
            //     wnd.Show();
            // }
        }

     //    internal void ShowBackstage(UIElement ui)
     //    {
     //        Dispatcher.Invoke(new Action(() =>
     //            {
     //                this.table.IsVisible = IsVisible.Collapsed;
     //                this.wndManager.IsVisible = IsVisible.Collapsed
     //                    ;
     //                this.backstage.Child = ui;
					// this.PhaseControl.IsVisible = IsVisible.Collapsed;
					// this.DeckStats.IsVisible = IsVisible.Collapsed;
     //                this.LimitedBackstage.IsVisible = IsVisible.Visible;
     //                backstage.IsVisible = IsVisible.Visible;
     //                this.Menu.IsEnabled = false;
     //                this.Menu.IsVisible = IsVisible.Collapsed;
     //            }));
     //    }

        internal void HideBackstage()
        {

            table.IsVisible = IsVisible;
            // wndManager.IsVisible = IsVisible;
            LimitedBackstage.IsVisible = IsVisible;
            backstage.IsVisible = IsVisible;
			this.PhaseControl.IsVisible = IsVisible;
			this.DeckStats.IsVisible = IsVisible;
			// this.Menu.IsEnabled = true;
   //          this.Menu.IsVisible = IsVisible;
            backstage.Child = null;

            // Keyboard.Focus(table); // Solve various issues, like disabled menus or non-available keyboard shortcuts
        }

        #region Limited

        protected void LimitedSaveClicked(object sender, RoutedEventArgs e)
        {
            if (this.PreGameLobby.IsVisible) return;
            // var sfd = new SaveFileDialog
            //               {
            //                   AddExtension = true,
            //                   Filter = "Octgn decks|*.o8d",
            //                   InitialDirectory = Program.GameEngine.Definition.GetDefaultDeckPath()
            //               };
            // if (!sfd.ShowDialog().GetValueOrDefault()) return;
            //
            // var dlg = backstage.Child as PickCardsDialog;
            // try
            // {
            //     if (dlg != null)
            //         dlg.LimitedDeck.Save(GameManager.Get().GetById(Program.GameEngine.Definition.Id), sfd.FileName);
            //     else
            //         Program.GameEngine.LoadedCards.Save(GameManager.Get().GetById(Program.GameEngine.Definition.Id), sfd.FileName);
            //
            // }
            // catch (UserMessageException ex)
            // {
            //     TopMostMessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            // }
        }

        protected void LimitedOkClicked(object sender, RoutedEventArgs e)
        {
            // if (backstage.Child is PickCardsDialog dlg) Program.GameEngine.LoadDeck(dlg.LimitedDeck, true);
            // HideBackstage();
        }

        protected void LimitedCancelClicked(object sender, RoutedEventArgs e)
        {
            Program.Client.Rpc.CancelLimitedReq();
            HideBackstage();
        }
        private void LimitedAddPacks(object sender, RoutedEventArgs e)
        {
            // LimitedDialog ld;
            // e.Handled = true;
            // if (LimitedDialog.Singleton == null)
            // {
            //     ld = new LimitedDialog { Owner = this };
            //     ld.Show();
            // }
            // else
            // {
            //     ld = LimitedDialog.Singleton;
            //     ld.Activate();
            // }
            // ld.showAddCardsCombo(true);
            MyHelper.NotImplemented();
        }
        private void LimitedLoadCardPool(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            // var dlg = backstage.Child as PickCardsDialog;
            // var loadDirectory = Program.GameEngine.Definition.GetDefaultDeckPath();
            //
            //
            // var ofd = new OpenFileDialog
            // {
            //     Filter = "Octgn deck files (*.o8d) | *.o8d",
            //     InitialDirectory = loadDirectory
            // };
            // if (ofd.ShowDialog() != true) return;
            // // Try to load the file contents
            // try
            // {
            //     var game = GameManager.Get().GetById(Program.GameEngine.Definition.Id);
            //     var newDeck = new Deck().Load(game, ofd.FileName);
            //     dlg.OpenCardPool(newDeck);
            // }
            // catch (DeckException ex)
            // {
            //     TopMostMessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            // }
            // catch (Exception ex)
            // {
            //     TopMostMessageBox.Show("Octgn couldn't load the deck.\r\nDetails:\r\n\r\n" + ex.Message, "Error",
            //                     MessageBoxButton.OK, MessageBoxImage.Error);
            // }
        }

        #endregion

        private void LoadDocument(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (this.PreGameLobby.IsVisible) return;
            // var s = sender as FrameworkElement;
            // if (s == null) return;
            // var document = s.DataContext as Document;
            // if (document == null) return;
            // var wnd = new RulesWindow(document) { Owner = this };
            // wnd.Show();

        }

        private void ToggleCardPreviewWindow(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            // if (_previewCardWindow == null)
            // {
            //     _previewCardWindow = new PreviewCardWindow() { Owner = this };
            //     _previewCardWindow.Closed += (a, b) =>
            //     {
            //         _previewCardWindow = null;
            //         CardPreviewToggleChecked.IsChecked = false;
            //     };
            //     _previewCardWindow.Show();
            //     CardPreviewToggleChecked.IsChecked = true;
            // }
            // else if (_previewCardWindow.IsInitialized)
            // {
            //     _previewCardWindow.Close();
            //     CardPreviewToggleChecked.IsChecked = false;
            // }
        }


        private void ToggleChatDockPanel(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            // toggle IsVisible
            // if (this.chatIsDocked)
            // {
            //     this.chatIsDocked = false;
            //     this.ChatToggleChecked.IsChecked = true;
            //     this.chatGrid.Width = 0;
            //     this.Column4ChatWidth.Width = new GridLength(0);
            //     if(this._chatWindow == null) // create a new chat window
            //     {
            //         this._chatWindow = new ChatWindow() { Owner = this };
            //         this._chatWindow.AddHandler(CardRun.ViewCardModelEvent, new EventHandler<CardModelEventArgs>(ViewCardModel));
            //         this._chatWindow.Closed += (a, b) =>
            //         {
            //             this._chatWindow = null;
            //             this.chatIsDocked = true;
            //             this.ChatToggleChecked.IsChecked = false;
            //             this.chatGrid.Width = DefaultChatWidth;
            //             this.Column4ChatWidth.Width = new GridLength(DefaultChatWidth);
            //             Keyboard.Focus(table);
            //         };
            //         this._chatWindow.Show();
            //
            //     }
            // }
            // else
            // {
            //     this.chatIsDocked = true;
            //     this.ChatToggleChecked.IsChecked = false;
            //     this.chatGrid.Width = DefaultChatWidth;
            //     this.Column4ChatWidth.Width = new GridLength(DefaultChatWidth);
            //     if(this._chatWindow != null && _chatWindow.IsInitialized)
            //     {
            //         this._chatWindow.Close();
            //     }
            // }
            //
            // Keyboard.Focus(table);
        }

        private void KickPlayer(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            var s = sender as InputElement;
            if (s == null) return;
            var player = s.DataContext as Player;
            if (player == null) return;
            if (Program.GameEngine.IsReplay) return;
            if (player == Player.LocalPlayer)
            {
                throw new UserMessageException("You cannot kick yourself.");
            }
            Program.Client.Rpc.Boot(player, "The host has booted them from the game.");
        }

        private bool chatIsMaxed = false;

        private bool chatVisible;
        private void ChatSplitDoubleClick(object sender, TappedEventArgs e)
        {
            // if (chatIsMaxed)
            // {
            //     ChatGridEmptyPart.Height = new GridLength(100, GridUnitType.Star);
            //     ChatGridChatPart.Height = new GridLength(playerTabs.ActualHeight);
            //     ChatSplit.DragIncrement = 1;
            //     chatIsMaxed = false;
            // }
            // else
            // {
            //     ChatGridEmptyPart.Height = new GridLength(0, GridUnitType.Star);
            //     ChatGridChatPart.Height = new GridLength(100, GridUnitType.Star);
            //     ChatSplit.DragIncrement = 10000;
            //     chatIsMaxed = true;
            // }
        }

        private async void MenuChangeBackgroundFromFileClick(object sender, RoutedEventArgs e)
        {
            if (this.PreGameLobby.IsVisible) return;
            var sub = SubscriptionModule.Get().IsSubscribed ?? false;
            if (!sub)
            {
                var box = MessageBoxManager
                    .GetMessageBoxStandard("OCTGN","You must be subscribed to do that.",icon:MsBox.Avalonia.Enums.Icon.Info);
                await box.ShowAsync();
                return;
            }
            
            var topLevel = TopLevel.GetTopLevel(this);
            // Show the dialog to choose the file
            var param = new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = [
                    new FilePickerFileType("All Images")
                    {
                        Patterns = [ "*.BMP", "*.JPG", "*.JPEG", "*.PNG" ]
                    },
                    new FilePickerFileType("BMP Files: (*.BMP)")
                    {
                        Patterns = [ "*.BMP" ]
                    },
                    new FilePickerFileType("JPEG Files: (*.JPG;*.JPEG)")
                    {
                        Patterns = [ "*.JPG","*.JPEG" ]
                    },
                    new FilePickerFileType("PNG Files: (*.PNG)")
                    {
                        Patterns = [ "*.PNG" ]
                    }
                ]
            };
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(param);
            
            if (files.Count>0)
            {
                await this.table.SetBackground(files[0], BackgroundStyle.UniformToFill);
                Prefs.DefaultGameBack = files[0].Name;
            }
        }

        private async void MenuChangeBackgroundReset(object sender, RoutedEventArgs e)
        {
            if (this.PreGameLobby.IsVisible) return;
            await this.table.ResetBackground();
            Prefs.DefaultGameBack = "";
        }

        // private void SubscribeNavigate(object sender, RequestNavigateEventArgs e)
        // {
        //     var url = SubscriptionModule.Get().GetSubscribeUrl(new SubType() { Description = "", Name = "" });
        //     if (url != null)
        //     {
        //         Program.LaunchUrl(url);
        //     }
        // }

        private void ButtonWaitingForPlayersCancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public new event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void GridSplitter_DragDelta(object sender, VectorEventArgs e)
        {
            // playerArea.MinHeight = playerTabs.DesiredSize.Height;
        }

        private void CardList_Click(object sender, RoutedEventArgs e) {
            Program.GameEngine.DeckStats.IsVisible = !Program.GameEngine.DeckStats.IsVisible;
        }

        private bool replayDragStarted = false;
        private bool _isFullScreen;
        private bool _showSubscribeMessage;

        private void ReplaySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e) {
            if (replayDragStarted) return;
            var val = (long)e.NewValue;

            if (!this.IsLoaded) return;

            ReplayEngine.FastForwardTo(TimeSpan.FromTicks(val));
        }

        private void ReplaySlider_DragStarted(object sender, VectorEventArgs e) {
            replayDragStarted = true;
        }

        private void ReplaySlider_DragCompleted(object sender, VectorEventArgs e) {
            replayDragStarted = false;
            var val = (long)((Slider)sender).Value;

            ReplayEngine.FastForwardTo(TimeSpan.FromTicks(val));
        }

        private void ReplaySpeed_Click(object sender, RoutedEventArgs e) {
            ReplayEngine.ToggleSpeed();
        }

        private void ReplayPlayButton_Click(object sender, RoutedEventArgs e) {
            ReplayEngine.TogglePlay();
        }

        private void ReplayPreviousEventButton_Click(object sender, RoutedEventArgs e) {
            ReplayEngine.RewindToPreviousEvent();
        }

        private void ReplayNextEventButton_Click(object sender, RoutedEventArgs e) {
            ReplayEngine.FastForwardToNextEvent();
        }

        private void ReplayPreviousTurnButton_Click(object sender, RoutedEventArgs e) {
            ReplayEngine.RewindToPreviousTurn();
        }

        private void ReplayNextTurnButton_Click(object sender, RoutedEventArgs e) {
            ReplayEngine.FastForwardToNextTurn();
        }

        private void ChatSplit_DragDelta(object sender, VectorEventArgs e)
        {
            // if (ChatGridChatPart.ActualHeight <= ChatGridChatPart.MinHeight && e.VerticalChange >= 0) // + VerticalChange means shrinking chat box
            // {
            //     ChatGridChatPart.Height = new GridLength(0);
            //     playerAreaGridSplitter.Margin = new Thickness(300,0,0,0);
            // }
            // else
            // {
            //     playerAreaGridSplitter.Margin = new Thickness(0);
            // }
        }
    }

    internal class CanPlayConverter : IMultiValueConverter
    {
        #region IMultiValueConverter Members

     

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion

        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            var turnPlayer = values[0] as Player;
            var player = values[1] as Player;
            var stopped = values[2] as bool?;

            string styleKey;
            if (player == Player.GlobalPlayer)
                styleKey = "InvisibleButton";
            else if (turnPlayer == null)
                styleKey = "PlayButton";
            else if (turnPlayer == Player.LocalPlayer)
                styleKey = "PlayButton";
            else if (turnPlayer != player)
                styleKey = "InvisibleButton";
            else if (stopped == true)
                styleKey = "HeldPauseButton";
            else
                styleKey = "PauseButton";
            return Application.Current.FindResource(styleKey);
        }
    }

    internal class ScaleConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            var d = (double)value;
            double scale = double.Parse((string)parameter, CultureInfo.InvariantCulture);
            return d * scale;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class GameMessageTextBlock : TextBlock
    {
        // Definicja AvaloniaProperty zamiast DependencyProperty
        public static readonly StyledProperty<IGameMessage> GameMessageProperty =
            AvaloniaProperty.Register<GameMessageTextBlock, IGameMessage>(nameof(GameMessage));

        public IGameMessage GameMessage
        {
            get => GetValue(GameMessageProperty);
            set => SetValue(GameMessageProperty, value);
        }

        static GameMessageTextBlock()
        {
            // Reakcja na zmianę właściwości
            GameMessageProperty.Changed.AddClassHandler<GameMessageTextBlock>((x, e) => OnGameMessageChanged(x, e));
        }

        private static void OnGameMessageChanged(GameMessageTextBlock sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (sender == null) return;
        
            // Czyszczenie istniejących inlines
            sender.Inlines.Clear();

            // Zakładam, że Gui.ChatControl.GameMessageToBlock zwraca obiekt Avalonii, zamiast WPF
            // var section = Gui.ChatControl.GameMessageToBlock(sender.GameMessage) as Avalonia.Documents.Section;
            // if (section == null) return;

            // Przenoszenie paragrafów z sekcji do TextBlocka
            // foreach (var paragraph in section.Blocks.OfType<Paragraph>())
            // {
            //     foreach (var inline in paragraph.Inlines)
            //     {
            //         sender.Inlines.Add(inline);
            //     }
            // }

            // Ustawienia marginesu i wyrównania
            sender.Margin = new Thickness(10, 0, 10, 0);
            sender.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
        }
    }

    internal class ValueAdditionConveter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double.TryParse((string)parameter, out double temp);
            return (double)value + temp;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }