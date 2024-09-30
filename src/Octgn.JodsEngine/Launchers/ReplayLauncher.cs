using Octgn.Core.DataManagers;
using Octgn.Library.Exceptions;
using Octgn.Online.Hosting;
using Octgn.Play;
using Octgn.Play.Save;
using Avalonia.Controls;
using Avalonia.Threading;
using Octgn.Core;
using Octgn.JodsEngine.Windows;
using Octgn.UI;

namespace Octgn.Launchers
{
    public class ReplayLauncher : LauncherBase
    {
        public override string Name => "Replay";

        private readonly string _replayPath;

        public ReplayLauncher(string replayPath) {
            if (string.IsNullOrWhiteSpace(replayPath)) throw new ArgumentNullException(nameof(replayPath));

            _replayPath = replayPath;
        }

        protected override async Task<Window> Load(ILoadingView loadingView) {
            try {
                var replayClient = new ReplayClient();
                Program.Client = replayClient;

                Program.IsHost = true;

                loadingView.UpdateStatus("Loading Replay..");
                ReplayReader reader = null;
                ReplayEngine engine = null;
                try {
                    reader = ReplayReader.FromStream(File.OpenRead(_replayPath));
                    engine = new ReplayEngine(reader, replayClient);

                    loadingView.UpdateStatus("Loading Game...");
                    var game = GameManager.Get().GetById(reader.Replay.GameId);

                    var hostedGame = new HostedGame()
                    {
                        Id = Guid.NewGuid(),
                        GameId = game.Id,
                        GameName = game.Name,
                        OctgnVersion = Const.OctgnVersion.ToString(),
                        GameVersion = game.Version.ToString(),
                        DateCreated = DateTime.UtcNow
                    };
                    Program.CurrentHostedGame = hostedGame;

                    loadingView.UpdateStatus("Loading Game Engine...");
                    Program.CurrentOnlineGameName = game.Name;


                    Program.GameEngine = new GameEngine(engine, game, reader.Replay.User,Program.ScriptEngine);
                } catch {
                    reader?.Dispose();
                    engine?.Dispose();

                    throw;
                }

                // var dispatcher = Dispatcher.CurrentDispatcher;

                MyHelper.NotImplemented();
                Window window = null;
                await Dispatcher.UIThread.InvokeAsync(() => {
                    // window = WindowManager.PlayWindow = new PlayWindow();
                    //
                    // window.Closed += PlayWindow_Closed;
                    //
                    // window.Show();
                }, DispatcherPriority.Background);

                return window;
            } catch (UserMessageException) {
                throw;
            } catch (Exception e) {
                var msg = $"Error launching replay from {_replayPath}: {e.Message}";

                Log.Warn(msg, e);

                throw new UserMessageException(UserMessageExceptionMode.Blocking, msg, e);
            }
        }

        private void PlayWindow_Closed(object sender, EventArgs e) {
            Log.Info("Play window closed, shutting down");

            Program.Exit();
        }
    }
}
