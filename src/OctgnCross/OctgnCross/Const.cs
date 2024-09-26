using System;

namespace Octgn;

public static class Const
{
    public const string ClientName = "Octgn.NET";
    public static readonly Version OctgnVersion = typeof(App).Assembly.GetName().Version;
    public static readonly Version BackwardCompatibility = new Version(3,1,0,0);
}