using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using log4net;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace Octgn;

public class InterProcess
{
    internal static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
	
    #region Singleton

    internal static InterProcess SingletonContext { get; set; }

    private static readonly object InterProcessSingletonLocker = new object();

    public static InterProcess Instance
    {
        get
        {
            if (SingletonContext == null)
            {
                lock (InterProcessSingletonLocker)
                {
                    if (SingletonContext == null)
                    {
                        SingletonContext = new InterProcess();
                    }
                }
            }
            return SingletonContext;
        }
    }

    #endregion Singleton

    public async Task KillOtherOctgn(bool force)
    {
        if (Environment.GetCommandLineArgs().Any(x => x.ToLowerInvariant().Contains("table"))) return;
        Log.Info("Getting octgn processes...");
        var pList = Process.GetProcessesByName("OCTGN");
        Log.Info("Got process list");
        if (pList != null && pList.Length > 0 && pList.Any(x => x.Id != Process.GetCurrentProcess().Id))
        {
            Log.Info("Found other octgn processes");
            if (!force)
            {
                var box = MessageBoxManager
                    .GetMessageBoxStandard("OCTGN","Another instance of OCTGN is current running. Would you like to close it?",ButtonEnum.YesNoCancel,Icon.Question);

                var res=await box.ShowAsync();
                if (res == ButtonResult.Yes)
                {
                    foreach (var p in Process.GetProcessesByName("OCTGN"))
                    {
                        if (p.Id != Process.GetCurrentProcess().Id)
                        {
                            Log.Info("Killing process...");
                            try
                            {
                                p.Kill();
                            }
                            catch (Exception ex)
                            {
                                box=MessageBoxManager.GetMessageBoxStandard("Error","Could not kill other OCTGN's. If you are updating you will need to manually kill them or reboot your machine first.",icon:Icon.Error);
                                await box.ShowAsync();
                                Log.Warn("KillOtherOctgn", ex);
                            }
                            Log.Info("Killed Process");
                        }
                    }
                }
                else if (res == ButtonResult.Cancel)
                {
                    Process.GetCurrentProcess().Kill();
                }
            }
            else
            {
                foreach (var p in Process.GetProcessesByName("OCTGN"))
                {
                    if (p.Id != Process.GetCurrentProcess().Id)
                    {
                        Log.Info("Killing process...");
                        try
                        {
                            p.Kill();
                        }
                        catch (Exception ex)
                        {
                            var box=MessageBoxManager.GetMessageBoxStandard("Error","Could not kill other OCTGN's. If you are updating you will need to manually kill them or reboot your machine first.",icon:Icon.Error);
                            await box.ShowAsync();
                            Log.Warn("KillOtherOctgn", ex);
                        }
                        Log.Info("Killed Process");
                    }
                }
            }
        }
    }
}