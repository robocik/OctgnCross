using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace Octgn.Extentions
{
    public static partial class StringExtensionMethods
    {
        private static HttpClient _client = new ();
        public static async Task<Bitmap> BitmapFromUri(this Uri uri)
        {
            var stream=await _client.GetStreamAsync(uri);
            var bim = new Bitmap(stream);
           return bim;
        }
        
        public static Bitmap BitmapFromFile(this string file)
        {
            var bim = new Bitmap(file);
            return bim;
        }
    }
}
