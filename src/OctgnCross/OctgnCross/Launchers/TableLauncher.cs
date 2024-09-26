using System;
using System.Threading.Tasks;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace Octgn.Launchers
{
    public class TableLauncher : UpdatingLauncher
    {
        private readonly int? hostPort;
        private readonly Guid? gameId;

        public TableLauncher(int? hostport, Guid? gameid) {
            this.hostPort = hostport;
            this.gameId = gameid;
            if (this.gameId == null) {
                this.Shutdown = true;
            }
        }

        public override Task BeforeUpdate() {
            return Task.CompletedTask;
        }

        public override Task AfterUpdate() {
            try {
                App.JodsEngine.HostGame(hostPort, gameId);
            } catch (Exception e) {
                this.Log.Warn("Couldn't host/join table mode", e);
                this.Shutdown = true;
                App.Exit();
            }

            return Task.CompletedTask;
        }
    }
}