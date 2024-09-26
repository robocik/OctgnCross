using System;
using System.IO;

namespace Octgn;

public interface IConfiguratorLoader
{
    Stream GetAppSettingsStream();
}

public class DefaultConfiguratorLoader : IConfiguratorLoader
{
    public Stream GetAppSettingsStream()
    {
        var configFile = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        return File.OpenRead(configFile);
    }
}