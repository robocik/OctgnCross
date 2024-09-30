
using Octgn.Core;
using Octgn.JodsEngine.Loaders;
using Octgn.JodsEngine.Windows;
using Octgn.UI;

namespace Octgn.Loaders
{
    public class EnvironmentLoader : ILoader
    {
        private readonly string _args;

        private readonly log4net.ILog Log
            = log4net.LogManager.GetLogger(typeof(EnvironmentLoader));

        public EnvironmentLoader(string args)
        {
            _args = args;
        }

        public string Name { get; } = "Environment";

        public Task Load(ILoadingView view) {
            return Task.Run(LoadSync);
        }

        private void LoadSync() {
            Log.Info("Getting Launcher");

            Program.Launcher = CommandLineHandler.Instance.HandleArguments(
                _args.Split(' '));
            
            if (Program.Launcher == null) {
                Log.Warn($"no launcher from command line args");
            
                return;
            }

            Program.DeveloperMode = CommandLineHandler.Instance.DevMode;
        }
    }
}
