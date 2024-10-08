using System.Text.RegularExpressions;
using Avalonia.Media;
using log4net;
using Octgn.Core;
using Octgn.Core.DiscordIntegration;
using Octgn.Core.Play;
using Octgn.DataNew;
using Octgn.Launchers;
using Octgn.Library;
using Octgn.Networking;
using Octgn.Online.Hosting;
using Octgn.Play;
using Octgn.Scripting;

namespace Octgn.JodsEngine;

public static class Program
{
    public static GameEngine GameEngine{ get; set; }
    public static Engine ScriptEngine { get; set; }
    public static bool IsGameRunning { get; set; }
    public static bool IsHost { get; set; }
    public static DiscordWrapper Discord { get; internal set; }
    internal static event EventHandler<ServerErrorEventArgs> ServerError;
    public static HostedGame CurrentHostedGame { get; internal set; }
    public static event Action OnOptionsChanged;
    public static GameSettings GameSettings { get; } = new GameSettings();
    public static bool InPreGame { get; set; }

    internal static IClient Client;
    
    public static GameMessageDispatcher GameMess { get; set; }

    public static ILauncher Launcher { get; internal set; }

    public static bool DeveloperMode { get; internal set; }
    public static string UserId { get; set; }
    public static string CurrentOnlineGameName { get; set; }

    internal static Tuple<string, object[]> Parse(Player player, string text)
    {
        string finalText = text;
        int i = 0;
        var args = new List<object>(2);
        Match match = Regex.Match(text, "{([^}]*)}");
        while (match.Success)
        {
            string token = match.Groups[1].Value;
            finalText = finalText.Replace(match.Groups[0].Value, "##$$%%^^LEFTBRACKET^^%%$$##" + i + "##$$%%^^RIGHTBRACKET^^%%$$##");
            i++;
            object tokenValue = token;
            switch (token)
            {
                case "me":
                    tokenValue = player;
                    break;
                default:
                    if (token.StartsWith("#"))
                    {
                        int id;
                        if (!int.TryParse(token.Substring(1), out id)) break;
                        ControllableObject obj = ControllableObject.Find(id);
                        if (obj == null) break;
                        tokenValue = obj;
                        break;
                    }
                    break;
            }
            args.Add(tokenValue);
            match = match.NextMatch();
        }
        args.Add(player);
        finalText = finalText.Replace("{", "").Replace("}", "");
        finalText = finalText.Replace("##$$%%^^LEFTBRACKET^^%%$$##", "{").Replace(
            "##$$%%^^RIGHTBRACKET^^%%$$##", "}");
        return new Tuple<string, object[]>(finalText, args.ToArray());
    }
    internal static void Print(Player player, string text, string color = null)
    {
        var p = Parse(player, text);
        if (color == null)
        {
            GameMess.Notify(p.Item1, p.Item2);
        }
        else
        {
            Color? c = null;
            if (String.IsNullOrWhiteSpace(color))
            {
                c = Colors.Black;
            }
            if (c == null)
            {
                try
                {
                    if (color.StartsWith("#") == false)
                    {
                        color = color.Insert(0, "#");
                    }
                    if (color.Length == 7)
                    {
                        color = color.Insert(1, "F");
                        color = color.Insert(1, "F");
                    }

                    c = Color.Parse(color);
                }
                catch
                {
                    c = Colors.Black;
                }
            }
            GameMess.NotifyBar(c.Value, p.Item1, p.Item2);
        }
    }
    
    public static void Exit()
    {
        // try { SSLHelper?.Dispose(); }
        // catch (Exception e) {
        //     Log.Error( "SSLHelper Dispose Exception", e );
        // };
        // Sounds.Close();
        try
        {
            Program.Client?.Rpc?.Leave(Player.LocalPlayer);
        }
        catch (Exception e)
        {
            //Log.Error( "Exit() Player leave exception", e );
        }
        LogManager.Shutdown();
        // Application.Current.Dispatcher.Invoke(new Action(() =>
        // {
        //     WindowManager.Shutdown();
        //
        //     //Apparently this can be null sometimes?
        //     if (Application.Current != null)
        //         Application.Current.Shutdown(0);
        // }));

    }
    
    public static void StopGame()
    {
        //X.Instance.Try(ChatLog.ClearEvents);
        Program.GameMess?.Clear();
        X.Instance.Try(()=>Program.Client?.Rpc?.Leave(Player.LocalPlayer));
        if (Client != null)
        {
            Client.Shutdown();
            Client = null;
        }
        if (GameEngine != null)
            GameEngine.End();
        GameEngine = null;
        IsGameRunning = false;
    }

    public static void LaunchUrl(string websitePath)
    {
        MyHelper.NotImplemented();
    }
}