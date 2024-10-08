using Avalonia.Platform;

namespace Octgn.Scripting;

public class FileHelper
{
    public static void CopyAssetsToInternalStorage(string fileName, string targetFile)
    {
        var targetDirectory=Path.GetDirectoryName(targetFile);
        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        if (!File.Exists(targetFile))
        {
            using (var assetStream = AssetLoader.Open(new Uri(fileName)))
            using (var targetStream = new FileStream(targetFile, FileMode.Create))
            {
                assetStream.CopyTo(targetStream);
            }
        }
    }

}