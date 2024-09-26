using NuGet.Protocol.Core.Types;
using Octgn.Library.Networking;

namespace OctgnCross.Core;

public class GameInfo
{
    public IPackageSearchMetadata Package { get; }
    
    public NamedUrl FeedUrl { get; }

    public GameInfo(IPackageSearchMetadata package, NamedUrl feedUrl)
    {
        Package = package;
        FeedUrl = feedUrl;
    }
}