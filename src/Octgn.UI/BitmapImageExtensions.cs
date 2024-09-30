using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace Octgn.UI
{
    public static partial class StringExtensionMethods
    {
        private static HttpClient _client = new ();
        public static async Task<Bitmap> BitmapFromUri(this Uri uri)
        {
            if (uri.IsFile)
            {
                return uri.AbsolutePath.BitmapFromFile();
            }
            var stream=await _client.GetStreamAsync(uri);
            var bim = new Bitmap(stream);
            return bim;
        }
        
        public static Bitmap BitmapFromFile(this string file)
        {
            var bim = new Bitmap(file);
            return bim;
        }
        
        public static async Task<Bitmap> BitmapFromFile(this IStorageFile file)
        {
            await using var stream =await file.OpenReadAsync();
            var bim = new Bitmap(stream);
            return bim;
        }
    }
}
