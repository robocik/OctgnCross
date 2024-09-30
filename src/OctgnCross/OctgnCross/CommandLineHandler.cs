using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using log4net;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using NDesk.Options;
using Octgn.Launchers;
using ILauncher = Avalonia.Platform.Storage.ILauncher;

namespace Octgn;

public class CommandLineHandler
{
    internal static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
	
    #region Singleton

    internal static CommandLineHandler SingletonContext { get; set; }

    private static readonly object CommandLineHandlerSingletonLocker = new object();

    public static CommandLineHandler Instance
    {
        get
        {
            if (SingletonContext == null)
            {
                lock (CommandLineHandlerSingletonLocker)
                {
                    if (SingletonContext == null)
                    {
                        SingletonContext = new CommandLineHandler();
                    }
                }
            }
            return SingletonContext;
        }
    }

    #endregion Singleton

    public bool DevMode { get; private set; }

	public bool ShutdownProgram { get; private set; }

    public async Task<Launchers.ILauncher> HandleArguments(string[] args)
    {
        try
        {
            if (args != null) Log.Debug(string.Join(Environment.NewLine, args));
            if (args.Length < 2) return new MainLauncher();
            Log.Info("Has arguments");
            if (args[1].StartsWith("octgn://", StringComparison.InvariantCultureIgnoreCase))
            {
                Log.Info("Using protocol");
                var uri = new Uri(args[1]);
                return HandleProtocol(uri);
            }
            var tableOnly = false;
            var editorOnly = false;
            int? hostport = null;
            Guid? gameid = null;
            string deckPath = null;
            var os = new OptionSet()
            {
                {"t|table", x => tableOnly = true},
                {"g|game=", x => gameid = Guid.Parse(x)},
                {"d|deck=", x => deckPath = x},
                {"x|devmode", x => DevMode = true},
                {"e|editor", x => editorOnly = true}
            };
            try
            {
                os.Parse(args);
            }
            catch (Exception e)
            {
                Log.Warn("Parse args exception: " + String.Join(",", Environment.GetCommandLineArgs()), e);
            }
            if (tableOnly)
            {
                if (gameid == null) {
                    var box=MessageBoxManager.GetMessageBoxStandard("Error","You must supply a GameId with -g=GUID on the command line.",icon:Icon.Error);
                    await box.ShowAsync();
                }
                return new TableLauncher(hostport, gameid);
            }

            if (File.Exists(args[1]))
            {
                Log.Info("Argument is file");
                var fi = new FileInfo(args[1]);
                if (fi.Extension.Equals(".o8d", StringComparison.InvariantCultureIgnoreCase))
                {
                    Log.Info("Argument is deck");
                    deckPath = args[1];
                }
            }

            if (deckPath != null || editorOnly)
            {
                return new DeckEditorLauncher(deckPath, true);
            }
        }
        catch (Exception e)
        {
            Log.Error("Error handling arguments", e);
            if (args != null) Log.Error(string.Join(Environment.NewLine, args));
        }
		return new MainLauncher();
    }

    internal Octgn.Launchers.ILauncher HandleProtocol(Uri url)
    {
        var host = url.Host.ToLowerInvariant();
        switch (host)
        {
            case "deck":
                // This is where we either launch the deck viewer(basically
                //   the same control we use for the deck manager, except
                //   it has the option to save the deck...or maybe, that's
                //   all that we do. That way we don't have to talk to 
                //   the current running octgn.
                break;
        }
        return new MainLauncher();
    }
}