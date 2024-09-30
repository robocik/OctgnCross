using System.Reflection;
using log4net;
using Microsoft.Extensions.Configuration;

namespace Octgn.UI;

public static class AppConfig
{
    internal static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    public static  string WebsitePath{ get; private set; }
    public static  string ChatServerHost{ get; private set; }
    public static  string GameServerPath{ get; private set; }
    public static  string UpdateInfoPath{ get; private set; }
    public static  string GameFeed{ get; private set; }
    public static  int GameFeedTimeoutSeconds{ get; private set; }
    public static  bool UseGamePackageManagement{ get; private set; }
    public static  string StaticWebsitePath{ get; private set; }

    public static IConfiguration Configuration { get; private set; }
    public static void Load(Stream stream)
    {
        Log.Info("Setting AppConfig");
        var builder = new ConfigurationBuilder()
            .AddJsonStream(stream);

        Configuration = builder.Build();
        if (Const.IsReleaseTest == false
            && Library.X.Instance.Debug == false)
        {
            WebsitePath = Configuration["AppSettings:WebsitePath"];
        }
        else
        {
            WebsitePath = Configuration["AppSettings:WebsitePathTest"];
        }
        StaticWebsitePath = Configuration["AppSettings:"+nameof(StaticWebsitePath)];
        ChatServerHost = Configuration["AppSettings:ChatServerHost"];
        GameServerPath = Configuration["AppSettings:GameServerPath"];
        GameFeed = Configuration["AppSettings:GameFeed"];
        GameFeedTimeoutSeconds = 60;
        if (int.TryParse(Configuration["AppSettings:GameFeedTimeoutSeconds"], out var parsedSeconds))
        {
            GameFeedTimeoutSeconds = parsedSeconds;
        }

        UseGamePackageManagement = bool.Parse(Configuration["AppSettings:UseGamePackageManagement"]);
        if (Const.IsReleaseTest)
            UpdateInfoPath = Configuration["AppSettings:UpdateCheckPathTest"];
        else
            UpdateInfoPath = Configuration["AppSettings:UpdateCheckPath"];

        Log.Info("Set AppConfig");
    }
}