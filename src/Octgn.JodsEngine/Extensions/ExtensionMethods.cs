using System.Reflection;
using Avalonia.Controls;
using Avalonia.Media;
using log4net;
using Octgn.Core.DataExtensionMethods;
using Octgn.Core.Util;
using Octgn.DataNew;
using Octgn.DataNew.Entities;
using Octgn.UI;
using Card = Octgn.Play.Card;
using Player = Octgn.Play.Player;

namespace Octgn.JodsEngine.Extensions;

public static class ExtensionMethods
{
    internal static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    
    /// <summary>
    /// Creates a <see cref="Octgn.Play.Card"/> from a <see cref="Octgn.DataNew.Entities.ICard"/> and stores its <see cref="Octgn.Play.CardIdentity"/>
    /// </summary>
    /// <param name="card"></param>
    /// <param name="player"></param>
    /// <returns></returns>
    public static Card ToPlayCard(this ICard card, Player player)
    {
        int id = card.GenerateCardId();
        var retCard = new Card(player, id, Program.GameEngine.Definition.GetCardById(card.Id), true, card.Size.Name);            return retCard;
    }

    public static ulong GenerateKey(this ICard card)
    {
        return ((ulong)Crypto.PositiveRandom()) << 32 | card.Id.Condense();
    }

    public static int GenerateCardId(this ICard card)
    {
        return (Player.LocalPlayer.Id) << 16 | Program.GameEngine.GetUniqueId();
    }

    internal static int GenerateCardId()
    {
        return (Player.LocalPlayer.Id) << 16 | Program.GameEngine.GetUniqueId();
    }

    public static FontFamily GetFontFamily(this DataNew.Entities.Font font, FontFamily defaultFont)
    {
        if (!System.IO.File.Exists(font.Src))
            return defaultFont;

        using(var pf = new System.Drawing.Text.PrivateFontCollection())
        {
            pf.AddFontFile(font.Src);
            if(pf.Families.Length == 0)
            {
                Log.WarnFormat("Could not load font {0}", font.Type);
                return defaultFont;
            }

            string font1 = "file:///" + Path.GetDirectoryName(font.Src) + "/#" + pf.Families[0].Name;
            Log.Info(string.Format("Loading font with path: {0}", font1).Replace("\\", "/"));
            // var ret = new FontFamily(font1.Replace("\\", "/") + ", " + defaultFont.Source);
            // return ret;
            return font1;
        }
    }

    public static void SetFont(this Control elem, Font font)
    {
        if (!font.IsSet()) return;
        Log.Info("Loading font " + font.Type);
        // var ff = font.GetFontFamily(elem.FontFamily);
        // elem.FontFamily = ff;
        // if (font.Size > 0) elem.FontSize = font.Size;
        // Log.Info(string.Format("Loaded font with source: {0}", elem.FontFamily.Source));
    }

    public static void SetFont(this TextBlock elem, Font font)
    {
        if (!font.IsSet()) return;
        Log.Info("Loading font " + font.Type);
        var ff = font.GetFontFamily(elem.FontFamily);
        elem.FontFamily = ff;
        if (font.Size > 0) elem.FontSize = font.Size;
        Log.Info(string.Format("Loaded font with source: {0}", elem.FontFamily.Name));
    }
}