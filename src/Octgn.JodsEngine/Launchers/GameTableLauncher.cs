﻿using System;
using System.Net;
using System.Reflection;
using log4net;
using Octgn.DataNew.Entities;
using Octgn.Core.DataManagers;
using Octgn.Core;
using Octgn.Library.Exceptions;
using Octgn.Play;
using Octgn.Library.Utils;
using Octgn.Library;
using Octgn.Online.Hosting;
using System.Threading.Tasks;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Octgn.Communication;
using Octgn.JodsEngine.Play;
using Octgn.UI;

namespace Octgn.Launchers
{
    public class GameTableLauncher
    {
        internal static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        internal int HostPort;
        internal Game HostGame;
        internal string HostUrl;

        public Task Launch(int? hostport, Guid? game)
        {
            HostGame = GameManager.Get().GetById(game.Value);
            if (hostport == null || hostport <= 0)
            {
                this.HostPort = new Random().Next(5000, 6000);
                while (!NetworkHelper.IsPortAvailable(this.HostPort)) this.HostPort++;
            }
            else
            {
                this.HostPort = hostport.Value;
            }
            // Host a game
            return this.Host();
        }

        private async Task Host()
        {
            await StartLocalGame(HostGame, Randomness.RandomRoomName(), "");
            Octgn.Play.Player.OnLocalPlayerWelcomed += PlayerOnOnLocalPlayerWelcomed;
            Program.GameSettings.UseTwoSidedTable = HostGame.UseTwoSidedTable;
            if (Program.GameEngine != null)
                Dispatcher.UIThread.Invoke(new Action(()=>Program.GameEngine.Begin()));
        }

        private void PlayerOnOnLocalPlayerWelcomed()
        {
            if (Octgn.Play.Player.LocalPlayer.Id == 1)
            {
                this.StartGame();
            }
        }

        private void StartGame()
        {
            Play.Player.OnLocalPlayerWelcomed -= this.PlayerOnOnLocalPlayerWelcomed;
            WindowManager.PlayWindow = new PlayWindow();
			WindowManager.PlayWindow.Show();
            WindowManager.PlayWindow.Closed += PlayWindowOnClosed;
   
        }

        private void PlayWindowOnClosed(object sender, EventArgs eventArgs)
        {
            Program.Exit();
        }

        async Task StartLocalGame(Game game, string name, string password)
        {
            var user = new User(Guid.NewGuid().ToString(), Prefs.Username);

            var hg = new HostedGame() {
                Id = Guid.NewGuid(),
                Name = name,
                HostUser = user,
                GameName = game.Name,
                GameId = game.Id,
                GameVersion = game.Version.ToString(),
                HostAddress = $"0.0.0.0:{HostPort}",
                Password = password,
                GameIconUrl = game.IconUrl,
                Spectators = true,
            };

            // We don't use a userid here becuase we're doing a local game.
            var hs = new HostedGameProcess(hg, X.Instance.Debug, true);
            hs.Start();

            Program.GameSettings.UseTwoSidedTable = HostGame.UseTwoSidedTable;
            Program.IsHost = true;
            Program.GameEngine = new GameEngine(game, Prefs.Nickname, false,password,true);

            
            var ip = IPAddress.Parse("127.0.0.1");

            for (var i = 0; i < 5; i++)
            {
                try
                {
                    Program.Client = new Octgn.Networking.ClientSocket(ip, HostPort);
                    await Program.Client.Connect();
                    return;
                }
                catch (Exception e)
                {
                    Log.Warn("Start local game error", e);
                    if (i == 4) throw;
                }
                Thread.Sleep(2000);
            }
            throw new UserMessageException("Cannot start local game. You may be missing a file.");
        }
    }
}