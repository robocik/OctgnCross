using System.Reflection;
using System.Timers;
using Avalonia.Platform;
using log4net;
using Octgn.Communication;
using Octgn.Core;
using Octgn.Site.Api;
using Octgn.Site.Api.Models;
using Octgn.UI;
using Timer = System.Timers.Timer;

namespace Octgn.JodsEngine;

public class SubscriptionModule
{
    internal static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    #region Singleton

    internal static SubscriptionModule SingletonContext { get; set; }

    private static readonly object SubscriptionModuleSingletonLocker = new object();

    public static SubscriptionModule Get()
    {
        lock (SubscriptionModuleSingletonLocker) return SingletonContext ?? (SingletonContext = new SubscriptionModule());
    }

    internal SubscriptionModule()
    {
        Log.Info("Creating");
        PrevSubValue = null;
        this.SubTypes = new List<SubType>();
        SubTypes.Add(new SubType { Description = "$3.00 per month", Name = "silver" });
        SubTypes.Add(new SubType { Description = "$33.00 per year", Name = "gold" });
        CheckTimer = new Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);
        CheckTimer.Elapsed += CheckTimerOnElapsed;
        CheckTimer.Start();
        BroadcastTimer = new Timer(TimeSpan.FromMinutes(3).TotalMilliseconds);
        BroadcastTimer.Elapsed += BroadcastTimerOnElapsed;
        BroadcastTimer.Start();
        Log.Info("Created");
        Task.Factory.StartNew(() => CheckTimerOnElapsed(null, null)).ContinueWith(
            x =>
            { if (x.Exception != null) Log.Info("Get Is Subbed Failed", x.Exception); });
        // Program.LobbyClient.Connected += LobbyClient_Connected;
        var sti = AssetLoader.Open(new Uri("avares://OctgnCross/Resources/subscriberbenefits.txt"));
        var benifits = new List<string>();
        using (var sr = new StreamReader(sti))
        {
            var l = sr.ReadLine();
            while (l != null)
            {
                benifits.Add(l);
                l = sr.ReadLine();
            }
        }
        Benefits = benifits;
    }

    #endregion Singleton

    public List<string> Benefits { get; internal set; }

    public List<SubType> SubTypes { get; set; }

    /// <summary>
    /// True if subbed,
    /// False if not,
    /// Null if unknown
    /// </summary>
    public bool? IsSubscribed
    {
        get
        {
            if (PrevSubValue == null)
                this.UpdateIsSubbed();
            return PrevSubValue;
        }
    }

    internal void UpdateIsSubbed()
    {
        Log.Info("Getting IsSubscribed");
        bool? ret = null;
        try
        {
            var username = Prefs.Username;
            var sessionKey = Prefs.SessionKey;
            // var password = Prefs.Password.Decrypt();

            if (string.IsNullOrEmpty(username)) ret = false;
            else if (string.IsNullOrWhiteSpace(sessionKey)) ret = false;
            else {
                var client = new ApiClient();
                var res = client.IsSubbed(username, sessionKey);

                switch (res) {
                    case IsSubbedResult.Ok:
                        ret = true;
                        break;
                    case IsSubbedResult.AuthenticationError:
                        // This means we need to refresh our session
                        // keep the sub active for now
                        ret = null;
                        break;
                    case IsSubbedResult.NoSubscription:
                    case IsSubbedResult.SubscriptionExpired:
                        ret = false;
                        break;
                }
            }
        }
        catch (Exception e)
        {
            Log.Warn("ce", e);
        }
        Log.InfoFormat("Is Subscribed = {0}", ret == null ? "Unknown" : ret.ToString());
        var prev = PrevSubValue;
        if (ret == null) // If we happen to not get a result back, then fuck it, don't want a failure to make the user seem like they don't have a sub.
            return;
        PrevSubValue = ret;
        if (ret != prev)
        {
            this.OnIsSubbedChanged((bool)ret);
        }
    }

    internal bool? PrevSubValue { get; set; }

    public event Action<bool> IsSubbedChanged;

    public string GetSubscribeUrl(SubType type)
    {
        return AppConfig.WebsitePath;
    }

    protected virtual void OnIsSubbedChanged(bool obj)
    {
        this.IsSubbedChanged?.Invoke(obj);
    }

    internal Timer CheckTimer { get; set; }
    internal Timer BroadcastTimer { get; set; }

    private void CheckTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
    {
        Log.Info("Check timer elapsed");
        this.UpdateIsSubbed();
    }
    private void BroadcastTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
    {
        //Log.Info("Broadcasting Sub");
        //if(PrevSubValue != null && PrevSubValue != false)
        //    Program.LobbyClient.SetSub((bool)PrevSubValue);
    }

    private void LobbyClient_Connected(object sender, ConnectedEventArgs results)
    {
        this.UpdateIsSubbed();
    }

}
public class SubType
{
    public string Name { get; set; }
    public string Description { get; set; }
    public override string ToString()
    {
        return Description;
    }
}