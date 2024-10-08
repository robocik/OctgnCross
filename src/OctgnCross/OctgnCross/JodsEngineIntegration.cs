using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using DialogHostAvalonia;
using log4net;
using Octgn.Communication;
using Octgn.Core;
using Octgn.Core.DataManagers;
using Octgn.JodsEngine;
using Octgn.JodsEngine.Windows;
using Octgn.Library;
using Octgn.Library.Exceptions;
using Octgn.Online.Hosting;
using Octgn.Scripting;
using Octgn.UI;
using Octgn.ViewModels;
using OctgnCross.Tabs.Play;
using static Community.CsharpSqlite.Sqlite3;

namespace Octgn;

public class JodsEngineIntegration
{
    private readonly static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    public Task<bool> HostGame(HostedGame hostedGame, HostedGameSource source, string username, string password) {
        DebugValidate(hostedGame, true);

        password ??= "";
        //duct-tape fix for a game server bug where online-hosted games lose the chosen password
        //TODO: remove this check when the bug is fixed server-side (see git issue ticket #2109)
        if (!password.Equals(hostedGame.Password))
            hostedGame.Password = password;

        var args = "-h ";
        if (CommandLineHandler.Instance.DevMode)
            args += "-x ";

        if (source == HostedGameSource.Lan) {
            new HostedGameProcess(
                hostedGame,
                X.Instance.Debug,
                true
            ).Start();
        }

        args += $"-u \"{username}\" ";
        args += "-k \"" + HostedGame.Serialize(hostedGame) + "\"";

        return LaunchJodsEngine(args);
    }

    public Task<bool> HostGame(int? hostPort, Guid? gameId)
    {
        if (gameId == null)
            throw new UserMessageException("Cannot launch jodsengine from command line: Game GUID is missing.");
        var game = GameManager.Get().GetById((Guid)gameId);
        if (game == null)
            throw new UserMessageException("Cannot launch jodsengine from command line: Game {0} is not installed.");

        var user = App.LobbyClient?.User
            ?? new User(Guid.NewGuid().ToString(), Prefs.Username);

        var hostedGame = new HostedGame()
        {
            Id = Guid.NewGuid(),
            Name = Prefs.LastRoomName ?? "",
            Password = "",
            HostUser = user,
            GameName = game.Name,
            GameId = game.Id,
            GameVersion = game.Version.ToString(),
            HostAddress = $"0.0.0.0:{Prefs.LastLocalHostedGamePort}",
            OctgnVersion = typeof(Server.Server).Assembly.GetName().Version.ToString(),
            Spectators = true,
            DateCreated = DateTimeOffset.Now,
            
        };

        var args = "-h ";
        if (CommandLineHandler.Instance.DevMode)
            args += "-x ";
        new HostedGameProcess(hostedGame, X.Instance.Debug, true).Start();
        args += $"-u \"{Prefs.Nickname}\" ";
        args += "-k \"" + HostedGame.Serialize(hostedGame) + "\"";

        return LaunchJodsEngine(args);
    }

    public Task<bool> LaunchDeckEditor(string deckPath = null) {
        if (string.IsNullOrWhiteSpace(deckPath)) {
            return LaunchJodsEngine("-e");
        } else {
            return LaunchJodsEngine($"-e -d \"{deckPath}\"");
        }
    }

    public Task<bool> JoinGame(HostedGameViewModel hostedGameViewModel, string username, bool spectate) {
        User user;
        if (App.LobbyClient.User != null) {
            user = App.LobbyClient.User;
        } else {
            user = new User(Guid.NewGuid().ToString(), Prefs.Username);
        }

        var hostedGame = hostedGameViewModel.HostedGame;

        DebugValidate(hostedGame, false);

        var args = "-j ";

        if (CommandLineHandler.Instance.DevMode)
            args += "-x ";

        new HostedGameProcess(
            hostedGame,
            X.Instance.Debug,
            true
        ).Start();

        args += $"-u \"{username}\" ";
        if (spectate) {
            args += "-s ";
        }
        args += "-k \"" + HostedGame.Serialize(hostedGame) + "\"";

        return LaunchJodsEngine(args);
    }

    public Task<bool> JoinGame(DataGameViewModel game, IPAddress host, int port, string username, string password, bool spectate) {
        var user = new User(Guid.NewGuid().ToString(), username);

        var hostedGame = new HostedGame() {
            Id = Guid.NewGuid(),
            GameId = game.Id,
            GameName = game.Name,
            OctgnVersion = Const.OctgnVersion.ToString(),
            GameVersion = game.Version.ToString(),
            HostAddress = $"{host}:{port}",
            Password = password,
            DateCreated = System.DateTime.UtcNow
        };

        DebugValidate(hostedGame, false);

        var args = "-j ";
        if (CommandLineHandler.Instance.DevMode)
            args += "-x ";

        args += $"-u \"{username}\" ";
        if (spectate) {
            args += "-s ";
        }

        args += "-k \"" + HostedGame.Serialize(hostedGame) + "\"";

        return LaunchJodsEngine(args);
    }

    public Task<bool> LaunchReplay(string replayFile) {
        if (string.IsNullOrWhiteSpace(replayFile)) throw new ArgumentNullException(nameof(replayFile));

        var args = $"-r=\"{replayFile}\"";

        return LaunchJodsEngine(args);
    }

    // private async Task<bool> LaunchJodsEngine(string args) {
    //     string enginePath;
    //     string engineDirectory;
    //     {
    //         var exeName = "Octgn.JodsEngine.exe";
    //         engineDirectory = ".\\";
    //         if (X.Instance.Debug) {
    //             engineDirectory = "..\\..\\..\\Octgn.JodsEngine\\bin\\Debug";
    //             exeName = "Octgn.JodsEngine.exe";
    //         }
    //
    //         engineDirectory = Path.GetFullPath(engineDirectory);
    //
    //         enginePath = Path.Combine(engineDirectory, exeName);
    //     }
    //
    //     Log.Info($"Launching engine {enginePath} - {args}");
    //
    //     var psi = new ProcessStartInfo(enginePath, args);
    //     psi.UseShellExecute = true;
    //     psi.WorkingDirectory = engineDirectory;
    //
    //     var proc = Process.Start(psi);
    //
    //     try {
    //         using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10))) {
    //             await Task.Run(async () => {
    //                 while (proc.MainWindowHandle == IntPtr.Zero) {
    //                     await Task.Delay(1000, cts.Token);
    //                     if (proc.HasExited) {
    //                         break;
    //                     }
    //                 }
    //             }, cts.Token);
    //         }
    //     } catch (OperationCanceledException) {
    //         Log.Warn("Engine did not show UI withing alloted time. Probably frozen.");
    //
    //         try {
    //             proc.Kill();
    //         } catch (Exception ex) {
    //             Log.Warn($"Error killing proc: {ex.Message}", ex);
    //         }
    //
    //         return false;
    //     }
    //
    //     if (proc.HasExited) {
    //         Log.Warn("Engine prematurely shutdown");
    //
    //         return false;
    //     }
    //
    //     return true;
    // }
    
    private async Task<bool> LaunchJodsEngine(string args)
    {

        //PYTHON TEST
        // var test = new Engine(true);
        // test.SetupEngine(true);
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var loadingWindow = new LoadingWindow(args);
            // await DialogHost.Show(loadingWindow, delegate (object _, DialogOpenedEventArgs args)
            // {
            //     loadingWindow.DialogSession = args.Session;
            // });
            loadingWindow.Show();
        });

        return true;
    }

    private static void DebugValidate(HostedGame hostedGame, bool isHosting) {
        //DateTimeOffset minCreatedDate;
        //if (isHosting)
        //    minCreatedDate = DateTimeOffset.UtcNow.AddMinutes(-2);
        //else
        //    minCreatedDate = DateTimeOffset.UtcNow.AddHours(-6);

        //DateTimeOffset maxCreatedDate = DateTimeOffset.UtcNow;

        //Debug.Assert(hostedGame.DateCreated.UtcDateTime >= minCreatedDate, $"Hosted game DateCreated is too ancient {hostedGame.DateCreated}");
        //Debug.Assert(hostedGame.DateCreated.UtcDateTime <= maxCreatedDate, $"Hosted game DateCreated is too far in the future {hostedGame.DateCreated}");

        //Debug.Assert(hostedGame.Id != Guid.Empty, $"Hosted game Id is empty");
    }
}