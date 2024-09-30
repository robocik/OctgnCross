using System.Reflection;
using Avalonia.Media.Imaging;
using Octgn.Core.DataExtensionMethods;
using Octgn.DataNew.Entities;
using Octgn.UI;

namespace Octgn.JodsEngine.Utils;

internal static class ImageUtils
{
    private static readonly Bitmap ReflectionBitmap;
    private static readonly Func<Uri, Bitmap> GetImageFromCache;

    static ImageUtils()
    {
        // ReflectionBitmap = new Bitmap();
        // MethodInfo methodInfo = typeof(Bitmap).GetMethod("CheckCache",
        //     BindingFlags.NonPublic | BindingFlags.Instance);
        // GetImageFromCache =
        //     (Func<Uri, Bitmap>)
        //     Delegate.CreateDelegate(typeof(Func<Uri, Bitmap>), ReflectionBitmap, methodInfo);
    }

    public static async Task GetCardImage(ICard card, Func<Bitmap,Task> action, bool proxyOnly = false)
    {
        //var uri = new Uri(card.GetPicture());
        var uri = proxyOnly ? new Uri(card.GetProxyPicture()) : new Uri(card.GetPicture());
        // Bitmap bmp = GetImageFromCache(uri);
        // if (bmp != null)
        // {
        //     await action(bmp);
        //     return;
        // }

        await action(await CreateFrozenBitmap(uri));
        return;
        // If the bitmap is not in cache, display the default face up picture and load the correct one async.
        //action(Program.GameEngine.CardFrontBitmap);
        //Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>{action(CreateFrozenBitmap(uri));}),DispatcherPriority.Input);
    }

    public static Task<Bitmap> CreateFrozenBitmap(string source)
    {
        return CreateFrozenBitmap(new Uri(source));
    }

    public static async Task<Bitmap> CreateFrozenBitmap(Uri uri)
    {
        Bitmap imgsrc =null;
        // catch bad Uri's and load Front Bitmap "?"
        try
        {
            
            imgsrc = await uri.BitmapFromUri();
        }
        catch (Exception)
        {
            imgsrc = Program.GameEngine.GetCardFront(Program.GameEngine.Definition.DefaultSize());
        }

        return imgsrc;
    }
}