using System.Reflection;
using log4net;
using Avalonia.Controls;
using Avalonia.Threading;
using Octgn.JodsEngine.Windows;

namespace Octgn.Launchers
{
    public abstract class LauncherBase : ILauncher
    {
        protected ILog Log { get; }

        public abstract string Name { get; }

        protected abstract Task<Window> Load(ILoadingView loadingView);

        protected LauncherBase() {
            Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }

        public async Task<bool> Launch(ILoadingView view) {
            Dispatcher.UIThread.VerifyAccess();

            view.UpdateStatus($"Launching {Name}...");

            var window = await Load(view);

            if (window == null) {
                Log.Warn("No window created");

                return false;
            }

            // do async so can run in backround
            await Task.Yield();

            // Application.Current.MainWindow = window;

            await Task.Delay(300);

            return true;
        }
    }
}