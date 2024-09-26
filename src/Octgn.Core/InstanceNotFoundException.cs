namespace Octgn.Core;

public class InstanceNotFoundException:Exception
{
    public InstanceNotFoundException(string message):base(message)
    {
    }
}

public interface IPackage
{
    string Id { get; }
    
    string Title { get; }
    

    IEnumerable<string> Authors { get; }

    IEnumerable<string> Owners { get; }


    Uri LicenseUrl { get; }


    bool RequireLicenseAcceptance { get; }

    bool DevelopmentDependency { get; }

    string Description { get; }
    
    Uri IconUrl { get; }
    
    bool IsAbsoluteLatestVersion { get; }
    Uri ProjectUrl { get; }
}

public static class MyHelper
{
    public static void NotImplemented()
    {
        throw new NotImplementedException();
    }
}